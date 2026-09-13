using Microsoft.UI.Xaml;

namespace ERPSystem.Mobile.WinUI;

/// <summary>الإقلاع على منصة ويندوز (معادل main/WinMain) — يستدعي بناء التطبيق العابر للمنصات.</summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}