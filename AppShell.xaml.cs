using Microsoft.Extensions.DependencyInjection;

namespace ApotekApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        ApplyRolePermissions();
    }

    private void ApplyRolePermissions()
    {
        string role = Preferences.Get("CurrentUserRole", "Kasir");

        var roleHeaderLabel = this.FindByName<Label>("RoleHeaderLabel");
        if (roleHeaderLabel != null)
        {
            roleHeaderLabel.Text = $"Role: {role}";
        }

        bool isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        SetMenuVisibility("DashboardMenu", isAdmin);
        SetMenuVisibility("ObatMenu", isAdmin);
        SetMenuVisibility("SupplierMenu", isAdmin);
        SetMenuVisibility("UserMenu", isAdmin);
        SetMenuVisibility("PembelianMenu", isAdmin);
        SetMenuVisibility("TransaksiMenu", true);
        SetMenuVisibility("LaporanMenu", true);
    }

    private void SetMenuVisibility(string menuName, bool isVisible)
    {
        var menu = this.FindByName<FlyoutItem>(menuName);
        if (menu != null)
        {
            menu.IsVisible = isVisible;
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Clear();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var loginPage = services?.GetService<LoginPage>();

        if (loginPage != null)
        {
            Application.Current!.MainPage = new NavigationPage(loginPage);
        }
    }
}