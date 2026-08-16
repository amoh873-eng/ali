using ERPSystem.Application.Interfaces;
using ERPSystem.Application.Services;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure-layer services.
/// Keeps Program.cs clean by encapsulating all DI registration in one place.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services: DbContext and application services.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">Application configuration (for connection string).</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with SQL Server
        // لماذا AddDbContext وليس AddDbContextPool؟
        // DbContextPool يعيد استخدام السياق بين الطلبات وهو أسرع،
        // لكننا نستخدم AddDbContext حالياً للبساطة.
        // في الإصدارات المستقبلية يمكن الترقية إلى Pool للتحسين.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    // مكان ملفات الترحيل (Migrations) داخل مشروع Infrastructure
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                }));

        // Register application services
        // AddScoped: ينشأ كائن جديد لكل طلب HTTP، ويُتلف بعد انتهاء الطلب.
        // هذا مناسب لـ Blazor Server لأن كل دائرة (Circuit) تمثل جلسة مستخدم.
        services.AddScoped<IAccountService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new AccountService(dbContext);
        });

        // ==================== موديول المستودعات ====================
        services.AddScoped<IUnitService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new UnitService(dbContext);
        });

        services.AddScoped<ICategoryService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new CategoryService(dbContext);
        });

        services.AddScoped<IItemService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new ItemService(dbContext);
        });

        services.AddScoped<IWarehouseService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new WarehouseService(dbContext);
        });

        services.AddScoped<IStockMovementService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new StockMovementService(dbContext);
        });

        services.AddScoped<IStockCountService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new StockCountService(dbContext);
        });

        // ==================== المحاسبة العامة: القيود المحاسبية ====================
        services.AddScoped<IJournalEntryService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new JournalEntryService(dbContext);
        });

        // ==================== موديول المبيعات ====================
        services.AddScoped<ICustomerService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new CustomerService(dbContext);
        });

        services.AddScoped<ISalesInvoiceService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var journalService = sp.GetRequiredService<IJournalEntryService>();
            return new SalesInvoiceService(dbContext, journalService);
        });

        services.AddScoped<ISalesQuoteService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var invoiceService = sp.GetRequiredService<ISalesInvoiceService>();
            return new SalesQuoteService(dbContext, invoiceService);
        });

        services.AddScoped<IPurchaseOrderService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var invoiceService = sp.GetRequiredService<IPurchaseInvoiceService>();
            return new PurchaseOrderService(dbContext, invoiceService);
        });

        services.AddScoped<ISalesReturnService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var journalService = sp.GetRequiredService<IJournalEntryService>();
            return new SalesReturnService(dbContext, journalService);
        });

        // ==================== التقارير المالية ====================
        services.AddScoped<IReportService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new ReportService(dbContext);
        });

        // ==================== موديول المشتريات ====================
        services.AddScoped<ISupplierService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new SupplierService(dbContext);
        });

        services.AddScoped<IPurchaseInvoiceService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var journalService = sp.GetRequiredService<IJournalEntryService>();
            return new PurchaseInvoiceService(dbContext, journalService);
        });

        services.AddScoped<IPurchaseReturnService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var journalService = sp.GetRequiredService<IJournalEntryService>();
            return new PurchaseReturnService(dbContext, journalService);
        });

        // ==================== موديول الموارد البشرية ====================
        services.AddScoped<IDepartmentService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new DepartmentService(dbContext);
        });

        services.AddScoped<IPositionService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new PositionService(dbContext);
        });

        services.AddScoped<IEmployeeService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new EmployeeService(dbContext);
        });

        services.AddScoped<ILeaveService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new LeaveService(dbContext);
        });

        // ==================== موديول المصاريف ====================
        services.AddScoped<IExpenseCategoryService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new ExpenseCategoryService(dbContext);
        });

        services.AddScoped<IExpenseService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var journalService = sp.GetRequiredService<IJournalEntryService>();
            return new ExpenseService(dbContext, journalService);
        });

        // ==================== موديول CRM ====================
        services.AddScoped<ILeadService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new LeadService(dbContext);
        });

        services.AddScoped<IOpportunityService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new OpportunityService(dbContext);
        });

        services.AddScoped<IActivityService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new ActivityService(dbContext);
        });

        services.AddScoped<IFollowUpService>(sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return new FollowUpService(dbContext);
        });

        return services;
    }
}
