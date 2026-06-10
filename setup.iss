[Setup]
AppId={{KARAVUL-MONITOR-001}}
AppName=Karavul
AppVersion=1.0.1
AppPublisher=Karavul
AppPublisherURL=https://github.com/ertugrulakdag/karavul
AppSupportURL=https://github.com/ertugrulakdag/karavul
AppUpdatesURL=https://github.com/ertugrulakdag/karavul
VersionInfoCompany=Karavul
VersionInfoDescription=Karavul Yerel Monitor Servisi
VersionInfoProductName=Karavul
VersionInfoProductVersion=1.0.1
VersionInfoCopyright=Copyright (C) 2026 Karavul
DefaultDirName={autopf64}\Karavul
DefaultGroupName=Karavul
OutputBaseFilename=KaravulSetup_v1.0.1
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
DisableWelcomePage=no
DisableDirPage=no
DisableProgramGroupPage=yes
SetupIconFile=src\Karavul.Host\wwwroot\img\favicon.ico
UninstallDisplayIcon={app}\favicon.ico

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Dirs]
; ProgramData klasörlerini oluştur ve tüm kullanıcılara yazma yetkisi ver
Name: "{commonappdata}\Karavul"; Permissions: authusers-modify
Name: "{commonappdata}\Karavul\logs"; Permissions: authusers-modify

[Files]
; Publish edilen dosyaları hedef klasöre taşı
Source: "C:\Publish\Karavul\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "src\Karavul.Host\wwwroot\img\favicon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Masaüstü ve Başlat menüsü kısayolları - Direkt URL açar, icon olarak exeyi kullanır
Name: "{commondesktop}\Karavul"; Filename: "http://127.0.0.1:9060"; IconFilename: "{app}\favicon.ico"
Name: "{commonprograms}\Karavul"; Filename: "http://127.0.0.1:9060"; IconFilename: "{app}\favicon.ico"

[Run]
; Servisi oluştur ve başlat
Filename: "{sys}\sc.exe"; Parameters: "create KaravulService binPath= ""{app}\Karavul.exe"" start= auto DisplayName= ""Karavul Monitoring Service"""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "description KaravulService ""Karavul Yerel Monitor Servisi v1.0.1"""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "start KaravulService"; Flags: runhidden

[UninstallRun]
; Uninstall işlemi sırasında servisi durdur ve sil
Filename: "{sys}\sc.exe"; Parameters: "stop KaravulService"; Flags: runhidden; RunOnceId: "StopService"
Filename: "{sys}\sc.exe"; Parameters: "delete KaravulService"; Flags: runhidden; RunOnceId: "DeleteService"

[Code]
var
  TelegramPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  TelegramPage := CreateInputQueryPage(wpSelectDir,
    'Telegram Bildirimleri', 'Telegram üzerinden bildirim almak istiyor musunuz?',
    'Eğer Telegram üzerinden sistem bildirimleri almak istiyorsanız, lütfen Bot Token bilginizi aşağıya girin.'#13#10'İstemiyorsanız alanı boş bırakıp devam edebilirsiniz.');
  TelegramPage.Add('Bot Token:', False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  AppSettingsPath: string;
  FileLines: TArrayOfString;
  i: Integer;
  BotToken: string;
begin
  // Kurulum başlamadan hemen önce mevcut servis varsa durdurup siliyoruz ki dosyalar kilitli kalmasın
  if CurStep = ssInstall then
  begin
    Exec(ExpandConstant('{sys}\sc.exe'), 'stop KaravulService', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000); // Kapanması için kısa bir süre bekle
    Exec(ExpandConstant('{sys}\sc.exe'), 'delete KaravulService', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1000); // Silinmesi için bekle
  end;

  // Kurulum dosyaları kopyalandıktan sonra (appsettings.json hedefe taşındıktan sonra)
  if CurStep = ssPostInstall then
  begin
    BotToken := Trim(TelegramPage.Values[0]);
    if BotToken <> '' then
    begin
      AppSettingsPath := ExpandConstant('{app}\appsettings.Production.json');
      if LoadStringsFromFile(AppSettingsPath, FileLines) then
      begin
        for i := 0 to GetArrayLength(FileLines) - 1 do
        begin
          // BotToken satırını bul ve '***' değerini değiştir
          if (Pos('BotToken', FileLines[i]) > 0) and (Pos('***', FileLines[i]) > 0) then
          begin
            StringChangeEx(FileLines[i], '***', BotToken, True);
          end;
        end;
        // Değişiklikleri dosyaya kaydet
        SaveStringsToFile(AppSettingsPath, FileLines, False);
      end;
    end;
  end;
end;
