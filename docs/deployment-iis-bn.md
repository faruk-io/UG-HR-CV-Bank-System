# Union Group CV Bank — লোকাল VM সার্ভারে IIS-এ হোস্ট করার নির্দেশনা

তারিখ: 2026-11-04 · অ্যাপ: ASP.NET Core 9 API + Angular 22 স্ট্যাটিক ক্লায়েন্ট + SQL Server `CvBank` ডেটাবেজ

এই ডকুমেন্টে যেসব সংখ্যা ও পাথ লেখা আছে, সেগুলো আজ এই ডেভ মেশিনে `dotnet publish -c Release` এবং `npx ng build` চালিয়ে যাচাই করা। তবু VM-এর নাম, IP, SQL ইনস্ট্যান্সের নাম আপনার জানা মান বসিয়ে তারপর কমান্ড চালান।

---

## ০. আর্কিটেকচার এক নজরে

| অংশ | কী | কোথায় চলবে |
|------|-----|-------------|
| Frontend | Angular এর স্ট্যাটিক ফাইল (HTML/JS/CSS) | IIS Website "CvBankWeb" |
| Backend API | `CvBank.Api.dll` (.NET 9, IIS In-Process) | IIS Website "CvBankApi" |
| Database | SQL Server ডেটাবেজ `CvBank` | VM-এর আগে থেকেই থাকা SQL ইনস্ট্যান্স (SmartPOS-এর ডেটাবেজ আলাদা — ছোয়া যাবে না) |
| আপলোড ফাইল | CV / evaluation স্ক্যান | ফোল্ডার `C:\CvBank\Documents` |
| ক্লায়েন্ট | HR-এর PC / ল্যাপটপ | ব্রাউজারে `http://<VM>:4400` খুলবে |

**যা ইনস্টল করা লাগবে না:** VM-এ Node.js লাগবে না (ফ্রন্টএন্ড বিল্ড এই ডেভ মেশিনেই হবে, শুধু ফলাফল কপি করবেন), `dotnet run`/`ng serve` কোনোদিনই চলবে না — IIS সব করবে।

---

## ১. VM-এ যা ইনস্টল করতে হবে (একবার)

1. **IIS (Web Server role)** — Server Manager → Add roles and features → Web Server (IIS)। এর ভেতর চালু রাখুন:
   - Web Server → Common HTTP Features → *Static Content*, *Default Document*
   - Performance → *Static Content Compression*
   - IIS Management Console
2. **.NET 9 ASP.NET Core Hosting Bundle** — এটিই IIS-কে `CvBank.Api.dll` চলতে দেয় (ANCMv2 মডিউল)।
   ডাউনলোড পেজ: https://dotnet.microsoft.com/en-us/download/dotnet/aspnet-core-9.0 → "Hosting Bundle" (ASP.NET Core Runtime 9.x)
   ইনস্টলের পর অবশ্যই রান: `iisreset`
3. **URL Rewrite module** (ফ্রন্টএন্ডের SPA রুট আর, অপশন A বেছে নিলে, reverse proxy-র জন্য)
   ডাউনলোড: https://www.iis.net/downloads/microsoft/url-rewrite (x64)
4. **SQL Server** — আগে থেকেই থাকলে নতুন কিছু না। শুধু `CvBank` ডেটাবেজের জন্য লগিন/পারমিশন (ধাপ ৩) দিতে হবে।

যাচাই (PowerShell, Administrator):

```powershell
Get-WebConfiguration -Filter "system.webServer/globalModules/add[@name='AspNetCoreModuleV2']"
```
কিছু না ফিরলে Hosting Bundle ইনস্টল হয়নি বা `iisreset` হয়নি।

---

## ২. ফোল্ডার ও পোর্ট প্ল্যান

### ফোল্ডার

| পাথ | কী | কে লিখতে পারবে |
|------|-----|------------------|
| `C:\CvBank\Api` | ব্যাকএন্ড publish আউটপুট (৬১টি ফাইল) | শুধু Administrator |
| `C:\CvBank\Api\appsettings.Production.json` | VM-এর কনফিগ (কানেকশন, CORS, JWT কী) | Administrator + অ্যাপ পুল |
| `C:\CvBank\Web` | ফ্রন্টএন্ড `dist\cvbank-web\browser` এর ভেতরের ফাইল | শুধু Administrator |
| `C:\CvBank\Documents` | আপলোড হওয়া CV/evaluation ফাইল | **অ্যাপ পুল Modify** (না হলে আপলোড ৫০০ এরর) |
| `C:\CvBank\Backups` | ডেটাবেজ + Documents ব্যাকআপ (Step 8d) | Administrator |

> **গুরুতর:** ডিফল্ট `Storage:DocumentRootPath` মান `App_Data/documents`, যা `C:\CvBank\Api\` এর ভেতর পড়ে। পরের আপডেটের সময় ওই ফোল্ডার মোছা/ওভাররাইট হলে **আপলোড করা CV ফাইল হারিয়ে যাবে**। তাই ধাপ ৫-এ এটাকে `C:\CvBank\Documents` (আলাদা, কখনো মোছা হয় না) করে দিচ্ছি।

### পোর্ট

| সার্ভিস | পোর্ট | কোন ঠিকানা থেকে |
|---------|-------|------------------|
| CvBankWeb (ফ্রন্টএন্ড) | **4400** | সব PC: `http://<VM-NAME>:4400` |
| CvBankApi (ব্যাকএন্ড) | **5140** | ব্রাউজার থেকে: `http://<VM-NAME>:5140` |
| SQL Server | 1433 (ডিফল্ট ইনস্ট্যান্স) | শুধু VM/অ্যাপ থেকে |
| SmartPOS (আলাদা অ্যাপ) | 4300 | **ছোয়া যাবে না** |

