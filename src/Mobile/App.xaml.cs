namespace ERPSystem.Mobile;

/// <summary>تطبيق الجوال: نافذة واحدة + AppShell للتنقل بين الشاشات.</summary>
public partial class App : Application
{
    public App()
    {
        Smoke.RunIfMarked();
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Title = "ERP الميداني";
        window.MinimumWidth = 760;
        window.MinimumHeight = 640;
        window.Width = 1000;
        window.Height = 760;
        return window;
    }
}