using Microsoft.Extensions.DependencyInjection;

namespace ApotekApp;

public partial class MainLayoutPage : ContentPage
{
<<<<<<< HEAD
    private readonly IServiceProvider? _serviceProvider;
=======
    private readonly IServiceProvider _serviceProvider;
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a

    private readonly Color _colorActive = Color.FromArgb("#3B82F6");      // Warna tombol aktif (Biru)
    private readonly Color _colorInactive = Color.FromArgb("Transparent");  // Transparan
    private readonly Color _textActive = Color.FromArgb("#FFFFFF");       // Teks Putih
    private readonly Color _textInactive = Color.FromArgb("#94A3B8");     // Teks Abu-abu

    public MainLayoutPage(IServiceProvider? serviceProvider = null)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Buka Dashboard otomatis saat halaman utama pertama kali dimuat
        if (MainContentArea.Content == null)
        {
            NavigateToMenu<DashboardPage>(BtnDashboard);
        }
    }

    // Helper untuk mengambil instance Page yang membutuhkan Dependency Injection
    private T GetPage<T>() where T : ContentPage
    {
        var services = _serviceProvider
            ?? Handler?.MauiContext?.Services
            ?? Application.Current?.Handler?.MauiContext?.Services;

        if (services != null)
        {
            return services.GetService<T>() ?? ActivatorUtilities.GetServiceOrCreateInstance<T>(services);
        }

        return Activator.CreateInstance<T>();
    }

    private void OnMenuClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn == BtnDashboard) NavigateToMenu<DashboardPage>(btn);
            else if (btn == BtnObat) NavigateToMenu<ObatPage>(btn);
            else if (btn == BtnSupplier) NavigateToMenu<SupplierPage>(btn);
            else if (btn == BtnUser) NavigateToMenu<UserPage>(btn);
            else if (btn == BtnPenjualan) NavigateToMenu<TransaksiPage>(btn);
            else if (btn == BtnPembelian) NavigateToMenu<PembelianPage>(btn);
            else if (btn == BtnLaporan) NavigateToMenu<LaporanPage>(btn);
        }
    }

    private void NavigateToMenu<T>(Button selectedButton) where T : ContentPage
    {
        try
        {
            ResetButtonStyles();

            // Ubah warna tombol yang sedang diklik menjadi nyala
            selectedButton.BackgroundColor = _colorActive;
            selectedButton.TextColor = _textActive;

            // Ambil instance halaman secara otomatis dari DI container
            var page = GetPage<T>();
            MainContentArea.Content = page.Content;
        }
        catch (Exception ex)
        {
            DisplayAlert("Error Navigasi", ex.Message, "OK");
        }
    }

    private void ResetButtonStyles()
    {
        Button[] menuButtons = { BtnDashboard, BtnObat, BtnSupplier, BtnUser, BtnPenjualan, BtnPembelian, BtnLaporan };

        foreach (var btn in menuButtons)
        {
            btn.BackgroundColor = _colorInactive;
            btn.TextColor = _textInactive;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Konfirmasi", "Apakah Anda yakin ingin keluar?", "Ya", "Tidak");
        if (confirm)
        {
<<<<<<< HEAD
            if (Application.Current is not null)
                Application.Current.MainPage = new LoginPage();
=======
            Application.Current.MainPage = new LoginPage();
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
        }
    }
}