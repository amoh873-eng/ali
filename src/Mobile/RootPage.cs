using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace ERPSystem.Mobile;

/// <summary>
/// الصفحة الجذرية — كل الشاشات تُبنى C# وتُتبدَّل عبر Content.
/// التصميم: خلفية فاتحة + بطاقات بيضاء مستديرة + لوحة نيلي + شرائط حالة + أزرار تعبئة.
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
    private VerticalStackLayout _itemsScroll;
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
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Theme.Bg;
        ShowLogin();
    }

    private void Show(ScrollView view)
    {
        Content = view;
    }

    // ════ تسجيل الدخول ════
    public void ShowLogin()
    {
        var root = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(28, 44, 28, 28) };

        var brand = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Theme.Primary,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
            WidthRequest = 84,
            HeightRequest = 84,
            HorizontalOptions = LayoutOptions.Center
        };
        brand.Content = new Label
        {
            Text = "ERP",
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        root.Add(brand);

        root.Add(new Label { Text = "ERP الميداني", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Theme.Ink, HorizontalTextAlignment = TextAlignment.Center });
        root.Add(new Label { Text = "نظام المبيعات والإدارة الميداني", FontSize = 13, TextColor = Theme.Muted, HorizontalTextAlignment = TextAlignment.Center });

        var form = new VerticalStackLayout { Spacing = 6 };
        form.Add(Theme.Section("تسجيل الدخول"));
        _email = Theme.Input("example@company.com");
        _password = Theme.Input("كلمة المرور");
        _password.IsPassword = true;
        form.Add(_email);
        form.Add(_password);

        var login = Theme.Filled("دخول");
        login.Clicked += (sender, e) => OnLoginClicked();
        form.Add(login);

        _status = new Label { Text = "", FontSize = 14, TextColor = Theme.Danger, LineBreakMode = LineBreakMode.WordWrap };
        form.Add(_status);

        root.Add(Theme.CardView(form));
        root.Add(new Label { Text = "v1.2 field app · " + Session.BaseUrl, FontSize = 11, TextColor = Theme.Muted, HorizontalTextAlignment = TextAlignment.Center });

        var sv = new ScrollView { Content = root };
        Show(sv);
    }
// ════ الرئيسية ════
    public void ShowHome()
    {
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };

        var userName = string.IsNullOrEmpty(Session.UserEmail) ? "مستخدم" : Session.UserEmail;
        root.Add(new Label { Text = "أهلاً، " + userName, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Theme.Ink });

        var lastSync = LocalStore.LastSync();
        var pending = LocalStore.CountPending();
        var chips = new HorizontalStackLayout { Spacing = 8 };
        chips.Add(Theme.Chip("آخر مزامنة: " + (string.IsNullOrEmpty(lastSync) ? "—" : lastSync), Theme.PrimarySoft, Theme.Primary));
        chips.Add(Theme.Chip(pending + " بانتظار الإرسال", pending > 0 ? Theme.WarningSoft : Theme.SuccessSoft, pending > 0 ? Theme.Warning : Theme.Success));
        root.Add(chips);

        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var r = 0;
        var c = 0;
        void Add(string icon, string title, string sub, Color tint, Action onTap)
        {
            grid.Add(MakeActionCard(icon, title, sub, tint, onTap), c, r);
            c++;
            if (c == 2) { c = 0; r++; grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); }
        }

        Add("📦", "الأصناف والبحث", "استعراض المخزون والأسعار", Theme.PrimarySoft, OnItemsClicked);
        Add("🧾", "فاتورة جديدة", "فواتير متعددة الأصناف", Theme.WarningSoft, OnInvoiceClicked);
        Add("💵", "تحصيل نقدي", "في العهدة", Theme.SuccessSoft, OnCustodyClicked);
        Add("↩️", "مردود مبيعات", "إرجاع أصناف من فاتورة", Theme.PrimarySoft, ShowReturn);
        if (Session.RolesCsv.Contains("Merchandiser"))
        {
            Add("📋", "فحص رف", "الموقع والكمية", Theme.SuccessSoft, ShowShelf);
            Add("🏷️", "رصد أسعار", "أسعار المنافسين", Theme.WarningSoft, ShowPriceCapture);
            Add("⚠️", "تحذيرات الانتهاء", "تواريخ الانتهاء", Theme.DangerSoft, OnWarningsClicked);
        }
        root.Add(grid);

        var logout = Theme.Ghost("تسجيل الخروج");
        logout.TextColor = Theme.Danger;
        logout.BorderColor = Theme.Danger;
        logout.Clicked += (sender, e) => OnLogoutClicked();
        root.Add(logout);

        var sv = new ScrollView { Content = root };
        Show(sv);
    }

    private Border MakeActionCard(string icon, string title, string sub, Color tint, Action onTap)
    {
        var iconBox = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = tint,
            WidthRequest = 42,
            HeightRequest = 42,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Content = new Label { Text = icon, FontSize = 20, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
        };
        var content = new VerticalStackLayout { Spacing = 8 };
        content.Add(iconBox);
        content.Add(new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Theme.Ink });
        content.Add(new Label { Text = sub, FontSize = 12, TextColor = Theme.Muted, LineBreakMode = LineBreakMode.WordWrap });

        var card = new Border
        {
            BackgroundColor = Theme.Card,
            Stroke = Theme.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Padding = new Thickness(12),
            MinimumHeightRequest = 108,
            Content = content
        };
        var tgr = new TapGestureRecognizer();
        tgr.Tapped += (s, e) => onTap();
        card.GestureRecognizers.Add(tgr);
        return card;
    }

    // ════ أدوات ════
    private static Label FieldLabel(string text) => new Label { Text = text, FontSize = 13, TextColor = Theme.Muted };

    private Entry NewEntry(string placeholder) => Theme.Input(placeholder);

    private Button NewButton(string text) => Theme.Ghost(text);

    private void Status(Label lbl, string text, bool ok)
    {
        lbl.Text = text;
        lbl.TextColor = ok ? Theme.Success : Theme.Danger;
    }

    private Button BackButton()
    {
        var b = Theme.BackButton();
        b.Clicked += (sender, e) => OnBackClicked();
        return b;
    }
