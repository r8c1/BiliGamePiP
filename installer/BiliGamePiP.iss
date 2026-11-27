#ifndef MyAppVersion
  #define MyAppVersion "1.2.1"
#endif
#define MyAppName "BiliGamePiP"
#define MyAppPublisher "BiliGamePiP"
#define MyAppURL "https://r8c1.github.io/BiliGamePiP/"
#define MyAppExeName "BiliGamePiP.exe"

[Setup]
AppId={{9F7C2B1A-4D83-4C12-9B24-E6A175C8F301}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL=https://github.com/r8c1/BiliGamePiP/issues
AppUpdatesURL=https://github.com/r8c1/BiliGamePiP/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=..\dist
OutputBaseFilename=BiliGamePiP-Setup-{#MyAppVersion}
SetupIconFile=..\src\BiliGamePiP\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=yes
LanguageDetectionMethod=uilanguage
#if VER >= 0x06030000
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#else
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
#endif

[Languages]
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
chinesesimplified.CreateDesktopShortcut=创建桌面快捷方式
english.CreateDesktopShortcut=Create a desktop shortcut
chinesesimplified.ShortcutsGroup=快捷方式：
english.ShortcutsGroup=Additional shortcuts:
chinesesimplified.LaunchAfterInstall=安装完成后立即启动 BiliGamePiP
english.LaunchAfterInstall=Launch BiliGamePiP after installation
chinesesimplified.UninstallShortcut=卸载 BiliGamePiP
english.UninstallShortcut=Uninstall BiliGamePiP

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopShortcut}"; GroupDescription: "{cm:ShortcutsGroup}"

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallShortcut}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchAfterInstall}"; Flags: nowait postinstall skipifsilent shellexec
