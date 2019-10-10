using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Linq;
using Yotsuba.Core.Models;

namespace Yotsuba.Core.Utilities
{
    public class DataOutput
    {
        private WordprocessingDocument _document { get; set; }
        private BoardModel _board { get; set; }
        private string _authorName { get; set; }

        private Body DocumentBody
        {
            get
            {
                if (_document == null)
                {
                    return null;
                }
                return _document.MainDocumentPart.Document.Body;
            }
        }

        private enum DocumentStyles
        {
            DocumentHeading,
            Author,
            WeekHeading,
            ProjectHeading,
            Text
        }

        private void AddToBody(string text, DocumentStyles formatstyle)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var run = CreateRun(text);

                // If the text is a WeekHeading, we should add a line break before it
                if (formatstyle == DocumentStyles.WeekHeading)
                {
                    run.PrependChild(new Break());
                }

                AddToBody(new List<Run> { run }, formatstyle);
            }
        }


        private void AddToBody(List<Run> runList, DocumentStyles formatstyle)
        {
            var new_paragraph = new Paragraph();
            foreach (Run runItem in runList)
            {
                new_paragraph.AppendChild(runItem);
            }

            // Temporary just format DocumentHeading for now
            ApplyStyleToParagraph(formatstyle, new_paragraph);

            DocumentBody.AppendChild(new_paragraph);
        }

        private Run CreateRun(string text)
        {
            var run = new Run();
            run.AppendChild(new Text(text));

            return run;
        }

        private void CloseDocument()
        {
            _document.Close();
        }

        // Return the unique list of categories
        private HashSet<string> GetCategories()
        {
            var categories = new HashSet<string>();

            foreach (var task in _board.TaskList)
            {
                categories.Add(task.Category);
            }

            return categories;
        }

        public DataOutput(BoardModel board, string authorname, string filepath)
        {
            this._document = WordprocessingDocument.Create(filepath, WordprocessingDocumentType.Document);
            MainDocumentPart mainPart = _document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            PrepareStyleForDocument();

            this._board = board;
            this._authorName = authorname;
        }

        public void WriteToFile()
        {
            AddToBody("Monthly Status Report", DocumentStyles.DocumentHeading);

            // Add author name
            AddToBody(_authorName, DocumentStyles.Author);

            // The board name is the name of the month
            AddToBody(_board.BoardName, DocumentStyles.Author);

            foreach (var week in GetCategories())
            {
                AddToBody(week, DocumentStyles.WeekHeading);

                // Get all tasks that is in the same week
                var tasks = _board.TaskList.Where(task => (task.Category == week)).ToList();

                // Group task by Tag
                var grouped_tasks = tasks.GroupBy(t => t.Tag).ToList();

                foreach (var group in grouped_tasks)
                {
                    AddToBody(group.Key, DocumentStyles.ProjectHeading);

                    foreach (var task in group)
                    {
                        if (!string.IsNullOrEmpty(task.Title))
                        {
                            var title_run = CreateRun(task.Title);
                            AddBulletList(new List<Run> { title_run });
                        }

                        if (!string.IsNullOrEmpty(task.Description))
                        {
                            var description_run = CreateRun(task.Description);
                            AddNestedBulletList(new List<Run> { description_run });
                        }
                    }
                }

                AddToBody("Hour Breakdown", DocumentStyles.ProjectHeading);

                foreach (var item in _board.Hours)
                {
                    if (item.Item1 == week)
                    {
                        string output = $"{item.Item2.Tag}: {item.Item2.Hours.ToString()} Hours";
                        AddBulletList(new List<Run> { CreateRun(output) });
                    }
                }
            }

            CloseDocument();
        }

        private void PrepareStyleForDocument()
        {

            // Add a StylesDefinitionsPart to the document
            StyleDefinitionsPart part = _document.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
            Styles root = new Styles();
            root.Save(part);

            // Create new style and add to document
            CreateDocumentHeadingStyle();
            CreateWeekHeadingStyle();
            CreateProjectHeadingStyle();
            CreateTextStyle();
            CreateAuthorStyle();
        }

        private void ApplyStyleToParagraph(DocumentStyles style, Paragraph p)
        {

            // If the paragraph has no ParagraphProperties object, create one.
            if (p.Elements<ParagraphProperties>().Count() == 0)
            {
                p.PrependChild<ParagraphProperties>(new ParagraphProperties());
            }

            // Get the paragraph properties element of the paragraph.
            ParagraphProperties pPr = p.Elements<ParagraphProperties>().First();

            switch (style)
            {
                case DocumentStyles.DocumentHeading:
                    {
                        // Set the style of the paragraph.
                        pPr.ParagraphStyleId = new ParagraphStyleId() { Val = "DocumentHeading" };

                        break;
                    }
                case DocumentStyles.Author:
                    {
                        // Set the style of the paragraph.
                        pPr.ParagraphStyleId = new ParagraphStyleId() { Val = "Author" };

                        break;
                    }
                case DocumentStyles.ProjectHeading:
                    {
                        pPr.ParagraphStyleId = new ParagraphStyleId() { Val = "ProjectHeading" };

                        break;
                    }
                case DocumentStyles.WeekHeading:
                    {
                        pPr.ParagraphStyleId = new ParagraphStyleId() { Val = "WeekHeading" };

                        break;
                    }
                case DocumentStyles.Text:
                    {
                        pPr.ParagraphStyleId = new ParagraphStyleId() { Val = "Text" };

                        break;
                    }
                default: break;
            }
        }

        // Create a new style with the specified styleid and stylename and add it to the specified
        // style definitions part.
        private void CreateDocumentHeadingStyle()
        {
            // Get the Styles part for this document.
            StyleDefinitionsPart styleDefinitionsPart = _document.MainDocumentPart.StyleDefinitionsPart;

            // Get access to the root element of the styles part.
            Styles styles = styleDefinitionsPart.Styles;

            // Create a new paragraph style and specify some of the properties.
            Style style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "DocumentHeading",
                CustomStyle = true
            };
            StyleName styleName = new StyleName() { Val = "DocumentHeading" };
            BasedOn basedOn = new BasedOn() { Val = "Normal" };
            NextParagraphStyle nextParagraphStyle = new NextParagraphStyle() { Val = "Normal" };
            style.Append(styleName);
            style.Append(basedOn);
            style.Append(nextParagraphStyle);

            // Create the StyleRunProperties object and specify some of the run properties.
            StyleRunProperties styleRunProperties = new StyleRunProperties();
            Bold bold = new Bold();

            // More info: https://stackoverflow.com/a/17221250/9017481
            Color color = new Color() { Val = "365F91", ThemeColor = ThemeColorValues.Accent1, ThemeShade = "BF" };

            RunFonts font = new RunFonts() { Ascii = "Calibri Light (Headings)" };
            // Specify a 18 point size.
            FontSize fontSize = new FontSize() { Val = "36" };
            styleRunProperties.Append(bold);
            styleRunProperties.Append(color);
            styleRunProperties.Append(font);
            styleRunProperties.Append(fontSize);

            // Centering text
            // https://stackoverflow.com/a/24827814/9017481
            ParagraphProperties paragraphRunProperties = new ParagraphProperties();
            Justification CenterHeading = new Justification { Val = JustificationValues.Center };
            paragraphRunProperties.Append(CenterHeading);

            // Add the run properties to the style.
            style.Append(styleRunProperties);
            style.Append(paragraphRunProperties);

            // Add the style to the styles part.
            styles.Append(style);
        }

        private void CreateWeekHeadingStyle()
        {
            // Get the Styles part for this document.
            StyleDefinitionsPart styleDefinitionsPart = _document.MainDocumentPart.StyleDefinitionsPart;

            // Get access to the root element of the styles part.
            Styles styles = styleDefinitionsPart.Styles;

            // Create a new paragraph style and specify some of the properties.
            Style style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "WeekHeading",
                CustomStyle = true
            };
            StyleName styleName = new StyleName() { Val = "WeekHeading" };
            BasedOn basedOn = new BasedOn() { Val = "Normal" };
            NextParagraphStyle nextParagraphStyle = new NextParagraphStyle() { Val = "Normal" };
            style.Append(styleName);
            style.Append(basedOn);
            style.Append(nextParagraphStyle);

            // Create the StyleRunProperties object and specify some of the run properties.
            StyleRunProperties styleRunProperties = new StyleRunProperties();
            Bold bold = new Bold();

            // More info: https://stackoverflow.com/a/17221250/9017481
            Color color = new Color() { Val = "365F91", ThemeColor = ThemeColorValues.Accent1, ThemeShade = "BF" };

            RunFonts font = new RunFonts() { Ascii = "Calibri Light (Headings)" };
            // Specify a 18 point size.
            FontSize fontSize = new FontSize() { Val = "32" };
            styleRunProperties.Append(bold);
            styleRunProperties.Append(color);
            styleRunProperties.Append(font);
            styleRunProperties.Append(fontSize);

            // Centering text
            // https://stackoverflow.com/a/24827814/9017481
            ParagraphProperties paragraphRunProperties = new ParagraphProperties();
            Justification CenterHeading = new Justification { Val = JustificationValues.Left };
            paragraphRunProperties.Append(CenterHeading);

            // Add the run properties to the style.
            style.Append(styleRunProperties);
            style.Append(paragraphRunProperties);

            // Add the style to the styles part.
            styles.Append(style);
        }

        private void CreateProjectHeadingStyle()
        {
            // Get the Styles part for this document.
            StyleDefinitionsPart styleDefinitionsPart = _document.MainDocumentPart.StyleDefinitionsPart;

            // Get access to the root element of the styles part.
            Styles styles = styleDefinitionsPart.Styles;

            // Create a new paragraph style and specify some of the properties.
            Style style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "ProjectHeading",
                CustomStyle = true
            };
            StyleName styleName = new StyleName() { Val = "ProjectHeading" };
            BasedOn basedOn = new BasedOn() { Val = "Normal" };
            NextParagraphStyle nextParagraphStyle = new NextParagraphStyle() { Val = "Normal" };
            style.Append(styleName);
            style.Append(basedOn);
            style.Append(nextParagraphStyle);

            // Create the StyleRunProperties object and specify some of the run properties.
            StyleRunProperties styleRunProperties = new StyleRunProperties();
            Bold bold = new Bold();

            // More info: https://stackoverflow.com/a/17221250/9017481
            Color color = new Color() { Val = "365F91", ThemeColor = ThemeColorValues.Accent1, ThemeShade = "BF" };

            RunFonts font = new RunFonts() { Ascii = "Calibri Light (Headings)" };
            // Specify a 18 point size.
            FontSize fontSize = new FontSize() { Val = "28" };
            styleRunProperties.Append(bold);
            styleRunProperties.Append(color);
            styleRunProperties.Append(font);
            styleRunProperties.Append(fontSize);

            // Centering text
            // https://stackoverflow.com/a/24827814/9017481
            ParagraphProperties paragraphRunProperties = new ParagraphProperties();
            Justification CenterHeading = new Justification { Val = JustificationValues.Left };
            paragraphRunProperties.Append(CenterHeading);

            // Add the run properties to the style.
            style.Append(styleRunProperties);
            style.Append(paragraphRunProperties);

            // Add the style to the styles part.
            styles.Append(style);
        }

        private void CreateTextStyle()
        {
            // Get the Styles part for this document.
            StyleDefinitionsPart styleDefinitionsPart = _document.MainDocumentPart.StyleDefinitionsPart;

            // Get access to the root element of the styles part.
            Styles styles = styleDefinitionsPart.Styles;

            // Create a new paragraph style and specify some of the properties.
            Style style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Text",
                CustomStyle = true
            };
            StyleName styleName = new StyleName() { Val = "Text" };
            BasedOn basedOn = new BasedOn() { Val = "Normal" };
            NextParagraphStyle nextParagraphStyle = new NextParagraphStyle() { Val = "Normal" };
            style.Append(styleName);
            style.Append(basedOn);
            style.Append(nextParagraphStyle);

            // Create the StyleRunProperties object and specify some of the run properties.
            StyleRunProperties styleRunProperties = new StyleRunProperties();

            // More info: https://stackoverflow.com/a/17221250/9017481
            Color color = new Color() { Val = "000000" };

            RunFonts font = new RunFonts() { Ascii = "Cambria (Body)" };
            FontSize fontSize = new FontSize() { Val = "24" };
            styleRunProperties.Append(color);
            styleRunProperties.Append(font);
            styleRunProperties.Append(fontSize);

            // Centering text
            // https://stackoverflow.com/a/24827814/9017481
            ParagraphProperties paragraphRunProperties = new ParagraphProperties();
            Justification CenterHeading = new Justification { Val = JustificationValues.Left };
            paragraphRunProperties.Append(CenterHeading);

            // Add the run properties to the style.
            style.Append(styleRunProperties);
            style.Append(paragraphRunProperties);

            // Add the style to the styles part.
            styles.Append(style);
        }

        private void CreateAuthorStyle()
        {
            // Get the Styles part for this document.
            StyleDefinitionsPart styleDefinitionsPart = _document.MainDocumentPart.StyleDefinitionsPart;

            // Get access to the root element of the styles part.
            Styles styles = styleDefinitionsPart.Styles;

            // Create a new paragraph style and specify some of the properties.
            Style style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Author",
                CustomStyle = true
            };
            StyleName styleName = new StyleName() { Val = "Author" };
            BasedOn basedOn = new BasedOn() { Val = "Normal" };
            NextParagraphStyle nextParagraphStyle = new NextParagraphStyle() { Val = "Normal" };
            style.Append(styleName);
            style.Append(basedOn);
            style.Append(nextParagraphStyle);

            // Create the StyleRunProperties object and specify some of the run properties.
            StyleRunProperties styleRunProperties = new StyleRunProperties();
            Bold bold = new Bold();

            // More info: https://stackoverflow.com/a/17221250/9017481
            Color color = new Color() { Val = "000000" };

            RunFonts font = new RunFonts() { Ascii = "Cambria (Body)" };
            FontSize fontSize = new FontSize() { Val = "24" };
            styleRunProperties.Append(bold);
            styleRunProperties.Append(color);
            styleRunProperties.Append(font);
            styleRunProperties.Append(fontSize);

            // Centering text
            // https://stackoverflow.com/a/24827814/9017481
            ParagraphProperties paragraphRunProperties = new ParagraphProperties();
            Justification CenterHeading = new Justification { Val = JustificationValues.Center };
            paragraphRunProperties.Append(CenterHeading);

            // Add the run properties to the style.
            style.Append(styleRunProperties);
            style.Append(paragraphRunProperties);

            // Add the style to the styles part.
            styles.Append(style);
        }


        public void AddBulletList(List<Run> runList)
        {
            // Introduce bulleted numbering in case it will be needed at some point
            NumberingDefinitionsPart numberingPart = _document.MainDocumentPart.NumberingDefinitionsPart;
            if (numberingPart == null)
            {
                numberingPart = _document.MainDocumentPart.AddNewPart<NumberingDefinitionsPart>("NumberingDefinitionsPart001");
                Numbering element = new Numbering();
                element.Save(numberingPart);
            }

            // Insert an AbstractNum into the numbering part numbering list.  The order seems to matter or it will not pass the 
            // Open XML SDK Productity Tools validation test.  AbstractNum comes first and then NumberingInstance and we want to
            // insert this AFTER the last AbstractNum and BEFORE the first NumberingInstance or we will get a validation error.
            var abstractNumberId = numberingPart.Numbering.Elements<AbstractNum>().Count() + 1;
            var abstractLevel = new Level(new NumberingFormat() { Val = NumberFormatValues.Bullet }, new LevelText() { Val = "·" }) { LevelIndex = 0 };
            var abstractNum1 = new AbstractNum(abstractLevel) { AbstractNumberId = abstractNumberId };

            if (abstractNumberId == 1)
            {
                numberingPart.Numbering.Append(abstractNum1);
            }
            else
            {
                AbstractNum lastAbstractNum = numberingPart.Numbering.Elements<AbstractNum>().Last();
                numberingPart.Numbering.InsertAfter(abstractNum1, lastAbstractNum);
            }

            // Insert an NumberingInstance into the numbering part numbering list.  The order seems to matter or it will not pass the 
            // Open XML SDK Productity Tools validation test.  AbstractNum comes first and then NumberingInstance and we want to
            // insert this AFTER the last NumberingInstance and AFTER all the AbstractNum entries or we will get a validation error.
            var numberId = numberingPart.Numbering.Elements<NumberingInstance>().Count() + 1;
            NumberingInstance numberingInstance1 = new NumberingInstance() { NumberID = numberId };
            AbstractNumId abstractNumId1 = new AbstractNumId() { Val = abstractNumberId };
            numberingInstance1.Append(abstractNumId1);

            if (numberId == 1)
            {
                numberingPart.Numbering.Append(numberingInstance1);
            }
            else
            {
                var lastNumberingInstance = numberingPart.Numbering.Elements<NumberingInstance>().Last();
                numberingPart.Numbering.InsertAfter(numberingInstance1, lastNumberingInstance);
            }

            foreach (Run runItem in runList)
            {
                // Create items for paragraph properties
                var numberingProperties = new NumberingProperties(new NumberingLevelReference() { Val = 0 }, new NumberingId() { Val = numberId });
                var spacingBetweenLines1 = new SpacingBetweenLines() { After = "0" };  // Get rid of space between bullets
                var indentation = new Indentation() { Left = "720", Hanging = "360" };  // correct indentation 

                ParagraphMarkRunProperties paragraphMarkRunProperties1 = new ParagraphMarkRunProperties();
                RunFonts runFonts1 = new RunFonts() { Ascii = "Symbol", HighAnsi = "Symbol" };
                paragraphMarkRunProperties1.Append(runFonts1);

                // create paragraph properties
                var paragraphProperties = new ParagraphProperties(numberingProperties, spacingBetweenLines1, indentation, paragraphMarkRunProperties1);

                // Create paragraph 
                var newPara = new Paragraph(paragraphProperties);

                // Add run to the paragraph
                newPara.AppendChild(runItem);

                ApplyStyleToParagraph(DocumentStyles.Text, newPara);

                // Add one bullet item to the body
                DocumentBody.AppendChild(newPara);
            }
        }

        public void AddNestedBulletList(List<Run> runList)
        {
            // Introduce bulleted numbering in case it will be needed at some point
            NumberingDefinitionsPart numberingPart = _document.MainDocumentPart.NumberingDefinitionsPart;
            if (numberingPart == null)
            {
                numberingPart = _document.MainDocumentPart.AddNewPart<NumberingDefinitionsPart>("NumberingDefinitionsPart002");
                Numbering element = new Numbering();
                element.Save(numberingPart);
            }

            // Insert an AbstractNum into the numbering part numbering list.  The order seems to matter or it will not pass the 
            // Open XML SDK Productity Tools validation test.  AbstractNum comes first and then NumberingInstance and we want to
            // insert this AFTER the last AbstractNum and BEFORE the first NumberingInstance or we will get a validation error.
            var abstractNumberId = numberingPart.Numbering.Elements<AbstractNum>().Count() + 1;
            var abstractLevel = new Level(new NumberingFormat() { Val = NumberFormatValues.Bullet }, new LevelText() { Val = "o" }) { LevelIndex = 1 };
            var abstractNum1 = new AbstractNum(abstractLevel) { AbstractNumberId = abstractNumberId };

            if (abstractNumberId == 1)
            {
                numberingPart.Numbering.Append(abstractNum1);
            }
            else
            {
                AbstractNum lastAbstractNum = numberingPart.Numbering.Elements<AbstractNum>().Last();
                numberingPart.Numbering.InsertAfter(abstractNum1, lastAbstractNum);
            }

            // Insert an NumberingInstance into the numbering part numbering list.  The order seems to matter or it will not pass the 
            // Open XML SDK Productity Tools validation test.  AbstractNum comes first and then NumberingInstance and we want to
            // insert this AFTER the last NumberingInstance and AFTER all the AbstractNum entries or we will get a validation error.
            var numberId = numberingPart.Numbering.Elements<NumberingInstance>().Count() + 1;
            NumberingInstance numberingInstance1 = new NumberingInstance() { NumberID = numberId };
            AbstractNumId abstractNumId1 = new AbstractNumId() { Val = abstractNumberId };
            numberingInstance1.Append(abstractNumId1);

            if (numberId == 1)
            {
                numberingPart.Numbering.Append(numberingInstance1);
            }
            else
            {
                var lastNumberingInstance = numberingPart.Numbering.Elements<NumberingInstance>().Last();
                numberingPart.Numbering.InsertAfter(numberingInstance1, lastNumberingInstance);
            }

            foreach (Run runItem in runList)
            {
                // Create items for paragraph properties
                var numberingProperties = new NumberingProperties(new NumberingLevelReference() { Val = 1 }, new NumberingId() { Val = numberId });
                var spacingBetweenLines1 = new SpacingBetweenLines() { After = "0" };  // Get rid of space between bullets
                var indentation = new Indentation() { Left = "1440", Hanging = "360" };  // correct indentation 

                ParagraphMarkRunProperties paragraphMarkRunProperties1 = new ParagraphMarkRunProperties();
                RunFonts runFonts1 = new RunFonts() { Ascii = "Symbol", HighAnsi = "Symbol" };
                paragraphMarkRunProperties1.Append(runFonts1);

                // create paragraph properties
                var paragraphProperties = new ParagraphProperties(numberingProperties, spacingBetweenLines1, indentation, paragraphMarkRunProperties1);

                // Create paragraph 
                var newPara = new Paragraph(paragraphProperties);

                // Add run to the paragraph
                newPara.AppendChild(runItem);

                ApplyStyleToParagraph(DocumentStyles.Text, newPara);

                // Add one bullet item to the body
                DocumentBody.AppendChild(newPara);
            }
        }
    }
}
