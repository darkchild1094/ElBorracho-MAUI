namespace ElBorracho;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // .NET 9/10: reemplaza el obsoleto MainPage
    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());
}