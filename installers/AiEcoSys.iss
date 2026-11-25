; AiEcoSys Windows installer (Inno Setup 6)
; Build with:
;   D:\Tools\InnoSetup\ISCC.exe D:\AiEcoSys\installers\AiEcoSys.iss
;
; The installer is per-user, installs to D:\AiEcoSys\Application by default,
; writes the resolved data directory to aiecoSys.paths.json, and never deletes
; user data without an explicit confirmation during uninstall.

#define MyAppName "AiEcoSys"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "The Jabar"
#define MyAppURL "https://github.com/thejabar/AiEcoSys"
#define MyAppExeName "AiEcoSys.exe"
#define MyAppId "{{7D4B2E8A-3C61-4F2A-9E7B-5A1C8D3F6E20}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName=D:\AiEcoSys\Application
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
UsePreviousAppDir=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=D:\AiEcoSys\Release\Installer
OutputBaseFilename=AiEcoSys-Setup
SetupIconFile=D:\AiEcoSys\assets\AiEcoSys.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
LicenseFile=D:\AiEcoSys\LICENSE
VersionInfoVersion=1.0.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
CloseApplications=yes
RestartApplications=no
AllowNoIcons=yes
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "D:\AiEcoSys\Release\Portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\AiEcoSys"; ValueType: string; ValueName: "InstallDirectory"; ValueData: "{app}"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\AiEcoSys"; ValueType: string; ValueName: "DataDirectory"; ValueData: "{code:GetDataDir}"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\AiEcoSys"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\AiEcoSys"; ValueType: none; ValueName: ""; Flags: uninsdeletekeyifempty

[UninstallDelete]
Type: files; Name: "{app}\aiecoSys.paths.json"
Type: dirifempty; Name: "{app}"

[Code]
var
  ResolvedDataDir: string;

function IsSafeDataDir(const Value: String): Boolean;
begin
  Result := (Length(Value) >= 4) and
            (Copy(Value, 2, 1) = ':') and
            (CompareText(Copy(Value, 1, 2), 'C:') <> 0);
end;

function GetDataDir(Param: String): String;
var
  Custom: String;
begin
  Custom := ExpandConstant('{param:DATA|}');
  Custom := RemoveBackslashUnlessRoot(Custom);

  if (Custom <> '') and IsSafeDataDir(Custom) then
  begin
    Result := Custom;
    exit;
  end;

  if CompareText(ExpandConstant('{app}'), 'D:\AiEcoSys\Application') = 0 then
    Result := 'D:\AiEcoSys\Data'
  else
    Result := AddBackslash(ExpandConstant('{app}')) + 'Data';
end;

procedure WritePathsFile;
var
  DataDir, JsonDataDir, PathsFile, Json: String;
begin
  DataDir := GetDataDir('');
  ResolvedDataDir := DataDir;

  if not DirExists(DataDir) then
    ForceDirectories(DataDir);
  ForceDirectories(AddBackslash(DataDir) + 'settings');
  ForceDirectories(AddBackslash(DataDir) + 'snapshots');

  JsonDataDir := DataDir;
  StringChangeEx(JsonDataDir, '\', '\\', True);
  Json := '{' + #13#10 +
          '  "DataDirectory": "' + JsonDataDir + '"' + #13#10 +
          '}' + #13#10;

  PathsFile := AddBackslash(ExpandConstant('{app}')) + 'aiecoSys.paths.json';
  if not SaveStringToFile(PathsFile, Json, False) then
    RaiseException('AiEcoSys could not write the path configuration: ' + PathsFile);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    WritePathsFile;

    if not FileExists(ExpandConstant('{app}\AiEcoSys.exe')) then
      RaiseException('AiEcoSys.exe was not installed correctly.');
    if not FileExists(ExpandConstant('{app}\catalogue\api.json')) then
      RaiseException('The bundled catalogue was not installed correctly.');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir, AppDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    if RegQueryStringValue(HKCU, 'Software\AiEcoSys', 'DataDirectory', DataDir) then
    begin
      AppDir := ExpandConstant('{app}');
      if (DataDir <> '') and
         (Length(DataDir) > 3) and
         (CompareText(DataDir, AppDir) <> 0) and
         DirExists(DataDir) then
      begin
        if MsgBox('Delete AiEcoSys user data at:' + #13#10 + DataDir + #13#10 + #13#10 +
                  'This removes settings, snapshots, and cached catalogue data. ' +
                  'It cannot be undone. Delete user data?',
                  mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
          DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
