#define MyAppName "大有图片工具"
#define MyAppVersion "1.3.0"
#define MyAppExeName "DayouImageTools.exe"

[Setup]
AppId={{caa784ff-b77e-409f-b3f6-5d6d1bf643e0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=大有
AppPublisherURL=https://github.com/dayouAI777/dayou-image-tools
AppSupportURL=https://github.com/dayouAI777/dayou-image-tools/issues
AppUpdatesURL=https://github.com/dayouAI777/dayou-image-tools/releases/latest
DefaultDirName={localappdata}\Programs\大有图片工具
DefaultGroupName=大有图片工具
DisableProgramGroupPage=yes
AllowNoIcons=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=bin
OutputBaseFilename=大有图片工具_安装程序_v1.3.0
SetupIconFile=DayouImageTools.ico
UninstallDisplayName=大有图片工具
UninstallDisplayIcon={app}\DayouImageTools.exe
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
VersionInfoVersion=1.3.0.0
VersionInfoProductName=大有图片工具
VersionInfoCompany=大有
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式："

[Files]
Source: "bin\DayouImageTools.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\DayouImageToolsUpdater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Magick.NET-Q8-x64.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Magick.NET.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Magick.Native-Q8-x64.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "third_party\Magick.NET\Notice.txt"; DestDir: "{app}\licenses"; DestName: "Magick.NET-Notice.txt"; Flags: ignoreversion
Source: "third_party\Magick.NET\Copyright.txt"; DestDir: "{app}\licenses"; DestName: "Magick.NET-Copyright.txt"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; DestName: "使用说明.txt"; Flags: ignoreversion
Source: "UPDATE_PACKAGE_FORMAT.md"; DestDir: "{app}"; DestName: "升级包制作说明.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\大有图片工具"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autoprograms}\升级大有图片工具"; Filename: "{app}\DayouImageToolsUpdater.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\大有图片工具"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{autoprograms}\卸载大有图片工具"; Filename: "{uninstallexe}"

[Registry]
; Replace the older development registration so Open With does not show a stale entry.
Root: HKCU; Subkey: "Software\Classes\Applications\LocalPhotoViewer.exe"; Flags: deletekey

; Register the application by executable name for the classic Open With picker.
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "大有图片工具"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\shell\open\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey

; Register the updater as an Open With handler for this app's custom update archive extension.
Root: HKCU; Subkey: "Software\Classes\Applications\DayouImageToolsUpdater.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "大有图片工具升级程序"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\DayouImageToolsUpdater.exe\SupportedTypes"; ValueType: string; ValueName: ".dayouupdate"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\DayouImageToolsUpdater.exe\shell\open\command"; ValueType: string; ValueData: """{app}\DayouImageToolsUpdater.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\DayouImageTools.Update"; ValueType: string; ValueData: "大有图片工具更新包"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\DayouImageTools.Update\DefaultIcon"; ValueType: string; ValueData: """{app}\DayouImageToolsUpdater.exe"",0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\DayouImageTools.Update\shell\open\command"; ValueType: string; ValueData: """{app}\DayouImageToolsUpdater.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\.dayouupdate"; ValueType: string; ValueData: "DayouImageTools.Update"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\.dayouupdate\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Update"; Flags: uninsdeletevalue uninsdeletekeyifempty

Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".bmp"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".dib"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jpg"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jpeg"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jfif"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".png"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".gif"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".tif"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".tiff"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ico"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".emf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".wmf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".rle"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jpe"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".apng"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cur"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".webp"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".avif"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".heic"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".heif"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jxl"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jp2"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".j2k"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jpf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jpx"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jpm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".mj2"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".psd"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".psb"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".xcf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".dds"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".tga"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".icb"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".vda"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".vst"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pcx"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pcc"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".sgi"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".rgb"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".rgba"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".bw"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".exr"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".hdr"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pic"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".dpx"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cin"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".fits"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".fit"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".fts"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pbm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pgm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ppm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pnm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pam"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pfm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".qoi"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pict"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pct"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pntg"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".svg"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".svgz"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".eps"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".epsf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".epsi"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ps"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pdf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ai"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ora"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".kra"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".clip"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".afphoto"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".dng"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".nef"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".nrw"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cr2"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cr3"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".arw"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".srf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".sr2"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".raf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".orf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".rw2"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pef"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ptx"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".srw"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".3fr"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ari"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".bay"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cap"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".eip"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".erf"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".fff"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".gpr"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".iiq"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".k25"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".kdc"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".mdc"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".mef"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".mos"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".mrw"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".obm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".qtk"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".rwl"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".x3f"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jng"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".mng"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".iff"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".lbm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ilbm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".mac"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".sun"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ras"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pcd"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pdb"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pfa"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pfb"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".fig"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".otb"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".icns"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".fpx"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jbig"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jbg"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".wbmp"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".xpm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".xbm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".viff"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".vif"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ani"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".miff"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".dcm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".dicom"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".xwd"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cdr"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cgm"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".odg"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".wpg"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jxr"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".hdp"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".wdp"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ktx"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".ktx2"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pvr"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".crw"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty

; Shared ProgID: it appears in each supported extension's Open With list.
Root: HKCU; Subkey: "Software\Classes\DayouImageTools.Image"; ValueType: string; ValueData: "大有图片工具"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\DayouImageTools.Image\DefaultIcon"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"",0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\DayouImageTools.Image\shell\open\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\.bmp\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.dib\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jpg\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jpeg\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jfif\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.png\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.gif\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.tif\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.tiff\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ico\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.emf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.wmf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.rle\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jpe\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.apng\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.cur\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.webp\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.avif\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.heic\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.heif\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jxl\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jp2\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.j2k\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jpf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jpx\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jpm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.mj2\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.psd\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.psb\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.xcf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.dds\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.tga\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.icb\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.vda\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.vst\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pcx\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pcc\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.sgi\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.rgb\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.rgba\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.bw\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.exr\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.hdr\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pic\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.dpx\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.cin\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.fits\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.fit\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.fts\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pbm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pgm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ppm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pnm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pam\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pfm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.qoi\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pict\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pct\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pntg\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.svg\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.svgz\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.eps\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.epsf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.epsi\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ps\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pdf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ai\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ora\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.kra\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.clip\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.afphoto\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.dng\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.nef\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.nrw\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.cr2\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.cr3\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.arw\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.srf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.sr2\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.raf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.orf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.rw2\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pef\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ptx\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.srw\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.3fr\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ari\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.bay\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.cap\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.eip\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.erf\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.fff\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.gpr\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.iiq\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.k25\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.kdc\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.mdc\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.mef\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.mos\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.mrw\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.obm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.qtk\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.rwl\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.x3f\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jng\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.mng\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.iff\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.lbm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ilbm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.mac\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.sun\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ras\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pcd\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pdb\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pfa\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pfb\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.fig\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.otb\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.icns\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.fpx\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jbig\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jbg\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.wbmp\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.xpm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.xbm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.viff\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.vif\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ani\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.miff\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.dcm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.dicom\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.xwd\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.cdr\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.cgm\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.odg\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.wpg\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.jxr\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.hdp\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.wdp\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ktx\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.ktx2\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.pvr\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.crw\OpenWithProgids"; ValueType: none; ValueName: "DayouImageTools.Image"; Flags: uninsdeletevalue uninsdeletekeyifempty

; The RegisteredApplications capability makes the app discoverable in Windows app settings too.
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "大有图片工具"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "本地图片浏览、编辑与批量处理工具"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bmp"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".dib"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpg"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpeg"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jfif"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".png"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gif"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tif"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tiff"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ico"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".emf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".wmf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".rle"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpe"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".apng"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cur"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".webp"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".avif"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heic"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heif"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jxl"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jp2"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".j2k"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpx"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mj2"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".psd"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".psb"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xcf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".dds"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tga"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".icb"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".vda"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".vst"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pcx"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pcc"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".sgi"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".rgb"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".rgba"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bw"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".exr"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".hdr"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pic"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".dpx"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cin"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".fits"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".fit"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".fts"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pbm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pgm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ppm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pnm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pam"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pfm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".qoi"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pict"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pct"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pntg"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".svg"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".svgz"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".eps"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".epsf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".epsi"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ps"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pdf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ai"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ora"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".kra"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".clip"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".afphoto"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".dng"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".nef"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".nrw"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cr2"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cr3"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".arw"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".srf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".sr2"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".raf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".orf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".rw2"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pef"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ptx"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".srw"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".3fr"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ari"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bay"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cap"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".eip"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".erf"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".fff"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gpr"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".iiq"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".k25"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".kdc"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mdc"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mef"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mos"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mrw"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".obm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".qtk"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".rwl"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".x3f"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jng"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mng"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".iff"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".lbm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ilbm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mac"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".sun"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ras"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pcd"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pdb"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pfa"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pfb"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".fig"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".otb"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".icns"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".fpx"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jbig"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jbg"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".wbmp"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xpm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xbm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".viff"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".vif"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ani"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".miff"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".dcm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".dicom"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xwd"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cdr"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cgm"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".odg"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".wpg"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jxr"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".hdp"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".wdp"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ktx"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ktx2"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pvr"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\大有图片工具\Capabilities\FileAssociations"; ValueType: string; ValueName: ".crw"; ValueData: "DayouImageTools.Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "大有图片工具"; ValueData: "Software\大有图片工具\Capabilities"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动大有图片工具"; Flags: postinstall nowait skipifsilent

[Code]
procedure SHChangeNotify(wEventId: Longint; uFlags: Longint; dwItem1: Longint; dwItem2: Longint);
external 'SHChangeNotify@shell32.dll stdcall';

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    SHChangeNotify($08000000, $0000, 0, 0);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    SHChangeNotify($08000000, $0000, 0, 0);
end;
