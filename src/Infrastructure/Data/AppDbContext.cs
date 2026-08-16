using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Data;

/// <summary>
/// Central database context for the ERP system.
/// This is the bridge between C# code and SQL Server.
/// 
/// يرث من IdentityDbContext لتضمين جداول الهوية (Users/Roles) إلى جانب جداول ERP.
/// </summary>
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    /// <summary>
    /// Constructor accepting DbContextOptions (used by DI to inject connection string).
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Chart of Accounts table.
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>
    /// Units of measure table (وحدات القياس).
    /// </summary>
    public DbSet<Unit> Units => Set<Unit>();

    /// <summary>
    /// Item categories table (فئات الأصناف).
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Inventory items table (الأصناف).
    /// </summary>
    public DbSet<Item> Items => Set<Item>();

    /// <summary>
    /// Warehouses table (المخازن).
    /// </summary>
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    /// <summary>
    /// Stock movements table (حركات المخزون).
    /// </summary>
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    /// <summary>
    /// Stock counts table (الجرد الدوري).
    /// </summary>
    public DbSet<StockCount> StockCounts => Set<StockCount>();

    /// <summary>
    /// Stock count lines table (بنود الجرد الدوري).
    /// </summary>
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();

    /// <summary>
    /// Customers table (العملاء).
    /// </summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>
    /// Sales invoices table (فواتير المبيعات).
    /// </summary>
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();

    /// <summary>
    /// Sales invoice lines table (بنود فواتير المبيعات).
    /// </summary>
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();

    /// <summary>
    /// Sales returns table (مردودات المبيعات).
    /// </summary>
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();

    /// <summary>
    /// Sales return lines table (بنود مردودات المبيعات).
    /// </summary>
    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();

    /// <summary>
    /// Sales quotes table (عروض الأسعار).
    /// </summary>
    public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();

    /// <summary>
    /// Sales quote lines table (بنود عروض الأسعار).
    /// </summary>
    public DbSet<SalesQuoteLine> SalesQuoteLines => Set<SalesQuoteLine>();

    /// <summary>
    /// Purchase orders table (أوامر الشراء).
    /// </summary>
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    /// <summary>
    /// Purchase order lines table (بنود أوامر الشراء).
    /// </summary>
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    /// <summary>
    /// Journal entries table (القيود المحاسبية).
    /// </summary>
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    /// <summary>
    /// Journal entry lines table (بنود القيود المحاسبية).
    /// </summary>
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    /// <summary>
    /// Suppliers table (الموردون).
    /// </summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    /// <summary>
    /// Purchase invoices table (فواتير المشتريات).
    /// </summary>
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();

    /// <summary>
    /// Purchase invoice lines table (بنود فواتير المشتريات).
    /// </summary>
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();

    /// <summary>
    /// Purchase returns table (مردودات المشتريات).
    /// </summary>
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();

    /// <summary>
    /// Purchase return lines table (بنود مردودات المشتريات).
    /// </summary>
    public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();

    /// <summary>
    /// Number sequences table (العدّادات التسلسلية لتوليد أرقام المستندات).
    /// </summary>
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    /// <summary>
    /// Departments table (الأقسام).
    /// </summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>
    /// Job positions table (المسميات الوظيفية).
    /// </summary>
    public DbSet<Position> Positions => Set<Position>();

    /// <summary>
    /// Employees table (الموظفون).
    /// </summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>
    /// Leave requests table (الإجازات).
    /// </summary>
    public DbSet<Leave> Leaves => Set<Leave>();

    /// <summary>
    /// Expense categories table (فئات المصاريف).
    /// </summary>
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    /// <summary>
    /// Expense entries table (سندات المصروف).
    /// </summary>
    public DbSet<ExpenseEntry> ExpenseEntries => Set<ExpenseEntry>();

    /// <summary>
    /// Leads table (العملاء المحتملون).
    /// </summary>
    public DbSet<Lead> Leads => Set<Lead>();

    /// <summary>
    /// Sales opportunities table (الفرص البيعية).
    /// </summary>
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

    /// <summary>
    /// CRM activities table (سجل التواصل).
    /// </summary>
    public DbSet<Activity> Activities => Set<Activity>();

    /// <summary>
    /// Scheduled follow-ups table (المتابعات المجدولة).
    /// </summary>
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();

    /// <summary>
    /// Exception logs table (سجل الأخطاء — لوحة إدارة النظام).
    /// </summary>
    public DbSet<ExceptionLog> ExceptionLogs => Set<ExceptionLog>();

    /// <summary>
    /// Configures the model using Fluent API from configuration classes.
    /// Keeps OnModelCreating clean by delegating each entity's config to its own class.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration classes in this assembly
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new UnitConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ItemConfiguration());
        modelBuilder.ApplyConfiguration(new WarehouseConfiguration());
        modelBuilder.ApplyConfiguration(new StockMovementConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new SalesInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new SalesInvoiceLineConfiguration());
        modelBuilder.ApplyConfiguration(new SalesReturnConfiguration());
        modelBuilder.ApplyConfiguration(new SalesReturnLineConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseInvoiceLineConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseReturnConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseReturnLineConfiguration());
        modelBuilder.ApplyConfiguration(new JournalEntryConfiguration());
        modelBuilder.ApplyConfiguration(new JournalEntryLineConfiguration());
        modelBuilder.ApplyConfiguration(new NumberSequenceConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new PositionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new LeaveConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseEntryConfiguration());
        modelBuilder.ApplyConfiguration(new LeadConfiguration());
        modelBuilder.ApplyConfiguration(new OpportunityConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityConfiguration());
        modelBuilder.ApplyConfiguration(new FollowUpConfiguration());
        modelBuilder.ApplyConfiguration(new ExceptionLogConfiguration());

        // ==================== Seed Data ====================
        // بذور أولية لشجرة الحسابات - هذه الحسابات الأساسية ستنشأ تلقائياً
        // عند أول ترحيل (Migration) لقاعدة البيانات.
        // تمثل الفئات الرئيسية الخمس في المحاسبة.
        SeedChartOfAccounts(modelBuilder);
        SeedInventoryData(modelBuilder);
        SeedSalesAccounts(modelBuilder);
        SeedHrData(modelBuilder);
        SeedPosData(modelBuilder);
    }

    /// <summary>
    /// Seeds default HR master data: a root department and a default position.
    /// These give the user a starting point before adding employees.
    /// </summary>
    private void SeedHrData(ModelBuilder modelBuilder)
    {
        var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Department>().HasData(
            new Department
            {
                Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                Code = "ADMIN",
                NameAr = "الإدارة العامة",
                NameEn = "General Administration",
                IsActive = true,
                IsSystem = true,
                CreatedAt = seedTime
            }
        );

        modelBuilder.Entity<Position>().HasData(
            new Position
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                Code = "ADMIN-MGR",
                NameAr = "مدير",
                NameEn = "Manager",
                DepartmentId = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                IsActive = true,
                IsSystem = true,
                CreatedAt = seedTime
            }
        );
    }

    /// <summary>
    /// Seeds the default walk-in customer used by the Point of Sale (POS) screen
    /// when no specific customer is selected (زبون نقدي).
    /// </summary>
    private void SeedPosData(ModelBuilder modelBuilder)
    {
        var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                Code = "CASH",
                NameAr = "زبون نقدي",
                NameEn = "Walk-in Cash Customer",
                IsActive = true,
                IsSystem = true,
                CreatedAt = seedTime
            }
        );
    }

    /// <summary>
    /// Seeds the default Chart of Accounts structure.
    /// Creates the 5 main account categories as system accounts.
    /// </summary>
    private void SeedChartOfAccounts(ModelBuilder modelBuilder)
    {
        var assetId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var liabilityId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var equityId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var revenueId = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var expenseId = Guid.Parse("10000000-0000-0000-0000-000000000005");

        modelBuilder.Entity<Account>().HasData(
            new Account
            {
                Id = assetId,
                Code = "1",
                NameAr = "الأصول",
                NameEn = "Assets",
                AccountType = Domain.Enums.AccountType.Asset,
                NormalBalance = Domain.Enums.NormalBalance.Debit,
                ParentAccountId = null,
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Account
            {
                Id = liabilityId,
                Code = "2",
                NameAr = "الخصوم",
                NameEn = "Liabilities",
                AccountType = Domain.Enums.AccountType.Liability,
                NormalBalance = Domain.Enums.NormalBalance.Credit,
                ParentAccountId = null,
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Account
            {
                Id = equityId,
                Code = "3",
                NameAr = "حقوق الملكية",
                NameEn = "Equity",
                AccountType = Domain.Enums.AccountType.Equity,
                NormalBalance = Domain.Enums.NormalBalance.Credit,
                ParentAccountId = null,
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Account
            {
                Id = revenueId,
                Code = "4",
                NameAr = "الإيرادات",
                NameEn = "Revenue",
                AccountType = Domain.Enums.AccountType.Revenue,
                NormalBalance = Domain.Enums.NormalBalance.Credit,
                ParentAccountId = null,
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Account
            {
                Id = expenseId,
                Code = "5",
                NameAr = "المصروفات",
                NameEn = "Expenses",
                AccountType = Domain.Enums.AccountType.Expense,
                NormalBalance = Domain.Enums.NormalBalance.Debit,
                ParentAccountId = null,
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }

    /// <summary>
    /// Seeds default inventory master data: units of measure, categories, and warehouses.
    /// These are required before the user can create items, so they are created
    /// automatically on the first migration / EnsureCreated.
    /// </summary>
    private void SeedInventoryData(ModelBuilder modelBuilder)
    {
        var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ===== وحدات القياس =====
        modelBuilder.Entity<Unit>().HasData(
            new Unit { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Code = "PCS", NameAr = "قطعة", NameEn = "Piece", IsActive = true, IsSystem = true, CreatedAt = seedTime },
            new Unit { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Code = "KG", NameAr = "كيلو جرام", NameEn = "Kilogram", IsActive = true, IsSystem = true, CreatedAt = seedTime },
            new Unit { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Code = "L", NameAr = "لتر", NameEn = "Liter", IsActive = true, IsSystem = true, CreatedAt = seedTime },
            new Unit { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Code = "BOX", NameAr = "عبوة", NameEn = "Box", IsActive = true, IsSystem = true, CreatedAt = seedTime },
            new Unit { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), Code = "M", NameAr = "متر", NameEn = "Meter", IsActive = true, IsSystem = true, CreatedAt = seedTime }
        );

        // ===== فئات الأصناف =====
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = Guid.Parse("40000000-0000-0000-0000-000000000001"), Code = "RAW", NameAr = "مواد خام", NameEn = "Raw Materials", IsActive = true, IsSystem = true, CreatedAt = seedTime },
            new Category { Id = Guid.Parse("40000000-0000-0000-0000-000000000002"), Code = "FIN", NameAr = "منتجات تامة", NameEn = "Finished Goods", IsActive = true, IsSystem = true, CreatedAt = seedTime },
            new Category { Id = Guid.Parse("40000000-0000-0000-0000-000000000003"), Code = "SUP", NameAr = "مستلزمات تشغيل", NameEn = "Operating Supplies", IsActive = true, IsSystem = true, CreatedAt = seedTime }
        );

        // ===== المخازن =====
        modelBuilder.Entity<Warehouse>().HasData(
            new Warehouse
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                Code = "MAIN",
                NameAr = "المخزن الرئيسي",
                NameEn = "Main Warehouse",
                Location = "المقر الرئيسي",
                IsActive = true,
                IsSystem = true,
                IsDefault = true,
                CreatedAt = seedTime
            }
        );
    }

    /// <summary>
    /// Seeds the system accounts required for automatic journal entries
    /// (الترابط المحاسبي). These are the accounts the sales/purchases modules
    /// debit and credit automatically, so they must exist before any document is posted.
    /// </summary>
    private void SeedSalesAccounts(ModelBuilder modelBuilder)
    {
        // Root accounts are seeded in SeedChartOfAccounts with these ids.
        var assetsRoot = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var liabilitiesRoot = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var revenueRoot = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var expenseRoot = Guid.Parse("10000000-0000-0000-0000-000000000005");

        var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Account>().HasData(
            // أصول
            new Account
            {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000001"),
                Code = "1100", NameAr = "الصندوق", NameEn = "Cash",
                AccountType = Domain.Enums.AccountType.Asset,
                NormalBalance = Domain.Enums.NormalBalance.Debit,
                ParentAccountId = assetsRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            },
            new Account
            {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000002"),
                Code = "1200", NameAr = "العملاء/المدينون", NameEn = "Accounts Receivable",
                AccountType = Domain.Enums.AccountType.Asset,
                NormalBalance = Domain.Enums.NormalBalance.Debit,
                ParentAccountId = assetsRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            },
            new Account
            {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000003"),
                Code = "1300", NameAr = "مخزون البضاعة", NameEn = "Inventory",
                AccountType = Domain.Enums.AccountType.Asset,
                NormalBalance = Domain.Enums.NormalBalance.Debit,
                ParentAccountId = assetsRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            },
            new Account
            {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000004"),
                Code = "1101", NameAr = "البنك", NameEn = "Bank",
                AccountType = Domain.Enums.AccountType.Asset,
                NormalBalance = Domain.Enums.NormalBalance.Debit,
                ParentAccountId = assetsRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            },
            // خصوم
            new Account
            {
                Id = Guid.Parse("12000000-0000-0000-0000-000000000001"),
                Code = "2100", NameAr = "ضريبة المبيعات المستحقة", NameEn = "Sales Tax Payable",
                AccountType = Domain.Enums.AccountType.Liability,
                NormalBalance = Domain.Enums.NormalBalance.Credit,
                ParentAccountId = liabilitiesRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            },
            new Account
            {
                Id = Guid.Parse("12000000-0000-0000-0000-000000000002"),
                Code = "2200", NameAr = "الموردون/الدائنون", NameEn = "Accounts Payable",
                AccountType = Domain.Enums.AccountType.Liability,
                NormalBalance = Domain.Enums.NormalBalance.Credit,
                ParentAccountId = liabilitiesRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            },
            // إيرادات
            new Account
            {
                Id = Guid.Parse("14000000-0000-0000-0000-000000000001"),
                Code = "4100", NameAr = "إيراد المبيعات", NameEn = "Sales Revenue",
                AccountType = Domain.Enums.AccountType.Revenue,
                NormalBalance = Domain.Enums.NormalBalance.Credit,
                ParentAccountId = revenueRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            },
            new Account
            {
                Id = Guid.Parse("14000000-0000-0000-0000-000000000002"),
                Code = "4110", NameAr = "مردودات المبيعات", NameEn = "Sales Returns and Allowances",
                AccountType = Domain.Enums.AccountType.Revenue,
                NormalBalance = Domain.Enums.NormalBalance.Credit,
                ParentAccountId = revenueRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            },
            // مصروفات
            new Account
            {
                Id = Guid.Parse("15000000-0000-0000-0000-000000000001"),
                Code = "5100", NameAr = "تكلفة البضاعة المباعة", NameEn = "Cost of Goods Sold",
                AccountType = Domain.Enums.AccountType.Expense,
                NormalBalance = Domain.Enums.NormalBalance.Debit,
                ParentAccountId = expenseRoot, IsActive = true, IsSystem = true, CreatedAt = seedTime
            }
        );
    }
}
