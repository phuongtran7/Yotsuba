using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using WinUI = Microsoft.UI.Xaml.Controls;

using Yotsuba.Helpers;
using Yotsuba.Services;
using System.Collections.ObjectModel;
using Yotsuba.Core.Models;
using System.Collections.Generic;
using Yotsuba.Core.Utilities;
using Windows.Storage;

namespace Yotsuba.Views
{
    public sealed partial class ShellPage : Page, INotifyPropertyChanged
    {
        private readonly KeyboardAccelerator _altLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);

        private BoardModel _selectedBoard;

        public BoardModel SelectedBoard
        {
            get { return _selectedBoard; }
            set
            {
                Set(ref _selectedBoard, value);
                OnPropertyChanged("SelectedBoard");
            }
        }

        ObservableCollection<BoardModel> BoardList { get; set; }

        public ShellPage()
        {
            InitializeComponent();
            DataContext = this;
            Initialize();
        }

        private void Initialize()
        {
            NavigationService.Frame = shellFrame;
            NavigationService.NavigationFailed += Frame_NavigationFailed;
            NavigationService.Navigated += Frame_Navigated;

            // Init BoardList
            BoardList = DataAccess.GetAllBoards();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Keyboard accelerators are added here to avoid showing 'Alt + left' tooltip on the page.
            // More info on tracking issue https://github.com/Microsoft/microsoft-ui-xaml/issues/8
            KeyboardAccelerators.Add(_altLeftKeyboardAccelerator);
            await Task.CompletedTask;
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw e.Exception;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(SettingsPage))
            {
                // Set the Header directly if Settings is selected
                navigationView.Header = "Settings";
                return;
            }

