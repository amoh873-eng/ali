using System.Text;

namespace ERPSystem.Mobile;

/// <summary>اختبار دخان المرحلة 4: أدوار المنسّق + فحص رف + رصد سعر + حدود + تحذير انتهاء عبر طبقة الجوال.</summary>
public static class Smoke4
{
    private static readonly string Marker = "D:/erp_mobile_smoke4.txt";
    private static readonly string Out = "D:/erp_mobile_smoke4_out.txt";

    public static bool RunIfMarked()
    {
        bool marked = false;
        try { var f = File.OpenRead(Marker); f.Dispose(); marked = true; }
        catch { marked = false; }
        if (!marked) return false;
        var t = new Thread(() => BackgroundRun());
        t.Start();
        return true;
    }

    private static void BackgroundRun()
    {
        string result;
        try { result = Run(); }
        catch (Exception ex) { result = "FAIL " + ex.Message; }
        try
        {
            var f = File.OpenWrite(Out);
            f.Write(Text.Utf8Encode(result));
            f.Flush();
            f.Dispose();
        }
        catch { }
    }

    private static string Run()
    {
        var lines = new List<string>();
        var login = ApiClient.LoginSync("smoke@erp.com", "Test@1234");
        if (string.IsNullOrEmpty(login.Token)) return "FAIL no-token";
        Session.Token = login.Token;
        Session.RolesCsv = login.RolesCsv;
        var isMerch = login.RolesCsv.Contains("Merchandiser");
        lines.Add("is_merch=" + isMerch);

        var itemId = "c043c052-fa7a-497e-8e82-a240db431f34";
        var checkId = ApiClient.CreateShelfCheckSync(itemId, "Ammam Store 2", "7", "offline demo", "");
        lines.Add("check_id=" + checkId);

        var capId = ApiClient.CreatePriceCaptureSync(itemId, "Market Jamal", "9.10");
        lines.Add("price_id=" + capId);

        var par = ApiClient.ParLevelsSync();
        var exp = ApiClient.ExpiryWarningsSync();
        lines.Add("par_has=8.0? " + par.Contains("8.0") + (par.Length > 120 ? par.Substring(0, 120) : par));
        lines.Add("exp=" + (exp.Length > 200 ? exp.Substring(0, 200) : exp));

        var ok = isMerch && !string.IsNullOrEmpty(checkId) && !string.IsNullOrEmpty(capId);
        lines.Insert(0, ok ? "PASS" : "FAIL");
        var sb = new StringBuilder();
        foreach (var ln in lines) sb.Append(ln).Append('\n');
        return sb.ToString();
    }
}