// ════ الأصناف ════
    public void ShowItems()
    {
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };

        var head = new HorizontalStackLayout { Spacing = 10 };
        head.Add(BackButton());
        head.Add(Theme.H1("الأصناف"));
        root.Add(head);

        var searchRow = new HorizontalStackLayout { Spacing = 8 };
        _search = Theme.Input("بحث بالاسم أو الكود");
        searchRow.Add(_search);
        var find = Theme.Filled("بحث / تحميل");
        find.FontSize = 14;
        find.HeightRequest = 42;
        find.Clicked += (sender, e) => OnSearchClicked();
        searchRow.Add(find);
        root.Add(searchRow);

        _itemsResult = new Label { Text = "اكتب كلمة بحث أو اضغط البحث لعرض كل المخزون", FontSize = 13, TextColor = Theme.Muted };
        root.Add(_itemsResult);

        _spin = new ActivityIndicator { Color = Theme.Primary, IsRunning = false, IsVisible = false };
        root.Add(_spin);

        _itemsScroll = new VerticalStackLayout { Spacing = 10 };
        root.Add(_itemsScroll);

        var sv = new ScrollView { Content = root };
        Show(sv);
    }

    private Border ItemCard(ItemDto it)
    {
        var nameRow = new HorizontalStackLayout { Spacing = 8 };
        nameRow.Add(Theme.Chip(it.Code, Theme.PrimarySoft, Theme.Primary));
        nameRow.Add(new Label
        {
            Text = it.NameAr,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Theme.Ink,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        });
        var hasStock = double.TryParse(it.CurrentStock, out var stk) && stk > 0;
        var stockChip = Theme.Chip(hasStock ? "متوفر (" + it.CurrentStock + ")" : "نفد",
            hasStock ? Theme.SuccessSoft : Theme.DangerSoft,
            hasStock ? Theme.Success : Theme.Danger);
        var metaRow = new HorizontalStackLayout { Spacing = 8 };
        metaRow.Add(new Label
        {
            Text = "السعر: " + it.SalePrice,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Theme.Primary,
            VerticalTextAlignment = TextAlignment.Center
        });
        metaRow.Add(stockChip);
        return Theme.CardView(nameRow, metaRow);
    }

    private void OnSearchClicked()
    {
        var term = Text.EmptyIfNull(_search.Text);
        _itemsResult.Text = "جارِ التحميل…";
        _itemsResult.TextColor = Theme.Muted;
        _spin.IsRunning = true;
        _spin.IsVisible = true;
        var t = new Thread(() =>
        {
            try
            {
                var items = ApiClient.ItemsSync(term);
                var added = new List<View>();
                added.Add(new Label { Text = items.Count + " صنف مطابق", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.Muted });
                foreach (var it in items) added.Add(ItemCard(it));
                Device.BeginInvokeOnMainThread(() =>
                {
                    _spin.IsRunning = false;
                    _spin.IsVisible = false;
                    _itemsScroll.Children.Clear();
                    foreach (var v in added) _itemsScroll.Add(v);
                    _itemsResult.Text = items.Count + " صنف";
                    _itemsResult.TextColor = Theme.Primary;
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() =>
                {
                    _spin.IsRunning = false;
                    _spin.IsVisible = false;
                    _itemsResult.Text = "فشل الجلب: " + msg;
                    _itemsResult.TextColor = Theme.Danger;
                });
            }
        });
        t.Start();
    }
// ════ الفاتورة ════
    public void ShowInvoice()
    {
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };

        var head = new HorizontalStackLayout { Spacing = 10 };
        head.Add(BackButton());
        head.Add(Theme.H1("فاتورة مبيعات"));
        root.Add(head);
        root.Add(Theme.P("حدد المستودع والعميل، أضف الأصناف ثم أنشئ الفاتورة"));

        _whLbl = new Label { Text = "المستودع: —", FontSize = 14, TextColor = Theme.Ink, VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.TailTruncation };
        _cuLbl = new Label { Text = "العميل: —", FontSize = 14, TextColor = Theme.Ink, VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.TailTruncation };
        _whBtn = Theme.Ghost("تبديل");
        _whBtn.Clicked += (sender, e) => CycleWarehouse();
        _cuBtn = Theme.Ghost("تبديل");
        _cuBtn.Clicked += (sender, e) => CycleCustomer();
        var whV = new VerticalStackLayout { Spacing = 6 };
        whV.Add(FieldLabel("المستودع"));
        whV.Add(_whLbl);
        whV.Add(_whBtn);
        var cuV = new VerticalStackLayout { Spacing = 6 };
        cuV.Add(FieldLabel("العميل"));
        cuV.Add(_cuLbl);
        cuV.Add(_cuBtn);
        var pickGrid = new Grid { ColumnSpacing = 12 };
        pickGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        pickGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        pickGrid.Add(whV, 0, 0);
        pickGrid.Add(cuV, 1, 0);
        root.Add(Theme.CardView(Theme.H2("الفاتورة"), pickGrid));

        var itCard = new VerticalStackLayout { Spacing = 10 };
        itCard.Add(FieldLabel("بحث عن صنف بالكود أو الاسم"));
        var searchRow = new HorizontalStackLayout { Spacing = 8 };
        _invoiceItemSearch = Theme.Input("كود أو اسم");
        searchRow.Add(_invoiceItemSearch);
        var searchBtn = Theme.Filled("بحث");
        searchBtn.FontSize = 14;
        searchBtn.HeightRequest = 42;
        searchBtn.Clicked += (sender, e) => OnItemSearchClicked();
        searchRow.Add(searchBtn);
        itCard.Add(searchRow);
        var itRow = new HorizontalStackLayout { Spacing = 8 };
        _itLbl = new Label { Text = "الصنف: — (ابحث أولاً)", FontSize = 14, TextColor = Theme.Ink, VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.TailTruncation };
        itRow.Add(_itLbl);
        _itBtn = Theme.Ghost("اختيار");
        _itBtn.Clicked += (sender, e) => CycleItem();
        itRow.Add(_itBtn);
        itCard.Add(itRow);
        root.Add(Theme.CardView(Theme.H2("الصنف"), itCard));

        var qv = new VerticalStackLayout { Spacing = 6 };
        qv.Add(FieldLabel("الكمية"));
        _qty = Theme.Input("الكمية", "1");
        qv.Add(_qty);
        var pv = new VerticalStackLayout { Spacing = 6 };
        pv.Add(FieldLabel("سعر الوحدة"));
        _price = Theme.Input("سعر الوحدة");
        pv.Add(_price);
        var qtyGrid = new Grid { ColumnSpacing = 12 };
        qtyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        qtyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        qtyGrid.Add(qv, 0, 0);
        qtyGrid.Add(pv, 1, 0);
        var addLine = Theme.Filled("إضافة للفاتورة");
        addLine.Clicked += (sender, e) => OnAddLineClicked();
        root.Add(Theme.CardView(Theme.H2("الكمية والسعر"), qtyGrid, addLine));

        _lines = new List<InvoiceLineDto>();
        _linesNames = new List<string>();
        _linesLbl = new Label { Text = "لا أصناف بعد", FontSize = 14, TextColor = Theme.Muted, LineBreakMode = LineBreakMode.WordWrap };
        _subTotalLbl = new Label { Text = "المجموع: 0", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Theme.Primary };
        var removeLast = Theme.Ghost("حذف آخر سطر");
        removeLast.TextColor = Theme.Danger;
        removeLast.BorderColor = Theme.Danger;
        removeLast.Clicked += (sender, e) => OnRemoveLastClicked();
        root.Add(Theme.CardView(Theme.H2("أصناف الفاتورة"), _linesLbl, _subTotalLbl, removeLast));

        _payLbl = new Label { Text = "طريقة الدفع: نقدي", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Theme.Primary, VerticalTextAlignment = TextAlignment.Center };
        var payBtn = Theme.Ghost("تبديل طريقة الدفع");
        payBtn.Clicked += (sender, e) => CyclePayment();
        var payRow = new HorizontalStackLayout { Spacing = 8 };
        payRow.Add(_payLbl);
        payRow.Add(payBtn);
        _invoiceNote = Theme.Input("ملاحظة الفاتورة (اختيارية)");
        _invPhotoPath = Theme.Input("صورة النسخة الورقية — مسار ملف (اختياري)");
        root.Add(Theme.CardView(Theme.H2("الدفع والملاحظات"), payRow, _invoiceNote, _invPhotoPath));

        var create = Theme.Filled("إنشاء الفاتورة");
        create.Clicked += (sender, e) => OnCreateClicked();
        root.Add(create);

        _invoiceResult = new Label { Text = "يُحمَّل المستودعات والعملاء والأصناف…", FontSize = 14, TextColor = Theme.Muted };
        root.Add(_invoiceResult);

        _spin = new ActivityIndicator { Color = Theme.Primary, IsRunning = false, IsVisible = false };
        root.Add(_spin);

        var sv = new ScrollView { Content = root };
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
            _whBtn.Text = _whs.Count > 0 ? "تبديل" : "لا مستودعات";
            _cuBtn.Text = _custs.Count > 0 ? "تبديل" : "لا عملاء";
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
        _linesLbl.Text = sb.ToString();
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
        _linesLbl.Text = "لا أصناف بعد";
        _subTotalLbl.Text = "المجموع: 0";
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
// ════ التحصيل النقدي ════
    public void ShowCustody()
    {
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };
        var head = new HorizontalStackLayout { Spacing = 10 };
        head.Add(BackButton());
        head.Add(Theme.H1("تحصيل نقدي"));
        root.Add(head);
        root.Add(Theme.P("يُسجَّل مبلغ محصَّل من العميل في عهدة المندوب الحالي (حساب 1110)"));

        _custCustomer = Theme.Input("معرف العميل");
        _custAmount = Theme.Input("المبلغ");
        _custNote = Theme.Input("ملاحظة (اختيارية)");
        var submit = Theme.Filled("تسجيل التحصيل");
        submit.Clicked += (sender, e) => OnCustodyCollectClicked();
        _custResult = new Label { Text = "", FontSize = 14, TextColor = Theme.Muted, LineBreakMode = LineBreakMode.WordWrap };

        root.Add(Theme.CardView(
            FieldLabel("معرف العميل"),
            _custCustomer,
            FieldLabel("المبلغ"),
            _custAmount,
            FieldLabel("ملاحظة"),
            _custNote,
            submit));

        root.Add(_custResult);
        var sv = new ScrollView { Content = root };
        Show(sv);
    }

    private void OnCustodyClicked() => ShowCustody();

    private void OnCustodyCollectClicked()
    {
        var customerId = Text.EmptyIfNull(_custCustomer.Text).Trim();
        var amount = Text.EmptyIfNull(_custAmount.Text).Trim();
        var note = Text.EmptyIfNull(_custNote.Text).Trim();
        if (string.IsNullOrEmpty(amount)) { _custResult.Text = "أدخل المبلغ"; _custResult.TextColor = Theme.Danger; return; }
        _custResult.Text = "جارٍ التسجيل…";
        _custResult.TextColor = Theme.Muted;
        var t = new Thread(() =>
        {
            try
            {
                var entryNumber = ApiClient.CollectCustodySync(customerId, amount, note);
                Device.BeginInvokeOnMainThread(() => { _custResult.Text = "تم تسجيل التحصيل ✓ (قيد " + entryNumber + ")"; _custResult.TextColor = Theme.Success; });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _custResult.Text = "فشل: " + msg; _custResult.TextColor = Theme.Danger; });
            }
        });
        t.Start();
    }
