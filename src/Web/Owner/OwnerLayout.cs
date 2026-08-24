namespace ERPSystem.Web.Owner;

public static class OwnerLayout
{
    public static string Wrap(string title, string secret, string body, string active = "")
    {
        string NavItem(string key, string label, string icon, string href)
        {
            var on = active == key ? "background:#6D5BD0;color:#fff;" : "color:#374151;";
            return $"<a href='/{secret}{href}' style='display:flex;align-items:center;gap:8px;padding:10px 14px;border-radius:10px;text-decoration:none;font-weight:600;font-size:.88rem;{on}'>{icon} {label}</a>";
        }
        return $@"<!doctype html><html lang='ar' dir='rtl'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>{title} — لوحة المورّد</title>
<link href='https://fonts.googleapis.com/css2?family=Cairo:wght@400;600;700&display=swap' rel='stylesheet'>
<style>
*{{box-sizing:border-box}}body{{margin:0;font-family:Cairo,sans-serif;background:#F0F1F7;color:#1F2937}}
.top{{background:#fff;border-bottom:1px solid #E5E7EB;padding:14px 24px;display:flex;align-items:center;gap:16px;position:sticky;top:0;z-index:10}}
.top h1{{margin:0;font-size:1.1rem;color:#6D5BD0}}
.wrap{{display:flex;min-height:calc(100vh - 56px)}}
.sidebar{{width:240px;background:#fff;border-left:1px solid #E5E7EB;padding:16px;display:flex;flex-direction:column;gap:6px}}
.main{{flex:1;padding:24px}}
.card{{background:#fff;border-radius:14px;box-shadow:0 2px 12px rgba(0,0,0,.06);padding:20px;margin-bottom:16px}}
.card h2{{margin:0 0 14px;font-size:1.05rem;color:#1F2937}}
.stat-grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px}}
.stat{{background:#F9FAFB;border:1px solid #E5E7EB;border-radius:12px;padding:14px;text-align:center}}
.stat b{{display:block;font-size:1.4rem;color:#6D5BD0}}
.stat span{{font-size:.82rem;color:#6B7280}}
.badge{{display:inline-block;padding:4px 10px;border-radius:20px;font-size:.8rem;font-weight:700}}
.badge-on{{background:#E8F5E9;color:#10B981}}.badge-off{{background:#FEE2E2;color:#EF4444}}
table{{width:100%;border-collapse:collapse}}td,th{{padding:10px;border-bottom:1px solid #E5E7EB;text-align:right;font-size:.9rem}}
input,select,textarea{{width:100%;padding:10px;border:1px solid #E5E7EB;border-radius:10px;font-family:Cairo,sans-serif}}
button.primary{{background:#6D5BD0;color:#fff;border:0;padding:10px 18px;border-radius:10px;cursor:pointer;font-weight:700}}
button.ghost{{background:#fff;border:1px solid #E5E7EB;padding:8px 14px;border-radius:10px;cursor:pointer}}
a.btn{{display:inline-block;background:#6D5BD0;color:#fff;padding:8px 14px;border-radius:10px;text-decoration:none;font-weight:600}}
.warn{{padding:10px 12px;border-radius:10px;font-size:.85rem;margin-bottom:12px}}
.warn-amber{{background:#FEF3C7;color:#92400E}}.warn-red{{background:#FEE2E2;color:#991B1B}}
</style></head><body>
<div class='top'><h1>◈ لوحة المورّد</h1><span style='margin-inline-start:auto;font-size:.85rem;color:#6B7280'>/{secret}</span><a href='/{secret}/logout' style='font-size:.85rem;color:#EF4444;text-decoration:none;font-weight:600'>خروج →</a></div>
<div class='wrap'>
<div class='sidebar'>
{NavItem("dash","الرئيسية","▦","/")}
{NavItem("brand","الهوية","🎨","/branding")}
{NavItem("feat","الموديولات","🧩","/features")}
{NavItem("reports","التقارير","📊","/reports")}
{NavItem("financials","المالية","💰","/financials")}
{NavItem("sub","الاشتراك","💳","/subscription")}
{NavItem("health","الصحة","🏥","/health")}
{NavItem("updates","التحديثات","⬆","/updates")}
{NavItem("trial","التجريبي","🧪","/trial")}
{NavItem("hist","السجل","🕘","/history")}
{NavItem("sup","الدعم","💬","/support")}
</div>
<div class='main'>{body}</div>
</div>
</body></html>";
    }
}
