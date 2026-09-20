using MySqlConnector;
using System.Collections.ObjectModel;
using ApotekApp.Services;

namespace ApotekApp;

public sealed class BarcodeObatItem
{
    public int Id { get; set; }
    public string KodeObat { get; set; } = "";
    public string NamaObat { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string BarcodeDisplay => string.IsNullOrWhiteSpace(Barcode) ? "Belum ada barcode" : Barcode;
    public string StatusBarcode => string.IsNullOrWhiteSpace(Barcode) ? "BELUM ADA" : "SUDAH ADA";
    public Color StatusColor => string.IsNullOrWhiteSpace(Barcode) ? Color.FromArgb("#EA580C") : Color.FromArgb("#059669");
}

public partial class BarcodeManualPage : ContentPage
{
    private readonly ObservableCollection<BarcodeObatItem> _items = new();
    private BarcodeObatItem? _selected;
    private int _copies = 1;

    public BarcodeManualPage()
    {
        InitializeComponent();
        CvObat.ItemsSource = _items;
        BarcodeGraphicsView.Drawable = new Code128Drawable("");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        try
        {
            await using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();
            await LoadItemsAsync(c, TxtCari?.Text);
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Barcode Manual", "Gagal memuat master obat:\n" + ex.Message, "OK");
        }
    }

    private async Task LoadItemsAsync(MySqlConnection c, string? keyword)
    {
        _items.Clear();
        const string sql = @"
            SELECT Id, KodeObat, NamaObat, COALESCE(Barcode,'') AS Barcode
            FROM obats
            WHERE COALESCE(IsAktif,1)=1
              AND (KodeObat LIKE @q OR NamaObat LIKE @q OR COALESCE(Barcode,'') LIKE @q)
            ORDER BY CASE WHEN COALESCE(Barcode,'')='' THEN 0 ELSE 1 END, NamaObat
            LIMIT 1000;";
        await using var cmd = new MySqlCommand(sql, c);
        cmd.Parameters.AddWithValue("@q", "%" + (keyword?.Trim() ?? "") + "%");
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            _items.Add(new BarcodeObatItem
            {
                Id = Convert.ToInt32(r["Id"]),
                KodeObat = r["KodeObat"]?.ToString() ?? "",
                NamaObat = r["NamaObat"]?.ToString() ?? "",
                Barcode = r["Barcode"]?.ToString() ?? ""
            });
        }
        if (LblJumlahObat is not null)
            LblJumlahObat.Text = $"{_items.Count:N0} obat";
    }

    private async void OnCariChanged(object sender, TextChangedEventArgs e) => await LoadAsync();
    private async void OnCariClicked(object sender, EventArgs e) => await LoadAsync();
    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadAsync();

    private void OnTambahObatBaruClicked(object sender, EventArgs e)
    {
        TxtBaruKode.Text = "";
        TxtBaruNama.Text = "";
        TxtBaruSatuan.Text = "Pcs";
        TxtBaruBarcode.Text = "";
        ModalObatBaru.IsVisible = true;
    }

    private void OnTutupObatBaruClicked(object sender, EventArgs e) => ModalObatBaru.IsVisible = false;

    private async void OnSimpanObatBaruClicked(object sender, EventArgs e)
    {
        var kode = TxtBaruKode.Text?.Trim() ?? "";
        var nama = TxtBaruNama.Text?.Trim() ?? "";
        var satuan = string.IsNullOrWhiteSpace(TxtBaruSatuan.Text) ? "Pcs" : TxtBaruSatuan.Text.Trim();
        var barcode = TxtBaruBarcode.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(kode) || string.IsNullOrWhiteSpace(nama))
        {
            await AppDialog.AlertAsync("Validasi", "Kode dan nama obat wajib diisi.", "OK");
            return;
        }
        if (!string.IsNullOrWhiteSpace(barcode) && barcode.Any(ch => !char.IsDigit(ch)))
        {
            await AppDialog.AlertAsync("Validasi", "Barcode gunakan angka saja.", "OK");
            return;
        }

        try
        {
            await using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();
            await using var tx = await c.BeginTransactionAsync();

            await using (var check = new MySqlCommand(@"
                SELECT COUNT(1) FROM obats
                WHERE KodeObat=@k OR NamaObat=@n OR (COALESCE(Barcode,'')<>'' AND Barcode=@b);", c, (MySqlTransaction)tx))
            {
                check.Parameters.AddWithValue("@k", kode);
                check.Parameters.AddWithValue("@n", nama);
                check.Parameters.AddWithValue("@b", barcode);
                if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0)
                {
                    await tx.RollbackAsync();
                    await AppDialog.AlertAsync("Data Sudah Ada", "Kode, nama, atau barcode sudah digunakan.", "OK");
                    return;
                }
            }

            await using var ins = new MySqlCommand(@"
                INSERT INTO obats(KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,Stok,StokMin,HargaBeli,HargaJual,IsAktif)
                VALUES(@k,@n,@b,@s,'Umum','',0,0,0,0,1);", c, (MySqlTransaction)tx);
            ins.Parameters.AddWithValue("@k", kode);
            ins.Parameters.AddWithValue("@n", nama);
            ins.Parameters.AddWithValue("@b", string.IsNullOrWhiteSpace(barcode) ? DBNull.Value : barcode);
            ins.Parameters.AddWithValue("@s", satuan);
            await ins.ExecuteNonQueryAsync();
            await tx.CommitAsync();

            ModalObatBaru.IsVisible = false;
            TxtCari.Text = kode;
            await LoadAsync();
            var created = _items.FirstOrDefault(x => x.KodeObat.Equals(kode, StringComparison.OrdinalIgnoreCase));
            if (created is not null)
            {
                _selected = created;
                TxtBarcode.Text = created.Barcode;
                LblNamaObat.Text = created.NamaObat;
                LblKodeObat.Text = $"Kode: {created.KodeObat}";
                UpdatePreview();
            }
            await AppDialog.AlertAsync("Berhasil", "Obat baru berhasil ditambahkan. Anda dapat langsung membuat barcode jika belum diisi.", "OK");
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Gagal Menyimpan", ex.Message, "OK");
        }
    }

    private void OnObatSelected(object sender, SelectionChangedEventArgs e)
    {
        _selected = e.CurrentSelection.FirstOrDefault() as BarcodeObatItem;
        if (_selected is null) return;
        LblNamaObat.Text = _selected.NamaObat;
        LblKodeObat.Text = $"Kode: {_selected.KodeObat}";
        TxtBarcode.Text = _selected.Barcode;
        UpdatePreview();
    }

    private async void OnGenerateClicked(object sender, EventArgs e)
    {
        if (_selected is null)
        {
            await AppDialog.AlertAsync("Barcode Manual", "Pilih obat terlebih dahulu.", "OK");
            return;
        }

        try
        {
            await using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();
            const string sql = "SELECT COALESCE(MAX(CAST(Barcode AS UNSIGNED)),199999999999) FROM obats WHERE Barcode REGEXP '^[0-9]+$' AND CHAR_LENGTH(Barcode)>=6;";
            await using var cmd = new MySqlCommand(sql, c);
            var max = Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 199999999999L);
            TxtBarcode.Text = Math.Max(200000000000L, max + 1).ToString();
            UpdatePreview();
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Generate Barcode", ex.Message, "OK");
        }
    }

