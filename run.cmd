@echo off
setlocal
set "ROOT=%~dp0"
if not exist "%ROOT%bin\DayouImageTools.exe" call "%ROOT%build.cmd"
if errorlevel 1 exit /b 1
start "" "%ROOT%bin\DayouImageTools.exe" %*
