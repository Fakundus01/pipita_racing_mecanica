#define MyAppName "Pipita Garage Desktop"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Pipita Garage"
#define MyAppExeName "PipitaDesktop.exe"
#define MyAppId "PipitaGarageDesktop"
#define MyPublishDir "..\\src\\PipitaDesktop\\bin\\Release\\net8.0-windows\\win-x64\\publish"
#define MyIconFile "..\\src\\PipitaDesktop\\Assets\\pipita-car.ico"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#MyIconFile}
OutputDir=..\\dist
OutputBaseFilename=PipitaGarageDesktopSetup
PrivilegesRequired=admin
DisableProgramGroupPage=yes
ChangesAssociations=no
CloseApplications=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\\reset-local-data.ps1"; DestDir: "{app}"; DestName: "Reiniciar datos locales.ps1"; Flags: ignoreversion

[Icons]
Name: "{group}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; IconFilename: "{app}\\{#MyAppExeName}"
Name: "{autodesktop}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\\{#MyAppExeName}"

[Run]
Filename: "{app}\\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