    private void OnBarcodeTextChanged(object sender, TextChangedEventArgs e) => UpdatePreview();
    private void OnMinusClicked(object sender, EventArgs e) { _copies = Math.Max(1, _copies - 1); LblCopies.Text = _copies.ToString(); }
    private void OnPlusClicked(object sender, EventArgs e) { _copies = Math.Min(100, _copies + 1); LblCopies.Text = _copies.ToString(); }

    private void UpdatePreview()
    {
        var value = TxtBarcode?.Text?.Trim() ?? "";
        LblPreviewValue.Text = value.Length == 0 ? "-" : value;
        BarcodeGraphicsView.Drawable = new Code128Drawable(value);
        BarcodeGraphicsView.Invalidate();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_selected is null)
        {
            await AppDialog.AlertAsync("Simpan Barcode", "Pilih obat terlebih dahulu.", "OK");
            return;
        }
        var barcode = TxtBarcode.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(barcode))
        {
            await AppDialog.AlertAsync("Simpan Barcode", "Nomor barcode belum diisi.", "OK");
            return;
        }
        if (barcode.Any(ch => !char.IsDigit(ch)))
        {
            await AppDialog.AlertAsync("Simpan Barcode", "Gunakan angka saja agar kompatibel dengan scanner barcode apotek.", "OK");
            return;
        }

        try
        {
            await using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();
            await using var tx = await c.BeginTransactionAsync();
            await using var check = new MySqlCommand("SELECT Id,NamaObat FROM obats WHERE Barcode=@b AND Id<>@id LIMIT 1", c, (MySqlTransaction)tx);
            check.Parameters.AddWithValue("@b", barcode);
            check.Parameters.AddWithValue("@id", _selected.Id);
            await using var r = await check.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                var usedBy = r["NamaObat"]?.ToString() ?? "obat lain";
                await r.DisposeAsync();
                await tx.RollbackAsync();
                await AppDialog.AlertAsync("Barcode Sudah Dipakai", $"Barcode {barcode} sudah digunakan oleh obat: {usedBy}", "OK");
                return;
            }
            await r.DisposeAsync();

            await using var update = new MySqlCommand("UPDATE obats SET Barcode=@b WHERE Id=@id", c, (MySqlTransaction)tx);
            update.Parameters.AddWithValue("@b", barcode);
            update.Parameters.AddWithValue("@id", _selected.Id);
            await update.ExecuteNonQueryAsync();
            await tx.CommitAsync();

            _selected.Barcode = barcode;
            await LoadItemsAsync(c, TxtCari.Text);
            UpdatePreview();
            await AppDialog.AlertAsync("Barcode Tersimpan", $"Barcode {barcode} berhasil disimpan ke master obat.", "OK");
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Simpan Barcode", "Gagal menyimpan barcode:\n" + ex.Message, "OK");
        }
    }

    private async void OnPrintClicked(object sender, EventArgs e)
    {
        if (_selected is null || string.IsNullOrWhiteSpace(TxtBarcode.Text))
        {
            await AppDialog.AlertAsync("Cetak Barcode", "Pilih obat dan isi/generate barcode terlebih dahulu.", "OK");
            return;
        }
        var barcode = TxtBarcode.Text.Trim();
        var body = BrowserPrintService.BarcodeHtml(_selected.NamaObat, _selected.KodeObat, barcode, _copies);
        var css = @".label{width:80mm;min-height:45mm;margin:0 auto 8mm;padding:4mm;text-align:center;page-break-after:always}.product{font-size:14px;font-weight:700}.code{font-size:10px;margin:2mm 0}.barcode{width:100%;height:22mm;display:block}.barcode-text{font:700 11px Arial;margin-top:1mm}@page{size:80mm auto;margin:3mm}";
        var ok = await BrowserPrintService.OpenPrintPreviewAsync($"Barcode {_selected.NamaObat}", body, css);
        if (!ok) await AppDialog.AlertAsync("Cetak Barcode", "Browser tidak dapat dibuka untuk menampilkan Print Preview.", "OK");
    }
}
