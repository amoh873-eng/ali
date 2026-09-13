using System.Text;
using System.Text.Json;

namespace ERPSystem.Mobile;

/// <summary>
/// مخزن محلي أولاً (Local-First): كاش للقراءة دون اتصال + طابور مزامنة + آخر مزامنة.
/// ملف JSON واحد (erp_mobile_store.json) — خلف واجهة Models مستقلة للّصق بـ SQLite لاحقاً.
/// </summary>
public static class LocalStore
{
    private static readonly string Db = "D:/erp_mobile_store.json";

    private static List<ItemDto> _items = new List<ItemDto>();
    private static List<CustomerDto> _customers = new List<CustomerDto>();
    private static List<WarehouseDto> _warehouses = new List<WarehouseDto>();
    private static List<OutboxItem> _outbox = new List<OutboxItem>();
    private static string _lastSync = "";

    public static void Reset()
    {
        _items = new List<ItemDto>();
        _customers = new List<CustomerDto>();
        _warehouses = new List<WarehouseDto>();
        _outbox = new List<OutboxItem>();
        _lastSync = "";
        Save();
    }

    public static string LastSync() { Load(); return _lastSync; }
    public static void SetLastSyncNow() { _lastSync = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"); Save(); }

    public static List<ItemDto> CachedItems() { Load(); return _items; }
    public static List<CustomerDto> CachedCustomers() { Load(); return _customers; }
    public static List<WarehouseDto> CachedWarehouses() { Load(); return _warehouses; }

    public static void CacheAll(List<ItemDto> items, List<CustomerDto> customers, List<WarehouseDto> warehouses)
    {
        _items = items;
        _customers = customers;
        _warehouses = warehouses;
        Save();
    }

    public static void Enqueue(string key, string payload)
    {
        Load();
        _outbox.Add(new OutboxItem(key, payload, "Pending"));
        Save();
    }

    public static List<OutboxItem> ListPending()
    {
        Load();
        var list = new List<OutboxItem>();
        foreach (var o in _outbox) if (o.Status == "Pending") list.Add(o);
        return list;
    }

    public static int CountPending()
    {
        Load();
        var n = 0;
        foreach (var o in _outbox) if (o.Status == "Pending") n++;
        return n;
    }

    public static void MarkStatus(string key, string status)
    {
        Load();
        var next = new List<OutboxItem>();
        foreach (var o in _outbox) next.Add(o.ClientKey == key ? new OutboxItem(o.ClientKey, o.Payload, status) : o);
        _outbox = next;
        Save();
    }

// ══ القراءة والكتابة ══
    private static void Load()
    {
        try
        {
            var f = File.OpenRead(Db);
            using var rd = new StreamReader(f);
            var sb = new StringBuilder();
            string? line;
            while ((line = rd.ReadLine()) is not null) sb.Append(line);
            f.Dispose();
            if (sb.Length == 0) return;
            var rootText = sb.ToString();

            _lastSync = JsonLite.Unescape(JsonLite.Field(rootText, "lastSync"));

            _items = new List<ItemDto>();
            foreach (var n in JsonLite.Objects(JsonLite.ArrayField(rootText, "items")))
            {
                _items.Add(new ItemDto(
                    JsonLite.Field(n, "id"),
                    JsonLite.Field(n, "code"),
                    JsonLite.Unescape(JsonLite.Field(n, "nameAr")),
                    JsonLite.Field(n, "nameEn") == "" ? null : JsonLite.Unescape(JsonLite.Field(n, "nameEn")),
                    JsonLite.Field(n, "salePrice"),
                    JsonLite.Field(n, "currentStock")));
            }

            _customers = new List<CustomerDto>();
            foreach (var n in JsonLite.Objects(JsonLite.ArrayField(rootText, "customers")))
            {
                _customers.Add(new CustomerDto(
                    JsonLite.Field(n, "id"),
                    JsonLite.Field(n, "code"),
                    JsonLite.Unescape(JsonLite.Field(n, "nameAr")),
                    JsonLite.Field(n, "phone") == "" ? null : JsonLite.Unescape(JsonLite.Field(n, "phone"))));
            }

            _warehouses = new List<WarehouseDto>();
            foreach (var n in JsonLite.Objects(JsonLite.ArrayField(rootText, "warehouses")))
            {
                _warehouses.Add(new WarehouseDto(
                    JsonLite.Field(n, "id"),
                    JsonLite.Field(n, "code"),
                    JsonLite.Unescape(JsonLite.Field(n, "nameAr"))));
            }

            _outbox = new List<OutboxItem>();
            foreach (var n in JsonLite.Objects(JsonLite.ArrayField(rootText, "outbox")))
            {
                var payloadB64 = JsonLite.Field(n, "payload");
                var payload = "";
                try
                {
                    payload = Text.Utf8Decode(Convert.FromBase64String(payloadB64));
                }
                catch { }
                _outbox.Add(new OutboxItem(
                    JsonLite.Field(n, "key"),
                    payload,
                    JsonLite.Field(n, "status")));
            }
        }
        catch { }
    }

    private static void Save()
    {
        var sb = new StringBuilder();
        sb.Append("{\"lastSync\":\"").Append(Text.Json(_lastSync)).Append("\",");
        AppendItems(sb); sb.Append(",");
        AppendCustomers(sb); sb.Append(",");
        AppendWarehouses(sb); sb.Append(",");
        AppendOutbox(sb);
        sb.Append("}");
        try
        {
            var f = File.OpenWrite(Db);
            f.Write(Text.Utf8Encode(sb.ToString()));
            f.Flush();
            f.Dispose();
        }
        catch { }
    }

    private static void AppendItems(StringBuilder sb)
    {
        sb.Append("\"items\":[");
        var first = true;
        foreach (var it in _items)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append("{\"id\":\"").Append(Text.Json(it.Id)).Append("\",\"code\":\"").Append(Text.Json(it.Code))
              .Append("\",\"nameAr\":\"").Append(Text.Json(it.NameAr)).Append("\",\"nameEn\":");
            sb.Append(it.NameEn is null ? "null" : "\"" + Text.Json(it.NameEn) + "\"");
            sb.Append(",\"salePrice\":\"").Append(Text.Json(it.SalePrice))
              .Append("\",\"currentStock\":\"").Append(Text.Json(it.CurrentStock)).Append("\"}");
        }
        sb.Append("]");
    }

