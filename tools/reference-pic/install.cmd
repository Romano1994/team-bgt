@echo off
setlocal
cd /d "%~dp0"

echo Stopping reference-pic if running...
taskkill /IM reference-pic.exe /F >nul 2>&1

echo Building...
call "%~dp0build.cmd"
rem shortcut: right after the kill, csc sometimes fails with CVT1108 - temp .res file locked, cause unknown. Retry once; dig in if it still fails.
if errorlevel 1 (
    echo Build failed, retrying once...
    ping -n 3 127.0.0.1 >nul
    call "%~dp0build.cmd"
)
if errorlevel 1 (
    echo Build failed. Install aborted.
    exit /b 1
)

rem Run from a fixed folder so the startup shortcut survives when this source folder (e.g. a plugin copy) is updated.
set DEST=%LOCALAPPDATA%\reference-pic
echo Copying to %DEST%...
if not exist "%DEST%" mkdir "%DEST%"
copy /Y "bin\reference-pic.exe" "%DEST%\" >nul
if errorlevel 1 (
    echo Copy failed. Install aborted.
    exit /b 1
)

echo Registering startup shortcut...
powershell -NoProfile -Command "$d = Join-Path $env:LOCALAPPDATA 'reference-pic'; $s = New-Object -ComObject WScript.Shell; $lnk = $s.CreateShortcut((Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup\reference-pic.lnk')); $lnk.TargetPath = (Join-Path $d 'reference-pic.exe'); $lnk.WorkingDirectory = $d; $lnk.Save()"
if errorlevel 1 (
    echo Failed to create shortcut.
    exit /b 1
)

echo Starting reference-pic...
rem Not "start": the tray app would inherit our output pipe and a caller reading it (e.g. Claude Code) would never finish.
powershell -NoProfile -Command "Start-Process (Join-Path $env:LOCALAPPDATA 'reference-pic\reference-pic.exe')"

echo Install complete. Press Win+Shift+D to start drawing.
endlocal
