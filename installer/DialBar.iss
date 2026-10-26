; DialBar setup: installs the UI, installs and starts the sensor helper service, adds shortcuts.
; Build it with scripts\package.ps1, which publishes both projects into ..\publish first.

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#define ServiceName "DialBarSensors"
#define LegacyServiceName "PcMeterSensors"

[Setup]
AppId={{8F3C2A51-7D4E-4B9A-9C61-2E5D0F1A7B33}
AppName=DialBar
AppVersion={#AppVersion}
AppPublisher=Ertan Asan
AppPublisherURL=https://github.com/ertanasan
DefaultDirName={autopf}\DialBar
DefaultGroupName=DialBar
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
OutputDir=..\dist
OutputBaseFilename=DialBar-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\src\DialBar.App\Assets\DialBar.ico
UninstallDisplayIcon={app}\DialBar.App.exe
UninstallDisplayName=DialBar
CloseApplications=yes

[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"

[Files]
Source: "..\publish\App\*";    DestDir: "{app}";        Flags: recursesubdirs ignoreversion
Source: "..\publish\Helper\*"; DestDir: "{app}\Helper"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\DialBar"; Filename: "{app}\DialBar.App.exe"
Name: "{autodesktop}\DialBar";  Filename: "{app}\DialBar.App.exe"; Tasks: desktopicon

[Run]
; Setup runs elevated; start the UI as the signed-in user so it is not elevated.
Filename: "{app}\DialBar.App.exe"; Description: "Launch DialBar"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{sys}\net.exe"; Parameters: "stop {#ServiceName}";   Flags: runhidden; RunOnceId: "StopService"
Filename: "{sys}\sc.exe";  Parameters: "delete {#ServiceName}"; Flags: runhidden; RunOnceId: "DeleteService"

[Code]
function Run(const FileName, Params: String): Integer;
begin
  if not Exec(ExpandConstant(FileName), Params, '', SW_HIDE, ewWaitUntilTerminated, Result) then
    Result := -1;
end;

// Stop the helper (net stop waits until it has exited) so its files can be replaced, and remove
// the service left by the app's previous name.
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Run('{sys}\net.exe', 'stop {#ServiceName}');
  Run('{sys}\net.exe', 'stop {#LegacyServiceName}');
  Run('{sys}\sc.exe', 'delete {#LegacyServiceName}');
  Result := '';
end;

procedure InstallService();
var
  BinPath: String;
begin
  // Inner quotes keep the path with spaces from being an unquoted service path.
  BinPath := '"\"' + ExpandConstant('{app}\Helper\DialBar.Helper.exe') + '\""';
  // create fails harmlessly on upgrade; config then brings an existing service up to date.
  Run('{sys}\sc.exe', 'create {#ServiceName} binPath= ' + BinPath + ' start= auto DisplayName= "DialBar Sensor Helper"');
  Run('{sys}\sc.exe', 'config {#ServiceName} binPath= ' + BinPath + ' start= auto');
  Run('{sys}\sc.exe', 'description {#ServiceName} "Reads CPU and disk temperatures for DialBar and shares them with the app over a local pipe."');
  Run('{sys}\sc.exe', 'failure {#ServiceName} reset= 86400 actions= restart/5000/restart/5000/none/0');
  if Run('{sys}\net.exe', 'start {#ServiceName}') <> 0 then
    MsgBox('The DialBar sensor helper service could not be started. CPU and disk temperatures will show N/A.',
      mbError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    InstallService();
end;

// Drop the app's "Start with Windows" entry (set from the tray menu) so nothing points at removed files.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'DialBar');
end;
