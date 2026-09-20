<<<<<<< HEAD
using MySqlConnector; using System.Collections.ObjectModel;
namespace ApotekApp;
public class DashAlert{public string Nama="",Kode="",StokText="",ExpText="";} public class DashSale{public string Nota="",Waktu="",Total="";}
public partial class DashboardPage:ContentPage{
 readonly ObservableCollection<DashAlert> alerts=new(); readonly ObservableCollection<DashSale> sales=new(); public DashboardPage(){InitializeComponent();CvAlert.ItemsSource=alerts;CvSales.ItemsSource=sales;}
 protected override async void OnAppearing(){base.OnAppearing();await LoadAsync();}
 public async Task LoadAsync(){try{await using var c=new MySqlConnection(MauiProgram.ConnectionString);await c.OpenAsync();var userName=Preferences.Get("CurrentUserName","Pengguna"); var username=Preferences.Get("CurrentUsername",userName); var role=Preferences.Get("CurrentUserRole","Pengguna"); LblWelcome.Text=$"Selamat datang, {userName}"; LblLoginInfo.Text=$"Login sebagai: {username} • {role} • {DateTime.Now:dddd, dd MMMM yyyy}";await Metric(c,"SELECT COUNT(*),COALESCE(SUM(grand_total),0) FROM penjualan WHERE DATE(tanggal)=CURDATE()",(cnt,total)=>{LblSales.Text=$"Rp {total:N0}";LblSalesCount.Text=$"{cnt:N0} transaksi";});await Metric(c,"SELECT COUNT(*),COALESCE(SUM(TotalHarga),0) FROM pembelians WHERE DATE(TanggalPembelian)=CURDATE()",(cnt,total)=>{LblPurchase.Text=$"Rp {total:N0}";LblPurchaseCount.Text=$"{cnt:N0} faktur";});await using(var cmd=new MySqlCommand("SELECT COUNT(*) FROM obats WHERE Stok<=StokMin",c))LblLow.Text=Convert.ToInt32(await cmd.ExecuteScalarAsync()).ToString("N0");await using(var cmd=new MySqlCommand("SELECT COUNT(*) FROM obats WHERE TanggalKadaluarsa IS NOT NULL AND TanggalKadaluarsa<>'' AND DATE(TanggalKadaluarsa)<=DATE_ADD(CURDATE(), INTERVAL 30 DAY)",c))LblExpiry.Text=Convert.ToInt32(await cmd.ExecuteScalarAsync()).ToString("N0");alerts.Clear();await using(var cmd=new MySqlCommand("SELECT KodeObat,NamaObat,Stok,StokMin,TanggalKadaluarsa FROM obats WHERE Stok<=StokMin OR (TanggalKadaluarsa IS NOT NULL AND DATE(TanggalKadaluarsa)<=DATE_ADD(CURDATE(), INTERVAL 30 DAY)) ORDER BY Stok ASC LIMIT 40",c)){await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())alerts.Add(new DashAlert{Kode=r[0]?.ToString()??"",Nama=r[1]?.ToString()??"",StokText=$"Stok {r[2]}",ExpText=DateTime.TryParse(r[4]?.ToString(),out var d)?$"Exp {d:dd/MM/yyyy}":""});}sales.Clear();await using(var cmd=new MySqlCommand("SELECT no_nota,tanggal,grand_total FROM penjualan ORDER BY tanggal DESC LIMIT 15",c)){await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())sales.Add(new DashSale{Nota=r[0]?.ToString()??"",Waktu=DateTime.TryParse(r[1]?.ToString(),out var d)?d.ToString("dd/MM/yyyy HH:mm"):"-",Total=$"Rp {Convert.ToDecimal(r[2]):N0}"});}}catch(Exception ex){System.Diagnostics.Debug.WriteLine(ex);}}
 static async Task Metric(MySqlConnection c,string sql,Action<int,decimal> set){await using var cmd=new MySqlCommand(sql,c);await using var r=await cmd.ExecuteReaderAsync();if(await r.ReadAsync())set(Convert.ToInt32(r[0]),Convert.ToDecimal(r[1]));}
=======
using MySqlConnector;
using System.Collections.ObjectModel;

namespace ApotekApp;

public class DashboardAlert
{
    public string NamaObat { get; set; } = "-";
    public int Stok { get; set; }
    public string TglExpired { get; set; } = "-";
}

public partial class DashboardPage : ContentPage
{
    private readonly ObservableCollection<DashboardAlert> _alerts = new();
    public DashboardPage()
    {
        InitializeComponent();
        CvAlert.ItemsSource = _alerts;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LblWelcome.Text = $"Selamat datang, {Preferences.Get("CurrentUserName", "Pengguna")}";
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        try
        {
            using var conn = new MySqlConnection(MauiProgram.ConnectionString);
            await conn.OpenAsync();

            int batasStok = Preferences.Get("BatasStokMenipis", 10);
            int batasExp = Preferences.Get("BatasHariExpired", 30);

            using (var cmd = new MySqlCommand("SELECT COUNT(*), COALESCE(SUM(Stok),0) FROM obats", conn))
            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    LblTotalObat.Text = Convert.ToInt32(r.GetValue(0)).ToString("N0");
                    LblTotalStok.Text = Convert.ToInt32(r.GetValue(1)).ToString("N0");
                }
            }

            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM obats WHERE Stok <= @batas", conn))
            {
                cmd.Parameters.AddWithValue("@batas", batasStok);
                LblStokMenipis.Text = Convert.ToInt32(await cmd.ExecuteScalarAsync()).ToString("N0");
            }

            using (var cmd = new MySqlCommand(@"SELECT COUNT(*) FROM obats
                WHERE TanggalKadaluarsa IS NOT NULL AND TanggalKadaluarsa <= @tgl", conn))
            {
                cmd.Parameters.AddWithValue("@tgl", DateTime.Today.AddDays(batasExp));
                LblAkanExpired.Text = Convert.ToInt32(await cmd.ExecuteScalarAsync()).ToString("N0");
            }

            _alerts.Clear();
            using var alertCmd = new MySqlCommand(@"SELECT NamaObat, Stok, TanggalKadaluarsa FROM obats
                WHERE Stok <= @batasStok OR (TanggalKadaluarsa IS NOT NULL AND TanggalKadaluarsa <= @tgl)
                ORDER BY Stok ASC, TanggalKadaluarsa ASC LIMIT 50", conn);
            alertCmd.Parameters.AddWithValue("@batasStok", batasStok);
            alertCmd.Parameters.AddWithValue("@tgl", DateTime.Today.AddDays(batasExp));
            using var ar = await alertCmd.ExecuteReaderAsync();
            while (await ar.ReadAsync())
                _alerts.Add(new DashboardAlert
                {
                    NamaObat = ar["NamaObat"]?.ToString() ?? "-",
                    Stok = Convert.ToInt32(ar["Stok"]),
                    TglExpired = ar["TanggalKadaluarsa"] == DBNull.Value ? "-" :
                        Convert.ToDateTime(ar["TanggalKadaluarsa"]).ToString("dd/MM/yyyy")
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Dashboard", "Gagal memuat data: " + ex.Message, "OK");
        }
    }
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
}
