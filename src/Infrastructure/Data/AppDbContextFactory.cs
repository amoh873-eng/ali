using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERPSystem.Infrastructure.Data;

/// <summary>
/// Design-time factory for AppDbContext.
/// يتيح لأداة `dotnet ef` إنشاء السياق دون تشغيل التطبيق الكامل
/// (خاصة أثناء توليد الـ Migrations). يستخدم نفس سلسلة الاتصال في appsettings.json.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=ERPSystemDb;Username=erp_app;Password=ErpPg#2026_K7q");
        return new AppDbContext(optionsBuilder.Options);
    }
}