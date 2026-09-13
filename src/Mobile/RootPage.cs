using System.Text;
namespace ERPSystem.Mobile;

/// <summary>
/// الصفحة الجذرية: كل الشاشات تُبنى C# وتُتبدَّل عبر Content (نمط بسيط وموثوق).
/// </summary>
public class RootPage : ContentPage
{
    public static RootPage? Instance = null;

    private Entry _email;
    private Entry _password;
    private Label _status;
    private List<WarehouseDto> _whs = new List<WarehouseDto>();
    private int _whIndex = 0;
    private List<CustomerDto> _custs = new List<CustomerDto>();
    private int _custIndex = 0;
    private List<ItemDto> _items = new List<ItemDto>();
    private List<ItemDto> _itemHits = new List<ItemDto>();
    private List<ItemDto> _results0 = new List<ItemDto>();
    private int _itemIndex = -1;
    private Label _whLbl;
    private Label _cuLbl;
    private Label _itLbl;
    private Button _whBtn;
    private Button _cuBtn;
    private Button _itBtn;
    private Entry _invPhotoPath;
    private Entry _invoiceItemSearch;
    private Label _payLbl;
    private int _payMethod = 1;
    private Entry _invoiceNote;
    private Entry _sPhotoPath;
    private Entry _qty;
    private Entry _price;
    private Entry _search;
    private Label _itemsResult;
    private Label _invoiceResult;
    private Entry _custCustomer;
    private Entry _custAmount;
    private Entry _custNote;
    private Label _custResult;
    private Entry _sItem;
    private Entry _sLocation;
    private Entry _sQty;
    private Entry _sNotes;
    private Label _sResult;
    private Entry _pItem;
    private Entry _pCompetitor;
    private Entry _pPrice;
    private Label _pResult;
    private Label _eResult;
    private ScrollView _itemsScroll;
    private ActivityIndicator _spin;
    private List<InvoiceLineDto> _lines = new List<InvoiceLineDto>();
    private List<string> _linesNames = new List<string>();
    private Label _linesLbl;
    private Label _subTotalLbl;
    private Entry _rInvoiceNo;
    private Label _rResult;
    private ReturnLookupDto _rLookup;
    private List<ReturnLookupLineDto> _rLines = new List<ReturnLookupLineDto>();
    private List<Entry> _rQtys = new List<Entry>();

    public RootPage()
    {
        Instance = this;
        ShowLogin();
    }

    private void Show(ScrollView view)
    {
        Content = view;
    }

    public void ShowLogin()
    {
        var stack = new VerticalStackLayout();
        var title = new Label(); title.Text = "ERP الميداني"; title.FontSize = 30; title.TextColor = Theme.Primary;
        var subtitle = new Label(); subtitle.Text = "تسجيل الدخول";
        _email = NewEntry("البريد الإلكتروني");
        _password = NewEntry("كلمة المرور"); _password.IsPassword = true;
        var login = NewButton("دخول"); login.Clicked += (sender, e) => OnLoginClicked();
        _status = new Label(); _status.Text = "";
        var footer = new Label(); footer.Text = "v1.2 field app · " + Session.BaseUrl;
        stack.Add(title); stack.Add(subtitle); stack.Add(_email); stack.Add(_password); stack.Add(login); stack.Add(_status); stack.Add(footer);
        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
    }

    public void ShowHome()
    {
        var stack = new VerticalStackLayout();
        var title = new Label(); title.Text = "الرئيسية"; title.FontSize = 22; title.TextColor = Theme.Primary;
        var userLbl = new Label(); userLbl.Text = "المستخدم: " + (string.IsNullOrEmpty(Session.UserEmail) ? "-" : Session.UserEmail);
        var lastSync = LocalStore.LastSync();
        var syncLbl = new Label();
        syncLbl.Text = "آخر مزامنة: " + (string.IsNullOrEmpty(lastSync) ? "—" : lastSync);
        var pendingLbl = new Label();
        pendingLbl.Text = LocalStore.CountPending() + " طلب بانتظار الإرسال";
        stack.Add(title); stack.Add(userLbl); stack.Add(syncLbl); stack.Add(pendingLbl);
        var b1 = NewButton("الأصناف والبحث"); b1.Clicked += (sender, e) => OnItemsClicked();
        var b2 = NewButton("فاتورة جديدة"); b2.Clicked += (sender, e) => OnInvoiceClicked();
        var b3 = NewButton("تسجيل الخروج"); b3.Clicked += (sender, e) => OnLogoutClicked();
        var b4 = NewButton("تحصيل نقدي في العهدة"); b4.Clicked += (sender, e) => OnCustodyClicked();
        var b5 = NewButton("مردود مبيعات"); b5.Clicked += (sender, e) => ShowReturn();
        stack.Add(b1); stack.Add(b2); stack.Add(b4); stack.Add(b5); stack.Add(b3);
        if (Session.RolesCsv.Contains("Merchandiser"))
        {
            var m1 = NewButton("فحص رف (منسّق)"); m1.Clicked += (sender, e) => ShowShelf();
            var m2 = NewButton("رصد أسعار المنافسين"); m2.Clicked += (sender, e) => ShowPriceCapture();
            var m3 = NewButton("تحذيرات الانتهاء"); m3.Clicked += (sender, e) => OnWarningsClicked();
            stack.Add(m1); stack.Add(m2); stack.Add(m3);
        }
        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
    }

