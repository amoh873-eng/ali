namespace ERPSystem.Application.DTOs.Reports;
public enum ReportCategory { Financial, Sales, Purchases, Inventory, Hr, Expenses, Pos, Crm, Executive }
public enum ReportParamKind { None, DateRange, Date, Account }
public record ReportDef(string Key, ReportCategory Cat, string TitleAr, string TitleEn, ReportParamKind[] Params, bool Landscape = true);
public static class ReportsCatalog
{
    public static IReadOnlyList<ReportDef> All = new List<ReportDef>
    {
        new("trial-balance", ReportCategory.Financial, "ميزان المراجعة", "Trial Balance", new[] { ReportParamKind.DateRange }),
        new("income-statement", ReportCategory.Financial, "قائمة الدخل", "Income Statement", new[] { ReportParamKind.DateRange }),
        new("balance-sheet", ReportCategory.Financial, "الميزانية", "Balance Sheet", new[] { ReportParamKind.DateRange }),
        new("general-ledger", ReportCategory.Financial, "دفتر الأستاذ", "General Ledger", new[] { ReportParamKind.Account, ReportParamKind.DateRange }),
        new("journal-register", ReportCategory.Financial, "سجل القيود", "Journal Register", new[] { ReportParamKind.DateRange }),
        new("vat-summary", ReportCategory.Financial, "ملخص الضريبة", "VAT Summary", new[] { ReportParamKind.DateRange }),
        new("vat-sales-register", ReportCategory.Financial, "سجل ضريبة المبيعات", "Sales Tax Register", new[] { ReportParamKind.DateRange }),
        new("sales-tax", ReportCategory.Financial, "تقرير ضريبة المبيعات", "Sales Tax Report", new[] { ReportParamKind.DateRange }),
        new("financial-ratios", ReportCategory.Financial, "المؤشرات المالية", "Financial Ratios", new[] { ReportParamKind.DateRange }),
        new("cash-flow", ReportCategory.Financial, "قائمة التدفق النقدي", "Cash Flow Statement", new[] { ReportParamKind.DateRange }),
        new("item-profitability", ReportCategory.Inventory, "ربحية الأصناف", "Item Profitability", new[] { ReportParamKind.DateRange }),
        new("slow-moving-stock", ReportCategory.Inventory, "بطيء الحركة / خامل", "Slow Moving", new[] { ReportParamKind.DateRange }),
        new("abc-analysis", ReportCategory.Inventory, "تصنيف ABC", "ABC Analysis", new[] { ReportParamKind.DateRange }),
        new("seasonality", ReportCategory.Financial, "الموسمية", "Seasonality", new[] { ReportParamKind.DateRange }),
        new("sales-by-customer", ReportCategory.Sales, "المبيعات حسب العميل", "Sales by Customer", new[] { ReportParamKind.DateRange }),
        new("sales-by-item", ReportCategory.Sales, "المبيعات حسب الصنف", "Sales by Item", new[] { ReportParamKind.DateRange }),
        new("stock-valuation", ReportCategory.Inventory, "تقييم المخزون", "Stock Valuation", new[] { ReportParamKind.DateRange }),
        new("ar-aging", ReportCategory.Financial, "أعمار الذمم - عملاء", "AR Aging", new[] { ReportParamKind.DateRange }),
        new("ap-aging", ReportCategory.Financial, "أعمار الذمم - موردون", "AP Aging", new[] { ReportParamKind.DateRange }),
        new("low-stock", ReportCategory.Inventory, "نقص المخزون", "Low Stock", new[] { ReportParamKind.DateRange }),
        new("employee-directory", ReportCategory.Hr, "دليل الموظفين", "Employees", new[] { ReportParamKind.DateRange }),
        new("expenses-by-category", ReportCategory.Expenses, "المصاريف حسب الفئة", "Expenses by Category", new[] { ReportParamKind.DateRange }),
        new("pos-daily", ReportCategory.Pos, "ملخص المبيعات", "POS Summary", new[] { ReportParamKind.DateRange }),
        new("leads-pipeline", ReportCategory.Crm, "مسار العملاء", "Leads Pipeline", new[] { ReportParamKind.DateRange }),
        new("kpi-summary", ReportCategory.Executive, "ملخص المؤشرات", "KPI Summary", new[] { ReportParamKind.DateRange }),
    };
}
