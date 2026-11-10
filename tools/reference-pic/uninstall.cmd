@echo off
setlocal

echo Stopping reference-pic if running...
taskkill /IM reference-pic.exe /F >nul 2>&1

echo Removing startup shortcut...
del /F /Q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\reference-pic.lnk" >nul 2>&1

echo Removing %LOCALAPPDATA%\reference-pic...
rmdir /S /Q "%LOCALAPPDATA%\reference-pic" >nul 2>&1

echo Uninstall complete.
endlocal