    public void ShowItems()
    {
        var stack = new VerticalStackLayout();
        var back = NewButton("← رجوع"); back.Clicked += (sender, e) => OnBackClicked();
        var title = new Label(); title.Text = "الأصناف"; title.FontSize = 20; title.TextColor = Theme.Primary;
        _search = NewEntry("بحث بالاسم أو الكود");
        var find = NewButton("بحث / تحميل الكل"); find.Clicked += (sender, e) => OnSearchClicked();
        _itemsResult = new Label(); _itemsResult.Text = "";
        _spin = new ActivityIndicator(); _spin.Color = Theme.Primary; _spin.IsRunning = false;
        stack.Add(back); stack.Add(title); stack.Add(_search); stack.Add(find); stack.Add(_itemsResult); stack.Add(_spin);
        var sv = new ScrollView(); sv.Content = stack;
        _itemsScroll = sv;
        Show(sv);
    }

public void ShowInvoice()
    {
        var stack = new VerticalStackLayout();
        var back = NewButton("← رجوع"); back.Clicked += (sender, e) => OnBackClicked();
        var title = new Label(); title.Text = "فاتورة مبيعات جديدة"; title.FontSize = 20; title.TextColor = Theme.Primary;

        _whLbl = new Label(); _whLbl.Text = "المستودع: —";
        _cuLbl = new Label(); _cuLbl.Text = "العميل: —";
        _itLbl = new Label(); _itLbl.Text = "الصنف: — (ابحث أولاً)";
        _whBtn = NewButton("تبديل المستودع"); _whBtn.Clicked += (sender, e) => CycleWarehouse();
        _cuBtn = NewButton("تبديل العميل"); _cuBtn.Clicked += (sender, e) => CycleCustomer();
        _itBtn = NewButton("اختيار من نتائج البحث"); _itBtn.Clicked += (sender, e) => CycleItem();

        _invoiceItemSearch = NewEntry("بحث صنف: كود/اسم");
        var find = NewButton("بحث"); find.Clicked += (sender, e) => OnItemSearchClicked();

        var hQty = new Label(); hQty.Text = "الكمية";
        _qty = NewEntry("الكمية"); _qty.Text = "1";
        var hPrice = new Label(); hPrice.Text = "سعر الوحدة (يعبأ تلقائياً)";
        _price = NewEntry("سعر الوحدة");
        var addLine = NewButton("➕ إضافة للفاتورة"); addLine.Clicked += (sender, e) => OnAddLineClicked();
        _linesLbl = new Label(); _linesLbl.Text = "الأصناف المضافة: لا شيء";
        _subTotalLbl = new Label(); _subTotalLbl.Text = "المجموع: 0";
        var removeLast = NewButton("حذف آخر سطر"); removeLast.Clicked += (sender, e) => OnRemoveLastClicked();
        _payLbl = new Label(); _payLbl.Text = "طريقة الدفع: نقدي";
        var payBtn = NewButton("تبديل طريقة الدفع"); payBtn.Clicked += (sender, e) => CyclePayment();
        var hNote = new Label(); hNote.Text = "ملاحظة (اختيارية)";
        _invPhotoPath = NewEntry("صورة النسخة الورقية (مسار ملف اختياري)");
        _invoiceNote = NewEntry("ملاحظة الفاتورة");

        var create = NewButton("إنشاء الفاتورة"); create.Clicked += (sender, e) => OnCreateClicked();
        _invoiceResult = new Label(); _invoiceResult.Text = "ُيُحمَّل المستودعات والعملاء والأصناف…";
        _spin = new ActivityIndicator(); _spin.Color = Theme.Primary; _spin.IsRunning = false;
        _lines = new List<InvoiceLineDto>();
        _linesNames = new List<string>();

        stack.Add(back); stack.Add(title);
        stack.Add(_whLbl); stack.Add(_whBtn);
        stack.Add(_cuLbl); stack.Add(_cuBtn);
        stack.Add(_invoiceItemSearch); stack.Add(find);
        stack.Add(_itLbl); stack.Add(_itBtn);
        stack.Add(hQty); stack.Add(_qty); stack.Add(hPrice); stack.Add(_price); stack.Add(addLine);
        stack.Add(_linesLbl); stack.Add(_subTotalLbl); stack.Add(removeLast);
        stack.Add(_payLbl); stack.Add(payBtn);
        stack.Add(hNote); stack.Add(_invoiceNote);
        stack.Add(_invPhotoPath);
        stack.Add(create); stack.Add(_invoiceResult); stack.Add(_spin);

        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
        PreparePicklistsAsync();
    }

