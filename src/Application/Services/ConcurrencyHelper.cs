using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// مساعد لحفظ التغييرات مع رسائل خطأ مفهومة للمستخدم عند تعارض التزامن.
/// لماذا نحتاج هذا؟ عند تزامن طلبين يعدّلان نفس السطر (نفس RowVersion)،
/// يرمي EF Core استثناء DbUpdateConcurrencyException غامض. هذا المساعد
/// يحوّله إلى رسالة واضحة بالعربية ليظهرها Snackbar بدلاً من رسالة تقنية.
/// نفس النمط المطبّق على Item/Account — نطبّقه هنا على دورات الرواتب أيضاً.
/// </summary>
internal static class ConcurrencyHelper
{
    /// <summary>
    /// يحفظ التغييرات، وإن حدث تعارض تزامني يرمي InvalidOperationException برسالة عربية.
    /// </summary>
    public static async Task SaveChangesWithFriendlyErrorAsync(DbContext context, string? friendlyMessage = null)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                friendlyMessage ?? "تعارض تزامني — قام مستخدم آخر بتعديل نفس البيانات. حدّث الصفحة وحاول مرة أخرى.");
        }
    }
}
