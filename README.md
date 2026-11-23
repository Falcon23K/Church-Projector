# Church-Projector

Church-Projector is an alternative to SongBeamer for projecting song lyrics, Bible verses, and other texts during services. Built with C# and Avalonia, it runs on both Windows and Linux platforms, offering a cross-platform solution for worship presentations.

## Features

- **Song Display**: Project song lyrics with a simple and user-friendly interface.
- **NDI® Output**: Optionally send song and Bible text with a transparent background while keeping the projector output active. Images and video are not sent over NDI.
- **Bible Search**: Quickly find and display Bible verses in various translations.
- **Multilingual Support**: Display texts in multiple languages to meet different linguistic needs.
- **Easy-to-use Interface**: Simple navigation and layout for quick song and verse projection.
- **Cross-platform**: Works on both Windows and Linux thanks to the Avalonia framework.

## Key Functionalities

1. **Quick Song Search**: Access a list of available songs or add new ones for immediate projection.
2. **Bible Verse Projection**: Search and project Bible verses by entering the book, chapter, and verse, and choose from different translations.
3. **Multilingual Bible Verses**: Easily switch between different languages for Bible verses and lyrics, useful for international or multilingual congregations.
4. **Customizable Presentation**: Control text size, color, and layout to match your service's aesthetic or presentation requirements.
5. **Simple Management of Worship Flow**: Organize and plan the worship flow, including songs, Bible readings, and other service elements.

## System Requirements

- **Operating Systems**: Windows and Linux
- **Technology Stack**: 
  - **Programming Language**: C#
  - **UI Framework**: Avalonia

## NDI Output

Enable **NDI output** in Settings -> Window and set the source name that will appear to NDI receivers. NDI sends generated song and Bible text and visible text overlays with a transparent background; images and video are excluded. Install the NDI Runtime separately before using NDI output; NDI Tools are optional. Downloads: [Windows](https://downloads.ndi.tv/SDK/NDI_SDK/NDI%206%20Runtime.exe), [macOS](https://downloads.ndi.tv/SDK/NDI_SDK_Mac/libNDI_for_Mac.pkg), and [Linux](https://downloads.ndi.tv/SDK/NDI_SDK_Linux/Install_NDI_SDK_v6_Linux.tar.gz). Church-Projector looks for the installed runtime in standard locations and supports `NDI_RUNTIME_DIR_V6` as an environment variable for custom locations.


## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests to improve the application.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

---

*NDI® is a registered trademark of Vizrt NDI AB. Learn more at [ndi.video](https://ndi.video/).*