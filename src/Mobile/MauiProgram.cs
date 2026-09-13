using Microsoft.Extensions.Logging;

namespace ERPSystem.Mobile;

/// <summary>نقطة الإقلاع الرسمية التي يكتشفها SDK (كقالب maui الرسمي).</summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // وضع الاختبار: إن وُجد أحد أعلام الدخان لا يشغَّل محرك المزامنة الخلفي (تحكم يدوي)
        if (!Smoke4.RunIfMarked())
        {
            if (!Smoke2.RunIfMarked())
            {
                if (!Smoke.RunIfMarked())
                {
                    SyncEngine.StartBackground();
                }
            }
        }

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { });

        return builder.Build();
    }
}