<<<<<<< HEAD
using ApotekApp.Services;
using MySqlConnector;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ApotekApp;

public sealed class LaporanItem
{
    public DateTime Tanggal { get; set; }
    public string TanggalFormatted => Tanggal.ToString("dd/MM/yyyy HH:mm");
    public string NomorNota { get; set; } = "";
    public string NamaObat { get; set; } = "";
    public string NamaPihak { get; set; } = "";
    public decimal Qty { get; set; }
    public decimal TotalRp { get; set; }
    public decimal Penjualan { get; set; }
    public decimal Hpp { get; set; }
    public decimal ModalBeli { get; set; }
    public string Keterangan { get; set; } = "";
    public string QtyFormatted => Qty == 0 ? "-" : Qty.ToString("N0");
    public string TotalRpFormatted => TotalRp.ToString("N0");
}

public partial class LaporanPage : ContentPage
{
    private readonly ObservableCollection<LaporanItem> _rows = new();
    private readonly ObservableCollection<LaporanItem> _pageRows = new();
    private const int ReportPageSize = 15;
    private int _reportPage = 1;
    private string _currentJenis = "Penjualan";

    public LaporanPage() { InitializeComponent(); Init(); }

    private void Init()
    {
        TglMulaiPicker.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        TglSelesaiPicker.Date = DateTime.Today;
        JenisLaporanPicker.ItemsSource = new[] { "Penjualan", "Pembelian", "Laba", "Stok Obat", "Modal & Keuntungan" };
        JenisLaporanPicker.SelectedIndex = 0;
        BtnJenisLaporan.Text = "Penjualan  ▾";
        LaporanCollectionView.ItemsSource = _pageRows;
    }

    protected override async void OnAppearing() { base.OnAppearing(); await LoadAsync(); }
    private async void OnTampilkanClicked(object sender, EventArgs e) => await LoadAsync();
    private async void OnFilterChanged(object sender, EventArgs e) => await LoadAsync();

    private async void OnLaporanPenjualanClicked(object sender, EventArgs e) { await SelectJenisAsync("Penjualan"); }
    private async void OnLaporanPembelianClicked(object sender, EventArgs e) { await SelectJenisAsync("Pembelian"); }
    private void OnJenisLaporanToggleClicked(object sender, EventArgs e) => JenisOptionsPanel.IsVisible = !JenisOptionsPanel.IsVisible;
    private async void OnJenisOptionClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is null) return;
        await SelectJenisAsync(b.CommandParameter.ToString() ?? "Penjualan");
    }
    private async Task SelectJenisAsync(string jenis)
    {
        JenisLaporanPicker.SelectedItem = jenis;
        BtnJenisLaporan.Text = jenis == "Laba" ? "Laba / Rugi  ▾" : $"{jenis}  ▾";
        JenisOptionsPanel.IsVisible = false;
        await LoadAsync();
    }

=======
using MySqlConnector;

namespace ApotekApp;

