#define MyAppName "Break30"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Visiko Srlu"
#define MyAppURL "https://github.com/jfondrix/Break30"
#define MyAppExeName "Break30.exe"

[Setup]
AppId={{CCAD7226-4F92-443A-86AB-33BCAC7985A0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=C:\Apps\Break30-installer
OutputBaseFilename=Break30Setup-v0.1.0
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startup"; Description: "Start Break30 when Windows starts"; GroupDescription: "Startup:"; Flags: checkedonce

[Files]
Source: "C:\Apps\Break30\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Break30"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Break30"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\Break30"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Break30"; Flags: nowait postinstall skipifsilent