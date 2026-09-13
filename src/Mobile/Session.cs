namespace ERPSystem.Mobile;

/// <summary>جلسة الجوال: التوكن والبريد وعنوان الخادم.</summary>
public static class Session
{
    public static string BaseUrl = "http://localhost:5187";
    public static string Token = "";
    public static string UserEmail = "";
    public static string RolesCsv = "";

    public static void Logout()
    {
        Token = "";
        UserEmail = "";
    }
}