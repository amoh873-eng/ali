using System.Net.Http;
using System.Text.Json;

namespace ERPSystem.Mobile;

/// <summary>عميل REST لخادم ERP — استدعاءات async ونسخ متزامنة (للمعالجات اللحظية).</summary>
public static class ApiClient
{
    // ── الإصدارات غير المتزامنة (تُحظر في خيط الاستدعاء — للواجهة البسيطة) ──
    public static LoginResponseDto LoginSync(string email, string password)
        => Await(LoginAsync(email, password));

    public static List<ItemDto> ItemsSync(string search)
        => Await(ItemsAsync(search));

    public static List<CustomerDto> CustomersSync(string search)
        => Await(CustomersAsync(search));

    public static List<WarehouseDto> WarehousesSync()
        => Await(WarehousesAsync());

    /// <summary>إنشاء فاتورة (متعدد الأصناف).</summary>
    public static InvoiceDto CreateInvoiceSync(List<InvoiceLineDto> lines, string customerId, string warehouseId, int paymentMethod, string note, string photoBase64 = "")
    {
        var key0 = Guid.NewGuid().ToString();
        var payload = MakeInvoicePayload(lines, customerId, warehouseId, note, key0, paymentMethod, photoBase64);
        var resp = Await(SendAsync("POST", "/api/sales-invoices", payload));
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return ParseInvoice(resp.body);
    }

    /// <summary>نسخة سطر واحد (متوافقة مع الاختبارات) — تحوَّل إلى قائمة أسطر.</summary>
    public static InvoiceDto CreateInvoiceSync(string customerId, string warehouseId, string itemId, string quantity, string unitPrice, int paymentMethod, string note, string photoBase64 = "")
    {
        return CreateInvoiceSync(ListOf(itemId, quantity, unitPrice), customerId, warehouseId, paymentMethod, note, photoBase64);
    }

    private static List<InvoiceLineDto> ListOf(string itemId, string quantity, string unitPrice)
    {
        var l = new List<InvoiceLineDto>();
        l.Add(new InvoiceLineDto(itemId, quantity, unitPrice));
        return l;
    }

    /// <summary>حمولة طلب ميداني كاملة (أسطر متعددة + مفتاح تكرار + طريقة دفع + صورة اختيارية).</summary>
    public static string MakeInvoicePayload(List<InvoiceLineDto> lines, string customerId, string warehouseId, string note, string clientKey, int paymentMethod = 1, string photoBase64 = "")
    {
        var body = "{"
            + "\"warehouseId\":\"" + warehouseId + "\","
            + "\"customerId\":\"" + customerId + "\","
            + "\"paymentMethod\":" + paymentMethod + ","
            + "\"lines\":[";
        var first = true;
        foreach (var ln in lines)
        {
            if (!first) body += ",";
            first = false;
            body += "{\"itemId\":\"" + ln.ItemId
                + "\",\"quantity\":" + ln.Quantity
                + ",\"unitPrice\":" + ln.UnitPrice + "}";
        }
        body += "],"
            + "\"note\":\"" + Text.Json(note) + "\","
            + "\"clientRequestId\":\"" + Text.Json(clientKey) + "\"";
        if (!string.IsNullOrEmpty(photoBase64)) body += ",\"photoBase64\":\"" + photoBase64 + "\"";
        body += "}";
        return body;
    }

    /// <summary>نسخة سطر واحد — تحوَّل إلى قائمة أسطر.</summary>
    public static string MakeInvoicePayload(string customerId, string warehouseId, string itemId, string quantity, string unitPrice, string note, string clientKey, int paymentMethod = 1, string photoBase64 = "")
    {
        return MakeInvoicePayload(ListOf(itemId, quantity, unitPrice), customerId, warehouseId, note, clientKey, paymentMethod, photoBase64);
    }

    /// <summary>نتيجة إرسال عنصر طابور: (نجاح، يحتاج مراجعة، مكرر، رقم الفاتورة، رسالة).</summary>
    public static (bool ok, bool needsReview, bool duplicate, string number, string message) SendInvoiceFromQueueSync(string payload)
    {
        var resp = Await(SendAsync("POST", "/api/sales-invoices", payload));
        if (!resp.ok) return (false, false, false, "", resp.body);
        var body = resp.body;
        var needs = JsonLite.Field(body, "needsReview") == "true";
        var dup = JsonLite.Field(body, "duplicate") == "true";
        var number = JsonLite.Unescape(JsonLite.Field(body, "invoiceNumber"));
        return (true, needs, dup, number, body);
    }