// ════ فحص الرف ════
    public void ShowShelf()
    {
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };
        var head = new HorizontalStackLayout { Spacing = 10 };
        head.Add(BackButton());
        head.Add(Theme.H1("فحص رف"));
        root.Add(head);
        root.Add(Theme.P("سجّل الصنف والموقع والكمية الملاحَظة في المتجر"));

        _sItem = Theme.Input("معرف الصنف");
        _sLocation = Theme.Input("الموقع/المتجر");
        _sQty = Theme.Input("الكمية الملاحَظة");
        _sNotes = Theme.Input("ملاحظة (اختيارية)");
        _sPhotoPath = Theme.Input("صورة فحص — مسار ملف (اختياري)");
        var submit = Theme.Filled("تسجيل الفحص");
        submit.Clicked += (sender, e) => OnShelfSubmitClicked();
        _sResult = new Label { Text = "", FontSize = 14, TextColor = Theme.Muted, LineBreakMode = LineBreakMode.WordWrap };

        root.Add(Theme.CardView(
            FieldLabel("معرف الصنف + الموقع + الكمية"),
            _sItem,
            _sLocation,
            _sQty,
            _sNotes,
            _sPhotoPath,
            submit));

        root.Add(_sResult);
        var sv = new ScrollView { Content = root };
        Show(sv);
    }

    public void OnShelfSubmitClicked()
    {
        var itemId = Text.EmptyIfNull(_sItem.Text).Trim();
        var location = Text.EmptyIfNull(_sLocation.Text).Trim();
        var qty = Text.EmptyIfNull(_sQty.Text).Trim();
        var notes = Text.EmptyIfNull(_sNotes.Text).Trim();
        if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(qty)) { _sResult.Text = "أدخل الصنف والكمية"; _sResult.TextColor = Theme.Danger; return; }
        _sResult.Text = "جارٍ الحفظ…";
        _sResult.TextColor = Theme.Muted;
        var t = new Thread(() =>
        {
            try
            {
                var photoPath = Text.EmptyIfNull(_sPhotoPath.Text).Trim();
                var photoB64 = photoPath.Length > 0 ? Convert.ToBase64String(Text.ReadBinaryFile(photoPath)) : "";
                var id = ApiClient.CreateShelfCheckSync(itemId, location, qty, notes, photoB64);
                Device.BeginInvokeOnMainThread(() => { _sResult.Text = "تم تسجيل الفحص ✓ (id " + id + ")"; _sResult.TextColor = Theme.Success; });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _sResult.Text = "فشل: " + msg; _sResult.TextColor = Theme.Danger; });
            }
        });
        t.Start();
    }

    // ════ رصد الأسعار ════
    public void ShowPriceCapture()
    {
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };
        var head = new HorizontalStackLayout { Spacing = 10 };
        head.Add(BackButton());
        head.Add(Theme.H1("رصد سعر منافس"));
        root.Add(head);
        root.Add(Theme.P("سجّل سعر المنافس للصنف المحدد"));

        _pItem = Theme.Input("معرف الصنف");
        _pCompetitor = Theme.Input("اسم المنافس");
        _pPrice = Theme.Input("السعر");
        var submit = Theme.Filled("حفظ الرصد");
        submit.Clicked += (sender, e) => OnPriceSubmitClicked();
        _pResult = new Label { Text = "", FontSize = 14, TextColor = Theme.Muted, LineBreakMode = LineBreakMode.WordWrap };

        root.Add(Theme.CardView(
            FieldLabel("معرف الصنف"),
            _pItem,
            FieldLabel("اسم المنافس"),
            _pCompetitor,
            FieldLabel("السعر الملاحَظ"),
            _pPrice,
            submit));

        root.Add(_pResult);
        var sv = new ScrollView { Content = root };
        Show(sv);
    }

    public void OnPriceSubmitClicked()
    {
        var itemId = Text.EmptyIfNull(_pItem.Text).Trim();
        var competitor = Text.EmptyIfNull(_pCompetitor.Text).Trim();
        var price = Text.EmptyIfNull(_pPrice.Text).Trim();
        if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(price)) { _pResult.Text = "أدخل الصنف والسعر"; _pResult.TextColor = Theme.Danger; return; }
        _pResult.Text = "جارٍ الحفظ…";
        _pResult.TextColor = Theme.Muted;
        var t = new Thread(() =>
        {
            try
            {
                var id = ApiClient.CreatePriceCaptureSync(itemId, competitor, price);
                Device.BeginInvokeOnMainThread(() => { _pResult.Text = "تم الحفظ ✓ (id " + id + ")"; _pResult.TextColor = Theme.Success; });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _pResult.Text = "فشل: " + msg; _pResult.TextColor = Theme.Danger; });
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
// ════ مردود المبيعات ════
    public void ShowReturn()
    {
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };
        var head = new HorizontalStackLayout { Spacing = 10 };
        head.Add(BackButton());
        head.Add(Theme.H1("مردود مبيعات"));
        root.Add(head);
        root.Add(Theme.P("أدخل رقم الفاتورة الأصلية ليبحث النظام عنها ويعود ببنودها (يتطلب اتصالاً)"));

        _rInvoiceNo = Theme.Input("مثال: SI-20260913-0005");
        var load = Theme.Filled("تحميل الفاتورة");
        load.Clicked += (sender, e) => OnReturnLoadClicked();
        _rResult = new Label { Text = "", FontSize = 14, TextColor = Theme.Muted, LineBreakMode = LineBreakMode.WordWrap };
        _spin = new ActivityIndicator { Color = Theme.Primary, IsRunning = false, IsVisible = false };

        root.Add(Theme.CardView(
            FieldLabel("رقم الفاتورة"),
            _rInvoiceNo,
            load));

        root.Add(_rResult);
        root.Add(_spin);
        var sv = new ScrollView { Content = root };
        Show(sv);
    }

    private void OnReturnLoadClicked()
    {
        var no = Text.EmptyIfNull(_rInvoiceNo.Text).Trim();
        if (no.Length == 0) { _rResult.Text = "أدخل رقم الفاتورة"; _rResult.TextColor = Theme.Danger; return; }
        if (!SyncEngine.IsOnline()) { _rResult.Text = "يتطلب المردود اتصالاً بالخادم"; _rResult.TextColor = Theme.Danger; return; }
        _rResult.Text = "جارٍ البحث…";
        _rResult.TextColor = Theme.Muted;
        _spin.IsRunning = true;
        _spin.IsVisible = true;
        var t = new Thread(() =>
        {
            try
            {
                var lookup = ApiClient.ReturnLookupSync(no);
                Device.BeginInvokeOnMainThread(() => { _spin.IsRunning = false; _spin.IsVisible = false; ShowReturnEditor(lookup); });
            }
            catch (Exception ex)
            {
                var err = JsonLite.Field(ex.Message, "error");
                var msg = err.Length > 0 ? JsonLite.Unescape(err) : ex.Message;
                Device.BeginInvokeOnMainThread(() => { _rResult.Text = "فشل: " + msg; _rResult.TextColor = Theme.Danger; _spin.IsRunning = false; _spin.IsVisible = false; });
            }
        });
        t.Start();
    }

    private void ShowReturnEditor(ReturnLookupDto lk)
    {
        _rLookup = lk;
        _rLines = lk.Lines;
        _rQtys = new List<Entry>();
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };

        var head = new HorizontalStackLayout { Spacing = 10 };
        head.Add(BackButton());
        head.Add(Theme.H1("مردود " + lk.InvoiceNumber));
        root.Add(head);
        root.Add(Theme.P("العميل: " + lk.CustomerName + "  ·  المخزن: " + lk.WarehouseName));

        if (lk.Lines.Count == 0)
        {
            root.Add(new Label { Text = "لا بنود قابلة للرد في هذه الفاتورة", TextColor = Theme.Danger });
        }
        else
        {
            root.Add(Theme.Section("أدخل كمية الرد لكل بند (اترك 0 لتخطيه)"));
            foreach (var line in lk.Lines)
            {
                var nameRow = new HorizontalStackLayout { Spacing = 8 };
                nameRow.Add(Theme.Chip(line.Code, Theme.PrimarySoft, Theme.Primary));
                nameRow.Add(new Label
                {
                    Text = line.NameAr,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Theme.Ink,
                    VerticalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                });
                var chipsRow = new HorizontalStackLayout { Spacing = 6 };
                chipsRow.Add(Theme.Chip("مباع " + line.Quantity, Theme.SuccessSoft, Theme.Success));
                chipsRow.Add(Theme.Chip("باقي " + line.Remaining, Theme.WarningSoft, Theme.Warning));
                chipsRow.Add(Theme.Chip("سعر " + line.UnitPrice, Theme.PrimarySoft, Theme.Primary));
                var qty = Theme.Input("كمية الرد (0 للتخطي)", "0");
                _rQtys.Add(qty);
                root.Add(Theme.CardView(
                    nameRow,
                    chipsRow,
                    FieldLabel("كمية الرد"),
                    qty));
            }
            var submit = Theme.Filled("تسجيل المردود");
            submit.Clicked += (sender, e) => OnReturnSubmitClicked();
            root.Add(submit);
        }

        _rResult = new Label { Text = "", FontSize = 14, TextColor = Theme.Muted, LineBreakMode = LineBreakMode.WordWrap };
        root.Add(_rResult);
        var sv = new ScrollView { Content = root };
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
        _rResult.Text = "جارٍ التسجيل…";
        _rResult.TextColor = Theme.Muted;
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

    // ════ شاشة نصية (تحذيرات وغيرها) ════
    private void ShowText(string titleText, string body)
    {
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };
        var head = new HorizontalStackLayout { Spacing = 10 };
        head.Add(BackButton());
        head.Add(Theme.H1(titleText));
        root.Add(head);
        root.Add(Theme.CardView(new Label { Text = body, FontSize = 13, TextColor = Theme.Ink, LineBreakMode = LineBreakMode.WordWrap }));
        var sv = new ScrollView { Content = root };
        Show(sv);
    }

    // ════ الانتقالات ════
    private void OnItemsClicked() => ShowItems();
    private void OnInvoiceClicked() => ShowInvoice();
    private void OnBackClicked() => ShowHome();

    private void OnLogoutClicked()
    {
        Session.Logout();
        ShowLogin();
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
        _invoiceResult.Text = "جارٍ الإنشاء…  " + _lines.Count + " سطر"; _invoiceResult.TextColor = Theme.Muted;
        _spin.IsRunning = true;
        _spin.IsVisible = true;
        var t = new Thread(() =>
        {
            try
            {
                if (SyncEngine.IsOnline())
                {
                    var inv = ApiClient.CreateInvoiceSync(_lines, customerId, warehouseId, _payMethod, note, invPhotoB64);
                    var text2 = "تم الإنشاء ✓ " + inv.InvoiceNumber + " (" + _lines.Count + " أصناف — الإجمالي " + inv.TotalAmount + ")";
                    Device.BeginInvokeOnMainThread(() => { _invoiceResult.Text = text2; _invoiceResult.TextColor = Theme.Success; ResetCartUi(); _spin.IsRunning = false; _spin.IsVisible = false; });
                }
                else
                {
                    var key2 = Guid.NewGuid().ToString();
                    var payload = ApiClient.MakeInvoicePayload(_lines, customerId, warehouseId, note, key2, _payMethod, invPhotoB64);
                    LocalStore.Enqueue(key2, payload);
                    Device.BeginInvokeOnMainThread(() => { _invoiceResult.Text = "دُوِّن دون اتصال ✓ — سيُرسل عند الاتصال (" + LocalStore.CountPending() + " بانتظار الإرسال)"; _invoiceResult.TextColor = Theme.Success; ResetCartUi(); _spin.IsRunning = false; _spin.IsVisible = false; });
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Device.BeginInvokeOnMainThread(() => { _invoiceResult.Text = "فشل الإنشاء: " + msg; _invoiceResult.TextColor = Theme.Danger; _spin.IsRunning = false; _spin.IsVisible = false; });
            }
        });
        t.Start();
    }
}