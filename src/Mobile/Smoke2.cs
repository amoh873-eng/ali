using System.Text;
using System.Text.Json;

namespace ERPSystem.Mobile;

/// <summary>
/// اختبار المرحلة 2 (Offline-First): دون إنترنت فعلي (منفذ مغلق) ← إنشاء طلبين ← عودة الاتصال
/// ← مزامنة ← تحقق عنصرية التكرار عبر إعادة إرسال نفس المفتاح ← تحقق NeedsReview بكمية تتجاوز الرصيد.
/// </summary>
public static class Smoke2
{
    private static readonly string Marker = "D:/erp_mobile_smoke2.txt";
    private static readonly string Out = "D:/erp_mobile_smoke2_out.txt";

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

        // 0) ادخال أولاً (الاتصال متاح) لنحصل على التوكن والمراجع
        var login = ApiClient.LoginSync("smoke@erp.com", "Test@1234");
        if (string.IsNullOrEmpty(login.Token)) return "FAIL no-token";
        Session.Token = login.Token;
        var item = ApiClient.ItemsSync("").FirstOrDefault();
        var wh = ApiClient.WarehousesSync().FirstOrDefault();
        var cu = ApiClient.CustomersSync("").FirstOrDefault();
        if (item is null || wh is null || cu is null) return "FAIL no refs";
        lines.Add("refs_item=" + item.Id);
        lines.Add("stock_current=" + item.CurrentStock);

        // 1) دون إنترنت فعلي (منفذ مغلق) — إنشاء محلي بلا شبكة
        var offlineUrl = "http://127.0.0.1:59999";
        Session.BaseUrl = offlineUrl;
        LocalStore.Reset();

        var k1 = "p2-k-" + NewGuid();
        var k2 = "p2-k-" + NewGuid();
        LocalStore.Enqueue(k1, ApiClient.MakeInvoicePayload(cu.Id, wh.Id, item.Id, "1", item.SalePrice, "Offline A", k1));
        LocalStore.Enqueue(k2, ApiClient.MakeInvoicePayload(cu.Id, wh.Id, item.Id, "2", item.SalePrice, "Offline B", k2));
        var pendingBefore = LocalStore.CountPending();
        lines.Add("pending_offline=" + pendingBefore);

        // 2) عودة الاتصال والمزامنة التلقائية
        Session.BaseUrl = "http://localhost:5187";
        SyncEngine.RunOnce();
        var pendingAfter = LocalStore.CountPending();
        lines.Add("pending_after_sync=" + pendingAfter);

        // 3) عنصرية التكرار: إرسال نفس الحمولة مرتين → الثانية duplicate وبنفس الرقم
        var p1 = ApiClient.MakeInvoicePayload(cu.Id, wh.Id, item.Id, "1", item.SalePrice, "Offline A", k1);
        var r1 = ApiClient.SendInvoiceFromQueueSync(p1);
        var r2 = ApiClient.SendInvoiceFromQueueSync(p1);
        lines.Add("dup_second=" + r2.duplicate + " same_num=" + (r1.number == r2.number) + " num=" + r1.number);

        // 4) المخزون المتنازع (كمية تتجاوز الرصيد) → NeedsReview لا رفض صامت
        var k3 = "p2-review-" + NewGuid();
        var p3 = ApiClient.MakeInvoicePayload(cu.Id, wh.Id, item.Id, "99999", item.SalePrice, "OverStock", k3);
        var r3 = ApiClient.SendInvoiceFromQueueSync(p3);
        lines.Add("needs_review=" + r3.needsReview);
        lines.Add("r3_ok=" + r3.ok + " r3_dup=" + r3.duplicate);
        var reason = JsonLite.Unescape(JsonLite.Field(r3.message, "reason"));
        lines.Add("decoded_reason=" + (reason.Length > 60 ? reason.Substring(0, 60) : reason));

        var ok = pendingBefore == 2 && pendingAfter == 0
            && r2.duplicate && r1.number == r2.number && !string.IsNullOrEmpty(r1.number)
            && r3.needsReview;
        lines.Insert(0, ok ? "PASS" : "FAIL");
        var sb = new StringBuilder();
        foreach (var ln in lines) sb.Append(ln).Append('\n');
        return sb.ToString();
    }

    private static string NewGuid()
    {
        return Guid.NewGuid().ToString();
    }
}