    private void PreparePicklistsAsync()
    {
        var t = new Thread(() =>
        {
            try
            {
                var items = ApiClient.ItemsSync("");
                var customers = ApiClient.CustomersSync("");
                var whs = ApiClient.WarehousesSync();
                LocalStore.CacheAll(items, customers, whs);
                ApplyPicklists(items, customers, whs);
            }
            catch
            {
                // دون اتصال: من الكاش المحلي
                ApplyPicklists(LocalStore.CachedItems(), LocalStore.CachedCustomers(), LocalStore.CachedWarehouses());
            }
        });
        t.Start();
    }

    private void ApplyPicklists(List<ItemDto> items, List<CustomerDto> customers, List<WarehouseDto> whs)
    {
        _items = items;
        _custs = customers;
        _whs = whs;
        _results0 = items;
        _itemHits = items;
        _itemIndex = -1;
        _whIndex = _whs.Count > 0 ? 0 : -1;
        _custIndex = _custs.Count > 0 ? 0 : -1;
        Device.BeginInvokeOnMainThread(() =>
        {
            _whBtn.Text = _whs.Count > 0 ? "تبديل المستودع" : "لا مستودعات";
            _cuBtn.Text = _custs.Count > 0 ? "تبديل العميل" : "لا عملاء";
            if (_whs.Count > 0) _whLbl.Text = "المستودع: " + _whs[0].NameAr + " (" + _whs[0].Code + ")";
            if (_custs.Count > 0) _cuLbl.Text = "العميل: " + _custs[0].NameAr + " (" + _custs[0].Code + ")";
            if (_itemIndex >= 0) { _itLbl.Text = "الصنف: " + _itemHits[_itemIndex].NameAr; _price.Text = _itemHits[_itemIndex].SalePrice; }
            _invoiceResult.Text = _whs.Count + " مستودع · " + _custs.Count + " عميل · " + _items.Count + " صنف — حدد ثم أنشئ";
        });
    }

    private void CycleWarehouse()
    {
        if (_whIndex < 0 && _whs.Count > 0) _whIndex = 0;
        if (_whIndex < 0) return;
        _whIndex = (_whIndex + 1) % _whs.Count;
        _whLbl.Text = "المستودع: " + _whs[_whIndex].NameAr + " (" + _whs[_whIndex].Code + ")";
    }

    private void CycleCustomer()
    {
        if (_custIndex < 0 && _custs.Count > 0) _custIndex = 0;
        if (_custIndex < 0) return;
        _custIndex = (_custIndex + 1) % _custs.Count;
        _cuLbl.Text = "العميل: " + _custs[_custIndex].NameAr + " (" + _custs[_custIndex].Code + ")";
    }

    private void OnItemSearchClicked()
    {
        var term = Text.EmptyIfNull(_invoiceItemSearch.Text).Trim().ToLower();
        var hits = new List<ItemDto>();
        foreach (var it in _items)
        {
            if (term.Length == 0
                || it.Code.ToLower().Contains(term)
                || it.NameAr.ToLower().Contains(term)
                || (it.NameEn is not null && it.NameEn.ToLower().Contains(term)))
                hits.Add(it);
            if (hits.Count >= 50) break;
        }
        _itemHits = hits;
        _itemIndex = hits.Count > 0 ? 0 : -1;
        Device.BeginInvokeOnMainThread(() =>
        {
            _itLbl.Text = _itemIndex >= 0 ? "الصنف: " + hits[_itemIndex].NameAr : "لا نتائج — جرّب كوداً أو اسماً";
            _price.Text = _itemIndex >= 0 ? hits[_itemIndex].SalePrice : "";
        });
    }