public partial class LaporanPage : ContentPage
{
    public LaporanPage(ApotekApp.Data.AppDbContext context) { InitializeComponent(); Init(); }
    public LaporanPage() { InitializeComponent(); Init(); }
    void Init(){TglMulaiPicker.Date=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);TglSelesaiPicker.Date=DateTime.Today;JenisLaporanPicker.SelectedIndex=0;}
    protected override async void OnAppearing(){base.OnAppearing();await LoadAsync();}
    async void OnTampilkanClicked(object s,EventArgs e)=>await LoadAsync();
    async void OnFilterChanged(object s,EventArgs e)=>await LoadAsync();
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
    public async Task LoadAsync()
    {
        try
        {
            var awal = TglMulaiPicker.Date.Date;
            var akhir = TglSelesaiPicker.Date.Date.AddDays(1);
<<<<<<< HEAD
            _currentJenis = JenisLaporanPicker.SelectedItem?.ToString() ?? "Penjualan";
            _rows.Clear();

            await using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();

            switch (_currentJenis)
            {
                case "Penjualan": await LoadPenjualanAsync(c, awal, akhir); break;
                case "Pembelian": await LoadPembelianAsync(c, awal, akhir); break;
                case "Laba": await LoadLabaAsync(c, awal, akhir); break;
                case "Stok Obat": await LoadStokAsync(c); break;
                case "Modal & Keuntungan": await LoadModalKeuntunganAsync(c, awal, akhir); break;
            }

            _reportPage = 1;
            ApplyReportPagination();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Laporan", "Gagal memuat laporan:\n" + ex.Message, "OK");
        }
    }

    private async Task LoadPenjualanAsync(MySqlConnection c, DateTime awal, DateTime akhir)
    {
        const string sql = """
            SELECT p.tanggal, p.no_nota,
                   COALESCE(NULLIF(d.nama_obat,''), o.NamaObat, '-') AS nama_obat,
                   d.qty, d.harga, d.harga_beli, d.subtotal, p.grand_total, p.diskon, p.bayar
            FROM detail_penjualan d
            INNER JOIN penjualan p ON p.no_nota=d.no_nota
            LEFT JOIN obats o ON o.Id=d.id_obat
            WHERE p.tanggal>=@a AND p.tanggal<@b
            ORDER BY p.tanggal DESC, d.id;
            """;
        await using var cmd = new MySqlCommand(sql, c);
        cmd.Parameters.AddWithValue("@a", awal); cmd.Parameters.AddWithValue("@b", akhir);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var harga = ToDecimal(r["harga"]);
            var qty = ToDecimal(r["qty"]);
            _rows.Add(new LaporanItem
            {
                Tanggal=ParseDate(r["tanggal"]), NomorNota=GetText(r["no_nota"]), NamaObat=GetText(r["nama_obat"]),
                Qty=qty, TotalRp=ToDecimal(r["subtotal"]), Penjualan=harga, Hpp=ToDecimal(r["harga_beli"]),
                Keterangan=$"Diskon Rp {ToDecimal(r["diskon"]):N0} • Bayar Rp {ToDecimal(r["bayar"]):N0}"
            });
        }
    }

    private async Task LoadPembelianAsync(MySqlConnection c, DateTime awal, DateTime akhir)
    {
        const string sql = """
            SELECT p.TanggalPembelian, p.NoFaktur,
                   COALESCE(NULLIF(d.nama_obat,''), o.NamaObat, '-') AS nama_obat,
                   d.Jumlah, d.HargaBeli, (d.Jumlah*d.HargaBeli) subtotal,
                   COALESCE(NULLIF(p.nama_supplier,''), s.NamaSupplier, '-') supplier
            FROM detail_pembelians d
            INNER JOIN pembelians p ON p.IdPembelian=d.PembelianId
            LEFT JOIN obats o ON o.Id=d.ObatId
            LEFT JOIN suppliers s ON s.Id=p.SupplierId
            WHERE p.TanggalPembelian>=@a AND p.TanggalPembelian<@b
            ORDER BY p.TanggalPembelian DESC, d.IdDetailPembelian;
            """;
        await using var cmd = new MySqlCommand(sql, c);
        cmd.Parameters.AddWithValue("@a", awal); cmd.Parameters.AddWithValue("@b", akhir);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var qty=ToDecimal(r["Jumlah"]); var harga=ToDecimal(r["HargaBeli"]);
            _rows.Add(new LaporanItem
            { Tanggal=ParseDate(r["TanggalPembelian"]), NomorNota=GetText(r["NoFaktur"]), NamaObat=GetText(r["nama_obat"]),
              NamaPihak=GetText(r["supplier"]), Qty=qty, TotalRp=ToDecimal(r["subtotal"]), Hpp=harga, Penjualan=harga,
              Keterangan=$"Supplier: {GetText(r["supplier"])}" });
        }
    }

    private async Task LoadLabaAsync(MySqlConnection c, DateTime awal, DateTime akhir)
    {
        const string sql = """
            SELECT p.tanggal,p.no_nota,COALESCE(NULLIF(d.nama_obat,''),o.NamaObat,'-') nama_obat,
                   d.qty,d.subtotal,d.harga_beli,(d.qty*d.harga_beli) hpp
            FROM detail_penjualan d
            INNER JOIN penjualan p ON p.no_nota=d.no_nota
            LEFT JOIN obats o ON o.Id=d.id_obat
            WHERE p.tanggal>=@a AND p.tanggal<@b
            ORDER BY p.tanggal DESC,d.id;
            """;
        await using var cmd=new MySqlCommand(sql,c); cmd.Parameters.AddWithValue("@a",awal); cmd.Parameters.AddWithValue("@b",akhir);
        await using var r=await cmd.ExecuteReaderAsync();
        while(await r.ReadAsync())
        {
            var sales=ToDecimal(r["subtotal"]); var hpp=ToDecimal(r["hpp"]);
            _rows.Add(new LaporanItem { Tanggal=ParseDate(r["tanggal"]),NomorNota=GetText(r["no_nota"]),NamaObat=GetText(r["nama_obat"]),Qty=ToDecimal(r["qty"]),TotalRp=sales-hpp,Penjualan=sales,Hpp=hpp,Keterangan=$"Penjualan Rp {sales:N0} • HPP Rp {hpp:N0}" });
        }
    }

    private async Task LoadStokAsync(MySqlConnection c)
    {
        const string sql="SELECT KodeObat,NamaObat,Stok,HargaBeli,HargaJual FROM obats ORDER BY NamaObat;";
        await using var cmd=new MySqlCommand(sql,c); await using var r=await cmd.ExecuteReaderAsync();
        while(await r.ReadAsync())
        {
            var stok=ToDecimal(r["Stok"]); var beli=ToDecimal(r["HargaBeli"]); var jual=ToDecimal(r["HargaJual"]);
            _rows.Add(new LaporanItem { Tanggal=DateTime.Today,NomorNota=GetText(r["KodeObat"]),NamaObat=GetText(r["NamaObat"]),Qty=stok,TotalRp=stok*beli,Hpp=beli,Penjualan=jual,Keterangan=$"Modal Rp {stok*beli:N0} • Nilai Jual Rp {stok*jual:N0}" });
        }
    }

    private async Task LoadModalKeuntunganAsync(MySqlConnection c, DateTime awal, DateTime akhir)
    {
        // Ringkasan per produk: modal pembelian, penjualan, HPP dan laba kotor.
        const string sql = """
WITH beli AS (
    SELECT d.ObatId, COALESCE(NULLIF(d.nama_obat,''),o.NamaObat,'-') nama,
           SUM(d.Jumlah) qty_beli, SUM(d.Jumlah*d.HargaBeli) modal_beli
    FROM detail_pembelians d
    INNER JOIN pembelians p ON p.IdPembelian=d.PembelianId
    LEFT JOIN obats o ON o.Id=d.ObatId
    WHERE p.TanggalPembelian>=@a AND p.TanggalPembelian<@b
    GROUP BY d.ObatId, nama
), jual AS (
    SELECT d.id_obat ObatId, COALESCE(NULLIF(d.nama_obat,''),o.NamaObat,'-') nama,
           SUM(d.qty) qty_jual, SUM(d.subtotal) penjualan,
           SUM(d.qty*d.harga_beli) hpp
    FROM detail_penjualan d
    INNER JOIN penjualan p ON p.no_nota=d.no_nota
    LEFT JOIN obats o ON o.Id=d.id_obat
    WHERE p.tanggal>=@a AND p.tanggal<@b
    GROUP BY d.id_obat, nama
), gabung AS (
    SELECT COALESCE(b.ObatId,j.ObatId) ObatId,
           COALESCE(b.nama,j.nama,'-') nama,
           COALESCE(b.qty_beli,0) qty_beli, COALESCE(b.modal_beli,0) modal_beli,
           COALESCE(j.qty_jual,0) qty_jual, COALESCE(j.penjualan,0) penjualan,
           COALESCE(j.hpp,0) hpp
    FROM beli b LEFT JOIN jual j ON j.ObatId=b.ObatId
    UNION
    SELECT j.ObatId,j.nama,COALESCE(b.qty_beli,0),COALESCE(b.modal_beli,0),j.qty_jual,j.penjualan,j.hpp
    FROM jual j LEFT JOIN beli b ON b.ObatId=j.ObatId WHERE b.ObatId IS NULL
)
SELECT * FROM gabung ORDER BY nama;
""";
        await using var cmd=new MySqlCommand(sql,c); cmd.Parameters.AddWithValue("@a",awal); cmd.Parameters.AddWithValue("@b",akhir);
        await using var r=await cmd.ExecuteReaderAsync();
        while(await r.ReadAsync())
        {
            var modal=ToDecimal(r["modal_beli"]); var sales=ToDecimal(r["penjualan"]); var hpp=ToDecimal(r["hpp"]);
            _rows.Add(new LaporanItem
            {
                Tanggal=DateTime.Today,
                NomorNota=$"Beli {ToDecimal(r["qty_beli"]):N0} / Jual {ToDecimal(r["qty_jual"]):N0}",
                NamaObat=GetText(r["nama"]), Qty=ToDecimal(r["qty_jual"]), TotalRp=sales-hpp,
                Penjualan=sales, Hpp=hpp, ModalBeli=modal,
                Keterangan=$"Modal beli Rp {modal:N0} • Penjualan Rp {sales:N0} • HPP Rp {hpp:N0} • Laba Rp {sales-hpp:N0}"
            });
        }
    }

    private void ApplyReportPagination()
    {
        var total = _rows.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)ReportPageSize));
        if (_reportPage > totalPages) _reportPage = totalPages;
        if (_reportPage < 1) _reportPage = 1;
        _pageRows.Clear();
        foreach (var row in _rows.Skip((_reportPage - 1) * ReportPageSize).Take(ReportPageSize)) _pageRows.Add(row);
        var first = total == 0 ? 0 : ((_reportPage - 1) * ReportPageSize) + 1;
        var last = total == 0 ? 0 : Math.Min(_reportPage * ReportPageSize, total);
        LblReportPage.Text = $"Halaman {_reportPage} dari {totalPages} • Menampilkan {first}-{last} dari {total} data";
        BtnPrevReport.IsEnabled = _reportPage > 1;
        BtnNextReport.IsEnabled = _reportPage < totalPages;
    }

    private void OnPrevReportClicked(object sender, EventArgs e) { if (_reportPage <= 1) return; _reportPage--; ApplyReportPagination(); }
    private void OnNextReportClicked(object sender, EventArgs e) { var totalPages = Math.Max(1, (int)Math.Ceiling(_rows.Count / (double)ReportPageSize)); if (_reportPage >= totalPages) return; _reportPage++; ApplyReportPagination(); }

    private void UpdateSummary()
    {
        TotalTransaksiLabel.Text = $"{_rows.Count} Data";
        var totalPembelian = _rows.Where(x => _currentJenis == "Pembelian").Sum(x => x.TotalRp);
        var totalPenjualan = _rows.Where(x => _currentJenis == "Penjualan").Sum(x => x.TotalRp);
        var laba = _currentJenis == "Laba" ? _rows.Sum(x => x.Penjualan - x.Hpp) : 0m;

        if (_currentJenis == "Penjualan")
        {
            TotalNominalLabel.Text = $"Rp {totalPenjualan:N0}";
            RataRataLabel.Text = _rows.Count == 0 ? "Belum ada penjualan" : $"Rata-rata Rp {totalPenjualan / _rows.Count:N0}";
            LabaKotorLabel.Text = $"Rp {_rows.Sum(x => x.TotalRp - (x.Qty * x.Hpp)):N0}";
            return;
        }
        if (_currentJenis == "Pembelian")
        {
            TotalNominalLabel.Text = $"Rp {totalPembelian:N0}";
            RataRataLabel.Text = _rows.Count == 0 ? "Belum ada pembelian" : $"Rata-rata Rp {totalPembelian / _rows.Count:N0}";
            LabaKotorLabel.Text = "Modal keluar";
            return;
        }
        if (_currentJenis == "Laba")
        {
            var s = _rows.Sum(x => x.Penjualan); var h = _rows.Sum(x => x.Hpp);
            TotalNominalLabel.Text = $"Rp {s:N0}";
            RataRataLabel.Text = $"HPP Rp {h:N0}";
            LabaKotorLabel.Text = $"Rp {laba:N0}";
            return;
        }
        if (_currentJenis == "Stok Obat")
        {
            var q = _rows.Sum(x => x.Qty); var m = _rows.Sum(x => x.TotalRp); var j = _rows.Sum(x => x.Qty * x.Penjualan);
            TotalNominalLabel.Text = $"{q:N0} Unit";
            RataRataLabel.Text = $"Modal Rp {m:N0}";
            LabaKotorLabel.Text = $"Rp {j - m:N0}";
            return;
        }
        if (_currentJenis == "Modal & Keuntungan")
        {
            var sales = _rows.Sum(x => x.Penjualan); var hpp = _rows.Sum(x => x.Hpp);
            var modalBeli = _rows.Sum(x => x.ModalBeli);
            TotalNominalLabel.Text = $"Modal Beli Rp {modalBeli:N0}";
            RataRataLabel.Text = $"Penjualan Rp {sales:N0} • HPP Rp {hpp:N0}";
            LabaKotorLabel.Text = $"Rp {sales - hpp:N0}";
            return;
        }
        var total = _rows.Sum(x => x.TotalRp);
        TotalNominalLabel.Text = $"Rp {total:N0}";
        RataRataLabel.Text = _rows.Count == 0 ? "Rp 0" : $"Rata-rata Rp {total / _rows.Count:N0}";
        LabaKotorLabel.Text = "Rp 0";
    }

    private async void OnCetakClicked(object sender, EventArgs e)
    {
        try
        {
            var rows=_rows.Select(x=>new[]{x.TanggalFormatted,x.NomorNota,x.NamaObat,x.QtyFormatted,x.TotalRpFormatted,x.NamaPihak,x.Keterangan}).ToList();
            var title=$"LAPORAN {_currentJenis.ToUpperInvariant()}";
            var subtitle=$"Periode {TglMulaiPicker.Date:dd/MM/yyyy} - {TglSelesaiPicker.Date:dd/MM/yyyy}";
            var headers=new[]{"TANGGAL","NOMOR","NAMA OBAT","QTY","NOMINAL","SUPPLIER","KETERANGAN"};
            var body=BrowserPrintService.ReportHtml(title,subtitle,headers,rows,BuildReportTotal());
            var css=@"
.report{width:100%;max-width:1200px;margin:0 auto}.report h1{margin:0 0 4px;font-size:22px}.subtitle{color:#64748B;margin-bottom:16px;font-size:12px}
table{width:100%;border-collapse:collapse;font-size:10px}th,td{border:1px solid #CBD5E1;padding:6px;text-align:left;vertical-align:top}th{background:#F1F5F9;font-weight:700}.report-footer{margin-top:12px;font-weight:700}
@page{size:A4 landscape;margin:10mm}
";
            var ok=await BrowserPrintService.OpenPrintPreviewAsync(title,body,css);
            if(!ok) await DisplayAlert("Cetak Laporan","Browser tidak dapat dibuka untuk menampilkan Print Preview.","OK");
        }
        catch(Exception ex){await DisplayAlert("Cetak Laporan", "Gagal mencetak:\n" + ex.Message, "OK");}
    }

    private string BuildReportTotal()
    {
        if(_currentJenis=="Laba") return $"PENJUALAN: Rp {_rows.Sum(x=>x.Penjualan):N0} | HPP: Rp {_rows.Sum(x=>x.Hpp):N0} | LABA: Rp {_rows.Sum(x=>x.TotalRp):N0}";
        if(_currentJenis=="Stok Obat") return $"TOTAL STOK: {_rows.Sum(x=>x.Qty):N0} UNIT | MODAL: Rp {_rows.Sum(x=>x.TotalRp):N0} | POTENSI LABA: Rp {_rows.Sum(x=>x.Qty*x.Penjualan)-_rows.Sum(x=>x.TotalRp):N0}";
        if(_currentJenis=="Penjualan") return $"PENJUALAN: Rp {_rows.Sum(x=>x.TotalRp):N0} | HPP: Rp {_rows.Sum(x=>x.Qty*x.Hpp):N0} | LABA KOTOR: Rp {_rows.Sum(x=>x.TotalRp)-_rows.Sum(x=>x.Qty*x.Hpp):N0}";
        if(_currentJenis=="Modal & Keuntungan") return $"MODAL PEMBELIAN: Rp {_rows.Sum(x=>x.ModalBeli):N0} | PENJUALAN: Rp {_rows.Sum(x=>x.Penjualan):N0} | HPP TERJUAL: Rp {_rows.Sum(x=>x.Hpp):N0} | LABA KOTOR: Rp {_rows.Sum(x=>x.Penjualan)-_rows.Sum(x=>x.Hpp):N0}";
        if(_currentJenis=="Pembelian") return $"TOTAL PEMBELIAN / MODAL KELUAR: Rp {_rows.Sum(x=>x.TotalRp):N0}";
        return $"TOTAL: Rp {_rows.Sum(x=>x.TotalRp):N0}";
    }

    private async void OnExportExcelClicked(object sender, EventArgs e)
    {
        try
        {
            var rows=_rows.Select(x=>new[]{x.TanggalFormatted,x.NomorNota,x.NamaObat,x.Qty.ToString("0.##",CultureInfo.InvariantCulture),x.TotalRp.ToString("0.##",CultureInfo.InvariantCulture),x.NamaPihak,x.Penjualan.ToString("0.##",CultureInfo.InvariantCulture),x.Hpp.ToString("0.##",CultureInfo.InvariantCulture),x.Keterangan});
            await ExcelService.SaveAsync($"Laporan_{_currentJenis.Replace(" & ","_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",new[]{"Tanggal","Nomor","Nama Obat","Qty","Nominal","Supplier","Penjualan","HPP","Keterangan"},rows);
            await DisplayAlert("Export Excel","Laporan berhasil diekspor ke Excel (.xlsx).","OK");
        }catch(Exception ex){await DisplayAlert("Export Excel",ex.Message,"OK");}
    }

    private static string GetText(object value)=>value==DBNull.Value||value is null?"-":value.ToString()??"-";
    private static decimal ToDecimal(object value)=>value==DBNull.Value?0:Convert.ToDecimal(value,CultureInfo.InvariantCulture);
    private static DateTime ParseDate(object value)=>DateTime.TryParse(value?.ToString(),out var date)?date:DateTime.MinValue;
=======
            string jenis = JenisLaporanPicker.SelectedItem?.ToString() ?? "Penjualan";
            using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();
            var rows = new List<LaporanItem>();

            if (jenis == "Penjualan")
            {
                const string sql = @"SELECT no_nota,tanggal,grand_total FROM penjualan WHERE tanggal>=@a AND tanggal<@b ORDER BY tanggal DESC";
                using var cmd = new MySqlCommand(sql, c);
                cmd.Parameters.AddWithValue("@a", awal); cmd.Parameters.AddWithValue("@b", akhir);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    rows.Add(new LaporanItem { Tanggal = Convert.ToDateTime(r["tanggal"]), NomorNota = r["no_nota"].ToString() ?? "", NamaUser = "Kasir", TotalRp = Convert.ToDecimal(r["grand_total"]) });
            }
            else if (jenis == "Pembelian")
            {
                const string sql = @"SELECT p.NoFaktur,p.TanggalPembelian,p.TotalHarga,COALESCE(s.NamaSupplier,'-') AS NamaSupplier FROM pembelians p LEFT JOIN suppliers s ON s.Id=p.SupplierId WHERE p.TanggalPembelian>=@a AND p.TanggalPembelian<@b ORDER BY p.TanggalPembelian DESC";
                using var cmd = new MySqlCommand(sql, c);
                cmd.Parameters.AddWithValue("@a", awal); cmd.Parameters.AddWithValue("@b", akhir);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    rows.Add(new LaporanItem { Tanggal = Convert.ToDateTime(r["TanggalPembelian"]), NomorNota = r["NoFaktur"].ToString() ?? "", NamaUser = r["NamaSupplier"].ToString() ?? "-", TotalRp = Convert.ToDecimal(r["TotalHarga"]) });
            }
            else
            {
                using var cmd = new MySqlCommand("SELECT KodeObat,NamaObat,Stok,HargaJual FROM obats ORDER BY NamaObat", c);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    rows.Add(new LaporanItem { Tanggal = DateTime.Today, NomorNota = r["KodeObat"].ToString() ?? "", NamaUser = r["NamaObat"].ToString() ?? "", TotalRp = Convert.ToDecimal(r["Stok"]) });
            }

            LaporanCollectionView.ItemsSource = rows;
            TotalTransaksiLabel.Text = $"{rows.Count} Data";
            TotalNominalLabel.Text = jenis == "Stok Obat" ? $"{rows.Sum(x => x.TotalRp):N0} Unit" : $"Rp {rows.Sum(x => x.TotalRp):N0}";
            RataRataLabel.Text = rows.Count == 0 ? (jenis == "Stok Obat" ? "0 Unit" : "Rp 0") : jenis == "Stok Obat" ? $"{rows.Average(x => x.TotalRp):N0} Unit" : $"Rp {rows.Average(x => x.TotalRp):N0}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Laporan", "Gagal memuat riwayat: " + ex.Message, "OK");
        }
    }

    async void OnCetakClicked(object s,EventArgs e)=>await DisplayAlert("Laporan","Laporan siap dicetak dari data yang tampil.","OK");
}
public class LaporanItem
{
    public DateTime Tanggal{get;set;} public string TanggalFormatted=>Tanggal.ToString("dd/MM/yyyy HH:mm");
    public string NomorNota{get;set;}=""; public string NamaUser{get;set;}=""; public decimal TotalRp{get;set;}
    public string TotalRpFormatted=>TotalRp.ToString("N0");
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
}
