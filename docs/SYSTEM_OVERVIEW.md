# مراجعة شاملة - نظام ERP المحاسبي

## 1) نظرة عامة
- ERP احادي المستأجر (VPS+DB لكل عميل) + لوحة مالك منفصلة.
- .NET 10, Identity, EF Core, MudBlazor 8 RTL, QuestPDF/ClosedXML.
- Domain / Application / Infrastructure / Web.

## 2) الوحدات
- محاسبة: شجرة حسابات + قيود مزدوجة + ميزان/قائمة دخل/ميزانية/دفتر/سجل قيود/تدفق/VAT.
- مبيعات/مشتريات: فواتير+مرتجعات+عروض (+DueDate/TaxAmount).
- مخزون: اصناف/مخازن/حركات/متوسط مرجح/جرد/تقييم.
- HR: موظف/اجازات/حضور/رواتب.
- مصاريف/POS/CRM.

## 3) التقارير (/reports)
- registry: ReportDefs + GenericReportTable (16 تقرير).
- عرض: ازرار + فلاتر من/الى/حتى + Excel/طباعة.

## 4) لوحة المالك
- x-vendor-9f3a1c + OwnerScheme + 404 + health/updates/trial.

## 5) الاعدادات
- SystemSettings (CurrencyCode JOD, DueDates).
- MoneyDisplay + /settings (لغة ar/en + عملة).

## 6) الجودة
- Identity, ReadOnlyConnection, Antiforgery, Digest يومي, Migrations 5.

## 7) التشغيل
- localdb + dotnet run --urls http://localhost:5186.