    private void CycleItem()
    {
        if (_itemIndex < 0 && _itemHits.Count > 0) _itemIndex = 0;
        if (_itemIndex < 0) return;
        _itemIndex = (_itemIndex + 1) % _itemHits.Count;
        _itLbl.Text = "الصنف: " + _itemHits[_itemIndex].NameAr;
        _price.Text = _itemHits[_itemIndex].SalePrice;
    }

    // ── سلة الفاتورة: أسطر متعددة ──
    private void OnAddLineClicked()
    {
        if (_itemIndex < 0 || _itemIndex >= _itemHits.Count)
        {
            _invoiceResult.Text = "اختر صنفاً من نتائج البحث أولاً"; _invoiceResult.TextColor = Theme.Danger;
            return;
        }
        var item = _itemHits[_itemIndex];
        var qty = Text.EmptyIfNull(_qty.Text).Trim();
        var price = Text.EmptyIfNull(_price.Text).Trim();
        if (qty.Length == 0 || price.Length == 0)
        {
            _invoiceResult.Text = "أدخل الكمية والسعر"; _invoiceResult.TextColor = Theme.Danger;
            return;
        }
        foreach (var ln in _lines)
        {
            if (ln.ItemId == item.Id)
            {
                _invoiceResult.Text = "الصنف مضاف مسبقاً في الفاتورة — الخادم يمنع التكرار"; _invoiceResult.TextColor = Theme.Danger;
                return;
            }
        }
        _lines.Add(new InvoiceLineDto(item.Id, qty, price));
        _linesNames.Add(item.NameAr + " ×" + qty);
        RefreshCart();
    }

    private void OnRemoveLastClicked()
    {
        if (_lines.Count == 0)
        {
            _invoiceResult.Text = "لا أسطر للحذف"; _invoiceResult.TextColor = Theme.Danger;
            return;
        }
        _lines.RemoveAt(_lines.Count - 1);
        _linesNames.RemoveAt(_linesNames.Count - 1);
        RefreshCart();
    }

    private void RefreshCart()
    {
        var sb = new StringBuilder();
        if (_lines.Count == 0) sb.Append("لا شيء");
        else
            for (var i = 0; i < _lines.Count; i++)
                sb.Append(i + 1).Append(". ").Append(_linesNames[i]).Append(" — سعر ").Append(_lines[i].UnitPrice).Append('\n');
        _linesLbl.Text = "الأصناف المضافة:\n" + sb.ToString();
        var total = 0.0;
        for (var i = 0; i < _lines.Count; i++)
            total += double.Parse(_lines[i].UnitPrice) * double.Parse(_lines[i].Quantity);
        _subTotalLbl.Text = "المجموع: " + (Math.Round(total * 100.0) / 100.0);
        _invoiceResult.Text = _lines.Count + " سطر في الفاتورة — أضف المزيد أو أنشئ"; _invoiceResult.TextColor = Theme.Ink;
    }

    private void ResetCartUi()
    {
        _lines = new List<InvoiceLineDto>();
        _linesNames = new List<string>();
        _linesLbl.Text = "الأصناف المضافة: لا شيء";
        _subTotalLbl.Text = "المجموع: 0";
    }

    private Entry NewEntry(string placeholder)
    {
        var e = new Entry();
        e.Placeholder = placeholder;
        return e;
    }

    private void Status(Label lbl, string text, bool ok)
    {
        lbl.Text = text;
        lbl.TextColor = ok ? Theme.Success : Theme.Danger;
    }

    private Button NewButton(string text)
    {
        var b = new Button();
        b.Text = text;
        b.FontSize = 16;
        b.TextColor = Theme.Ink;
        b.CornerRadius = 12;
        b.BorderWidth = 2;
        b.BorderColor = Theme.Primary;
        return b;
    }

    // ── المعالجات ──
    private void OnLoginClicked()
    {
        var email = Text.EmptyIfNull(_email.Text).Trim();
        var password = Text.EmptyIfNull(_password.Text);
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) { _status.Text = "أدخل البريد وكلمة المرور"; _status.TextColor = Theme.Danger; return; }

