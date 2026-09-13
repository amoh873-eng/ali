# تشغيل تطبيق ERP الميداني على الهاتف (Android)

## 0) ما هو الجاهز الآن؟
- الخادم (API) يعمل الآن على **كل الواجهات** (`0.0.0.0:5187`) — أي قابل للوصول من الهاتف على نفس الشبكة.
- اختبار موثّق عبر IP الجهاز نجح (تسجيل دخول + جلب 53 صنفاً).
- **متبقي فقط: بناء نسخة Android من التطبيق وتثبيت الملف على هاتفك** + فتح المنفذ في جدار الحماية.

---

## 1) السماح للمنفذ في جدار الحماية (مرة واحدة)
افتح **PowerShell كمسؤول** على هذا الجهاز ونفّذ:

```powershell
New-NetFirewallRule -DisplayName 'ERP API 5187 LAN' -Direction Inbound -Action Allow `
  -Protocol TCP -LocalPort 5187 -Profile Private,Public
```

> بديل (واجهة): لوحة التحكم ← Windows Defender Firewall ← Advanced ← Inbound Rules ←
> New Rule ← Port ← TCP 5187 ← Allow.

## 2) تأكيد أن الهاتف والجهاز على نفس الشبكة
- من الهاتف، افتح المتصفح وادخل:
  `http://192.168.1.254:5187/health`
  يجب أن ترى `{"status":"ok",...}`.
  (192.168.1.254 هو IP هذا الجهاز حالياً — احصل عليه بأمر `ipconfig` إن تغيّر.)
- **نعم شغّل التحقق على هاتفك قبل خطوة البناء** — إن لم يفتح، فهناك جدار حماية أو شبكة مختلفة.

## 3) إضافة هدف Android إلى المشروع (سطر واحد في csproj)
في `src/Mobile/ERPSystem.Mobile.csproj` غيّر سطر الهدف إلى:

```xml
<TargetFrameworks>net10.0-android35.0;net10.0-windows10.0.19041.0</TargetFrameworks>
```

وأضف داخل `<PropertyGroup>`:

```xml
<AndroidSupportedAbis>arm64-v8a;x86_64</AndroidSupportedAbis>
```

ثم أضف أذونات الإنترنت إلى ملف `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

ولأن الاتصال بالخادم عبر HTTP غير مشفّر، أضف داخل وسم `<application>`:

```xml
android:usesCleartextTraffic="true"
```

## 4) تثبيت حزم عمل Android + SDK (تنزيلات كبيرة ~4-6 جيجا)
```powershell
dotnet workload install maui-android
```
عند أول بناء سيقوم SDK بتجهيز Android SDK تلقائياً في `%LOCALAPPDATA%\Android\Sdk`
(يحتاج JDK 17+ — جهازك يملك JDK 25 بالفعل).

> **ملاحظة**: مساحة C: على هذا الجهاز الآن 12 جيجا فقط — يُفضَّل تحرير مساحة قبل البدء،
> أو تنفيذ البناء على جهاز تطوير آخر (خطوات 3 و4 و5 هي نفسها في كل مكان).

## 5) عنوان الخادم من داخل التطبيق على الهاتف
- **تلقائياً الآن**: `Session.cs` يختار من نفسه — على Android يستخدم `http://192.168.1.254:5187`
  (IP هذا الجهاز الحالي)، وعلى سطح المكتب يعود لـ `localhost`.
- إن اختلف IP جهازك (استعلم بأمر `ipconfig`)، عدّل السطر:

```csharp
public static string BaseUrl = DeviceInfo.Platform == DevicePlatform.Android
    ? "http://192.168.1.254:5187"   // ← غيّره إلى IP جهازك
    : (Environment.GetEnvironmentVariable("ERP_API_URL") ?? "http://localhost:5187");
```

## 6) البناء والتثبيت
> هدف Android الكامل هو `net10.0-android36.0` (وليس المختصر).
> على هذا الجهاز التزمت هذه الأوامر الناجحة فعلياً (SDK في `C:\Users\User az\AppData\Local\Android\Sdk`
> وJDK 25 مفروض عبر تجاوز فحص النسخة لأن `jar` مفقود من JDK 21 المثبت):

### أ) بالكابل (USB Debugging مفعّل على الهاتف)
```powershell
dotnet build -f net10.0-android36.0 -t:Run -c Debug "-p:AndroidSdkDirectory=C:\Users\User az\AppData\Local\Android\Sdk" "-p:JavaSdkDirectory=D:\Program Files\Microsoft\jdk-25.0.4.101-hotspot" "-p:LatestSupportedJavaVersion=25.0.99"
```

### ب) ملف APK تنصّبه يدوياً (المسار المُنتج فعلياً الآن)
```powershell
dotnet publish src/Mobile/ERPSystem.Mobile.csproj -f net10.0-android36.0 -c Release -p:AndroidPackageFormats=apk `
 "-p:AndroidSdkDirectory=C:\Users\User az\AppData\Local\Android\Sdk" `
 "-p:JavaSdkDirectory=D:\Program Files\Microsoft\jdk-25.0.4.101-hotspot" `
 "-p:LatestSupportedJavaVersion=25.0.99" -p:RunAOTCompilation=false -p:PublishTrimmed=false
```

الناتج الموقّع جاهز الآن في:
`src/Mobile/bin/Release/net10.0-android36.0/publish/com.erpsystem.mobile-Signed.apk` (~61 ميغا)
— انسخه للهاتف وثبّته (سمح "مصادر غير معروفة" إن طُلب).

> لماذا `-p:AndroidPackageFormats=apk`؟ لأن bundletool الافتراضي يفشل هنا بخطأ
> (Invalid dex file indices) ناتج عن البيئة؛ إنتاج APK مباشر يتجاوزه.
> لإعادة بناء سطح المكتب فقط: `dotnet build -f net10.0-windows10.0.19041.0`.

## 7) الدخول
- سجّل الدخول بـ `smoke@erp.com / Test@1234` (دور منسّق) أو أي حساب أعليته.
- الخادم يعمل عبر `dotnet run --project src/Api` أو سكربت `_start_mobile.ps1`.

## ملاحظات
- جناح الكاميرا: يُحضر على Android حقيقياً (لم يُقرر بعد تشغيليه) — الإدخال الحالي بمسار ملف.
- النسخة Windows (سطح المكتب) ما زالت تعمل كالمعتاد؛ إضافة هدف Android في csproj لا تعطلها،
  لكن تأكد أن أمر البناء يحدد الهدف صراحةً حتى لا يبني الجهتين دائماً:
  `dotnet build -f net10.0-windows10.0.19041.0`.