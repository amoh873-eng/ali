using Microsoft.Maui.Devices;

namespace ERPSystem.Mobile;

/// <summary>جلسة الجوال: التوكن والبريد وعنوان الخادم.</summary>
public static class Session
{
    // عنوان الخادم:
    //   - على Android: يذهب تلقائياً إلى IP هذا الجهاز على الشبكة المحلية (غيّر القيمة
    //     إن اختلف IP جهازك — استعلم عنه بأمر ipconfig في PowerShell).
    //   - على سطح المكتب: localhost كافٍ، أو تجاوز عبر متغير البيئة ERP_API_URL.
    public static string BaseUrl = DeviceInfo.Platform == DevicePlatform.Android
        ? "http://192.168.1.254:5187"
        : (Environment.GetEnvironmentVariable("ERP_API_URL") ?? "http://localhost:5187");
    public static string Token = "";
    public static string UserEmail = "";
    public static string RolesCsv = "";

    public static void Logout()
    {
        Token = "";
        UserEmail = "";
    }
}