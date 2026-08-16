using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure.Data;

namespace ERPSystem.Web.Middleware;

/// <summary>
/// يلتقط أي استثناء غير معالَج في خط الأنابيب، يسجّله في جدول ExceptionLogs،
/// ثم يعيد رميه حتى يتولّى معالج الخطأ القياسي عرض صفحة الخطأ المعتادة دون تغيير سلوكها.
/// (المرحلة الأولى — لوحة إدارة النظام: مراقبة الصحة وسجل الأخطاء)
/// </summary>
public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await TryLogAsync(context, ex);
            throw; // إعادة الرمي للحفاظ على سلوك عرض الخطأ الأصلي
        }
    }

    private async Task TryLogAsync(HttpContext context, Exception ex)
    {
        try
        {
            // Scope مستقل حتى لا يتأثر DbContext الرئيسي (خصوصًا لو كان الخطأ ناتجًا عن قاعدة البيانات نفسها)
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.ExceptionLogs.Add(new ExceptionLog
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                Message = ex.Message,
                StackTrace = ex.ToString(),
                UserId = context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : null,
                RequestPath = context.Request.Path,
                ExceptionType = ex.GetType().FullName
            });

            await dbContext.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            // لا نسمح لفشل التسجيل بإخفاء الاستثناء الأصلي
            _logger.LogError(logEx, "Failed to persist exception log");
        }
    }
}