            if (SelectedBoard == null)
            {
                // Disable Header
                navigationView.Header = null;
            }
        }

        private void OnItemInvoked(WinUI.NavigationView sender, WinUI.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigationService.Navigate(typeof(SettingsPage));
                SelectedBoard = null;
                return;
            }

            // For some reason if the Board is already selected and select it again the InvokedItem is null.
            if (args.InvokedItem != null)
            {
                // Get the selected Board in the BoardList and then send it over to the BoardPage for display
                SelectedBoard = BoardList.Where(b => b.BoardName == (string)args.InvokedItem).FirstOrDefault();
                NavigationService.Navigate(typeof(BoardPage), SelectedBoard.TaskList);
            }
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = NavigationService.GoBack();
            args.Handled = result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void NewBoardButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NewBoard_SplitView.IsPaneOpen = true;
        }

        private void NewBoardSaveButton_Clicked(object sender, RoutedEventArgs e)
        {
            var new_id = Guid.NewGuid().GetHashCode();
            var new_board = new BoardModel(new_id, NewBoardNameTextBox.Text);
            BoardList.Add(new_board);

            // Update database
            DataAccess.AddBoard(new_id, NewBoardNameTextBox.Text);

            NewBoardNameTextBox.Text = string.Empty;
            NewBoard_SplitView.IsPaneOpen = false;
        }

        private void EditBoardButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            EditBoardNameTextBox.Text = SelectedBoard.BoardName;
            EditBoard_SplitView.IsPaneOpen = true;
        }

        private void EditBoardSaveButton_Clicked(object sender, RoutedEventArgs e)
        {
            SelectedBoard.BoardName = EditBoardNameTextBox.Text;
            //Re-invoke current page to trigger reload
            NavigationService.Navigate(typeof(BoardPage), SelectedBoard.TaskList);

            // Update Database
            DataAccess.UpdateBoard(SelectedBoard.ID, SelectedBoard.BoardName);

            EditBoard_SplitView.IsPaneOpen = false;
        }

        private async void EditBoardDeleteButton_Clicked(object sender, RoutedEventArgs e)
        {
            ContentDialog deleteFileDialog = new ContentDialog
            {
                Title = "Delete board permanently?",
                Content = "If you delete this board, you won't be able to recover it. Do you want to delete it?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await deleteFileDialog.ShowAsync();

            // Delete the file if the user clicked the primary button.
            // Otherwise, do nothing.
            if (result == ContentDialogResult.Primary)
            {
                // Update Database
                DataAccess.DeleteBoard(SelectedBoard.ID);

                BoardList.Remove(SelectedBoard);
                SelectedBoard = null;

                // Navigate back to blank main page
                NavigationService.Navigate(typeof(MainPage));

                EditBoard_SplitView.IsPaneOpen = false;
            }
        }

        private void EditBoardCancelButton_Clicked(object sender, RoutedEventArgs e)
        {
            EditBoardNameTextBox.Text = string.Empty;
            EditBoard_SplitView.IsPaneOpen = false;
        }

        private void NewTaskButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NewTask_SplitView.IsPaneOpen = true;
        }

        private void NewTaskSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dates = WeekPicker.SelectedDates;
            var sorted = dates.OrderBy(x => x.Date); // Sort the selected dates
            string FormattedWeekString = $"{sorted.First().ToString("MM/dd/yyyy")} - {sorted.Last().ToString("MM/dd/yyyy")}";

            var new_task_id = Guid.NewGuid().GetHashCode();
            SelectedBoard.TaskList.Add(new TaskModel
            {
                ID = new_task_id,
                BoardID = SelectedBoard.ID,
                Title = NewTaskNameTextBox.Text,
                Description = NewTaskDescriptionTextBox.Text,
                Tag = NewTaskTagTextBox.Text,
                Category = FormattedWeekString,
            });

            // Update Database
            DataAccess.AddTaskToBoard(new_task_id, SelectedBoard.ID, NewTaskNameTextBox.Text, NewTaskDescriptionTextBox.Text, NewTaskTagTextBox.Text, FormattedWeekString);

            // Clear the filled data
            NewTaskNameTextBox.Text = string.Empty;
            NewTaskDescriptionTextBox.Text = string.Empty;
            NewTaskTagTextBox.Text = string.Empty;
            WeekPicker.SelectedDates.Clear();

            // Close SplitView pane
            NewTask_SplitView.IsPaneOpen = false;

            NavigationService.Navigate(typeof(BoardPage), SelectedBoard.TaskList);
        }

        private void NewTaskCancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the filled data
            NewTaskNameTextBox.Text = string.Empty;
            NewTaskDescriptionTextBox.Text = string.Empty;
            NewTaskTagTextBox.Text = string.Empty;
            WeekPicker.SelectedDates.Clear();

            // Close SplitView pane
            NewTask_SplitView.IsPaneOpen = false;
        }

        private void WeekPicker_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            // Only allow two dates to be selected
            if (sender.SelectedDates.Count > 2)
            {
                // Remove the first selected
                sender.SelectedDates.RemoveAt(0);
            }
        }

        private void ReportHourButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ReportHourStackPane.Children.Count != 0)
            {
                // Remove all the children in the stackpane
                ReportHourStackPane.Children.Clear();
            }

            List<Tuple<string, HourModel>> ListOfHour = DataAccess.GetAllHourInBoard(SelectedBoard.ID);

            TextBlock titleTextBlock = new TextBlock
            {
                Text = "Report Hours",
                Width = 256,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = this.Resources["TitleTextBlockStyle"] as Style,
            };

            ReportHourStackPane.Children.Add(titleTextBlock);

            foreach (var HourData in ListOfHour)
            {
                TextBlock categoryTextBlock = new TextBlock
                {
                    Text = HourData.Item1,
                    Width = 256,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Left,
                };

                ReportHourStackPane.Children.Add(categoryTextBlock);

                TextBox textBox = new TextBox
                {
                    Header = HourData.Item2.Tag,
                    Text = HourData.Item2.Hours.ToString(),
                    PlaceholderText = "Hours Worked On",
                    Width = 256,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Left,
                };

                ReportHourStackPane.Children.Add(textBox);
            }

            StackPanel ButtonStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Button SaveButton = new Button
            {
                Content = "Save",
                Margin = new Thickness(10)
            };
            SaveButton.Click += ReportHourSaveButton_Click;

            Button CancelButton = new Button
            {
                Content = "Cancel",
                Margin = new Thickness(10)
            };
            CancelButton.Click += ReportHourCancelButton_Click;

            ButtonStackPanel.Children.Add(SaveButton);
            ButtonStackPanel.Children.Add(CancelButton);

            ReportHourStackPane.Children.Add(ButtonStackPanel);

            // Open Report Hour pane
            ReportHourSplitView.IsPaneOpen = true;
        }

        private void ReportHourSaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Loop through all the children
            var ChildrenOfThePane = ReportHourStackPane.Children;

            string CurrentCategory = string.Empty;

            foreach (var item in ChildrenOfThePane)
            {
                if (item is TextBlock textblock)
                {
                    if (textblock.Text == "Report Hours")
                    {
                        continue;
                    }
                    else
                    {
                        CurrentCategory = textblock.Text;
                    }
                }
                else if (item is TextBox textbox)
                {
                    if (!CurrentCategory.Equals(string.Empty))
                    {
                        if (float.TryParse(textbox.Text, out float input_hour))
                        {
                            // Update Database
                            DataAccess.UpdateHourForTag(SelectedBoard.ID, CurrentCategory, textbox.Header.ToString(), input_hour);

                            // Update Hour for BoardModel, so that it can be written out later
                            HourModel hourfortag = new HourModel
                            {
                                Tag = textbox.Header.ToString(),
                                Hours = input_hour,
                            };
                            SelectedBoard.Hours.Add(new Tuple<string, HourModel>(CurrentCategory, hourfortag));
                        }
                        else
                        {
                            HourIsEmptyTip.Title = $"{textbox.Header.ToString()} is empty.";
                            HourIsEmptyTip.IsOpen = true;
                            return;
                        }
                    }
                }
            }

            // Remove all the children in the stackpane
            ReportHourStackPane.Children.Clear();
            // Close Report Hour pane
            ReportHourSplitView.IsPaneOpen = false;
        }

        private void ReportHourCancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Remove all the children in the stackpane
            ReportHourStackPane.Children.Clear();

            // Close Report Hour pane
            ReportHourSplitView.IsPaneOpen = false;
        }

        private async void ExportBoard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            string fileName = $"{SelectedBoard.BoardName}.docx";

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            string filepath = $"{localFolder.Path}/{fileName}";

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            string authorname = (string)localSettings.Values["AuthorName"];

            var writer = new DataOutput(SelectedBoard, authorname, filepath);
            writer.WriteToFile();

            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                StorageFile newFile = await localFolder.GetFileAsync(fileName);
                await newFile.MoveAsync(folder, fileName, NameCollisionOption.ReplaceExisting);

                // Add option to make the explorer select the newly created file
                var ItemToSelect = await folder.GetFileAsync(fileName);
                var option = new FolderLauncherOptions();
                option.ItemsToSelect.Add(ItemToSelect);

                // Open system file explorer to where the user has saved the word file
                await Launcher.LaunchFolderAsync(folder, option);
            }
        }
    }
}
