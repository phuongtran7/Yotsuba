<br />
<p align="center">
    <img src="Yotsuba/Assets/StoreLogo.scale-400.png" alt="Logo" width="80" height="80">
  </a>

  <h3 align="center">Yotsuba</h3>

  <p align="center">
  		A simple application that allows the user to track and organized completed tasks in a quick and efficient manner, along with the ability to export .docx report without having Word installed.
  </p>
</p>

## About The Project

![Intro](Documents/Assets/Introduction.gif)

Every month I have to sit down and write a report about what I completed. At first, I used Trello to keep track of what I was doing and wrote a small console application to take the Trello board and convert it to an Word document for processing later.

It worked great for what it was. However, the console application used LaTeX for intermediate format and then convert it to PDF and Word using MiKTeX and Pandoc. With just a small task like that, the dependencies are rather large and difficult to install. Furthermore, the application requires Internet connection to get the data from Trello.

So I set out to solve all these problems and arrived with this. A simple GUI to track and organize tasks. It also has the ability to write to Word document directly, without having Word installed, thanks to [Open XML SDK](https://github.com/OfficeDev/Open-XML-SDK).

The project took inspiration from Microsoft's [XAML Controls Gallery](https://github.com/microsoft/Xaml-Controls-Gallery) for its general visual and behaviors.

### Installation

1. Install Visual Studio and Universal Windows Platform development workdload.
2. Clone the repo
```
git clone https://github.com/phuongtran7/Yotsuba.git
```
3. Build the project.

### Built With

1. [Windows Template Studio](https://github.com/microsoft/WindowsTemplateStudio).
2. [Microsoft SQlite Library](https://docs.microsoft.com/en-us/windows/uwp/data-access/sqlite-databases).
3. [Windows Community Toolkit](https://github.com/windows-toolkit/WindowsCommunityToolkit).
4. [Open XML SDK](https://github.com/OfficeDev/Open-XML-SDK).

### Usage

See [Wiki](https://github.com/phuongtran7/Yotsuba/wiki) for more detailed usage.

### Reason For The Name

It's just that I really love the [Yotsuba&!](https://en.wikipedia.org/wiki/Yotsuba%26!) manga. The entire series is about every day adventure of Yotsuba, an energetic, cheerful girl. From catching cicadas, to riding a bike; from running an errand to camping overnight, she does everything with absolute joy. It's a very cute, fun series to read.

## License

Distributed under the MIT License. See [LICENSE](https://github.com/phuongtran7/Yotsuba/blob/master/LICENSE) for more information.

## Contact

Phuong Tran

Project Link: [https://github.com/phuongtran7/Yotsuba](https://github.com/phuongtran7/Yotsuba)