# havWndSwitcher

A WinForms background app that registers global hotkeys to switch windows with optional switching rules.

## Table of Contents

- [Requirements](#requirements)
- [How to run](#how-to-run)
- [Start with Windows (Optional)](#start-with-windows-optional)
- [Building the executable](#building-the-executable)
- [Localization](#localization)
- [Contributing](#contributing)
- [License](#license)

## Requirements
- Windows 10 or later
- [.NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime)

## How to run

1) Extract the [ZIP](https://github.com/Havoc7891/havWndSwitcher/releases/download/v1.0.0.0/havWndSwitcher-1.0.0.0-win-x64.zip) anywhere (e.g., `C:\Apps\havWndSwitcher`).
2) Run havWndSwitcher.exe (no admin required).
3) Look for the tray icon. Use the global hotkeys to switch windows.

![Screenshot](/screenshot/havWndSwitcher.png)

## Start with Windows (Optional)
Option A - Startup folder (Per-user)
- Press Win + R -> type: `shell:startup` -> OK
- Place a shortcut to havWndSwitcher.exe in that folder

Option B - Registry (Per-user)
Create a file named 'havWndSwitcher-Startup.reg' with the content below, edit the path, then double-click to add:

```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run]
"havWndSwitcher"="\"C:\\Apps\\havWndSwitcher\\havWndSwitcher.exe\""
```

## Building the executable
Compile the executable with: `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true`

## Localization

The app currently supports the following languages:

- **English (en)**
- **German (de)**

If you would like to contribute translations for additional languages, please submit a pull request.

The translation files are in the `languages` folder; use `languages/en.json` as the template.

## Contributing

Thank you for your interest! Suggestions for features and bug reports are always welcome via issues.

To maintain a consistent design and quality for this project, changes are implemented by the maintainer rather than via direct pull requests, except for localization updates.

## License

Copyright &copy; 2025 Ren&eacute; Nicolaus

Released under the [MIT license](/LICENSE).
