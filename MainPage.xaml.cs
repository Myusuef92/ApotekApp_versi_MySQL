namespace ApotekApp;

public partial class MainPage : ContentPage
{
    private Button? _active;
    public MainPage()
    {
        InitializeComponent();
        ApplyIdentity();
        ApplyPermissions();
        _ = NavigateAsync(BtnDashboard, "DASHBOARD", new DashboardPage());
    }
    private void ApplyIdentity()
    {
        var name=Preferences.Get("CurrentUserName","Pengguna");
        var role=Preferences.Get("CurrentUserRole","Kasir");
        LblSidebarUser.Text=name; LblSidebarRole.Text=role; LblHeaderUser.Text=$"{name} • {role}";
    }
    private void ApplyPermissions()
    {
        var role=Preferences.Get("CurrentUserRole","Kasir");
        var admin=role.Equals("Admin",StringComparison.OrdinalIgnoreCase);
        var apoteker=role.Equals("Apoteker",StringComparison.OrdinalIgnoreCase);
        BtnObat.IsVisible=admin||apoteker; BtnSupplier.IsVisible=admin||apoteker; BtnUser.IsVisible=admin; BtnBarcode.IsVisible=admin||apoteker;
        BtnPembelian.IsVisible=admin||apoteker; BtnResep.IsVisible=admin||apoteker; BtnStok.IsVisible=admin||apoteker;
        BtnPelanggan.IsVisible=true; BtnPenjualan.IsVisible=true; BtnLaporan.IsVisible=true; BtnSetting.IsVisible=admin;
    }
    private async Task NavigateAsync(Button btn,string title,ContentPage page)
    {
        try
        {
            if(_active!=null){_active.BackgroundColor=Colors.Transparent;_active.TextColor=Color.FromArgb("#B7CCC8");}
            btn.BackgroundColor=Color.FromArgb("#1D4ED8"); btn.TextColor=Colors.White; _active=btn; LblPageTitle.Text=title;
            MainContent.Content=page.Content;
            switch(page){
                case DashboardPage d: await d.LoadAsync(); break;
                case ObatPage o: await o.LoadAsync(); break;
                case SupplierPage s: await s.LoadAsync(); break;
                case UserPage u: await u.LoadAsync(); break;
                case PembelianPage p: await p.LoadAsync(); break;
                case LaporanPage l: await l.LoadAsync(); break;
                case TransaksiPage t: t.InitializeForHost(); break;
                case SettingPage st: await st.LoadSettingsAsync(); break;
                case PelangganPage pl: await pl.LoadAsync(); break;
                case ResepPage r: await r.LoadAsync(); break;
                case StokPage sk: await sk.LoadAsync(); break;
                case BarcodeManualPage bm: await bm.LoadAsync(); break;
            }
        }catch(Exception ex){await DisplayAlert("Gagal Membuka Menu",ex.Message,"OK");}
    }
    private Task Nav(Button b,string t,ContentPage p)=>NavigateAsync(b,t,p);
    public async Task OpenBarcodeManualAsync() => await Nav(BtnBarcode,"BARCODE MANUAL",new BarcodeManualPage());
    public async Task OpenPelangganAsync() => await Nav(BtnPelanggan,"DATA PELANGGAN / PASIEN",new PelangganPage());
    private async void OnMenuDashboardClicked(object s,EventArgs e)=>await Nav(BtnDashboard,"DASHBOARD",new DashboardPage());
    private async void OnMenuPenjualanClicked(object s,EventArgs e)=>await Nav(BtnPenjualan,"KASIR / PENJUALAN",new TransaksiPage());
    private async void OnMenuPembelianClicked(object s,EventArgs e)=>await Nav(BtnPembelian,"PEMBELIAN & PENERIMAAN BARANG",new PembelianPage());
    private async void OnMenuResepClicked(object s,EventArgs e)=>await Nav(BtnResep,"RESEP & PELAYANAN KEFARMASIAN",new ResepPage());
    private async void OnMenuStokClicked(object s,EventArgs e)=>await Nav(BtnStok,"PERSEDIAAN & MUTASI STOK",new StokPage());
    private async void OnMenuObatClicked(object s,EventArgs e)=>await Nav(BtnObat,"MASTER DATA OBAT",new ObatPage());
    private async void OnMenuPelangganClicked(object s,EventArgs e)=>await Nav(BtnPelanggan,"DATA PELANGGAN / PASIEN",new PelangganPage());
    private async void OnMenuSupplierClicked(object s,EventArgs e)=>await Nav(BtnSupplier,"MASTER SUPPLIER",new SupplierPage());
    private async void OnMenuUserClicked(object s,EventArgs e)=>await Nav(BtnUser,"USER PENGGUNA & HAK AKSES",new UserPage());
    private async void OnMenuBarcodeClicked(object s,EventArgs e)=>await Nav(BtnBarcode,"BARCODE MANUAL",new BarcodeManualPage());
    private async void OnMenuLaporanClicked(object s,EventArgs e)=>await Nav(BtnLaporan,"LAPORAN & ANALITIK",new LaporanPage());
    private async void OnMenuSettingClicked(object s,EventArgs e)=>await Nav(BtnSetting,"PENGATURAN SISTEM & SATUSEHAT",new SettingPage());
    private async void OnLogoutClicked(object s,EventArgs e)
    {
        if(!await DisplayAlert("Keluar","Akhiri sesi pengguna?","Ya","Batal")) return;
        foreach (var key in new[]{"CurrentUserId","CurrentUserName","CurrentUsername","CurrentUserRole"}) Preferences.Remove(key);
        Application.Current!.MainPage=new NavigationPage(new LoginPage());
    }
}
