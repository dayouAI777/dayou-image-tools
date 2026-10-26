@echo off
setlocal EnableDelayedExpansion
set "ROOT=%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo .NET Framework C# compiler was not found.
  exit /b 1
)
if not exist "%ROOT%bin" mkdir "%ROOT%bin"
if not exist "%ROOT%DayouImageTools.ico" (
  echo Missing application icon: "%ROOT%DayouImageTools.ico"
  exit /b 1
)
set "MAGICK=%ROOT%third_party\Magick.NET"
set "FACADE=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\netstandard.dll"
if not exist "%MAGICK%\Magick.NET-Q8-x64.dll" (
  set "PYTHON=%LOCALAPPDATA%\Programs\Python\Python312\python.exe"
  if not exist "!PYTHON!" set "PYTHON=python"
  "!PYTHON!" "%ROOT%tools\restore_magick.py"
  if errorlevel 1 exit /b 1
)
if not exist "%MAGICK%\Magick.NET-Q8-x64.dll" (
  echo Missing Magick.NET files under "%MAGICK%".
  exit /b 1
)
if not exist "%MAGICK%\Magick.NET.Core.dll" (
  echo Missing Magick.NET.Core.dll under "%MAGICK%".
  exit /b 1
)
if not exist "%MAGICK%\Magick.Native-Q8-x64.dll" (
  echo Missing Magick.Native-Q8-x64.dll under "%MAGICK%".
  exit /b 1
)
if not exist "%FACADE%" (
  echo .NET Standard reference facade was not found: "%FACADE%"
  exit /b 1
)
"%CSC%" /nologo /target:winexe /platform:x64 /win32icon:"%ROOT%DayouImageTools.ico" /out:"%ROOT%bin\DayouImageTools.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.VisualBasic.dll /reference:"%FACADE%" /reference:"%MAGICK%\Magick.NET-Q8-x64.dll" /reference:"%MAGICK%\Magick.NET.Core.dll" "%ROOT%AssemblyInfo.cs" "%ROOT%Program.cs" "%ROOT%PhotoEditorForm.cs" "%ROOT%BatchToolsForm.cs" "%ROOT%ImageDecoder.cs"
if errorlevel 1 exit /b 1
copy /y "%ROOT%DayouImageTools.ico" "%ROOT%bin\DayouImageTools.ico" >nul
copy /y "%MAGICK%\Magick.NET-Q8-x64.dll" "%ROOT%bin\Magick.NET-Q8-x64.dll" >nul
copy /y "%MAGICK%\Magick.NET.Core.dll" "%ROOT%bin\Magick.NET.Core.dll" >nul
copy /y "%MAGICK%\Magick.Native-Q8-x64.dll" "%ROOT%bin\Magick.Native-Q8-x64.dll" >nul
copy /y "%ROOT%bin\DayouImageTools.exe" "%ROOT%bin\LocalPhotoViewer.exe" >nul
"%CSC%" /nologo /target:winexe /platform:x64 /win32icon:"%ROOT%DayouImageTools.ico" /out:"%ROOT%bin\DayouImageToolsUpdater.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Xml.Linq.dll /reference:System.Web.Extensions.dll "%ROOT%UpdaterAssemblyInfo.cs" "%ROOT%GitHubReleaseClient.cs" "%ROOT%UpdaterProgram.cs"
if errorlevel 1 exit /b 1
echo Built: "%ROOT%bin\DayouImageTools.exe"
echo Built: "%ROOT%bin\DayouImageToolsUpdater.exe"
