#define MyAppName "TweakHub"
#define MyAppVersion GetStringFileInfo("..\publish-standalone\TweakHub.exe", "ProductVersion")
#define MyAppPublisher "PrimeBuild-pc"
#define MyAppExeName "TweakHub.exe"

[Setup]
AppId={{9A5F5DA6-6E2B-4D45-9B6E-5B1F7B2E2C2F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

SetupIconFile=..\ico.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[CustomMessages]
english.CreateDesktopIcon=Create a &desktop icon
english.AdditionalIcons=Additional icons:
english.LaunchApp=Launch {#MyAppName}
russian.CreateDesktopIcon=Создать значок на &рабочем столе
russian.AdditionalIcons=Дополнительные значки:
russian.LaunchApp=Запустить {#MyAppName}
chinesesimplified.CreateDesktopIcon=创建桌面快捷方式(&D)
chinesesimplified.AdditionalIcons=附加图标：
chinesesimplified.LaunchApp=启动 {#MyAppName}
spanish.CreateDesktopIcon=Crear un icono en el &escritorio
spanish.AdditionalIcons=Iconos adicionales:
spanish.LaunchApp=Iniciar {#MyAppName}
italian.CreateDesktopIcon=Crea un'icona sul &desktop
italian.AdditionalIcons=Icone aggiuntive:
italian.LaunchApp=Avvia {#MyAppName}

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish-standalone\*"; DestDir: "{app}"; Excludes: "portable.flag,Data\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent
