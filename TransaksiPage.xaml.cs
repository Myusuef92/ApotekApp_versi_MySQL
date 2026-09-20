<<<<<<< HEAD
using ApotekApp.Services;
=======
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ApotekApp;

public partial class TransaksiPage : ContentPage
{
<<<<<<< HEAD
    private readonly string _connString = MauiProgram.ConnectionString;
=======
    private readonly string _connString = "Server=localhost;Database=db_apotek;Uid=root;Pwd=;";
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
    public ObservableCollection<CartItemModel> ListKeranjang { get; set; } = new();

    private decimal _persenPpn = 0;
    private string _ukuranKertas = "58mm";

    // VARIABEL PENYIMPANAN TEMPORER STRUK UNTUK PROSES PRINT
    private string _lastNoNota = "";
    private DateTime _lastTanggal = DateTime.Now;
    private decimal _lastTotal = 0;
    private decimal _lastDiskon = 0;
    private decimal _lastPpn = 0;
    private decimal _lastGrandTotal = 0;
    private decimal _lastBayar = 0;
    private decimal _lastKembali = 0;
    private string _lastNamaApotek = "APOTEK SEHAT";
    private string _lastAlamat = "";
    private string _lastTelp = "";
    private string _lastSia = "";
    private string _lastFooter = "-- Terima Kasih --";
<<<<<<< HEAD
=======
    private string _lastLogoPath = "";
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
    private List<CartItemModel> _lastCartSnapshot = new();

