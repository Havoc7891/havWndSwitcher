# havWndSwitcher

A WinForms background app that registers global hotkeys to switch windows with optional switching rules.

## Table of Contents

- [Requirements](#requirements)
- [How to run](#how-to-run)
- [Start with Windows (Optional)](#start-with-windows-optional)
- [Tray menu](#tray-menu)
- [Building from source](#building-from-source)
- [Localization](#localization)
- [Contributing](#contributing)
- [License](#license)

## Requirements

- Windows 10 or later
- [.NET 10 Desktop Runtime (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0/runtime)

## How to run

1) Extract the [ZIP](https://github.com/Havoc7891/havWndSwitcher/releases/download/v1.1.0.0/havWndSwitcher-1.1.0.0-win-x64.zip) anywhere (e.g., `C:\Apps\havWndSwitcher`).
2) Run havWndSwitcher.exe (no admin required).
3) Find the tray icon and right-click it to open the settings menu. Use the global hotkeys to switch windows.

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

## Tray menu

Find the tray icon and right-click it to open the settings menu.

Menu items:

- **About**: shows the version and information about the app
- **Settings...**: opens the dialog for global shortcuts, the fullscreen override key, boost duration, and language
- **Prefer Per-Monitor**: prefers windows on the same monitor as the active window
- **Skip Fullscreen Windows**: skips fullscreen targets during normal switching unless the fullscreen override key is held
- **Suspend When Fullscreen**: pauses switching while the active window is fullscreen unless the fullscreen override key is held
- **Boost New Windows**: prioritizes newly opened windows from the app you last switched to, ahead of monitor and fullscreen-skip rules
- **Taskbar Windows Only**: filters out tool windows and owned dialogs when selecting non-minimized windows
- **Prefer Main Window Per Process**: selects one window per process, preferring its main window
- **Include Minimized Windows**: includes minimized windows and restores them when selected
- **Reset to Defaults**: restores all settings and shortcuts to their defaults
- **Restart Hotkeys**: registers the configured shortcuts again
- **Open Config Folder**: opens `%AppData%\havWndSwitcher` in File Explorer
- **Reload Config**: reloads saved settings from `config.json`; shortcut changes take effect after Restart Hotkeys
- **Exit**: quits the app

## Building from source

Install the .NET 10 SDK (x64) on Windows 10 or later. Clone or download this repository, then open a terminal in its root folder.

Build the app:

```powershell
dotnet build havWndSwitcher.csproj -c Release
```

Run `bin\Release\net10.0-windows\havWndSwitcher.exe`.

To publish a Windows x64 build for distribution:

```powershell
dotnet publish havWndSwitcher.csproj -c Release -r win-x64 --self-contained false -p:PublishReadyToRun=false -p:DebugType=none -p:DebugSymbols=false
```

The output is in `bin\Release\net10.0-windows\win-x64\publish`. Distribute the entire folder, including `languages`. This build requires the .NET 10 Desktop Runtime (x64) on the target machine.

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

Copyright &copy; 2025-2026 Ren&eacute; Nicolaus

Released under the [MIT license](/LICENSE).
