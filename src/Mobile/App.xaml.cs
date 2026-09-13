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
        return new Window(new AppShell());
    }
}