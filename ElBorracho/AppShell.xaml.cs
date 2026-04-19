namespace ElBorracho;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("generador", typeof(Views.GeneradorTablasPage));
    }
}