    /// <summary>تحصيل ميداني في عهدة المندوب (المستخدم الحالي هو المندوب).</summary>
    public static string CollectCustodySync(string customerId, string amount, string note)
    {
        var body = "{\"customerId\":\"" + Text.Json(customerId)
            + "\",\"amount\":" + amount
            + ",\"note\":\"" + Text.Json(note) + "\",\"paymentMethod\":1}";
        var resp = Await(SendAsync("POST", "/api/reps/custody", body));
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return JsonLite.Unescape(JsonLite.Field(resp.body, "entryNumber"));
    }

    /// <summary>البحث عن فاتورة مرحلة للمردود مع الأسطر القابلة للرد.</summary>
    public static ReturnLookupDto ReturnLookupSync(string invoiceNumber)
    {
        var body = Await(GetBodyAsync("/api/sales-returns/lookup?invoiceNumber=" + invoiceNumber.Trim()));
        var lines = new List<ReturnLookupLineDto>();
        foreach (var n in JsonLite.Objects(JsonLite.ArrayField(body, "lines")))
            lines.Add(new ReturnLookupLineDto(
                JsonLite.Field(n, "itemId"),
                JsonLite.Field(n, "code"),
                JsonLite.Unescape(JsonLite.Field(n, "nameAr")),
                JsonLite.Field(n, "quantity"),
                JsonLite.Field(n, "unitPrice"),
                JsonLite.Field(n, "remaining")));
        return new ReturnLookupDto(
            JsonLite.Field(body, "invoiceId"),
            JsonLite.Unescape(JsonLite.Field(body, "invoiceNumber")),
            JsonLite.Field(body, "customerId"),
            JsonLite.Unescape(JsonLite.Field(body, "customerName")),
            JsonLite.Field(body, "warehouseId"),
            JsonLite.Unescape(JsonLite.Field(body, "warehouseName")),
            JsonLite.Field(body, "invoiceDate"),
            JsonLite.Field(body, "totalAmount"),
            lines);
    }

    /// <summary>إنشاء مردود مبيعات (يعتمد أسعار الفاتورة الأصلية).</summary>
    public static ReturnDto CreateSalesReturnSync(string salesInvoiceId, string warehouseId, List<CreateReturnLineDto> lines, string note)
    {
        var payload = "{\"salesInvoiceId\":\"" + salesInvoiceId
            + "\",\"warehouseId\":\"" + warehouseId
            + "\",\"note\":\"" + Text.Json(note) + "\",\"lines\":[";
        var first = true;
        foreach (var ln in lines)
        {
            if (!first) payload += ",";
            first = false;
            payload += "{\"itemId\":\"" + ln.ItemId + "\",\"quantity\":" + ln.Quantity + "}";
        }
        payload += "]}";
        var resp = Await(SendAsync("POST", "/api/sales-returns", payload));
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return new ReturnDto(
            JsonLite.Field(resp.body, "id"),
            JsonLite.Unescape(JsonLite.Field(resp.body, "returnNumber")),
            JsonLite.Unescape(JsonLite.Field(resp.body, "invoiceNumber")),
            JsonLite.Field(resp.body, "totalAmount"));
    }

    /// <summary>الحمولة مبنية للاختبار المباشر للتحصيل.</summary>
    public static string MakeCustodyPayload(string customerId, string amount, string note)
        => "{\"customerId\":\"" + Text.Json(customerId)
            + "\",\"amount\":" + amount
            + ",\"note\":\"" + Text.Json(note) + "\",\"paymentMethod\":1}";

    /// <summary>حمولة محلية كاملة للفحص + إرسال فور (يعيد مسار الصورة).</summary>
    public static string CreateShelfCheckSync(string itemId, string location, string qty, string notes, string photoBase64)
    {
        var body = "{\"itemId\":\"" + Text.Json(itemId)
            + "\",\"location\":\"" + Text.Json(location)
            + "\",\"observedQty\":" + qty
            + ",\"notes\":\"" + Text.Json(notes) + "\"";
        if (!string.IsNullOrEmpty(photoBase64)) body += ",\"photoOne\":\"" + photoBase64 + "\"";
        body += "}";
        var resp = Await(SendAsync("POST", "/api/shelf/checks", body));
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return JsonLite.Field(resp.body, "id");
    }

    /// <summary>إرسال رصد سعر منافس (يعيد المعرف).</summary>
    public static string CreatePriceCaptureSync(string itemId, string competitor, string price)
    {
        var body = "{\"itemId\":\"" + Text.Json(itemId)
            + "\",\"competitorName\":\"" + Text.Json(competitor)
            + "\",\"price\":" + price + "}";
        var resp = Await(SendAsync("POST", "/api/shelf/price-captures", body));
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return JsonLite.Field(resp.body, "id");
    }

