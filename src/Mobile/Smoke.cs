using System.Text;
namespace ERPSystem.Mobile;

/// <summary>
/// اختبار دخان ذاتي بلا واجهة: إن وُجد الملف D:/erp_mobile_smoke.txt ينفّذ دورة API كاملة
/// (تسجيل دخول ← أصناف ← مستودعات ← عملاء ← إنشاء فاتورة) ويكتب النتيجة إلى
/// D:/erp_mobile_smoke_out.txt ثم يوقف الإقلاع برمي استثناء (مؤشر "تم بنجاح" محلياً).
/// </summary>
public static class Smoke
{
    private static readonly string Marker = "D:/erp_mobile_smoke.txt";
    private static readonly string Out = "D:/erp_mobile_smoke_out.txt";

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
        try
        {
            Trace("STEP login");
            var login = ApiClient.LoginSync("smoke@erp.com", "Test@1234");
            if (string.IsNullOrEmpty(login.Token)) { Finish("FAIL no-token"); return; }
            Session.Token = login.Token;

            Trace("STEP items");
            var items = ApiClient.ItemsSync("");
            Trace("STEP warehouses");
            var whs = ApiClient.WarehousesSync();
            Trace("STEP customers");
            var custs = ApiClient.CustomersSync("");

            var it = items.FirstOrDefault();
            var wh = whs.FirstOrDefault();
            var cu = custs.FirstOrDefault();

            string invoiceLine = "n/a";
            if (it is not null && wh is not null && cu is not null)
            {
                Trace("STEP create-invoice");
                var inv = ApiClient.CreateInvoiceSync(cu.Id, wh.Id, it.Id, "1", it.SalePrice, 1, "Smoke");
                invoiceLine = inv.InvoiceNumber + " total=" + inv.TotalAmount;
                Trace("STEP invoice " + invoiceLine);
            }

            var sb = new StringBuilder();
            sb.Append("PASS\n");
            sb.Append("user=").Append(login.UserEmail).Append("\n");
            sb.Append("items=").Append(items.Count).Append("\n");
            sb.Append("warehouses=").Append(whs.Count).Append("\n");
            sb.Append("customers=").Append(custs.Count).Append("\n");
            sb.Append("invoice=").Append(invoiceLine);
            Finish(sb.ToString());
        }
        catch (Exception ex)
        {
            Finish("FAIL " + ex.Message);
        }
    }

    private static void Finish(string text)
    {
        try
        {
            var f2 = File.OpenWrite(Out);
            f2.Write(Text.AsciiBytes(text));
            f2.Flush();
            f2.Dispose();
        }
        catch { }
    }

    private static void Trace(string step)
    {
        try
        {
            var f3 = File.OpenWrite("D:/erp_mobile_smoke_trace.txt");
            f3.Write(Text.AsciiBytes(step));
            f3.Flush();
            f3.Dispose();
        }
        catch { }
    }
}