`<VM-NAME>` = VM-এর কম্পিউটার নাম (CMD-এ `hostname`) অথবা স্থির IP, যেমন `http://192.168.10.20:4400`। পোর্ট 4400/5140 বাধ্যতামূলক না — খালি যেকোনো পোর্ট দিন, তবে তখন ধাপ ৫ ও ৭-এর মানও বদলাতে হবে।

> **সতর্কতা — এই গাইডের 5140 শুধু উদাহরণ।** আপনার IIS-এ API সাইটটা আসলে যে পোর্টে বাঁধা (যেমন **5041**), সেই নম্বরটাই সব জায়গায় বসাতে হবে: `environment.prod.ts`, ফায়ারওয়াল রুল, চেকলিস্ট। ভুল পোর্ট ধরে পরীক্ষা করলে মনে হবে "সার্ভার বন্ধ", অথচ আসল সাইট অন্য পোর্টে চলছে।
>
> আসল পোর্ট বের করার উপায় — VM-এ IIS Manager → সাইট → **Bindings** → Port নম্বর; অথবা CMD-এ:
> ```bat
> %windir%\system32\inetsrv\appcmd list sites
> ```
> আউটপুটে `bindings:http/*:5041:` এভাবে পোর্টটা দেখা যায়।

---

## ৩. ডেটাবেজ তৈরি + অ্যাপ পুলকে পারমিশন

অ্যাপ প্রথমবার চললেই EF Core মাইগ্রেশন চালিয়ে টেবিল বানিয়ে নেয় (`DbInitializer` → `MigrateAsync`), তারপর Role/Permission/Settings আর একটি `admin` ইউজার সিড করে। তাই টেবিল নিজে থেকে বানানোর দরকার নেই — শুধু খালি ডেটাবেজ + লগিন লাগবে।

> **খালি ডেটাবেজটা আগে নিজে বানাতেই হবে।** না থাকলে `MigrateAsync` নিজে থেকে `CREATE DATABASE [CvBank]` চালানোর চেষ্টা করে, আর app pool আইডির সাধারণত সেই ক্ষমতা থাকে না। ফল: `SqlException … CREATE DATABASE permission denied in database 'master'` (Error **262**) → অ্যাপ শুরু হওয়ার আগেই ক্র্যাশ → IIS-এ **500.30**।
>
> আগে দেখুন ডেটাবেজটা আদৌ আছে কি না (SSMS-এ admin হিসেবে):
> ```sql
> SELECT name, create_date FROM sys.databases ORDER BY name;
> ```
> তালিকায় `CvBank` না থাকলে নিচের `CREATE DATABASE` চালান। পুরনো ডেটা restore করলে এই ধাপ লাগে না।

> **`CvBankApi` শুধু উদাহরণ পুলের নাম।** SQL লগিনটা হবে `IIS APPPOOL\` + আপনার API সাইট **আসলে যে পুল ব্যবহার করে** তার নাম। নামটা বের করুন:
> ```powershell
> Import-Module WebAdministration
> Get-ChildItem IIS:\Sites    | Select-Object Name, PhysicalPath, @{n='Pool';e={$_.applicationPool}}
> Get-ChildItem IIS:\AppPools | Select-Object Name, @{n='Identity';e={$_.processModel.identityType}}
> ```
> উদাহরণ নামটা হুবহু চালিয়ে দিলে SQL দেবে **`Msg 15401 … Windows NT user or group 'IIS APPPOOL\CvBankApi' not found`** — ওটা কোনো ক্ষতি করে না, শুধু কিছুই তৈরি হয় না। Identity `ApplicationPoolIdentity` হলেই লগিনের নাম `IIS APPPOOL\<পুল>`; `LocalSystem`/`NetworkService`/কাস্টম ইউজার হলে সেই অ্যাকাউন্টের নাম বসবে। একই পুল API আর ওয়েব দুই সাইটেই লাগানো থাকতে পারে — তখন লগিনটা একটাই যথেষ্ট।

### অপশন ২.১ (সুপারিশ: Windows লগিন, পাসওয়ার্ড ফাইলে থাকে না)

SQL Server Management Studio → Security → Logins → New Login:
- Login name: **`IIS APPPOOL\CvBankApi`** (Windows authentication, VM-এর নিজের মেশিন)
- User Mapping: ডেটাবেজ **CvBank** → db_owner
- Settings → Server Roles: `public` যথেষ্ট

তারপর খালি ডেটাবেজ (Query window):

```sql
IF DB_ID('CvBank') IS NULL
    CREATE DATABASE CvBank;
