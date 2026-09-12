using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ERPSystem.Infrastructure.Data;

/// <summary>
/// Design-time factory for AppDbContext.
/// يتيح لأداة `dotnet ef` إنشاء السياق دون تشغيل التطبيق الكامل
/// (خاصة أثناء توليد الـ Migrations). يقرأ سلسلة الاتصال من user-secrets
/// (نفس UserSecretsId في ERPSystem.Web.csproj) فلا تُخزَّن أي كلمة مرور في الكود.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // 1) متغير بيئة أولاً (للإنتاج/النشر)
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        // 2) ثم user-secrets للتطوير المحلي
        if (string.IsNullOrWhiteSpace(cs))
        {
            try
            {
                var cfg = new ConfigurationBuilder()
                    .AddUserSecrets("0dfb3154-14fd-439a-a6d1-13d610a172f6")
                    .Build();
                cs = cfg.GetConnectionString("DefaultConnection");
            }
            catch { /* سيتم التعامل أدناه */ }
        }

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection غير مهيأة. شغّل:");
        //      dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
        //      أو عرّف متغير البيئة ConnectionStrings__DefaultConnection.

        optionsBuilder.UseNpgsql(cs);
        return new AppDbContext(optionsBuilder.Options);
    }
}