    private static void AppendCustomers(StringBuilder sb)
    {
        sb.Append("\"customers\":[");
        var first = true;
        foreach (var c in _customers)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append("{\"id\":\"").Append(Text.Json(c.Id)).Append("\",\"code\":\"").Append(Text.Json(c.Code))
              .Append("\",\"nameAr\":\"").Append(Text.Json(c.NameAr)).Append("\",\"phone\":");
            sb.Append(c.Phone is null ? "null" : "\"" + Text.Json(c.Phone) + "\"");
            sb.Append("}");
        }
        sb.Append("]");
    }

    private static void AppendWarehouses(StringBuilder sb)
    {
        sb.Append("\"warehouses\":[");
        var first = true;
        foreach (var w in _warehouses)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append("{\"id\":\"").Append(Text.Json(w.Id)).Append("\",\"code\":\"").Append(Text.Json(w.Code))
              .Append("\",\"nameAr\":\"").Append(Text.Json(w.NameAr)).Append("\"}");
        }
        sb.Append("]");
    }

    private static void AppendOutbox(StringBuilder sb)
    {
        sb.Append("\"outbox\":[");
        var first = true;
        foreach (var o in _outbox)
        {
            if (!first) sb.Append(",");
            first = false;
            // الحمولة تُخزَّن Base64 (نص ASCII آمن — بلا تعشيش ترميز)
            var b64 = Convert.ToBase64String(Text.Utf8Encode(o.Payload));
            sb.Append("{\"key\":\"").Append(Text.Json(o.ClientKey))
              .Append("\",\"payload\":\"").Append(Text.Json(b64))
              .Append("\",\"status\":\"").Append(Text.Json(o.Status)).Append("\"}");
        }
        sb.Append("]");
    }
}