```

কানেকশন স্ট্রিং (ধাপ ৫-এ বসবে):
```
Server=localhost;Database=CvBank;Trusted_Connection=True;TrustServerCertificate=True;
```

### অপশন ২.২ (SQL লগিন)

```sql
CREATE LOGIN CvBankApp WITH PASSWORD = '<strong password>';
CREATE USER CvBankApp FOR LOGIN CvBankApp;
ALTER ROLE db_owner ADD MEMBER CvBankApp;
```
কানেকশন: `Server=localhost;Database=CvBank;User Id=CvBankApp;Password=<strong password>;TrustServerCertificate=True;`

> **সতর্কতা:** পাসওয়ার্ড শুধু `appsettings.Production.json`-এ রাখবেন, কোড/রিপো/চ্যাটে নয়। এই ফাইলটা NTFS-এ Administrators + অ্যাপ পুল-এর জন্য সীমাবদ্ধ করুন (ধাপ ৬)।

### পুরনো ডেটা VM-এ নিতে চাইলে

ডেভ মেশিনে: `BACKUP DATABASE CvBank TO DISK='C:\temp\CvBank.bak' WITH COPY_ONLY;`
VM-এ: `RESTORE DATABASE CvBank FROM DISK='C:\temp\CvBank-2026-11-XX.bak' WITH REPLACE;`
(ফাইল + ডেটাবেজ দুটোই নিতে হবে, নাহলে রেকর্ড থাকবে কিন্তু CV ফাইল ভাঙা লিংক। ব্যাকআপের পূর্ণ প্রক্রিয়া Step 8d-এ আলাদা ধাপে ঠিক করব।)

---

## ৪. ব্যাকএন্ড publish করে VM-এ কপি

**ডেভ মেশিনে (এখানে):**

```bat
cd C:\Users\Araf\Documents\Qoder\2026-11-02\cb0c7e3b
dotnet publish backend\CvBank.Api\CvBank.Api.csproj -c Release -o C:\CvBank\Api
```

> ফোল্ডারের নাম নয়, **পুরো `.csproj` পাথ** দিতে হবে। `backend\CvBank.Api` ফোল্ডারে `CvBank.slnx`-ও আছে, তাই শুধু ফোল্ডার দিলে MSBuild দ্বিধায় পড়ে `MSB1050` এরর দেয়।

আউটপুট যাচাই (আজকের রান): ৬১টি ফাইল, `CvBank.Api.dll`, `CvBank.Api.exe`, `CvBank.Api.runtimeconfig.json`, `appsettings.json`, এবং `web.config` —

```xml
<aspNetCore processPath="dotnet" arguments=".\CvBank.Api.dll"
            stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess" />
