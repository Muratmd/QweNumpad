; Inno Setup script for QweNumpad.
;
; Per-user install: no admin prompt, files go to %LOCALAPPDATA%\Programs\QweNumpad.
; That matches the app itself, which runs asInvoker and only writes HKCU.
;
; Build (after `dotnet publish ... --output publish`):
;   .github\scripts\build-installer.ps1 -Version 1.0.0
; Output: installer\Output\QweNumpad-Setup-<version>.exe

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif
; File-version resources only take dotted numbers, so "1.2.0-beta.1" becomes "1.2.0".
#ifndef AppNumericVersion
  #define AppNumericVersion "0.0.0"
#endif

#define AppName      "QweNumpad"
#define AppExe       "QweNumpad.exe"
#define AppPublisher "Murat Dursun"
#define AppUrl       "https://github.com/Muratmd/QweNumpad"

[Setup]
; Fixed AppId: keeps upgrades and uninstall entries matched across versions. Never change it.
AppId={{6C1E9B47-5B2A-4E43-9C0B-7A8E2F51D3A4}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppNumericVersion}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
LicenseFile=..\LICENSE
OutputDir=Output
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Close a running copy before overwriting it (the keyboard hook holds the exe open).
CloseApplications=force
RestartApplications=no

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
turkish.StartupTask=Windows açılışında otomatik başlat
english.StartupTask=Start automatically when I sign in to Windows
turkish.DesktopTask=Masaüstü kısayolu oluştur
english.DesktopTask=Create a desktop shortcut
turkish.LaunchNow=QweNumpad'i şimdi başlat
english.LaunchNow=Launch QweNumpad now

[Tasks]
Name: "startup"; Description: "{cm:StartupTask}"
Name: "desktopicon"; Description: "{cm:DesktopTask}"; Flags: unchecked

[Files]
Source: "..\publish\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Same value the app's own "start with Windows" setting writes (StartupRegistry.cs),
; so the checkbox in Settings and this task stay in sync.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; \
  ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchNow}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExe}"; Flags: runhidden; RunOnceId: "KillApp"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // The app's own Settings checkbox can write this value too, outside [Registry].
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#AppName}');

    // Learned field rules and settings live in %APPDATA%\QweNumpad. Ask instead of deleting
    // silently: a reinstall should be able to keep them.
    if DirExists(ExpandConstant('{userappdata}\{#AppName}')) then
      if SuppressibleMsgBox(
           'QweNumpad ayarları ve öğrenilmiş alan kuralları da silinsin mi?' + #13#10 +
           'Delete QweNumpad settings and learned field rules as well?',
           mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES then
        DelTree(ExpandConstant('{userappdata}\{#AppName}'), True, True, True);
  end;
end;