    public TransaksiPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadPengaturanApotek();
        UpdateTampilanKeranjang();
    }

    public async void LoadPengaturanApotek()
    {
        try
        {
            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();

            string query = "SELECT * FROM pengaturan WHERE id = 1 LIMIT 1";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                _persenPpn = Convert.ToDecimal(reader["pajak_ppn"]);
                _ukuranKertas = reader["ukuran_kertas"]?.ToString() ?? "58mm";
            }
        }
        catch
        {
            _persenPpn = 0;
            _ukuranKertas = "58mm";
        }

        if (LblLabelPpn != null) LblLabelPpn.Text = $"PPN ({_persenPpn:G29}%)";
        HitungRingkasan();
    }

    // =========================================================
    // 1. CARI / SCAN OBAT (BARCODE, KODE OBAT, DAN NAMA OBAT)
    // =========================================================
    private void OnScanBarcodeCompleted(object sender, EventArgs e)
    {
        ProsesCariObat(TxtBarcode?.Text);
    }

    private void OnCariObatClicked(object sender, EventArgs e)
    {
        ProsesCariObat(TxtBarcode?.Text);
    }

    private async void ProsesCariObat(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;

        try
        {
            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();

            string query = @"SELECT * FROM obats 
                            WHERE Barcode = @Key 
                               OR KodeObat = @Key 
                               OR LOWER(NamaObat) LIKE LOWER(@KeyLike) 
                            LIMIT 1";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Key", keyword.Trim());
            cmd.Parameters.AddWithValue("@KeyLike", $"%{keyword.Trim()}%");

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.Read())
            {
                int id = Convert.ToInt32(reader["id"]);
                string nama = reader["NamaObat"].ToString();
                decimal harga = Convert.ToDecimal(reader["HargaJual"]);
                int stok = Convert.ToInt32(reader["Stok"]);

                if (stok <= 0)
                {
                    await DisplayAlert("Stok Habis", $"Stok obat '{nama}' sudah habis!", "OK");
                    return;
                }

                var existing = ListKeranjang.FirstOrDefault(x => x.IdObat == id);
                if (existing != null)
                {
                    if (existing.Qty + 1 > stok)
                    {
                        await DisplayAlert("Stok Tidak Cukup", $"Stok obat '{nama}' hanya tersisa {stok}.", "OK");
                        return;
                    }
                    existing.Qty++;
                    existing.Subtotal = existing.Qty * existing.Harga;
                }
                else
                {
                    ListKeranjang.Add(new CartItemModel
                    {
                        IdObat = id,
                        NamaObat = nama,
                        Harga = harga,
                        Qty = 1,
                        Subtotal = harga,
                        StokMaksimal = stok
                    });
                }

                TxtBarcode.Text = string.Empty;
                TxtBarcode.Focus();
                UpdateTampilanKeranjang();
            }
            else
            {
                await DisplayAlert("Tidak Ditemukan", $"Obat dengan pencarian '{keyword}' tidak ditemukan.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Database", ex.Message, "OK");
        }
    }

    // =========================================================
    // 2. LOGIKA KERANJANG & RINGKASAN HARGA
    // =========================================================
    private void OnQtyItemChanged(object sender, TextChangedEventArgs e)
    {
        var entry = sender as Entry;
        var item = entry?.BindingContext as CartItemModel;

        if (item != null && int.TryParse(entry.Text, out int newQty))
        {
            if (newQty > item.StokMaksimal)
            {
                DisplayAlert("Stok Melebihi Maksimal", $"Stok tersedia hanya {item.StokMaksimal}", "OK");
                item.Qty = item.StokMaksimal;
                entry.Text = item.StokMaksimal.ToString();
            }
            else if (newQty <= 0)
            {
                item.Qty = 1;
                entry.Text = "1";
            }
            else
            {
                item.Qty = newQty;
            }

            item.Subtotal = item.Qty * item.Harga;
            HitungRingkasan();
        }
    }

    private void OnHapusItemClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var item = button?.BindingContext as CartItemModel;
        if (item != null)
        {
            ListKeranjang.Remove(item);
            UpdateTampilanKeranjang();
        }
    }

    private void UpdateTampilanKeranjang()
    {
        int no = 1;
        foreach (var item in ListKeranjang)
        {
            item.No = no++;
        }

        if (ContainerKeranjang != null)
        {
            BindableLayout.SetItemsSource(ContainerKeranjang, ListKeranjang);
        }

        HitungRingkasan();
    }

    private void HitungRingkasan()
    {
        decimal total = ListKeranjang.Sum(x => x.Subtotal);
        decimal diskon = decimal.TryParse(TxtDiskon?.Text, out decimal d) ? d : 0;
        decimal dpp = total - diskon;
        if (dpp < 0) dpp = 0;

        decimal ppn = dpp * (_persenPpn / 100m);
        decimal grandTotal = dpp + ppn;

        decimal bayar = decimal.TryParse(TxtNominalBayar?.Text, out decimal b) ? b : 0;
        decimal kembalian = bayar - grandTotal;

        if (LblTotal != null) LblTotal.Text = $"Rp {total:N0}";
        if (LblPpn != null) LblPpn.Text = $"Rp {ppn:N0}";
        if (LblGrandTotal != null) LblGrandTotal.Text = $"Rp {grandTotal:N0}";

        if (LblKembalian != null)
        {
            if (kembalian >= 0)
            {
                LblKembalian.Text = $"Rp {kembalian:N0}";
                LblKembalian.TextColor = Microsoft.Maui.Graphics.Color.Parse("#16A34A");
            }
            else
            {
                LblKembalian.Text = "Kurang!";
                LblKembalian.TextColor = Microsoft.Maui.Graphics.Color.Parse("#DC2626");
            }
        }
    }

    private void OnDiskonChanged(object sender, TextChangedEventArgs e) => HitungRingkasan();
    private void OnNominalBayarChanged(object sender, TextChangedEventArgs e) => HitungRingkasan();

    // =========================================================
    // 3. PROSES BAYAR & PENYIMPANAN DATABASE
    // =========================================================
    private async void OnBayarCetakClicked(object sender, EventArgs e)
    {
        if (ListKeranjang.Count == 0)
        {
            await DisplayAlert("Peringatan", "Keranjang belanja masih kosong!", "OK");
            return;
        }

        decimal total = ListKeranjang.Sum(x => x.Subtotal);
        decimal diskon = decimal.TryParse(TxtDiskon?.Text, out decimal d) ? d : 0;
        decimal dpp = total - diskon;
        if (dpp < 0) dpp = 0;

        decimal ppn = dpp * (_persenPpn / 100m);
        decimal grandTotal = dpp + ppn;
        decimal bayar = decimal.TryParse(TxtNominalBayar?.Text, out decimal b) ? b : 0;

        if (bayar < grandTotal)
        {
            await DisplayAlert("Peringatan", "Nominal pembayaran masih kurang!", "OK");
            return;
        }

        decimal kembalian = bayar - grandTotal;
        string noNota = "PJ-" + DateTime.Now.ToString("yyyyMMddHHmmss");

        try
        {
            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();
<<<<<<< HEAD
            using var trans = conn.BeginTransaction();
=======
            using var trans = await conn.BeginTransactionAsync();
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a

            try
            {
                // A. Insert ke tabel 'penjualan'
<<<<<<< HEAD
                string qPenjualan = @"INSERT INTO penjualan (no_nota, tanggal, total, diskon, grand_total, bayar, kembali, user_id) 
                                     VALUES (@Nota, @Tanggal, @Total, @Diskon, @GrandTotal, @Bayar, @Kembali, @UserId)";
=======
                string qPenjualan = @"INSERT INTO penjualan (no_nota, tanggal, total, diskon, grand_total, bayar, kembali) 
                                     VALUES (@Nota, NOW(), @Total, @Diskon, @GrandTotal, @Bayar, @Kembali)";
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a

                using (var cmd = new MySqlCommand(qPenjualan, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@Nota", noNota);
<<<<<<< HEAD
                    cmd.Parameters.AddWithValue("@Tanggal", DateTime.Now);
=======
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.Parameters.AddWithValue("@Diskon", diskon);
                    cmd.Parameters.AddWithValue("@GrandTotal", grandTotal);
                    cmd.Parameters.AddWithValue("@Bayar", bayar);
                    cmd.Parameters.AddWithValue("@Kembali", kembalian);
<<<<<<< HEAD
                    var currentUserId = Preferences.Get("CurrentUserId", 0);
                    cmd.Parameters.AddWithValue("@UserId", currentUserId > 0 ? currentUserId : DBNull.Value);
=======
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                    await cmd.ExecuteNonQueryAsync();
                }

                // B. Insert detail & potong stok
                string detailStrukText = "";
                foreach (var item in ListKeranjang)
                {
<<<<<<< HEAD
                    string qDetail = @"INSERT INTO detail_penjualan (no_nota, id_obat, nama_obat, harga, harga_beli, qty, subtotal) 
                                      VALUES (@Nota, @IdObat, @NamaObat, @Harga, @HargaBeli, @Qty, @Subtotal)";
=======
                    string qDetail = @"INSERT INTO detail_penjualan (no_nota, id_obat, harga, qty, subtotal) 
                                      VALUES (@Nota, @IdObat, @Harga, @Qty, @Subtotal)";
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                    using (var cmdD = new MySqlCommand(qDetail, conn, trans))
                    {
                        cmdD.Parameters.AddWithValue("@Nota", noNota);
                        cmdD.Parameters.AddWithValue("@IdObat", item.IdObat);
<<<<<<< HEAD
                        cmdD.Parameters.AddWithValue("@NamaObat", item.NamaObat);
                        cmdD.Parameters.AddWithValue("@Harga", item.Harga);
                        cmdD.Parameters.AddWithValue("@HargaBeli", GetHargaBeli(conn, trans, item.IdObat));
=======
                        cmdD.Parameters.AddWithValue("@Harga", item.Harga);
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                        cmdD.Parameters.AddWithValue("@Qty", item.Qty);
                        cmdD.Parameters.AddWithValue("@Subtotal", item.Subtotal);
                        await cmdD.ExecuteNonQueryAsync();
                    }

<<<<<<< HEAD
                    string qStok = "UPDATE obats SET Stok = Stok - @Qty WHERE id = @IdObat AND Stok >= @Qty";
=======
                    string qStok = "UPDATE obats SET Stok = Stok - @Qty WHERE id = @IdObat";
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                    using (var cmdS = new MySqlCommand(qStok, conn, trans))
                    {
                        cmdS.Parameters.AddWithValue("@Qty", item.Qty);
                        cmdS.Parameters.AddWithValue("@IdObat", item.IdObat);
<<<<<<< HEAD
                        var stockRows = await cmdS.ExecuteNonQueryAsync();
                        if (stockRows != 1) throw new InvalidOperationException($"Stok obat '{item.NamaObat}' berubah atau tidak mencukupi. Transaksi dibatalkan.");
=======
                        await cmdS.ExecuteNonQueryAsync();
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                    }

                    detailStrukText += $"{item.NamaObat}\n  {item.Qty} x Rp {item.Harga:N0} = Rp {item.Subtotal:N0}\n";
                }

                // C. Baca Pengaturan Profil Apotek (Database + Preferences Fallback)
                string namaApotek = Preferences.Get("NamaApotek", "APOTEK SEHAT");
                string alamat = Preferences.Get("AlamatApotek", "Jl. Kesehatan No. 1, Jakarta");
                string telp = Preferences.Get("TeleponApotek", "0812-3456-7890");
                string sia = "SIA: 440/001/SIA/2026";
                string footer = "-- Terima Kasih Semoga Lekas Sembuh --";
<<<<<<< HEAD
=======
                string logoPath = Preferences.Get("AppLogoPath", string.Empty);
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a

                using (var cmdSet = new MySqlCommand("SELECT * FROM pengaturan WHERE id = 1 LIMIT 1", conn, trans))
                {
                    using var rSet = await cmdSet.ExecuteReaderAsync();
                    if (rSet.Read())
                    {
                        if (rSet["nama_apotek"] != DBNull.Value && !string.IsNullOrWhiteSpace(rSet["nama_apotek"].ToString()))
                            namaApotek = rSet["nama_apotek"].ToString();
                        if (rSet["alamat"] != DBNull.Value && !string.IsNullOrWhiteSpace(rSet["alamat"].ToString()))
                            alamat = rSet["alamat"].ToString();
                        if (rSet["no_telepon"] != DBNull.Value && !string.IsNullOrWhiteSpace(rSet["no_telepon"].ToString()))
                            telp = rSet["no_telepon"].ToString();
                        if (rSet["sia_sipa"] != DBNull.Value && !string.IsNullOrWhiteSpace(rSet["sia_sipa"].ToString()))
                            sia = rSet["sia_sipa"].ToString();
                        if (rSet["catatan_struk"] != DBNull.Value && !string.IsNullOrWhiteSpace(rSet["catatan_struk"].ToString()))
                            footer = rSet["catatan_struk"].ToString();
<<<<<<< HEAD
=======
                        if (rSet["logo_path"] != DBNull.Value && !string.IsNullOrWhiteSpace(rSet["logo_path"].ToString()))
                            logoPath = rSet["logo_path"].ToString();
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                    }
                }

                await trans.CommitAsync();

                // D. SIMPAN TEMPORER DATA UNTUK DI-PRINT SISTEM
                _lastNoNota = noNota;
                _lastTanggal = DateTime.Now;
                _lastTotal = total;
                _lastDiskon = diskon;
                _lastPpn = ppn;
                _lastGrandTotal = grandTotal;
                _lastBayar = bayar;
                _lastKembali = kembalian;
                _lastNamaApotek = namaApotek;
                _lastAlamat = alamat;
                _lastTelp = telp;
                _lastSia = sia;
                _lastFooter = footer;
<<<<<<< HEAD
=======
                _lastLogoPath = logoPath;
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                _lastCartSnapshot = ListKeranjang.Select(x => new CartItemModel
                {
                    NamaObat = x.NamaObat,
                    Qty = x.Qty,
                    Harga = x.Harga,
                    Subtotal = x.Subtotal
                }).ToList();

                // E. Penyesuaian Ukuran Frame Popup
                if (FrameStruk != null)
                {
                    FrameStruk.WidthRequest = _ukuranKertas == "80mm" ? 420 : 340;
                }

<<<<<<< HEAD
=======
                // F. Tampilkan Data ke Popup Modal
                if (ImgStrukLogo != null)
                {
                    if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                    {
                        ImgStrukLogo.Source = ImageSource.FromFile(logoPath);
                        ImgStrukLogo.IsVisible = true;
                    }
                    else
                    {
                        ImgStrukLogo.IsVisible = false;
                    }
                }

>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                if (LblStrukNamaApotek != null) LblStrukNamaApotek.Text = namaApotek;
                if (LblStrukAlamat != null)
                {
                    string infoDetail = alamat;
                    if (!string.IsNullOrWhiteSpace(telp)) infoDetail += $"\nTelp: {telp}";
                    if (!string.IsNullOrWhiteSpace(sia)) infoDetail += $" | {sia}";
                    LblStrukAlamat.Text = infoDetail;
                }

                if (LblStrukNota != null) LblStrukNota.Text = $"Nota: {noNota}";
                if (LblStrukTanggal != null) LblStrukTanggal.Text = $"Tgl: {_lastTanggal:dd/MM/yyyy HH:mm}";
                if (LblStrukDetail != null) LblStrukDetail.Text = detailStrukText;
                if (LblStrukTotal != null) LblStrukTotal.Text = $"Rp {total:N0}";
                if (LblStrukDiskon != null) LblStrukDiskon.Text = $"Rp {diskon:N0}";
                if (LblStrukLabelPpn != null) LblStrukLabelPpn.Text = $"PPN ({_persenPpn:G29}%)";
                if (LblStrukPpn != null) LblStrukPpn.Text = $"Rp {ppn:N0}";
                if (LblStrukGrandTotal != null) LblStrukGrandTotal.Text = $"Rp {grandTotal:N0}";
                if (LblStrukBayar != null) LblStrukBayar.Text = $"Rp {bayar:N0}";
                if (LblStrukKembali != null) LblStrukKembali.Text = $"Rp {kembalian:N0}";
                if (LblStrukFooter != null) LblStrukFooter.Text = footer;

<<<<<<< HEAD
                // Setelah pembayaran berhasil, pengguna menentukan sendiri
                // apakah struk ingin dicetak atau tidak. Tidak ada print otomatis.
=======
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
                if (ModalStruk != null) ModalStruk.IsVisible = true;
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Transaksi", ex.Message, "OK");
        }
    }

    // =========================================================
    // 4. ACTION MODAL STRUK (MENGARAH KE SISTEM PRINT)
    // =========================================================
    private async void OnCetakStrukClicked(object sender, EventArgs e)
    {
        try
        {
<<<<<<< HEAD
            var items = _lastCartSnapshot.Select(x => (x.NamaObat, x.Qty, x.Harga, x.Subtotal));
            var body = BrowserPrintService.ReceiptHtml(
                _lastNamaApotek, _lastAlamat, _lastTelp, _lastSia,
                _lastNoNota, _lastTanggal, items, _lastTotal, _lastDiskon,
                _lastPpn, _lastGrandTotal, _lastBayar, _lastKembali, _lastFooter, _ukuranKertas);

            var width = _ukuranKertas == "80mm" ? "80mm" : "58mm";
            var css = $@"
.receipt{{width:{width};max-width:{width};margin:0 auto;background:#fff;color:#111827;font-size:10px;line-height:1.35;padding:0}}
.receipt header{{text-align:center}} .store{{font-size:16px;font-weight:700}}
.receipt hr{{border:0;border-top:1px dashed #111827;margin:8px 0}}
.meta{{font-size:10px}} .item{{margin:5px 0}} .item .name{{font-weight:700}}
.item span,.totals span{{float:right}} .totals>div{{clear:both;overflow:hidden;margin:3px 0}}
.grand{{font-weight:700;font-size:12px}} .change{{font-weight:700}} .footer{{text-align:center;margin-top:10px}}
@page{{size:{width} auto;margin:0}}
@media screen{{body{{background:#e5e7eb;padding:18px}}.receipt{{box-shadow:0 2px 10px rgba(0,0,0,.12);padding:3mm}}}}
@media print{{html,body{{width:{width};margin:0;padding:0;background:#fff}}.receipt{{width:{width};max-width:{width};margin:0;padding:0;box-shadow:none}}}}
";

            var ok = await BrowserPrintService.OpenPrintPreviewAsync($"Struk {_lastNoNota}", body, css);
            if (!ok)
            {
                await DisplayAlert("Cetak Struk", "Browser tidak dapat dibuka untuk menampilkan Print Preview.", "OK");
                return;
            }

            if (ModalStruk != null) ModalStruk.IsVisible = false;
            OnBatalClicked(null, EventArgs.Empty);
=======
            string logoHtml = "";
            if (!string.IsNullOrEmpty(_lastLogoPath) && File.Exists(_lastLogoPath))
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(_lastLogoPath);
                string base64Image = Convert.ToBase64String(imageBytes);
                logoHtml = $"<img src='data:image/png;base64,{base64Image}' style='max-height:60px; margin-bottom:5px;' /><br/>";
            }

            var itemRowsHtml = new StringBuilder();
            foreach (var item in _lastCartSnapshot)
            {
                itemRowsHtml.AppendLine($@"
                    <div style='margin-bottom: 5px;'>
                        <div style='font-weight: bold;'>{item.NamaObat}</div>
                        <div style='display:flex; justify-content:space-between;'>
                            <span>{item.Qty} x Rp {item.Harga:N0}</span>
                            <span>Rp {item.Subtotal:N0}</span>
                        </div>
                    </div>");
            }

            string paperWidthCss = _ukuranKertas == "80mm" ? "72mm" : "52mm";

            string htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'/>
                <title>Struk Pembayaran</title>
                <style>
                    @page {{ size: {_ukuranKertas} auto; margin: 0; }}
                    body {{
                        font-family: 'Courier New', Courier, monospace;
                        width: {paperWidthCss};
                        margin: 0 auto;
                        padding: 8px;
                        font-size: 11px;
                        color: #000;
                    }}
                    .text-center {{ text-align: center; }}
                    .line {{ border-bottom: 1px dashed #000; margin: 6px 0; }}
                    .flex {{ display: flex; justify-content: space-between; }}
                    .bold {{ font-weight: bold; }}
                </style>
                <script>
                    window.onload = function() {{
                        window.print();
                    }};
                </script>
            </head>
            <body>
                <div class='text-center'>
                    {logoHtml}
                    <div style='font-size:14px; font-weight:bold;'>{_lastNamaApotek}</div>
                    <div>{_lastAlamat}</div>
                    <div>Telp: {_lastTelp}</div>
                    {(!string.IsNullOrWhiteSpace(_lastSia) ? $"<div>{_lastSia}</div>" : "")}
                </div>

                <div class='line'></div>

                <div>Nota: {_lastNoNota}</div>
                <div>Tgl : {_lastTanggal:dd/MM/yyyy HH:mm}</div>

                <div class='line'></div>

                {itemRowsHtml}

                <div class='line'></div>

                <div class='flex'><span>Subtotal</span><span>Rp {_lastTotal:N0}</span></div>
                <div class='flex'><span>Diskon</span><span>Rp {_lastDiskon:N0}</span></div>
                <div class='flex'><span>PPN ({_persenPpn:G29}%)</span><span>Rp {_lastPpn:N0}</span></div>
                <div class='flex bold' style='font-size:12px;'><span>Grand Total</span><span>Rp {_lastGrandTotal:N0}</span></div>
                <div class='flex'><span>Bayar</span><span>Rp {_lastBayar:N0}</span></div>
                <div class='flex bold'><span>Kembali</span><span>Rp {_lastKembali:N0}</span></div>

                <div class='line'></div>

                <div class='text-center' style='margin-top:10px;'>
                    {_lastFooter}
                </div>
            </body>
            </html>";

            string filePath = Path.Combine(FileSystem.CacheDirectory, "struk_pembayaran.html");
            await File.WriteAllTextAsync(filePath, htmlContent);

            // BUKA FILE KE SISTEM WINDOWS UNTUK DI-PRINT
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });

            if (ModalStruk != null) ModalStruk.IsVisible = false;
            OnBatalClicked(null, null);
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Cetak Struk", ex.Message, "OK");
        }
    }

    private void OnSelesaiTanpaCetakClicked(object sender, EventArgs e)
    {
        if (ModalStruk != null) ModalStruk.IsVisible = false;
        OnBatalClicked(null, null);
    }

    private void OnBatalClicked(object sender, EventArgs e)
    {
        ListKeranjang.Clear();
        if (TxtDiskon != null) TxtDiskon.Text = "0";
        if (TxtNominalBayar != null) TxtNominalBayar.Text = "0";
        if (TxtBarcode != null) TxtBarcode.Text = string.Empty;
        UpdateTampilanKeranjang();
    }

    private void OnHoldClicked(object sender, EventArgs e)
    {
        DisplayAlert("Info", "Transaksi berhasil ditahan sementara.", "OK");
    }
    public void InitializeForHost()
    {
        LoadPengaturanApotek();
        UpdateTampilanKeranjang();
    }

<<<<<<< HEAD
    private static decimal GetHargaBeli(MySqlConnection conn, MySqlTransaction trans, int obatId)
    {
        using var cmd = new MySqlCommand("SELECT COALESCE(HargaBeli,0) FROM obats WHERE Id=@id LIMIT 1", conn, trans);
        cmd.Parameters.AddWithValue("@id", obatId);
        return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0);
    }


}



=======

}

>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
// MODEL DATA ITEM KERANJANG
public class CartItemModel : INotifyPropertyChanged
{
    private int _no;
    private int _qty;
    private decimal _subtotal;

    public int No { get => _no; set { _no = value; OnPropertyChanged(); } }
    public int IdObat { get; set; }
    public string NamaObat { get; set; } = "";
    public decimal Harga { get; set; }
    public int StokMaksimal { get; set; }

    public int Qty
    {
        get => _qty;
        set { _qty = value; OnPropertyChanged(); }
    }

    public decimal Subtotal
    {
        get => _subtotal;
        set { _subtotal = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}