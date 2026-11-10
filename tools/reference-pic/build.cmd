@echo off
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist bin mkdir bin

"%CSC%" /nologo /codepage:65001 /target:winexe /out:bin\reference-pic.exe /win32manifest:app.manifest /r:System.Windows.Forms.dll /r:System.Drawing.dll src\*.cs
if errorlevel 1 exit /b 1

"%CSC%" /nologo /codepage:65001 /target:exe /out:bin\tests.exe /main:ReferencePic.Tests.TestMain /r:System.Windows.Forms.dll /r:System.Drawing.dll src\*.cs tests\*.cs
if errorlevel 1 exit /b 1

bin\tests.exe
if errorlevel 1 exit /b 1

endlocal
