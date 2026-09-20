using ApotekApp.Services;
using MySqlConnector;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ApotekApp;

public sealed class PembelianItemModel
{
    public int Id { get; set; }
    public string NoNota { get; set; } = "";
    public string NamaSupplier { get; set; } = "";
    public string Tanggal { get; set; } = "";
    public decimal Total { get; set; }
}

public sealed class PurchaseLine
{
    public int ObatId { get; set; }
    public string NamaObat { get; set; } = "";
    public int Qty { get; set; }
    public decimal Harga { get; set; }
    public decimal Subtotal => Qty * Harga;
}


public sealed class PurchaseSelectableItem : System.ComponentModel.INotifyPropertyChanged
{
    public int ObatId { get; init; }
    public string KodeObat { get; init; } = "";
    public string NamaObat { get; init; } = "";
    private bool _isSelected;
    private string _qty = "1";
    private string _harga = "0";
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public string QtyText { get => _qty; set { if (_qty == value) return; _qty = value; PropertyChanged?.Invoke(this, new(nameof(QtyText))); } }
    public string HargaText { get => _harga; set { if (_harga == value) return; _harga = value; PropertyChanged?.Invoke(this, new(nameof(HargaText))); } }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public partial class PembelianPage : ContentPage
{
    private readonly ObservableCollection<PembelianItemModel> _list = new();
    private readonly ObservableCollection<PurchaseLine> _lines = new();
    private readonly ObservableCollection<PurchaseSelectableItem> _availableObat = new();
    private readonly Dictionary<string, PurchaseSelectableItem> _obatByNama = new(StringComparer.OrdinalIgnoreCase);

    public PembelianPage()
    {
        InitializeComponent();
        CvPembelianList.ItemsSource = _list;
        CvItemPembelian.ItemsSource = _lines;
        CvPilihObat.ItemsSource = _availableObat;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private static string ParseDate(object value)
    {
        var text = value?.ToString()?.Trim() ?? "";
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt) ||
               DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out dt)
            ? dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : text;
    }