    /// <summary>قراءة حدود الرفوف (JSON خام للعرض).</summary>
    public static string ParLevelsSync()
    {
        var resp = Await(SendAsync("GET", "/api/shelf/par-levels", null));
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return resp.body;
    }

    /// <summary>قراءة تحذيرات الانتهاء (JSON خام للعرض).</summary>
    public static string ExpiryWarningsSync()
    {
        var resp = Await(SendAsync("GET", "/api/shelf/expiry-warnings", null));
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return resp.body;
    }

    private static T Await<T>(Task<T> f) => f.GetAwaiter().GetResult();

    // ── الإصدارات اللاتزامنية ──
    public static async Task<LoginResponseDto> LoginAsync(string email, string password)
    {
        var body = "{\"email\":\"" + Text.Json(email) + "\",\"password\":\"" + Text.Json(password) + "\"}";
        var resp = await SendAsync("POST", "/api/auth/login", body);
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        var token = JsonLite.Field(resp.body, "token");
        var userBlock = JsonLite.ObjectField(resp.body, "user");
        var userEmail = userBlock == "" ? "" : JsonLite.Unescape(JsonLite.Field(userBlock, "email"));
        var rolesRaw = JsonLite.ArrayField(userBlock, "roles").Replace("[", "").Replace("]", "").Replace("\"", "");
        return new LoginResponseDto(token, userEmail, rolesRaw);
    }

    public static async Task<List<ItemDto>> ItemsAsync(string search)
    {
        var body = await GetBodyAsync("/api/items");
        var list = new List<ItemDto>();
        foreach (var n in JsonLite.Objects(body))
        {
            list.Add(new ItemDto(
                JsonLite.Field(n, "id"),
                JsonLite.Field(n, "code"),
                JsonLite.Unescape(JsonLite.Field(n, "nameAr")),
                JsonLite.Field(n, "nameEn") == "" ? null : JsonLite.Unescape(JsonLite.Field(n, "nameEn")),
                JsonLite.Field(n, "salePrice"),
                JsonLite.Field(n, "currentStock")));
        }
        return list;
    }

    public static async Task<List<CustomerDto>> CustomersAsync(string search)
    {
        var body = await GetBodyAsync("/api/customers");
        var list = new List<CustomerDto>();
        foreach (var n in JsonLite.Objects(body))
        {
            list.Add(new CustomerDto(
                JsonLite.Field(n, "id"),
                JsonLite.Field(n, "code"),
                JsonLite.Unescape(JsonLite.Field(n, "nameAr")),
                JsonLite.Field(n, "phone") == "" ? null : JsonLite.Unescape(JsonLite.Field(n, "phone"))));
        }
        return list;
    }

    public static async Task<List<WarehouseDto>> WarehousesAsync()
    {
        var body = await GetBodyAsync("/api/warehouses");
        var list = new List<WarehouseDto>();
        foreach (var n in JsonLite.Objects(body))
        {
            list.Add(new WarehouseDto(
                JsonLite.Field(n, "id"),
                JsonLite.Field(n, "code"),
                JsonLite.Unescape(JsonLite.Field(n, "nameAr"))));
        }
        return list;
    }

    public static async Task<InvoiceDto> CreateInvoiceAsync(string customerId, string warehouseId, string itemId, string quantity, string unitPrice)
    {
        var body = MakeInvoicePayload(customerId, warehouseId, itemId, quantity, unitPrice, "Mobile", "");
        var resp = await SendAsync("POST", "/api/sales-invoices", body);
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return ParseInvoice(resp.body);
    }

    private static InvoiceDto ParseInvoice(string json)
    {
        return new InvoiceDto(
            JsonLite.Field(json, "id"),
            JsonLite.Unescape(JsonLite.Field(json, "invoiceNumber")),
            JsonLite.Field(json, "totalAmount"),
            JsonLite.Unescape(JsonLite.Field(json, "note")));
    }

    private static async Task<string> GetBodyAsync(string path)
    {
        var resp = await SendAsync("GET", path, null);
        if (!resp.ok) throw new InvalidOperationException(resp.body);
        return resp.body;
    }

    private static async Task<(bool ok, string body)> SendAsync(string method, string path, string? jsonBody)
    {
        using var http = new HttpClient();
        var req = new HttpRequestMessage(new HttpMethod(method), Session.BaseUrl + path);
        if (!string.IsNullOrEmpty(Session.Token)) req.Headers.Add("Authorization", "Bearer " + Session.Token);
        if (jsonBody is not null)
        {
            var content = new ByteArrayContent(Text.AsciiBytes(jsonBody));
            content.Headers.Add("Content-Type", "application/json");
            req.Content = content;
        }
        var resp = await http.SendAsync(req);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        return (resp.IsSuccessStatusCode, Text.Utf8Decode(bytes));
    }
}