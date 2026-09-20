#define MyAppName "ApotekPOS"
#define MyAppVersion "4.2.1"
#define MyAppPublisher "MY"
#define MyAppExeName "ApotekApp.exe"

[Setup]
AppId={A6D0C9D7-1F6B-4D2D-8C7A-340000000002}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ApotekPOS
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=Setup-ApotekPOS
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
Compression=lzma
SolidCompression=yes
WizardStyle=modern dynamic
UninstallDisplayName={#MyAppName}

[Files]
Source: "..\bin\Publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Buat shortcut di Desktop"; GroupDescription: "Shortcut tambahan:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Jalankan {#MyAppName}"; Flags: nowait postinstall skipifsilent
