===============
havWndSwitcher

VERSION 1.0.0.0
===============

Copyright (c) 2025 René Nicolaus

Build: Windows (.NET 8, WinForms)
Source Code: https://github.com/Havoc7891/havWndSwitcher

========
Contents
========
1. What it does
2. Requirements
3. How to run
4. Start with Windows (Optional)
5. Tray menu
6. Building the executable
7. Notes
8. Uninstall
9. Troubleshooting
10. Changelog
11. License

===============
1. What it does
===============
A WinForms background app that registers global hotkeys to switch windows with optional switching rules.

===============
2. Requirements
===============
- Windows 10 or later
- .NET 8 Desktop Runtime (x64)

If you don't already have the .NET 8 Desktop Runtime, download and install from: https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime

=============
3. How to run
=============
1) Extract the ZIP anywhere (e.g., C:\Apps\havWndSwitcher).
2) Run havWndSwitcher.exe (No admin required).
3) Look for the tray icon. Use the global hotkeys to switch windows.

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
Right-click the tray icon for settings.

==========================
6. Building the executable
==========================
Compile the executable with: dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true

========
7. Notes
========
- Uses global system-wide hotkeys; if hotkeys are taken, the app shows an error.
- No elevation required. Works in the user session.

============
8. Uninstall
============
- Exit from the tray -> delete the app folder
- Delete config file in %AppData%\havWndSwitcher (Win + R -> %AppData% -> Enter)
- If you enabled auto-start: remove the shortcut from shell:startup or delete the 'havWndSwitcher' value from:
  HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run

==================
9. Troubleshooting
==================
- Hotkeys don't work: Another app may own the combos. Pick different hotkeys in the settings dialog.
- No tray icon: Make sure Windows hasn't hidden it; expand the tray overflow.

=============
10. Changelog
=============

Version 1.0 - 2025-12-31
- First release.

===========
11. License
===========
Licensed under MIT - see LICENSE.txt

===========
End of file
===========