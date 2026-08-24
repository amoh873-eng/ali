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
            try{
                var notifier=scope.ServiceProvider.GetService<ERPSystem.Infrastructure.Services.ErrorNotifierService>();
                if(notifier!=null && notifier.ShouldNotify(ex))
                    await notifier.NotifyAsync($"[Error] {ex.GetType().Name}", $"{ex.Message}\n{context.Request.Path}\n{ex.StackTrace}");
            }catch(Exception nEx){ _logger.LogError(nEx,"Notify failed"); }
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "Failed to persist exception log");
        }
    }
}