```

`web.config` এই ফাইলটাই IIS-কে বলে "সাইটটা .NET Core অ্যাপ"। এটি publish-এ তৈরি হয়, নিজে লেখার দরকার নেই।

তারপর `C:\CvBank\Api` ফোল্ডারটা VM-এর `C:\CvBank\Api`-এ কপি করুন (নেটওয়ার্ক শেয়ার/RDP ড্রাইভ ম্যাপিং — যা আপনার কাছে সহজ)।

---

## ৫. VM-এর কনফিগ: `C:\CvBank\Api\appsettings.Production.json`

ফাইলটা না থাকলে নতুন বানান, থাকলে মান মিলিয়ে নিন:

> **ফাইলটা শুধু VM-এ বানাবেন — প্রজেক্টের ভেতরে নয়।** জায়গা: `C:\CvBank\Api\appsettings.Production.json`, ঠিক `CvBank.Api.dll`-এর পাশে। কারণ .NET Web SDK `appsettings.*.json`-কে `CopyToPublishDirectory` হিসেবে চিহ্নিত করে — প্রজেক্টে ফাইলটা রাখলে পরের `dotnet publish`-এ সেটা আউটপুটে চলে আসবে এবং VM-এ কপি করার সময় আপনার আসল কী-সহ ফাইলটা ওভাররাইট হয়ে যাবে।

> **`C:\CvBank\Api` এই গাইডের উদাহরণ পাথ মাত্র।** ফাইলটা বানাবেন আপনার API সাইটের **আসল physical path**-এ — IIS Manager → সাইট → Basic Settings → Physical path, অথবা `appcmd list sites`। যেমন কারও কারও VM-ে এটা `C:\inetpub\wwwroot\UGCVBank\Backend`। একই IIS-ে অন্য সাইট (যেমন Payroll/SmartPOS) চললে সেগুলোর ফোল্ডার/পুল ছোঁবেন না।

> **কোণা-বন্ধনী `<...>`-এর প্রতিটা placeholder বদলাতেই হবে।** টেমপ্লেট হুবহু কপি করে রাখলে দুটো ফাঁদ:
> - `Server=<VM-এর সঠিক SQL ইনস্ট্যান্স>` কোনো সার্ভারের নাম নয় — অ্যাপ কানেক্টই করতে পারবে না। আসল নাম বসান, যেমন `Server=WIN-FJ7MU6TI1H9\\SQLEXPRESS` (JSON-এ ব্যাকস্লাশ দুবার)। এক মেশিনে ডিফল্ট (`MSSQLSERVER`) ও named (`MSSQL$SQLEXPRESS`) দুই ইনস্ট্যান্স থাকলে `Server=.` আর `Server=...\SQLEXPRESS` **আলাদা দুটো সার্ভার** — ডেটাবেজ একটায় থাকলে অন্যটায় পাওয়া যায় না।
> - `SigningKey` যাচাই হয় **UTF-8 বাইট** গুনে (`Encoding.UTF8.GetByteCount >= 32`), অক্ষর গুনে নয়। তাই ছোট একটা বাংলা placeholder-ও ৩২ বাইট পেরিয়ে যেতে পারে এবং কোড সেটাকে আসল কী ধরে নেয় — ফলে টোকেন সই হয় এমন একটা মান দিয়ে যা সবাই দেখে ফেলেছে। কী অবশ্যই নিজে তৈরি করা random মান হতে হবে।
>
> **`Trusted_Connection=True`-এর সাথে `User Id=…;Password=…` মেশাবেন না** — পরস্পরবিরোধী, Windows auth জিতে যায় আর SQL লগিনটা উপেক্ষিত হয়। অথচ পাসওয়ার্ডটা খোলা টেক্সটে ফাইলে পড়ে থাকে। যেকোনো একটা পদ্ধতি রাখুন।

```json
{
  "ConnectionStrings": {
    "CvBank": "Server=localhost;Database=CvBank;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Cors": {
    "AllowedOrigin": "http://<VM-NAME>:4400,http://<VM-IP>:4400,http://localhost:4400"
  },
  "Storage": {
    "DocumentRootPath": "C:\\CvBank\\Documents",
    "MaxUploadBytes": 10485760
  },
  "Jwt": {
    "Issuer": "CvBank.Api",
    "Audience": "CvBank.Web",
    "ExpireMinutes": 480,
    "SigningKey": "<32+ character secret>"
  },
  "Seed": {
    "AdminPassword": "<প্রথমবারের জন্য>"
  }
}
```

প্রতিটি কী মানে:

| কী | কী করে | ভুল হলে কী হবে |
|----|--------|-----------------|
| `ConnectionStrings:CvBank` | কোন SQL ইনস্ট্যান্স/ডেটাবেজ | Login failed for `IIS APPPOOL\CvBankApi` / টেবিল না পাওয়া |
| `Cors:AllowedOrigin` | কোন ঠিকানা থেকে ব্রাউজার API ডাকতে পারবে। **কমা-আলাদা তালিকা** দিন — মেশিনের নাম, IP আর `localhost` তিনটাই রাখুন, খালি-স্পেস/শেষে `/` না দিয়ে | লগিন স্ক্রিনে "Network error"/CORS এরর |
| `Storage:DocumentRootPath` | CV/evaluation ফাইল কোথায় জমা | আপলোড ৫০০, বা (relative রাখলে) আপডেটে ফাইল হারানো |
| `Jwt:SigningKey` | লগিন টোকেন সই করা | অ্যাপ শুরুতেই `Jwt:SigningKey is missing or shorter than 32 characters` |
| `Jwt:ExpireMinutes` | ৪৮০ মিনিট = ৮ ঘণ্টা পর লগিন আবার করতে হবে | — |
| `Seed:AdminPassword` | নতুন ডেটাবেজে `admin`-এর প্রথম পাসওয়ার্ড | না দিলে ডিফল্ট `Admin@123` বসে যায় এবং লগে সতর্কতা থাকে |

**`Jwt:SigningKey` এবং পাসওয়ার্ড আপনার নিজের লেখা জিনিস** — আমি শুধু নাম ও জায়গা বলছি, মান আমি লিখব বা কোথাও রাখব না। কী তৈরির কমান্ড (VM-এ PowerShell):

```powershell
-join ((48..57)+(65..90)+(97..122) | Get-Random -Count 48 | ForEach-Object {[char]$_})
```
ফলাফলটা `SigningKey`-এ বসান (কমপক্ষে ৩২ অক্ষর)।

> **মনে রাখবেন:** `SigningKey` বদলালে + অ্যাপ রিস্টার্ট হলে **আগে ইস্যু হওয়া সব লগিন টোকেন বাতিল** — সব ইউজারকে একবার আবার লগিন করতে হবে। এটি নিরাপত্তার জন্য ভালো, কিন্তু টানা কাজের মাঝখানে করবেন না।

**`Seed:AdminPassword`** শুধু প্রথমবার দরকার (ডেটাবেজ খালি থাকলে)। সিড হয়ে গেলে লগিন করার পর User Management থেকে পাসওয়ার্ড বদলান, তারপর এই লাইনটা ফাইল থেকে সরিয়ে দিন।

---

## ৬. IIS-এ ব্যাকএন্ড সাইট (CvBankApi)

IIS Manager (Start → `inetmgr`):

1. **Application Pools → Add Application Pool**
   - Name: `CvBankApi`
   - .NET CLR Version: **No Managed Code** ← এটাই সবচেয়ে বেশি ভুল হয়; In-Process ANCM নিজেকে নিজে ম্যানেজ করে
   - Managed pipeline mode: Integrated
   - Identity: ApplicationPoolIdentity (ডিফল্ট)
2. **Sites → Add Website**
   - Site name: `CvBankApi`
   - Application pool: `CvBankApi` (উপরে বানানোটা বেছে নিন; Default App Pool **ব্যবহার করবেন না**)
   - Physical path: `C:\CvBank\Api`
   - Binding: Type `http`, IP `All Unassigned`, Port `5140`, Host name খালি
3. **Folder permission (Command Prompt, Administrator):**

```bat
icacls "C:\CvBank\Documents" /grant "IIS APPPOOL\CvBankApi:(OI)(CI)M" /T
icacls "C:\CvBank\Api\logs" /grant "IIS APPPOOL\CvBankApi:(OI)(CI)M" /T
```
(`C:\CvBank\Documents` এবং `C:\CvBank\Api\logs` ফোল্ডার আগেই বানিয়ে রাখুন। `logs` শুধু stdout লগ চালু করলে দরকার।)

4. **appsettings.Production.json সুরক্ষা:**

```bat
icacls "C:\CvBank\Api\appsettings.Production.json" /inheritance:r ^
  /grant "BUILTIN\Administrators:F" /grant "IIS APPPOOL\CvBankApi:R"
```

5. সাইট বেছে **Browse :5140 → http** চাপুন → `http://localhost:5140/api/health` → `{"status":"ok"}` ধরনের উত্তর আসলে ব্যাকএন্ড চলছে।

---

## ৭. ফ্রন্টএন্ড বিল্ড + IIS স্ট্যাটিক সাইট (CvBankWeb)

**প্রথমে একটাই লাইন বদলান — `frontend\src\environments\environment.prod.ts`:**

```ts
export const environment = {
  apiBaseUrl: 'http://ug-vm:5140'   // <- VM-এর নাম বা স্থির IP বসান
};
```

`environment.ts` (ডেভের জন্য) `localhost:5140`-এ থাকে, তাই এই মেশিনে `ng serve` আগের মতোই চলবে। `npx ng build` ডিফল্টভাবে production কনফিগারেশন ব্যবহার করে, আর সেই সময় `environment.ts`-এর জায়গায় `environment.prod.ts` বসে যায় (`angular.json`-এর `fileReplacements`)। ঠিকানা বদলালে শুধু এই এক লাইন এডিট + আবার বিল্ড।

**ডেভ মেশিনে:**

```bat
cd C:\Users\Araf\Documents\Qoder\2026-11-02\cb0c7e3b\frontend
npx ng build
```
আউটপুট: `frontend\dist\cvbank-web\browser` (আজকের রান ≈ ২.৩ MB; initial bundle ৫৯৪ kB — বিল্ডে ২টি *warning* আসে, error না, ফাইল তৈরি হয়)।

> **production build-এর warning ২টি স্বাভাবিক:** (ক) initial bundle ৫০০ kB budget ছাড়িয়েছে, (খ) pdfmake CommonJS মডিউল। কোনোটিই সাইট বন্ধ করে না।

`browser` ফোল্ডারের **ভেতরের সব** VM-এ `C:\CvBank\Web`-এ কপি করুন (`index.html`, `main-*.js`, `chunk-*.js`, `styles-*.css`, `favicon.svg`, `images\icons.svg` ইত্যাদি)।

**IIS Manager:**

1. Application Pool `CvBankWeb` — No Managed Code, Integrated, ApplicationPoolIdentity।
2. Add Website: name `CvBankWeb`, pool `CvBankWeb`, physical path `C:\CvBank\Web`, binding http port **4400**।
3. Default Document-এ `index.html` আছে কিনা দেখুন (IIS-এ ডিফল্টভাবে থাকে)।

**`C:\CvBank\Web\web.config` বানান** (SPA রুটের জন্য — না বানালে `http://<VM>:4400/candidates/1` সরাসরি খুললে 404 দেবে, আর রিফ্রেশে অ্যাপ ভাঙবে):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="SPA fallback" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
  <location path="index.html" inheritInChildApplications="false">
    <system.webServer>
      <httpProtocol>
        <customHeaders>
          <add name="Cache-Control" value="no-cache" />
        </customHeaders>
      </httpProtocol>
    </system.webServer>
  </location>
</configuration>
```

কারণ: `main-<hash>.js` ফাইলের নাম প্রতি বিল্ডে বদলায়, তাই সেগুলো দীর্ঘ ক্যাশে রাখা নিরাপদ; কিন্তু `index.html` ক্যাশে থাকলে পুরনো ভার্সন আটকে যায় — তাই ওটাতে `no-cache`।

---

## ৮. ফায়ারওয়াল (অন্য PC থেকে ঢুকতে)

CMD (Administrator):

```bat
netsh advfirewall firewall add rule name="CvBank Web 4400" dir=in action=allow protocol=TCP localport=4400
netsh advfirewall firewall add rule name="CvBank API 5140" dir=in action=allow protocol=TCP localport=5140
```

কোন PC থেকে দেখা যাচ্ছে না হলে VM-এ: `Test-NetConnection <VM-NAME> -Port 4400`।

**দুই পোর্টের নিয়ম আলাদা** — 4400 খোলা থাকলেও 5140 বন্ধ থাকতে পারে। তখন ওয়েবসাইট খোলে কিন্তু লগিনে "Failed to fetch" দেখায়। কোন অবস্থা কী বোঝায়:

| ক্লায়েন্ট PC থেকে `curl`/ব্রাউজার যা পায় | মানে | করণীয় |
|-------------------------------------------|------|--------|
| কোনো উত্তর নেই, ~১০–২০ সেকেন্ড পর **timeout** | ফায়ারওয়াল পোর্টটা চুপচাপ ফেলে দিচ্ছে (allow rule নেই) | উপরের `netsh` কমান্ডটা VM-এ চালান |
| সাথে সাথে **Connection refused** | পোর্টে কেউ শুনছেই না — IIS সাইট/binding নেই বা বন্ধ | ধাপ ৬-এর সাইট + binding দেখুন |
| **HTTP 500.30** — "ASP.NET Core app failed to start" | অ্যাপ শুরু হওয়ার সময় exception — সাধারণত ডেটাবেজ/migration ধাপ | ধাপ ১১-এর Event Viewer কমান্ড + ধাপ ৩ ও ৫ |
| **HTTP 502.5 / 500.31** | Hosting Bundle নেই বা .NET ভার্সন মিলছে না | ধাপ ১ + `iisreset` |
| **HTTP 200/401 কিন্তু ব্রাউজারে CORS** | পোর্ট ঠিক, শুধু ঠিকানা তালিকা মিলছে না | `Cors:AllowedOrigin` + `iisreset` |

**যেকোনো HTTP status নম্বর পাওয়া মানেই ফায়ারওয়াল খোলা** — তখন `netsh` রুল যোগ করার দরকার নেই, সমস্যা অ্যাপের ভেতরে।

VM-এ চালিয়ে দেখুন (PowerShell, Administrator):

```powershell
netstat -ano | findstr ":5140"                      # LISTENING দেখাতে হবে
curl.exe -i http://localhost:5140/api/health        # VM-এর ভেতর থেকে 200 আসে কি না
Get-NetFirewallRule -DisplayName "CvBank API 5140"  # rule আছে কি না
```

`netstat`-এ LISTENING আছে + VM-এর ভেতরে 200 আসে, কিন্তু অন্য PC timeout পায় — এটা নিশ্চিতভাবে ফায়ারওয়ালের কাজ, CORS-এর নয়।

---

## ৯. প্রথমবার চালু + যাচাই চেকলিস্ট

| # | টেস্ট | কোথা থেকে | আশা ফলাফল |
|---|--------|-----------|-------------|
| 1 | `http://<VM>:5140/api/health` | VM ব্রাউজার/PowerShell | HTTP 200 |
| 2 | `http://<VM>:4400` | VM ব্রাউজার | লগিন স্ক্রিন, শিরোনামে "Union Group CV Bank & Interview Evaluation" |
| 3 | লগিন `admin` / Seed পাসওয়ার্ড | VM | Dashboard খোলে, উপরে ডান পাশে ইউজারের নাম |
| 4 | `http://<VM>:4400` অন্য PC থেকে | ক্লায়েন্ট PC | লগিন স্ক্রিন আসে (না আসলে ফায়ারওয়াল/CORS) |
| 5 | ক্লায়েন্ট PC-তে লগিন | ক্লায়েন্ট | ব্যর্থ হলে `Cors:AllowedOrigin`-এ ঠিকানা মিলিয়ে দেখুন |
| 6 | CV Bank → Add Candidate → CV আপলোড | যেকোনো PC | `C:\CvBank\Documents`-এ ফাইল আসে |
| 7 | Evaluation → PDF Download / Print | যেকোনো PC | ফাইল/প্রিন্ট ঠিকঠাক (প্রিন্ট ক্লায়েন্টের ব্রাউজারেই হয়, সার্ভার লাগে না) |
| 8 | Reports → Print → Save as PDF | যেকোনো PC | Candidate/Interview রিপোর্ট landscape A4, উপরে "Union Group CV Bank" |
| 9 | Activity Log | admin | উপরের সব কাজ লগ হয়েছে |

---

## ১০. আপডেট করার নিয়ম (প্রতিবার)

1. VM-এ `C:\CvBank\Backups`-এ ডেটাবেজ + Documents ব্যাকআপ নিন (Step 8d)।
2. ডেভ মেশিনে `dotnet publish` এবং `npx ng build` চালান।
3. VM-এ `C:\CvBank\Api` ফোল্ডারে একটি খালি ফাইল রাখুন নাম `app_offline.htm` → অ্যাপ গ্রেসফুলি থামে, ফাইল লক ছাড়ে।
4. নতুন ফাইল কপি করুন। **`appsettings.Production.json`, `C:\CvBank\Documents` এবং ডেটাবেজ ওভাররাইট/মোছবেন না।** ফাইলটা যেহেতু শুধু VM-এ বানানো (ধাপ ৫), প্রজেক্টে নেই — তাই publish আউটপুটেও সেটা আসে না। কপি করার সময় সবসময় ওই তিনটা বাদ দিন; প্রজেক্টে ভুলে ফাইলটা বানালে publish সেটা বহন করবে এবং VM-এর আসল কী মুছে যাবে।
5. `app_offline.htm` মুছে দিন।
6. **অ্যাপ পুল একবার রিসাইকেল করুন — এটা বাধ্যতামূলক শেষ ধাপ।** শুধু ফাইল মোছার ওপর ভরসা করবেন না: কখনো কখনো ওয়ার্কার প্রসেস পুরনো "অফলাইন" অবস্থায় আটকে থাকে — ফাইল নেই, তবু 503 দিতে থাকে (2026-11-08-এ ঠিক এটাই ঘটেছিল; রিসাইকেলেই ঠিক হয়)। রিসাইকেল শুধু ওই পুলের অ্যাপগুলোকে কয়েক সেকেন্ড থামায়, একই IIS-এর অন্য সাইট ছোঁয় না।
7. ফ্রন্টএন্ড: ওয়েব ফোল্ডারের পুরনো `main-*.js`/`chunk-*.js` সরিয়ে নতুন কপি করুন (পুরনো হ্যাশ-ফাইল থেকে গেলেও ক্ষতি নেই, শুধু জায়গা নেয়)। ফোল্ডারের `web.config` (SPA fallback) মুছবেন না — নতুন বিল্ডে ওটা থাকে না, তাই ওভাররাইট কপিতে এমনিতেই থেকে যায়।
8. **যাচাই করে তবেই শেষ:** `api/health`-এ 200 আসছে কি না দেখুন, তারপর চেকলিস্টের ১–৩ নম্বর টেস্ট দু'মিনিটে চালান।

> **এই VM-এর আসল মান (2026-11-08-এ যাচাই করা):** পুল **`UGCVBank`**; API ফোল্ডার **`C:\inetpub\wwwroot\UGCVBank\Backend`** (সাইট `UGCVBankApi`, পোর্ট **5041**); ফ্রন্টএন্ড ফোল্ডার **`C:\inetpub\wwwroot\UGCVBank\Fontend`** (সাইট `UGCVBankApp`, পোর্ট **4400**; ফোল্ডারের বানান আসলেই `Fontend`)। গাইডের `C:\CvBank\Api` / `C:\CvBank\Web` / পুল `CvBankApi` শুধু উদাহরণ — কমান্ডে সবসময় আসল মান বসান।

প্রতিবার আপডেটের শেষে VM-এ PowerShell (Administrator)-এ এই দুটো:

```powershell
& "$env:windir\system32\inetsrv\appcmd.exe" recycle apppool "UGCVBank"
curl.exe -s -i http://localhost:5041/api/health
```

দ্বিতীয় কমান্ডে `HTTP/1.1 200 OK` + `{"status":"ok",...}` এলেই আপডেট সফল। 503 এলে পুলটা আবার রিসাইকেল করুন; 500.30 এলে ধাপ ১১-এর Event Viewer কমান্ডে আসল exception পড়ুন।

রোলব্যাক: আগের ব্যাকআপ ফোল্ডার + ডেটাবেজ `.bak` ফেরত দিন।

---

## ১১. সমস্যা দেখলে কী দেখবেন

**আসল কারণটা পড়ার সবচেয়ে দ্রুত উপায়** — VM-এ PowerShell (Administrator) খুলে:

```powershell
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='IIS AspNetCore Module V2'} -MaxEvents 3 |
  Format-List TimeCreated, Message
```

`Message`-এর ভেতরে হুবহু exception লেখা থাকে (যেমন `Jwt:SigningKey is missing…` বা `SqlException … error: 26`)। অনুমান না করে ওই লাইনটা পড়েই সিদ্ধান্ত নিন। কিছু না এলে Event Viewer → Windows Logs → Application-এ চোখ রাখুন।

| লক্ষণ | কারণ | সমাধান |
|-------|------|--------|
| **500.30 — ASP.NET Core app failed to start** | অ্যাপ শুরু হওয়ার সময় exception। এই প্রজেক্টে startup-এ ডেটাবেজ ধাপ চলে (`Program.cs` → `DbInitializer` → `MigrateAsync`), তাই সাধারণ কারণ SQL কানেকশন/লগিন/migration ব্যর্থ | উপরের `Get-WinEvent` কমান্ডে আসল exception পড়ুন → ধাপ ৩ ও ৫ |
| 502.5 / 500.31 Process Failure | Hosting Bundle নেই, বা .NET ভার্সন মিলছে না, বা app pool-এ "No Managed Code" নয় | Bundle ইনস্টল → `iisreset`; pool সেটিং দেখুন |
| অ্যাপ চলে কিন্তু **লগিনে 500**, Event Viewer/stdout-এ "Jwt:SigningKey is missing or shorter than 32 characters" | Production ফাইলে কী নেই/৩২ অক্ষরের কম। **user-secrets শুধু Development-এ লোড হয়**, IIS-এ Production চলে তাই ওখান থেকে কী আসে না | ধাপ ৫-এ মান বসান → `iisreset` |
| Event Viewer-এ `SqlException … error: 26` বা "Login failed for…" | `ConnectionStrings:CvBank`-এ ভুল ইনস্ট্যান্স নাম, বা app pool আইডির SQL লগিন/ম্যাপিং নেই | ধাপ ৩ + ধাপ ৫-এর কানেকশন স্ট্রিং |
| Event Viewer-এ **"CREATE DATABASE permission denied in database 'master'"** (Error **262**) + 500.30 | লগিন ঠিক আছে, কানেক্টও হয়েছে — কিন্তু ওই ইনস্ট্যান্সে **`CvBank` ডেটাবেজটা নেই**, আর app pool আইডির ডেটাবেজ বানানোর অধিকার নেই | SSMS-এ admin হিসেবে খালি `CvBank` বানান, তারপর ওই লগিনকে শুধু `CvBank`-এ `db_owner` দিন (ধাপ ৩) → app pool recycle |
| 404.19 / handler error | `web.config` কপি হয়নি বা ANCM মডিউল নেই | publish ফাইল + Bundle চেক |
| লগিনে "Failed to fetch", DevTools-এ request-এর **কোনো status নেই** (timeout), অথচ ওয়েব পোর্ট ঠিক খোলে | VM-এর ফায়ারওয়ালে **API পোর্টের** inbound allow rule নেই — request IIS পর্যন্ত পৌঁছায়ই না, তাই CORS-এর প্রশ্নও আসে না | ধাপ ৮-এর `netsh … localport=<আসল API পোর্ট>` কমান্ড VM-এ চালান। কোনো HTTP status (যেমন 500.30) ফেরত এলে এই সারি প্রযোজ্য নয় — ফায়ারওয়াল খোলা আছে |
| লগিনে "Network error", ব্রাউজার কনসোলে CORS | `Cors:AllowedOrigin`-এ ব্রাউজারের ঠিকানা নেই (তালিকায় হুবহু মিল লাগে) | ঠিকানাটা তালিকায় যোগ করুন, যেমন `http://ug-vm:4400,http://192.168.10.20:4400,http://localhost:4400` → `iisreset` |
| লগিন সফল কিন্তু ৮ ঘণ্টা পর লগ আউট | টোকেন মেয়াদ (480 মিনিট) | স্বাভাবিক; `ExpireMinutes` বাড়াতে চাইলে বলুন |
| সবাই হঠাৎ লগ আউট | `SigningKey` বদলেছে + restart | স্বাভাবিক, আবার লগিন করলেই চলবে |
| SQL "Login failed for IIS APPPOOL\CvBankApi" | লগিন/ইউজ ম্যাপিং নেই, বা `Trusted_Connection`-এর বদলে SQL auth দরকার | ধাপ ৩ আবার করুন |
| আপলোড 500 / ফাইল নিলে 404 | `Documents` ফোল্ডারে Modify পারমিশন নেই, বা পাথ relative থেকে গেছে | ধাপ ৬-এর `icacls` + absolute পাথ |
| `/candidates/1` সরাসরি খুললে 404 | ফ্রন্টএন্ডে SPA fallback web.config নেই | ধাপ ৭-এর ফাইল বসান → `iisreset` |
| আপডেটের পর ব্রাউজারে পুরনো স্ক্রিন | `index.html` ক্যাশে আটকেছে | ধাপ ৭-এর `Cache-Control: no-cache`, তারপর Ctrl+F5 |
| আপডেটের পর স্থায়ী **503**, পুল/সাইট Started, `app_offline.htm` নেই | ওয়ার্কার প্রসেস পুরনো অফলাইন অবস্থায় আটকে আছে | পুল রিসাইকেল করুন (ধাপ ১০-এর কমান্ড) → তারপর health-এ 200 যাচাই করুন |
| ২০ মিনিট ব্যবহার না করলে প্রথমবার খুলতে ধীরে চলে | অ্যাপ পুলের idle timeout (ডিফল্ট ২০ মিনিট) — অ্যাপ ঘুমিয়ে পড়ে, প্রথম অনুরোধে আবার জাগে | স্বাভাবিক আচরণ, এরর নয়। সবসময় চালু রাখতে চাইলে idle timeout 0 + Start Mode AlwaysRunning আলাদা ধাপে করা যায় |
| HTTP-তে শুধু হোম খোলে, API-তে redirect খায় | `UseHttpsRedirection()` + HTTPS binding নেই | হোস্ট নেটওয়ার্কে HTTP-only রাখতে চাইলে কোডে ফ্ল্যাগ করতে হবে (ধাপ ১৩) |

---

## ১২. অপশন A (ঐচ্ছিক): একটাই ঠিকানা — reverse proxy

উপরের বর্ণনা **অপশন B** (দুটি সাইট, দুটি পোর্ট) — সবচেয়ে সহজ, কোনো অতিরিক্ত মডিউল ছাড়াই চলে, কিন্তু ক্লায়েন্ট PC-কে 5140-এ যেতে হয় এবং CORS ঠিকানা মিলিয়ে নিতে হয়।

চাইলে একটাই সাইট/পোর্ট করা যায়: `CvBankWeb` (4400) স্ট্যাটিক ফাইল দেবে, আর `/api/*` ARR reverse proxy দিয়ে `http://localhost:5140`-এ পাঠাবে।

- লাভ: ক্লায়েন্ট শুধু `http://<VM>:4400` দেখে, 5140 খোলা লাগে না, CORS-এর ঝামেলা নেই, ফ্রন্টএন্ডের API ঠিকানা বদলাতে বিল্ড রান লাগে না।
- খরচ: VM-এ **ARR (Application Request Routing)** ইনস্টল + "Enable proxy" চালু করতে হয়।
- প্রয়োজনীয় রুল (`C:\CvBank\Web\web.config`-এর `<rules>`-এর ভেতর, SPA fallback-এর **আগে**):

```xml
<rule name="API to backend" stopProcessing="true">
  <match url="^api/(.*)" />
  <action type="Rewrite" url="http://localhost:5140/api/{R:1}" />
</rule>
```

---

## ১৩. হোস্টিংয়ের আগে কোডে পদক্ষেপের অবস্থা

| # | কী | অবস্থা |
|---|-----|--------|
| 1 | ফ্রন্টএন্ডের API ঠিকানা | **সম্পন্ন (2026-11-04)** — `src/environments/environment.ts` + `environment.prod.ts`, `angular.json`-এ `fileReplacements`, `auth.models.ts` এখন `environment.apiBaseUrl` পড়ে। হোস্টের আগে শুধু prod ফাইলের এক লাইনে VM-এর নাম/IP বসিয়ে বিল্ড |
| 2 | `UseHttpsRedirection()` শর্তসাপেক্ষ করা | বাকি — HTTP-only ইন্ট্রানেটে এটি লগে warning দেয়; HTTPS লাগলে আবার দরকার |
| 3 | Step 8d ব্যাকআপ স্ক্রিপ্ট/প্রসিডিউর | বাকি — ডেটাবেজ + `C:\CvBank\Documents` দুটোই নিরাপদে রাখা, হোস্টিংয়ের আগেই ঠিক করা ভালো |
| 4 | `Cors:AllowedOrigin` কমা-আলাদা **তালিকা** | **সম্পন্ন (2026-11-04)** — `Program.cs`; মেশিনের নাম, IP আর `localhost` একসাথে অনুমোদন করা যায়। কী না দিলে ডিফল্ট `http://localhost:4400` |

চাইলে `assets/config.json`-এর রানটাইম পদ্ধতিও নেওয়া যেত (ঠিকানা বদলালে বিল্ড ছাড়াই চলত); এখন environment ফাইলই আছে, প্রয়োজনে পরে সেটা আলাদা ধাপে করা যাবে। ২ ও ৩ এর জন্য আপনার হুকুমের অপেক্ষা।
