#ifndef AppVersion
  #define AppVersion "1.0.0.0"
#endif
#ifndef X86Source
  #error X86Source must be defined
#endif
#ifndef X64Source
  #error X64Source must be defined
#endif
#ifndef PrerequisitePath
  #error PrerequisitePath must be defined
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif
#ifndef OutputBaseFilename
  #define OutputBaseFilename "WinLic-Setup"
#endif

#include "includes\ProductMetadata.iss"

[Setup]
#ifdef TestInstallMode
AppId={{8E9E3612-C2B0-4A58-ACB4-8D1BEA0FE519}
#else
AppId={{#ProductAppId}
#endif
AppName={#ProductName}
AppVersion={#AppVersion}
AppPublisher={#ProductPublisher}
DefaultDirName={code:GetDefaultInstallDir}
DefaultGroupName={#ProductName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile=..\WinLicApp\winlic.ico
UninstallDisplayIcon={app}\{#ProductExe}
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x86compatible
#ifdef TestInstallMode
PrivilegesRequired=lowest
#else
PrivilegesRequired=admin
#endif
MinVersion=6.1sp1
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#ProductName}
VersionInfoCompany={#ProductPublisher}
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
ChangesEnvironment=no
DisableWelcomePage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "vietnamese"; MessagesFile: "languages\Vietnamese.isl"

#include "includes\Messages.iss"

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#X86Source}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: UseX86Payload
Source: "{#X64Source}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: UseX64Payload
Source: "{#PrerequisitePath}"; Flags: dontcopy noencryption

[Icons]
Name: "{group}\WinLic License Audit"; Filename: "{app}\{#ProductExe}"; WorkingDir: "{userdocs}"
Name: "{group}\WinLic Audit CLI"; Filename: "{cmd}"; Parameters: "/k &quot;&quot;{app}\{#CliExe}&quot; --help&quot;"; WorkingDir: "{userdocs}"
Name: "{autodesktop}\WinLic License Audit"; Filename: "{app}\{#ProductExe}"; Tasks: desktopicon; WorkingDir: "{userdocs}"

[Run]
Filename: "{app}\{#ProductExe}"; Description: "Launch WinLic License Audit"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallDelete]
Type: files; Name: "{app}\installation-manifest.json"

[Code]
const
  DotNet48Release = 528040;
#ifdef TestInstallMode
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{8E9E3612-C2B0-4A58-ACB4-8D1BEA0FE519}_is1';
#else
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#ProductAppId}_is1';
#endif

var
  SelectedPayload: String;
  FrameworkWasInstalled: Boolean;

function NativeArm64: Boolean;
begin
  Result := IsArm64;
end;

function UseX64Payload: Boolean;
begin
#ifdef TestForceX86
  Result := False;
#else
  Result := IsX64OS and (not NativeArm64);
#endif
end;

function UseX86Payload: Boolean;
begin
  Result := not UseX64Payload;
end;

function GetDefaultInstallDir(Param: String): String;
begin
  if UseX64Payload then Result := ExpandConstant('{autopf64}\WinLic')
  else Result := ExpandConstant('{autopf32}\WinLic');
end;

function DotNetRelease: Cardinal;
begin
  Result := 0;
  if not RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Result) then
    RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Result);
end;

function VersionPart(var S: String): Integer;
var P: Integer; T: String;
begin
  P := Pos('.', S);
  if P = 0 then begin T := S; S := ''; end
  else begin T := Copy(S, 1, P - 1); Delete(S, 1, P); end;
  Result := StrToIntDef(T, 0);
end;

function CompareVersion(A, B: String): Integer;
var I, AV, BV: Integer;
begin
  Result := 0;
  for I := 1 to 4 do begin
    AV := VersionPart(A); BV := VersionPart(B);
    if AV < BV then begin Result := -1; Exit; end;
    if AV > BV then begin Result := 1; Exit; end;
  end;
end;

function InitializeSetup: Boolean;
var V: TWindowsVersion; Existing: String;
begin
  Result := False;
  GetWindowsVersionEx(V);
  if (V.Major = 6) and (V.Minor = 2) then begin MsgBox(ExpandConstant('{cm:Win8Blocked}'), mbError, MB_OK); Exit; end;
  if (V.Major = 6) and (V.Minor = 1) and (V.ServicePackMajor < 1) then begin MsgBox(ExpandConstant('{cm:Win7Sp1Required}'), mbError, MB_OK); Exit; end;
  if ((V.Major = 6) and ((V.Minor = 1) or (V.Minor = 3))) and (not WizardSilent) then
    MsgBox(ExpandConstant('{cm:EolWarning}'), mbInformation, MB_OK);
#ifdef TestInstallMode
  if RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', Existing) and
#else
  if RegQueryStringValue(HKLM, UninstallKey, 'DisplayVersion', Existing) and
#endif
     (CompareVersion(Existing, '{#AppVersion}') > 0) then begin
    MsgBox(ExpandConstant('{cm:DowngradeBlocked}'), mbError, MB_OK); Exit;
  end;
  if NativeArm64 and (DotNetRelease < DotNet48Release) then begin
    MsgBox(ExpandConstant('{cm:ArmFrameworkMissing}'), mbError, MB_OK); Exit;
  end;
  Result := True;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Code: Integer;
begin
  Result := '';
  if DotNetRelease >= DotNet48Release then Exit;
  if not WizardSilent then MsgBox(ExpandConstant('{cm:FrameworkInstall}'), mbInformation, MB_OK);
  ExtractTemporaryFile('{#ExtractFileName(PrerequisitePath)}');
  if not Exec(ExpandConstant('{tmp}\{#ExtractFileName(PrerequisitePath)}'),
    '/q /norestart /ChainingPackage WinLic', '', SW_HIDE, ewWaitUntilTerminated, Code) then begin
    Result := 'Unable to start the Microsoft .NET Framework installer.'; Exit;
  end;
  if (Code <> 0) and (Code <> 1641) and (Code <> 3010) then begin
    Result := FmtMessage('Microsoft .NET Framework setup failed with exit code %d.', [Code]); Exit;
  end;
  FrameworkWasInstalled := True;
  if (Code = 1641) or (Code = 3010) then NeedsRestart := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var Manifest, FrameworkJson: String;
begin
  if CurStep = ssPostInstall then begin
    if UseX64Payload then SelectedPayload := 'x64' else SelectedPayload := 'x86';
    if FrameworkWasInstalled then FrameworkJson := 'true' else FrameworkJson := 'false';
    Manifest := '{' + #13#10 +
      '  "schemaVersion": 1,' + #13#10 +
      '  "productVersion": "{#AppVersion}",' + #13#10 +
      '  "payload": "' + SelectedPayload + '",' + #13#10 +
      '  "frameworkInstalledBySetup": ' + FrameworkJson + #13#10 +
      '}' + #13#10;
    SaveStringToFile(ExpandConstant('{app}\installation-manifest.json'), Manifest, False);
  end;
end;
