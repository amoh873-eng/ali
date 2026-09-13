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
المحلي `localhost` لا يعمل على الهاتف. عدّل في `src/Mobile/Session.cs` سطر العنوان إلى IP جهازك:

```csharp
public static string BaseUrl = "http://192.168.1.254:5187";
```

## 6) البناء والتثبيت
### أ) بالكابل (أسـهل — USB Debugging مفعّل على الهاتف)
```powershell
dotnet build -f net10.0-android -t:Run -c Debug
```
### ب) ملف APK تنصّبه يدوياً
```powershell
dotnet publish -f net10.0-android -c Release
```
سيُنتج ملف التثبيت في:
`src/Mobile/bin/Release/net10.0-android/publish/*.apk`
— انسخه للهاتف وثبّته (سمح "مصادر غير معروفة" إن طُلب).

## 7) الدخول
- سجّل الدخول بـ `smoke@erp.com / Test@1234` (دور منسّق) أو أي حساب أعليته.
- الخادم يعمل عبر `dotnet run --project src/Api` أو سكربت `_start_mobile.ps1`.

## ملاحظات
- جناح الكاميرا: يُحضر على Android حقيقياً (لم يُقرر بعد تشغيليه) — الإدخال الحالي بمسار ملف.
- النسخة Windows (سطح المكتب) ما زالت تعمل كالمعتاد؛ إضافة هدف Android في csproj لا تعطلها،
  لكن تأكد أن أمر البناء يحدد الهدف صراحةً حتى لا يبني الجهتين دائماً:
  `dotnet build -f net10.0-windows10.0.19041.0`.