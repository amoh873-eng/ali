using System.Text;

namespace ERPSystem.Mobile;

/// <summary>
/// محرك المزامنة: يرسل عناصر طابور LocalStore عند توفر الاتصال، مع عنصرية مفتاح التكرار
/// ومنطق NeedsReview. لا يمزج بيانات: يحترم حالات الخادم (Synced / NeedsReview) ويترك Pending
/// للمحاولة التالية عند فشل الشبكة.
/// </summary>
public static class SyncEngine
{
    private static bool _started = false;

    public static void StartBackground()
    {
        if (_started) return;
        _started = true;
        var t = new Thread(() => Loop());
        t.Start();
    }

    private static void Loop()
    {
        while (true)
        {
            try { RunOnce(); }
            catch { }
            Thread.Sleep(15000);
        }
    }

    public static bool IsOnline()
    {
        try { ApiClient.WarehousesSync(); return true; }
        catch { return false; }
    }

    /// <summary>محاولة إرسال عناصر الطابور المعلّقة (الأقدم أولاً حسب الترتيب).</summary>
    public static void RunOnce()
    {
        var pending = LocalStore.ListPending();
        foreach (var item in pending)
        {
            var r = ApiClient.SendInvoiceFromQueueSync(item.Payload);
            if (r.ok || r.needsReview || r.duplicate)
            {
                LocalStore.MarkStatus(item.ClientKey, r.needsReview ? "NeedsReview" : "Synced");
            }
            // فشل الشبكة → يبقى Pending لإعادة المحاولة لاحقاً
        }
        LocalStore.SetLastSyncNow();
    }
}