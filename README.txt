===============
havWndSwitcher

VERSION 1.1.0.0
===============

Copyright (c) 2025-2026 René Nicolaus

Build: Windows (.NET 10, WinForms)
Source Code: https://github.com/Havoc7891/havWndSwitcher

========
Contents
========
1. What it does
2. Requirements
3. How to run
4. Start with Windows (Optional)
5. Tray menu
6. Notes
7. Uninstall
8. Troubleshooting
9. Changelog
10. License

===============
1. What it does
===============
A WinForms background app that registers global hotkeys to switch windows with optional switching rules.

===============
2. Requirements
===============
- Windows 10 or later
- .NET 10 Desktop Runtime (x64)

If you don't already have the .NET 10 Desktop Runtime, download and install from: https://dotnet.microsoft.com/en-us/download/dotnet/10.0/runtime

=============
3. How to run
=============
1) Extract the ZIP anywhere (e.g., C:\Apps\havWndSwitcher).
2) Run havWndSwitcher.exe (No admin required).
3) Find the tray icon and right-click it to open the settings menu. Use the global hotkeys to switch windows.

================================
4. Start with Windows (Optional)
================================
Option A - Startup folder (Per-user)
- Press Win + R -> type: shell:startup -> OK
- Place a shortcut to havWndSwitcher.exe in that folder

Option B - Registry (Per-user)
Create a file named 'havWndSwitcher-Startup.reg' with the content below, edit the path, then double-click to add:

Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run]
"havWndSwitcher"="\"C:\\Apps\\havWndSwitcher\\havWndSwitcher.exe\""

============
5. Tray menu
============
Find the tray icon and right-click it to open the settings menu.

Menu items:
- About: shows the version and information about the app
- Settings...: opens the dialog for global shortcuts, the fullscreen override key, boost duration, and language
- Prefer Per-Monitor: prefers windows on the same monitor as the active window
- Skip Fullscreen Windows: skips fullscreen targets during normal switching unless the fullscreen override key is held
- Suspend When Fullscreen: pauses switching while the active window is fullscreen unless the fullscreen override key is held
- Boost New Windows: prioritizes newly opened windows from the app you last switched to, ahead of monitor and fullscreen-skip rules
- Taskbar Windows Only: filters out tool windows and owned dialogs when selecting non-minimized windows
- Prefer Main Window Per Process: selects one window per process, preferring its main window
- Include Minimized Windows: includes minimized windows and restores them when selected
- Reset to Defaults: restores all settings and shortcuts to their defaults
- Restart Hotkeys: registers the configured shortcuts again
- Open Config Folder: opens %AppData%\havWndSwitcher in File Explorer
- Reload Config: reloads saved settings from config.json; shortcut changes take effect after Restart Hotkeys
- Exit: quits the app

========
6. Notes
========
- Uses global system-wide hotkeys; if hotkeys are taken, the app shows an error.
- No elevation required. Works in the user session.

============
7. Uninstall
============
- Exit from the tray -> delete the app folder
- Delete config file in %AppData%\havWndSwitcher (Win + R -> %AppData% -> Enter)
- If you enabled auto-start: remove the shortcut from shell:startup or delete the 'havWndSwitcher' value from:
  HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run

==================
8. Troubleshooting
==================
- Hotkeys don't work: Another app may own the combos. Pick different hotkeys in the settings dialog.
- No tray icon: Make sure Windows hasn't hidden it; expand the tray overflow.

============
9. Changelog
============

Version 1.1.0.0 - 2026-09-26
- Upgraded to .NET 10.
- Reject F12 for global hotkeys because Windows reserves it for debugging.

Version 1.0.0.0 - 2025-12-31
- First release.

===========
10. License
===========
Licensed under MIT - see LICENSE.txt

===========
End of file
===========