        _status.Text = "جارٍ الدخول…";
        var t = new Thread(() =>
        {
            try
            {
                var resp = ApiClient.LoginSync(email, password);
                var ok = !string.IsNullOrEmpty(resp.Token);
                if (ok)
                {
                    var token = resp.Token;
                    var userEmail = resp.UserEmail;
                    var rolesCsv = resp.RolesCsv;
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Session.Token = token;
                        Session.UserEmail = userEmail;
                        Session.RolesCsv = rolesCsv;
                        ShowHome();
                    });
                    var sy = new Thread(() => { try { SyncEngine.RunOnce(); } catch { } });
                    sy.Start();
                }
                else
                {
                    Device.BeginInvokeOnMainThread(() => { _status.Text = "فشل الدخول: استجابة غير متوقعة"; _status.TextColor = Theme.Danger; });
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _status.Text = "فشل الدخول: " + msg; _status.TextColor = Theme.Danger; });
            }
        });
        t.Start();
    }

    public void ShowCustody()
    {
        var stack = new VerticalStackLayout();
        var back = NewButton("← رجوع"); back.Clicked += (sender, e) => OnBackClicked();
        var title = new Label(); title.Text = "تحصيل نقدي في العهدة"; title.FontSize = 20;
        var hCu = new Label(); hCu.Text = "معرف العميل (المندوب الحالي هو الحامل)";
        _custCustomer = NewEntry("معرف العميل");
        _custAmount = NewEntry("المبلغ");
        _custNote = NewEntry("ملاحظة");
        var submit = NewButton("تسجيل التحصيل"); submit.Clicked += (sender, e) => OnCustodyCollectClicked();
        _custResult = new Label(); _custResult.Text = "";
        stack.Add(back); stack.Add(title); stack.Add(hCu); stack.Add(_custCustomer);
        stack.Add(_custAmount); stack.Add(_custNote); stack.Add(submit); stack.Add(_custResult);
        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
    }

    private void OnCustodyClicked() => ShowCustody();

    private void OnCustodyCollectClicked()
    {
        var customerId = Text.EmptyIfNull(_custCustomer.Text).Trim();
        var amount = Text.EmptyIfNull(_custAmount.Text).Trim();
        var note = Text.EmptyIfNull(_custNote.Text).Trim();
        if (string.IsNullOrEmpty(amount)) { _custResult.Text = "أدخل المبلغ"; return; }
        _custResult.Text = "جارٍ التسجيل…";
        var t = new Thread(() =>
        {
            try
            {
                var entryNumber = ApiClient.CollectCustodySync(customerId, amount, note);
                Device.BeginInvokeOnMainThread(() => { _custResult.Text = "تم تسجيل التحصيل ✓ (قيد " + entryNumber + ")"; });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _custResult.Text = "فشل: " + msg; });
            }
        });
        t.Start();
    }

    public void ShowShelf()
    {
        var stack = new VerticalStackLayout();
        var back = NewButton("← رجوع"); back.Clicked += (sender, e) => OnBackClicked();
        var title = new Label(); title.Text = "فحص رف"; title.FontSize = 20;
        var h1 = new Label(); h1.Text = "معرف الصنف + الموقع + الكمية الملاحَظة";
        _sItem = NewEntry("معرف الصنف");
        _sLocation = NewEntry("الموقع/المتجر");
        _sQty = NewEntry("الكمية الملاحَظة");
        _sNotes = NewEntry("ملاحظة");
        var submit = NewButton("تسجيل الفحص"); submit.Clicked += (sender, e) => OnShelfSubmitClicked();
        var ph = new Label(); ph.Text = "صورة فحص (مسار ملف على الجهاز اختياري)";
        _sPhotoPath = NewEntry("مثال: D:/photo.jpg");
        _sResult = new Label(); _sResult.Text = "";
        stack.Add(back); stack.Add(title); stack.Add(h1); stack.Add(_sItem);
        stack.Add(_sLocation); stack.Add(_sQty); stack.Add(_sNotes); stack.Add(ph); stack.Add(_sPhotoPath); stack.Add(submit); stack.Add(_sResult);
        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
    }

    public void OnShelfSubmitClicked()
    {
        var itemId = Text.EmptyIfNull(_sItem.Text).Trim();
        var location = Text.EmptyIfNull(_sLocation.Text).Trim();
        var qty = Text.EmptyIfNull(_sQty.Text).Trim();
        var notes = Text.EmptyIfNull(_sNotes.Text).Trim();
        if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(qty)) { _sResult.Text = "أدخل الصنف والكمية"; return; }
        _sResult.Text = "جارٍ الحفظ…";
        var t = new Thread(() =>
        {
            try
            {
                var photoPath = Text.EmptyIfNull(_sPhotoPath.Text).Trim();
                var photoB64 = photoPath.Length > 0 ? Convert.ToBase64String(Text.ReadBinaryFile(photoPath)) : "";
                var id = ApiClient.CreateShelfCheckSync(itemId, location, qty, notes, photoB64);
                Device.BeginInvokeOnMainThread(() => { _sResult.Text = "تم تسجيل الفحص ✓ (id " + id + ")"; });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _sResult.Text = "فشل: " + msg; });
            }
        });
        t.Start();
    }

    public void ShowPriceCapture()
    {
        var stack = new VerticalStackLayout();
        var back = NewButton("← رجوع"); back.Clicked += (sender, e) => OnBackClicked();
        var title = new Label(); title.Text = "رصد سعر منافس"; title.FontSize = 20;
        _pItem = NewEntry("معرف الصنف");
        _pCompetitor = NewEntry("اسم المنافس");
        _pPrice = NewEntry("السعر");
        var submit = NewButton("حفظ الرصد"); submit.Clicked += (sender, e) => OnPriceSubmitClicked();
        _pResult = new Label(); _pResult.Text = "";
        stack.Add(back); stack.Add(title); stack.Add(_pItem); stack.Add(_pCompetitor);
        stack.Add(_pPrice); stack.Add(submit); stack.Add(_pResult);
        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
    }

    public void OnPriceSubmitClicked()
    {
        var itemId = Text.EmptyIfNull(_pItem.Text).Trim();
        var competitor = Text.EmptyIfNull(_pCompetitor.Text).Trim();
        var price = Text.EmptyIfNull(_pPrice.Text).Trim();
        if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(price)) { _pResult.Text = "أدخل الصنف والسعر"; return; }
        _pResult.Text = "جارٍ الحفظ…";
        var t = new Thread(() =>
        {
            try
            {
                var id = ApiClient.CreatePriceCaptureSync(itemId, competitor, price);
                Device.BeginInvokeOnMainThread(() => { _pResult.Text = "تم الحفظ ✓ (id " + id + ")"; });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _pResult.Text = "فشل: " + msg; });
            }
        });
        t.Start();
    }

    public void OnWarningsClicked()
    {
        var t = new Thread(() =>
        {
            try
            {
                var json = ApiClient.ExpiryWarningsSync();
                var text = json.Replace("},{", "}\n{").Replace("\\\"", "\"");
                Device.BeginInvokeOnMainThread(() => ShowText("تحذيرات الانتهاء", text));
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => ShowText("تحذيرات الانتهاء", "فشل: " + msg));
            }
        });
        t.Start();
    }

    public void ShowReturn()
    {
        var stack = new VerticalStackLayout();
        var back = NewButton("← رجوع"); back.Clicked += (sender, e) => OnBackClicked();
        var title = new Label(); title.Text = "مردود مبيعات"; title.FontSize = 20; title.TextColor = Theme.Primary;
        var hint = new Label(); hint.Text = "أدخل رقم الفاتورة الأصلية ليبحث عنها النظام (يتطلب اتصالاً)";
        _rInvoiceNo = NewEntry("مثال: SI-20260913-0005");
        var load = NewButton("تحميل الفاتورة"); load.Clicked += (sender, e) => OnReturnLoadClicked();
        _rResult = new Label(); _rResult.Text = "";
        _spin = new ActivityIndicator(); _spin.Color = Theme.Primary; _spin.IsRunning = false;
        stack.Add(back); stack.Add(title); stack.Add(hint); stack.Add(_rInvoiceNo);
        stack.Add(load); stack.Add(_rResult); stack.Add(_spin);
        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
    }

    private void OnReturnLoadClicked()
    {
        var no = Text.EmptyIfNull(_rInvoiceNo.Text).Trim();
        if (no.Length == 0) { _rResult.Text = "أدخل رقم الفاتورة"; _rResult.TextColor = Theme.Danger; return; }
        if (!SyncEngine.IsOnline()) { _rResult.Text = "يتطلب المردود اتصالاً بالخادم"; _rResult.TextColor = Theme.Danger; return; }
        _rResult.Text = "جارٍ البحث…"; _rResult.TextColor = Theme.Ink;
        _spin.IsRunning = true;
        var t = new Thread(() =>
        {
            try
            {
                var lookup = ApiClient.ReturnLookupSync(no);
                Device.BeginInvokeOnMainThread(() => { _spin.IsRunning = false; ShowReturnEditor(lookup); });
            }
            catch (Exception ex)
            {
                var err = JsonLite.Field(ex.Message, "error");
                var msg = err.Length > 0 ? JsonLite.Unescape(err) : ex.Message;
                Device.BeginInvokeOnMainThread(() => { _rResult.Text = "فشل: " + msg; _rResult.TextColor = Theme.Danger; _spin.IsRunning = false; });
            }
        });
        t.Start();
    }

    private void ShowReturnEditor(ReturnLookupDto lk)
    {
        _rLookup = lk;
        _rLines = lk.Lines;
        _rQtys = new List<Entry>();
        var stack = new VerticalStackLayout();
        var back = NewButton("← رجوع"); back.Clicked += (sender, e) => OnBackClicked();
        var title = new Label(); title.Text = "مردود " + lk.InvoiceNumber; title.FontSize = 20; title.TextColor = Theme.Primary;
        var info = new Label(); info.Text = "العميل: " + lk.CustomerName + " · المخزن: " + lk.WarehouseName;
        stack.Add(back); stack.Add(title); stack.Add(info);
        if (lk.Lines.Count == 0)
        {
            var none = new Label(); none.Text = "لا بنود قابلة للرد في هذه الفاتورة"; none.TextColor = Theme.Danger;
            stack.Add(none);
        }
        else
        {
            var head = new Label(); head.Text = "أدخل كمية الرد لكل بند (اترك 0 لتخطيه):";
            stack.Add(head);
            foreach (var line in lk.Lines)
            {
                var lbl = new Label();
                lbl.Text = line.Code + " · " + line.NameAr + " — مباع " + line.Quantity + " · باقي " + line.Remaining + " · سعر " + line.UnitPrice;
                var qty = NewEntry("كمية الرد (0 للتخطي)");
                qty.Text = "0";
                _rQtys.Add(qty);
                stack.Add(lbl); stack.Add(qty);
            }
            var submit = NewButton("تسجيل المردود"); submit.Clicked += (sender, e) => OnReturnSubmitClicked();
            stack.Add(submit);
        }
        _rResult = new Label(); _rResult.Text = "";
        stack.Add(_rResult);
        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
    }

    private void OnReturnSubmitClicked()
    {
        var lines = new List<CreateReturnLineDto>();
        for (var i = 0; i < _rLines.Count; i++)
        {
            var q = Text.EmptyIfNull(_rQtys[i].Text).Trim();
            if (q.Length == 0 || q == "0") continue;
            lines.Add(new CreateReturnLineDto(_rLines[i].ItemId, q));
        }
        if (lines.Count == 0) { _rResult.Text = "حدد كمية رد على الأقل"; _rResult.TextColor = Theme.Danger; return; }
        _rResult.Text = "جارٍ التسجيل…"; _rResult.TextColor = Theme.Ink;
        var t = new Thread(() =>
        {
            try
            {
                var r = ApiClient.CreateSalesReturnSync(_rLookup.InvoiceId, _rLookup.WarehouseId, lines, "Mobile");
                Device.BeginInvokeOnMainThread(() =>
                {
                    _rResult.Text = "تم المردود ✓ " + r.ReturnNumber + " (" + lines.Count + " أصناف — الإجمالي " + r.TotalAmount + ")";
                    _rResult.TextColor = Theme.Success;
                });
            }
            catch (Exception ex)
            {
                var err = JsonLite.Field(ex.Message, "error");
                var msg = err.Length > 0 ? JsonLite.Unescape(err) : ex.Message;
                Device.BeginInvokeOnMainThread(() => { _rResult.Text = "فشل: " + msg; _rResult.TextColor = Theme.Danger; });
            }
        });
        t.Start();
    }

    private void ShowText(string titleText, string body)
    {
        var stack = new VerticalStackLayout();
        var back = NewButton("← رجوع"); back.Clicked += (sender, e) => OnBackClicked();
        var title = new Label(); title.Text = titleText; title.FontSize = 20;
        var bodyLbl = new Label(); bodyLbl.Text = body;
        stack.Add(back); stack.Add(title); stack.Add(bodyLbl);
        var sv = new ScrollView(); sv.Content = stack;
        Show(sv);
    }