    public async Task LoadAsync()
    {
        try
        {
            _list.Clear();
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT p.IdPembelian, p.NoFaktur, p.TanggalPembelian,
                       p.TotalHarga, COALESCE(NULLIF(p.nama_supplier,''), s.NamaSupplier, '-') AS NamaSupplier
                FROM pembelians p
                LEFT JOIN suppliers s ON s.Id = p.SupplierId
                ORDER BY p.TanggalPembelian DESC;
                """;

            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                _list.Add(new PembelianItemModel
                {
                    Id = Convert.ToInt32(reader["IdPembelian"]),
                    NoNota = reader["NoFaktur"]?.ToString() ?? "",
                    NamaSupplier = reader["NamaSupplier"]?.ToString() ?? "-",
                    Tanggal = ParseDate(reader["TanggalPembelian"]),
                    Total = Convert.ToDecimal(reader["TotalHarga"])
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Pembelian", ex.Message, "OK");
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadAsync();

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = e.NewTextValue?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(keyword))
        {
            CvPembelianList.ItemsSource = _list;
            return;
        }

        CvPembelianList.ItemsSource = _list
            .Where(x => (x.NoNota + " " + x.NamaSupplier)
                .Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async void OnTambahPembelianClicked(object sender, EventArgs e)
    {
        _lines.Clear();
        TxtNoFaktur.Text = "";
        TxtCatatan.Text = "";
        TxtDaftarItem.Text = "";
        TxtKodeObat.Text = "";
        TxtQty.Text = "";
        TxtHargaBeli.Text = "";
        if (CmbObatSatu is not null) CmbObatSatu.SelectedIndex = -1;
        LblTotalPembelian.Text = "Total: Rp 0";

        CmbSupplier.ItemsSource = await GetSuppliersAsync();
        CmbSupplier.SelectedIndex = -1;
        await LoadAvailableObatAsync();
        ModalLayout.IsVisible = true;
    }

    private static async Task<List<string>> GetSuppliersAsync()
    {
        var suppliers = new List<string>();
        await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(
            "SELECT NamaSupplier FROM suppliers ORDER BY NamaSupplier;", connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            suppliers.Add(reader.GetString(0));

        return suppliers;
    }

    private async Task LoadAvailableObatAsync()
    {
        _availableObat.Clear();
        _obatByNama.Clear();
        if (CmbObatSatu is not null) CmbObatSatu.ItemsSource = null;
        await using var c = new MySqlConnection(MauiProgram.ConnectionString);
        await c.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT Id,KodeObat,NamaObat,HargaBeli FROM obats ORDER BY NamaObat;", c);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var hb = Convert.ToDecimal(r["HargaBeli"]);
            var item = new PurchaseSelectableItem
            {
                ObatId = Convert.ToInt32(r["Id"]),
                KodeObat = r["KodeObat"]?.ToString() ?? "",
                NamaObat = r["NamaObat"]?.ToString() ?? "",
                HargaText = hb.ToString("0.##", CultureInfo.InvariantCulture)
            };
            _availableObat.Add(item);
            _obatByNama[item.NamaObat] = item;
        }
        if (CmbObatSatu is not null) CmbObatSatu.ItemsSource = _availableObat.Select(x => x.NamaObat).ToList();
        ApplyPurchaseSearch(TxtCariObatPembelian?.Text);
    }

    private void OnObatSatuSelected(object sender, EventArgs e)
    {
        var nama = CmbObatSatu?.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(nama) || !_obatByNama.TryGetValue(nama, out var item)) return;
        TxtKodeObat.Text = item.KodeObat;
        TxtHargaBeli.Text = item.HargaText;
    }

    private void OnPilihSemuaObatChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_availableObat.Count == 0) return;
        foreach (var item in _availableObat) item.IsSelected = e.Value;
        ApplyPurchaseSearch(TxtCariObatPembelian?.Text);
    }

    private void ApplyPurchaseSearch(string? keyword)
    {
        if (CvPilihObat is null) return;
        var key = keyword?.Trim() ?? "";
        CvPilihObat.ItemsSource = string.IsNullOrWhiteSpace(key)
            ? _availableObat
            : _availableObat.Where(x => x.NamaObat.Contains(key, StringComparison.OrdinalIgnoreCase) || x.KodeObat.Contains(key, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void OnCariObatPembelianChanged(object sender, TextChangedEventArgs e) => ApplyPurchaseSearch(e.NewTextValue);

    private async void OnTambahkanYangDipilihClicked(object sender, EventArgs e)
    {
        var selected = _availableObat.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0)
        {
            await DisplayAlert("Pilih Obat", "Centang minimal satu obat.", "OK");
            return;
        }
        var added = 0;
        foreach (var x in selected)
        {
            if (!int.TryParse(x.QtyText, out var qty) || qty <= 0) continue;
            if (!decimal.TryParse(x.HargaText.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var harga) || harga < 0) continue;
            AddLine(x.ObatId, x.NamaObat, qty, harga);
            x.IsSelected = false;
            added++;
        }
        if (ChkPilihSemuaObat is not null) ChkPilihSemuaObat.IsChecked = false;
        await DisplayAlert("Tambah Item", $"{added} obat berhasil dimasukkan ke daftar pembelian.", "OK");
    }

    private async void OnTambahItemClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtKodeObat.Text) && CmbObatSatu?.SelectedItem is string selectedName && _obatByNama.TryGetValue(selectedName, out var selectedItem))
            TxtKodeObat.Text = selectedItem.KodeObat;

        if (string.IsNullOrWhiteSpace(TxtKodeObat.Text) ||
            !int.TryParse(TxtQty.Text, out var qty) || qty <= 0 ||
            !decimal.TryParse(
                TxtHargaBeli.Text?.Replace(",", "."),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var harga) || harga < 0)
        {
            await DisplayAlert("Peringatan", "Isi kode/barcode, qty, dan harga beli dengan benar.", "OK");
            return;
        }

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT Id, NamaObat
                FROM obats
                WHERE KodeObat = @key OR Barcode = @key
                LIMIT 1;
                """;

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@key", TxtKodeObat.Text.Trim());
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                await DisplayAlert("Tidak ditemukan", "Kode atau barcode obat tidak ditemukan.", "OK");
                return;
            }

            var id = Convert.ToInt32(reader["Id"]);
            var nama = reader["NamaObat"]?.ToString() ?? "";
            AddLine(id, nama, qty, harga);
            ClearItemInputs();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Item", ex.Message, "OK");
        }
    }

    private void AddLine(int obatId, string nama, int qty, decimal harga)
    {
        var existing = _lines.FirstOrDefault(x => x.ObatId == obatId);
        if (existing is null)
        {
            _lines.Add(new PurchaseLine
            {
                ObatId = obatId,
                NamaObat = nama,
                Qty = qty,
                Harga = harga
            });
        }
        else
        {
            existing.Qty += qty;
            existing.Harga = harga;
        }

        LblTotalPembelian.Text = $"Total: Rp {_lines.Sum(x => x.Subtotal):N0}";
    }

    private void ClearItemInputs()
    {
        TxtKodeObat.Text = "";
        TxtQty.Text = "";
        TxtHargaBeli.Text = "";
        if (CmbObatSatu is not null) CmbObatSatu.SelectedIndex = -1;
        TxtKodeObat.Focus();
    }

    private async void OnTambahBanyakClicked(object sender, EventArgs e)
    {
        var text = TxtDaftarItem.Text ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlert(
                "Tambah Banyak",
                "Format setiap baris: KODE atau BARCODE | QTY | HARGA BELI",
                "OK");
            return;
        }

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            var processed = 0;
            foreach (var raw in text.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = raw.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length < 3 ||
                    !int.TryParse(parts[1], out var qty) || qty <= 0 ||
                    !decimal.TryParse(
                        parts[2].Replace(",", "."),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var harga))
                    continue;

                await using var command = new MySqlCommand(
                    "SELECT Id, NamaObat FROM obats WHERE KodeObat=@key OR Barcode=@key LIMIT 1;",
                    connection);
                command.Parameters.AddWithValue("@key", parts[0]);

                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    continue;

                AddLine(
                    Convert.ToInt32(reader["Id"]),
                    reader["NamaObat"]?.ToString() ?? "",
                    qty,
                    harga);
                processed++;
            }

            TxtDaftarItem.Text = "";
            await DisplayAlert("Tambah Banyak", $"{processed} baris berhasil dimasukkan.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tambah Banyak", ex.Message, "OK");
        }
    }

    private async void OnSimpanClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNoFaktur.Text) ||
            CmbSupplier.SelectedItem is null ||
            _lines.Count == 0)
        {
            await DisplayAlert(
                "Peringatan",
                "Faktur, supplier, dan minimal satu item wajib diisi.",
                "OK");
            return;
        }

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = connection.BeginTransaction();

            var supplierName = CmbSupplier.SelectedItem.ToString() ?? "";
            await using (var supplierCommand = new MySqlCommand(
                "SELECT Id FROM suppliers WHERE NamaSupplier=@nama LIMIT 1;",
                connection,
                transaction))
            {
                supplierCommand.Parameters.AddWithValue("@nama", supplierName);
                var supplierValue = await supplierCommand.ExecuteScalarAsync();
                if (supplierValue is null || supplierValue == DBNull.Value) throw new InvalidOperationException("Supplier tidak ditemukan.");
                var supplierId = Convert.ToInt32(supplierValue);
                var total = _lines.Sum(x => x.Subtotal);

                await using var purchaseCommand = new MySqlCommand("""
                    INSERT INTO pembelians
                        (NoFaktur,TanggalPembelian,TotalHarga,Catatan,SupplierId,nama_supplier)
                    VALUES
                        (@f,@tanggal,@total,@catatan,@supplier,@supplierName);
                    SELECT last_insert_rowid();
                    """, connection, transaction);

                purchaseCommand.Parameters.AddWithValue("@f", TxtNoFaktur.Text.Trim());
                purchaseCommand.Parameters.AddWithValue("@tanggal", DateTime.Now);
                purchaseCommand.Parameters.AddWithValue("@total", total);
                purchaseCommand.Parameters.AddWithValue("@catatan", TxtCatatan.Text?.Trim() ?? "");
                purchaseCommand.Parameters.AddWithValue("@supplier", supplierId);
                purchaseCommand.Parameters.AddWithValue("@supplierName", supplierName);
                var purchaseId = Convert.ToInt64(await purchaseCommand.ExecuteScalarAsync());
                if (purchaseId <= 0) throw new InvalidOperationException("Nomor ID pembelian tidak berhasil dibuat.");

                foreach (var line in _lines)
                {
                    await using var detailCommand = new MySqlCommand("""
                        INSERT INTO detail_pembelians
                            (PembelianId,ObatId,nama_obat,Jumlah,HargaBeli)
                        VALUES
                            (@p,@o,@nama,@q,@h);
                        """, connection, transaction);
                    detailCommand.Parameters.AddWithValue("@p", purchaseId);
                    detailCommand.Parameters.AddWithValue("@o", line.ObatId);
                    detailCommand.Parameters.AddWithValue("@nama", line.NamaObat);
                    detailCommand.Parameters.AddWithValue("@q", line.Qty);
                    detailCommand.Parameters.AddWithValue("@h", line.Harga);
                    await detailCommand.ExecuteNonQueryAsync();

                    await using var stockCommand = new MySqlCommand("""
                        UPDATE obats
                        SET Stok = Stok + @qty,
                            HargaBeli = @harga
                        WHERE Id = @id;
                        """, connection, transaction);
                    stockCommand.Parameters.AddWithValue("@qty", line.Qty);
                    stockCommand.Parameters.AddWithValue("@harga", line.Harga);
                    stockCommand.Parameters.AddWithValue("@id", line.ObatId);
                    await stockCommand.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
            ModalLayout.IsVisible = false;
            await LoadAsync();
            await DisplayAlert("Berhasil", "Pembelian tersimpan dan stok bertambah.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Gagal Simpan", ex.Message, "OK");
        }
    }

    private void OnBatalClicked(object sender, EventArgs e) => ModalLayout.IsVisible = false;
}
