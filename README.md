# Karavul
<img width="1914" height="957" alt="image" src="https://github.com/user-attachments/assets/93565c7e-e239-419e-a4e8-ff84fdd2a602" />

**TR:** Karavul, web sitelerini, API endpointlerini ve servisleri izlemek (uptime & performance monitoring) için tasarlanmış, local olarak çalışan bir Windows Service uygulamasıdır.  
**EN:** Karavul is a locally running Windows Service application designed for uptime and performance monitoring of websites, API endpoints, and services.

### 📥 En Son Sürümü İndirin / Download the Latest Release
- **[Sürüm Notları / Release Notes](https://github.com/ertugrulakdag/Karavul/releases/tag/v1.0.3)**

📦 Installation / Kurulum

- **TR:** Linkteki `KaravulSetup_v1.0.3.exe` dosyasını indirin ve çalıştırın. Kurulum tamamlandıktan sonra yönetim paneline `http://127.0.0.1:9060` adresinden erişebilirsiniz.
*(Varsayılan Giriş Bilgileri -> Kullanıcı Adı: Admin | Şifre: admin)*
- **EN:** Download and run the `KaravulSetup_v1.0.3.exe` file below. Once installed, you can access the management panel at `http://127.0.0.1:9060`.
*(Default Login Credentials -> Username: Admin | Password: admin)*
---

## Özellikler / Features

**TR:**
- **HTTP/HTTPS İzleme:** Beklenen durum kodu (status code), yanıt süresi limitleri ve timeout denetimleri.
- **SSL Sertifika Denetimi:** HTTPS adreslerinin SSL sertifika geçerlilik ve bitiş tarihlerini kontrol etme.
- **Olay (Incident) Yönetimi:** Uygulama kesintilerinde otomatik olay kaydı açma ve kapanma sürelerini hesaplama. Manuel olarak kapatılabilme özelliği.
- **Bildirimler:** Olay anında İletişim Gruplarına (Contact Group) e-posta, SMS (Mock) ve Telegram üzerinden bildirim gönderme. Tekrar eden alarm (repeat alert) mekanizması.

**EN:**
- **HTTP/HTTPS Monitoring:** Expected status code, response time limits, and timeout checks.
- **SSL Certificate Validation:** Checking SSL certificate validity and expiration dates for HTTPS endpoints.
- **Incident Management:** Automatic incident creation during downtime, calculation of downtime durations, and manual resolve capabilities.
- **Notifications:** Sending notifications via Email, SMS (Mock), and Telegram to Contact Groups during incidents. Includes a repeat alert mechanism.

---

## Teknolojiler / Technologies

**TR:**
- **Backend:** .NET 10 Worker Service & ASP.NET Core
- **Database:** SQLite (Dapper kullanarak)
- **UI:** ASP.NET Core Razor Pages (Vanilla CSS & JS ile modern dark theme)
- **Dağıtım:** Inno Setup ile Windows Service olarak kurulum

**EN:**
- **Backend:** .NET 10 Worker Service & ASP.NET Core
- **Database:** SQLite (using Dapper)
- **UI:** ASP.NET Core Razor Pages (modern dark theme using Vanilla CSS & JS)
- **Deployment:** Windows Service installation via Inno Setup

---

## Local'de Çalıştırma / Running Locally (Geliştirme Modu / Dev Mode)

**TR:** Projeyi local bilgisayarınızda geliştirmek veya terminal üzerinden test etmek için aşağıdaki komutu kullanabilirsiniz:
**EN:** To develop the project on your local machine or test it directly via the terminal, use the following command:

```bash
dotnet run --project src\Karavul.Host\Karavul.Host.csproj
```

**TR:** Uygulama çalıştıktan sonra tarayıcınızdan **[http://127.0.0.1:9066](http://127.0.0.1:9066)** adresine giderek yönetim arayüzüne ulaşabilirsiniz.
**EN:** Once the application is running, open your browser and navigate to **[http://127.0.0.1:9066](http://127.0.0.1:9066)** to access the management interface.

**TR: Varsayılan Giriş Bilgileri:**
**EN: Default Login Credentials:**
- **Kullanıcı Adı / Username:** `Admin`
- **Şifre / Password:** `admin`
*(TR: Güvenlik sebebiyle ilk girişte şifre değiştirmeniz istenecektir.)*
*(EN: For security reasons, you will be prompted to change your password upon first login.)*

---

## Kurulum ve Dağıtım / Setup and Deployment

**TR:** Uygulamayı Windows Servis olarak kurmanın iki yolu vardır. En kolay yol sayfanın üst kısmındaki linkten hazır `.exe` dosyasını indirip kurmaktır. Alternatif olarak projeyi kendiniz derleyip kurulum dosyası oluşturabilirsiniz.
**EN:** There are two ways to install the application as a Windows Service. The easiest way is to download and run the ready-to-use `.exe` from the link at the top of the page. Alternatively, you can build the project and generate the installer yourself.

### Yöntem 1: Hazır Kurulum Dosyası / Method 1: Ready Installer

**TR:** Sayfanın en üstündeki bağlantıya ([Download KaravulSetup_v1.0.3.exe](#-en-son-sürümü-indirin--download-the-latest-release)) tıklayarak indireceğiniz dosyayı çalıştırın ve ekrandaki adımları izleyin.
**EN:** Click the link at the top of the page ([Download KaravulSetup_v1.0.3.exe](#-en-son-sürümü-indirin--download-the-latest-release)), run the downloaded file, and follow the on-screen steps.

### Yöntem 2: Kaynak Koddan Kurulum Dosyası Oluşturmak / Method 2: Generating Installer from Source Code (Inno Setup)

#### Projeyi Publish Etmek / Publishing the Project

**TR:** Proje kök dizininde (`C:\GIT\Karavul`) terminali açın ve aşağıdaki komutu çalıştırın:
**EN:** Open a terminal in the project root directory (`C:\GIT\Karavul`) and run the following command:

```bash
dotnet publish src\Karavul.Host\Karavul.Host.csproj -c Release -r win-x64 --self-contained false -o src\Karavul.Host\bin\Release\net10.0-windows\win-x64\publish
```

#### Setup Dosyasını Oluşturmak / Creating the Installer

**TR:**
1. Bilgisayarınızda [Inno Setup Compiler](https://jrsoftware.org/isdl.php) yüklü olmalıdır.
2. Proje klasöründeki `setup.iss` dosyasını Inno Setup ile açın.
3. Üst menüden **Build -> Compile** (veya `Ctrl+F9`) seçeneğine tıklayın.
4. Tamamlandığında proje dizini altındaki `Output` klasöründe kurulum dosyanız hazır olacaktır.

**EN:**
1. Ensure [Inno Setup Compiler](https://jrsoftware.org/isdl.php) is installed on your computer.
2. Open the `setup.iss` file located in the project folder with Inno Setup.
3. Click **Build -> Compile** from the top menu (or press `Ctrl+F9`).
4. Once completed, your setup file will be ready in the `Output` folder within the project directory.

#### Kurulum Sonrası Klasör Yapısı / Post-Installation Directory Structure
- **Uygulama Dosyaları / App Files:** `C:\Program Files\Karavul\`
- **Veritabanı ve Loglar / DB & Logs:** `C:\ProgramData\Karavul\`
- **Web Arayüz Portu / Web UI Port:** `9066` (TR: Localhost'a kısıtlıdır / EN: Restricted to Localhost)

---

## Telegram Bildirim Ayarları / Telegram Notification Settings

**TR:** Karavul üzerinden Telegram bildirimleri alabilmek için `appsettings.json` dosyasındaki `Telegram` ayarlarını yapılandırmanız gerekmektedir:
**EN:** To receive Telegram notifications via Karavul, you must configure the `Telegram` settings in your `appsettings.json` file:

```json
"Telegram": {
  "BotToken": "YOUR_BOT_TOKEN",
  "ChannelName": "Kanal Adı (Örn: Karavul)"
}
```
<img width="1356" height="978" alt="image" src="https://github.com/user-attachments/assets/a785dd88-0fc5-46d8-8e61-4acf2c4c3ab2" />

### Bilgileri Nasıl Alırsınız? / How to Get the Information?

1. **BotToken Alma / Getting the BotToken:**
   - **TR:** Telegram'da arama çubuğuna **@BotFather** yazarak resmi bota gidin. `/newbot` komutunu göndererek yeni bir bot oluşturun. BotFather size uzun bir **HTTP API Token** (BotToken) verecektir.
   - **EN:** Search for **@BotFather** in Telegram and go to the official bot. Send the `/newbot` command to create a new bot. BotFather will provide you with a long **HTTP API Token** (BotToken).

2. **ChannelName:**
   - **TR:** Karavul arayüzünde "Bildirim Geçmişi" tablosunda görünecek temsili isimdir. Görsel amaçlıdır.
   - **EN:** This is the display name that will appear in the "Notification Logs" table in the Karavul UI. It is purely for visual purposes.

---

## Çoklu Dil Desteği / Multi-Language Support (i18n)

**TR:** 
Karavul, hem kullanıcı arayüzü (UI) hem de arka plan bildirimleri için **Türkçe (tr)** ve **İngilizce (en)** dil desteğine sahiptir.
- **Web Arayüzü:** Login ekranında veya üst menüdeki dil değiştirme butonu ile dil anlık olarak değiştirilebilir. Seçiminiz tarayıcı çerezlerinde saklanır.
- **Bildirimler:** Arka planda gönderilen Telegram vb. bildirimlerin dili, `appsettings.json` içerisindeki `Language` parametresi ile belirlenir (`tr` veya `en`).

**EN:** 
Karavul features **Turkish (tr)** and **English (en)** language support for both the user interface (UI) and background notifications.
- **Web UI:** You can instantly switch the language using the language toggle button on the login screen or top menu. Your selection is stored in browser cookies.
- **Notifications:** The language for background notifications (like Telegram) is determined by the `Language` parameter in `appsettings.json` (`tr` or `en`).

---

## Yapılandırma / Configuration (appsettings.json)

**TR:** `appsettings.Production.json` (veya `appsettings.Development.json`) dosyası içindeki `Karavul` düğümü altındaki tüm ayarların açıklamaları:
**EN:** Explanations for all settings under the `Karavul` node in the `appsettings.Production.json` (or `appsettings.Development.json`) file:

- **`WebPort`**: (int) Web yönetim arayüzünün çalışacağı HTTP portudur. / The HTTP port for the web management interface. (Default: 9066)
- **`DatabasePath`**: (string) SQLite veritabanı dosyasının tam fiziksel yolu. / Full physical path to the SQLite database file.
- **`LogPath`**: (string) Log dosyalarının kaydedileceği klasör dizini. / Directory path where log files will be saved.
- **`RetentionDays`**: (int) Olayların ve bildirim geçmişinin veritabanında kaç gün saklanacağını belirler. / Number of days to keep incidents and notification history before deletion.
- **`CheckWorkerIntervalMs`**: (int) Aktif monitörlerin kaç milisaniyede bir sıraya alınıp kontrol edileceği. / How often (in milliseconds) active monitors are queued and checked.
- **`CleanupIntervalHours`**: (int) Eski verileri silen temizlik görevinin kaç saatte bir çalışacağı. / How often (in hours) the database cleanup worker runs to delete old data.
- **`Language`**: (string) Arka plan bildirimleri (Telegram vb.) için kullanılacak sistem dili. `tr` veya `en` alabilir. / The system language used for background notifications. Accepts `tr` or `en`.

