namespace ERPSystem.Mobile;

/// <summary>جلسة الجوال: التوكن والبريد وعنوان الخادم.</summary>
public static class Session
{
    // عنوان الخادم:
    //   - سطح المكتب: localhost كافٍ.
    //   - الهاتف على نفس الشبكة: ضع IP جهازك هنا مثل "http://192.168.1.50:5187"
    //     (أو مباشرة اختبره على سطح المكتب عبر متغير البيئة ERP_API_URL)
    public static string BaseUrl = Environment.GetEnvironmentVariable("ERP_API_URL") ?? "http://localhost:5187";
    public static string Token = "";
    public static string UserEmail = "";
    public static string RolesCsv = "";

    public static void Logout()
    {
        Token = "";
        UserEmail = "";
    }
}