private void OnItemsClicked() => ShowItems();
    private void OnInvoiceClicked() => ShowInvoice();
    private void OnBackClicked() => ShowHome();

    private void OnLogoutClicked()
    {
        Session.Logout();
        ShowLogin();
    }

    private void OnSearchClicked()
    {
        var term = Text.EmptyIfNull(_search.Text);
        _itemsResult.Text = "جارٍ التحميل…"; _itemsResult.TextColor = Theme.Ink;
        _spin.IsRunning = true;
        var t = new Thread(() =>
        {
            try
            {
                var items = ApiClient.ItemsSync(term);
                var cards = new VerticalStackLayout();
                var count = new Label(); count.Text = "عدد النتائج: " + items.Count; count.FontSize = 15; count.TextColor = Theme.Primary;
                cards.Add(count);
                foreach (var it in items)
                {
                    var card = new VerticalStackLayout();
                    var name = new Label(); name.Text = it.NameAr + "  (" + it.Code + ")"; name.FontSize = 15; name.TextColor = Theme.Primary;
                    var meta = new Label(); meta.Text = "السعر " + it.SalePrice + "  ·  الكمية " + it.CurrentStock; meta.FontSize = 13;
                    var rule = Theme.Rule();
                    card.Add(name); card.Add(meta); card.Add(rule);
                    cards.Add(card);
                }
                Device.BeginInvokeOnMainThread(() =>
                {
                    _itemsScroll.Content = cards;
                    _spin.IsRunning = false;
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _itemsResult.Text = "فشل الجلب: " + msg; _itemsResult.TextColor = Theme.Danger; _spin.IsRunning = false; });
            }
        });
        t.Start();
    }

    private void CyclePayment()
    {
        _payMethod = _payMethod >= 3 ? 1 : _payMethod + 1;
        var name = _payMethod == 1 ? "نقدي" : (_payMethod == 2 ? "بطاقة" : "آجل");
        _payLbl.Text = "طريقة الدفع: " + name;
    }

    private void OnCreateClicked()
    {
        var warehouseId = _whIndex >= 0 && _whIndex < _whs.Count ? _whs[_whIndex].Id : "";
        var customerId = _custIndex >= 0 && _custIndex < _custs.Count ? _custs[_custIndex].Id : "";
        var note = Text.EmptyIfNull(_invoiceNote.Text).Trim();
        var invPhotoPath = Text.EmptyIfNull(_invPhotoPath.Text).Trim();
        var invPhotoB64 = invPhotoPath.Length > 0 ? Convert.ToBase64String(Text.ReadBinaryFile(invPhotoPath)) : "";
        if (string.IsNullOrEmpty(warehouseId) || string.IsNullOrEmpty(customerId))
        {
            _invoiceResult.Text = "اختر المستودع والعميل أولاً"; _invoiceResult.TextColor = Theme.Danger;
            return;
        }
        if (_lines.Count == 0)
        {
            _invoiceResult.Text = "أضف صنفاً واحداً على الأقل للفاتورة"; _invoiceResult.TextColor = Theme.Danger;
            return;
        }
        _invoiceResult.Text = "جارٍ الإنشاء…  " + _lines.Count + " سطر"; _invoiceResult.TextColor = Theme.Ink;
        _spin.IsRunning = true;
        var t = new Thread(() =>
        {
            try
            {
                if (SyncEngine.IsOnline())
                {
                    var inv = ApiClient.CreateInvoiceSync(_lines, customerId, warehouseId, _payMethod, note, invPhotoB64);
                    var text2 = "تم الإنشاء ✓ " + inv.InvoiceNumber + " (" + _lines.Count + " أصناف — الإجمالي " + inv.TotalAmount + ")";
                    Device.BeginInvokeOnMainThread(() => { _invoiceResult.Text = text2; _invoiceResult.TextColor = Theme.Success; ResetCartUi(); _spin.IsRunning = false; });
                }
                else
                {
                    var key2 = Guid.NewGuid().ToString();
                    var payload = ApiClient.MakeInvoicePayload(_lines, customerId, warehouseId, note, key2, _payMethod, invPhotoB64);
                    LocalStore.Enqueue(key2, payload);
                    Device.BeginInvokeOnMainThread(() => { _invoiceResult.Text = "دُوِّن دون اتصال ✓ — سيُرسل عند الاتصال (" + LocalStore.CountPending() + " بانتظار الإرسال)"; _invoiceResult.TextColor = Theme.Success; ResetCartUi(); _spin.IsRunning = false; });
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _invoiceResult.Text = "فشل الإنشاء: " + msg; _invoiceResult.TextColor = Theme.Danger; _spin.IsRunning = false; });
            }
        });
        t.Start();
    }
}