#define AppName "NEXUS"
#define AppVersion "2.0.0"
#define AppExeName "NEXUS.exe"

[Setup]
AppId={{B8E81C5B-31B7-43D9-9469-DF0A647112C3}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=NEXUS
AppPublisherURL=https://github.com/estluismiguelyucrate-blip/NEXUS
DefaultDirName={localappdata}\\Programs\\NEXUS
DefaultGroupName=NEXUS
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
SetupLogging=yes
Compression=lzma2/ultra64
SolidCompression=yes
OutputDir=..\\artifacts
OutputBaseFilename=NEXUS-Setup-v{#AppVersion}
UninstallDisplayIcon={app}\\{#AppExeName}
VersionInfoDescription=NEXUS Windows installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoVersion=2.0.0.0

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "..\\publish\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\\NEXUS"; Filename: "{app}\\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\\NEXUS"; Filename: "{app}\\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\\{#AppExeName}"; Description: "Iniciar NEXUS ahora"; Flags: postinstall nowait skipifsilent
