using MySqlConnector;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
<<<<<<< HEAD
using ApotekApp.Services;

namespace ApotekApp;

public sealed class ObatItem
{
    public int No { get; set; }
    public int Id { get; set; }
    public string KodeObat { get; set; } = string.Empty;
    public string NamaObat { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Satuan { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string LokasiRak { get; set; } = string.Empty;
=======

namespace ApotekApp;

public class ObatItem
{
    public int No { get; set; }
    public int Id { get; set; }
    public string KodeObat { get; set; } = "";
    public string NamaObat { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Satuan { get; set; } = "";
    public string Kategori { get; set; } = "";
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
    public int Stok { get; set; }
    public string HargaFormatted { get; set; } = "Rp 0";
    public decimal HargaBeli { get; set; }
    public decimal HargaJual { get; set; }
    public int StokMin { get; set; }
    public DateTime? Expired { get; set; }
}

public partial class ObatPage : ContentPage
{
<<<<<<< HEAD
    private readonly ObservableCollection<ObatItem> _items = new();
    private readonly List<ObatItem> _filteredItems = new();
    private readonly ObservableCollection<ObatItem> _pageItems = new();
    private int _pageSize = 10;
    private int _currentPage = 1;
    private int _editId;

    public ObatPage()
    {
        InitializeComponent();
        BindingContext = this;
        CvObatList.ItemsSource = _pageItems;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private static DateTime? ParseDatabaseDate(object value)
    {
        if (value is null || value == DBNull.Value) return null;
        var text = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt)) return dt;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out dt)) return dt;
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial) && serial > 20000 && serial < 60000)
        {
            try { return DateTime.FromOADate(serial); } catch { }
        }
        return null;
    }
=======
    readonly ObservableCollection<ObatItem> _items = new();
    int _editId;

    public ObatPage() { InitializeComponent(); CvObatList.ItemsSource = _items; }

    protected override async void OnAppearing() { base.OnAppearing(); await LoadAsync(); }
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a

    public async Task LoadAsync()
    {
        try
        {
            _items.Clear();
<<<<<<< HEAD

            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT Id, KodeObat, NamaObat, Barcode, Satuan, Kategori,
                       Stok, StokMin, HargaBeli, HargaJual, TanggalKadaluarsa, LokasiRak
                FROM obats
                ORDER BY NamaObat;
                """;

            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var no = 1;

            while (await reader.ReadAsync())
            {
                var hargaJual = Convert.ToDecimal(reader["HargaJual"]);

                _items.Add(new ObatItem
                {
                    No = no++,
                    Id = Convert.ToInt32(reader["Id"]),
                    KodeObat = reader["KodeObat"]?.ToString() ?? string.Empty,
                    NamaObat = reader["NamaObat"]?.ToString() ?? string.Empty,
                    Barcode = reader["Barcode"]?.ToString() ?? string.Empty,
                    Satuan = reader["Satuan"]?.ToString() ?? string.Empty,
                    Kategori = reader["Kategori"]?.ToString() ?? string.Empty,
                    LokasiRak = reader["LokasiRak"]?.ToString() ?? string.Empty,
                    Stok = Convert.ToInt32(reader["Stok"]),
                    StokMin = Convert.ToInt32(reader["StokMin"]),
                    HargaBeli = Convert.ToDecimal(reader["HargaBeli"]),
                    HargaJual = hargaJual,
                    HargaFormatted = $"Rp {hargaJual:N0}",
                    Expired = ParseDatabaseDate(reader["TanggalKadaluarsa"])
                });
            }

            RenumberItems();
            ApplySearch(TxtSearch.Text);
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Error Obat", ex.Message, "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _currentPage = 1;
        ApplySearch(e.NewTextValue);
    }

    private void OnObatListSizeChanged(object sender, EventArgs e)
    {
        if (CvObatList.Height <= 0) return;
        var calculated = Math.Clamp((int)Math.Floor((CvObatList.Height - 20) / 52.0), 8, 24);
        if (calculated == _pageSize) return;
        _pageSize = calculated;
        ApplySearch(TxtSearch.Text);
    }

    private void ApplySearch(string? keyword)
    {
        var key = keyword?.Trim() ?? string.Empty;

        _filteredItems.Clear();
        _filteredItems.AddRange(string.IsNullOrWhiteSpace(key)
            ? _items
            : _items.Where(item =>
                item.KodeObat.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                item.NamaObat.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                item.Barcode.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                item.Kategori.Contains(key, StringComparison.OrdinalIgnoreCase)));

        var totalPages = Math.Max(1, (int)Math.Ceiling(_filteredItems.Count / (double)_pageSize));
        if (_currentPage > totalPages)
            _currentPage = totalPages;
        if (_currentPage < 1)
            _currentPage = 1;

        var pageItems = _filteredItems
            .Skip((_currentPage - 1) * _pageSize)
            .Take(_pageSize)
            .ToList();

        _pageItems.Clear();
        foreach (var item in pageItems)
            _pageItems.Add(item);
        UpdatePagination(totalPages);
    }

    private void UpdatePagination(int totalPages)
    {
        if (LblPageInfo is null || BtnPrevPage is null || BtnNextPage is null)
            return;

        var total = _filteredItems.Count;
        var first = total == 0 ? 0 : ((_currentPage - 1) * _pageSize) + 1;
        var last = total == 0 ? 0 : Math.Min(_currentPage * _pageSize, total);

        LblPageInfo.Text = $"Halaman {_currentPage} dari {totalPages}  •  Menampilkan {first}-{last} dari {total} data";
        BtnPrevPage.IsEnabled = _currentPage > 1;
        BtnNextPage.IsEnabled = _currentPage < totalPages;
    }

    private void OnPrevPageClicked(object sender, EventArgs e)
    {
        if (_currentPage <= 1) return;
        _currentPage--;
        ApplySearch(TxtSearch.Text);
    }

    private void OnNextPageClicked(object sender, EventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(_filteredItems.Count / (double)_pageSize));
        if (_currentPage >= totalPages) return;
        _currentPage++;
        ApplySearch(TxtSearch.Text);
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadAsync();
    }

    private void OnTambahObatClicked(object sender, EventArgs e)
    {
        _editId = 0;
        LblModalTitle.Text = "Tambah Obat";
        ClearForm();
        ModalLayout.IsVisible = true;
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        var item = GetItemFromButton(sender);
        if (item is null)
            return;

        _editId = item.Id;
        LblModalTitle.Text = "Edit Obat";

        TxtKode.Text = item.KodeObat;
        TxtBarcode.Text = item.Barcode;
        TxtNama.Text = item.NamaObat;
        TxtKategori.Text = item.Kategori;
        TxtLokasi.Text = item.LokasiRak;
        TxtSatuan.Text = item.Satuan;
        TxtStok.Text = item.Stok.ToString();
        TxtStokMin.Text = item.StokMin.ToString();
        TxtHargaBeli.Text = item.HargaBeli.ToString("0.##", CultureInfo.InvariantCulture);
        TxtHargaJual.Text = item.HargaJual.ToString("0.##", CultureInfo.InvariantCulture);
        DtpExpired.Date = item.Expired ?? DateTime.Today;

        ModalLayout.IsVisible = true;
    }

    private bool _deleteInProgress;

    private async void OnHapusClicked(object sender, EventArgs e)
    {
        await HandleDeleteButtonAsync(sender);
    }

    private async Task HandleDeleteButtonAsync(object sender)
    {
        if (_deleteInProgress)
            return;

        if (sender is not Button button)
            return;

        _deleteInProgress = true;

        try
        {
            var id = GetIdFromButton(button);
            if (id <= 0)
            {
                await AppDialog.AlertAsync("Hapus Obat", "ID data tidak ditemukan.", "OK");
                return;
            }

            var item = _items.FirstOrDefault(x => x.Id == id);
            if (item is null)
            {
                await AppDialog.AlertAsync("Hapus Obat", "Data tidak ditemukan pada daftar.", "OK");
                return;
            }

            await DeleteObatAsync(item);
        }
        finally
        {
            _deleteInProgress = false;
        }
    }

    private static int GetIdFromButton(Button button)
    {
        if (button.BindingContext is ObatItem item)
            return item.Id;

        if (button.CommandParameter is int id)
            return id;

        return int.TryParse(button.CommandParameter?.ToString(), out id) ? id : 0;
    }

    private async Task DeleteObatAsync(ObatItem item)
    {        var confirm = await AppDialog.ConfirmAsync(
            "Konfirmasi Hapus",
            $"Hapus obat \"{item.NamaObat}\"?",
            "Ya, Hapus",
            "Batal");

        if (!confirm)
            return;

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            const string checkSql = """
                SELECT
                    (SELECT COUNT(*) FROM detail_penjualan WHERE id_obat = @id) +
                    (SELECT COUNT(*) FROM detail_pembelians WHERE ObatId = @id);
                """;

            await using (var checkCommand = new MySqlCommand(checkSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@id", item.Id);
                var usedCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (usedCount > 0)
                {
                    await AppDialog.AlertAsync(
                        "Tidak Dapat Dihapus",
                        $"Obat \"{item.NamaObat}\" sudah digunakan pada transaksi. Data tidak dihapus agar riwayat transaksi tetap aman.",
                        "OK");
                    return;
                }
            }

            using var transaction = connection.BeginTransaction();

            // Arsipkan master obat terlebih dahulu agar penghapusan tidak merusak riwayat.
            const string archiveSql = """
                INSERT INTO arsip_obat
                    (obat_id_lama,kode_obat,nama_obat,barcode,satuan,kategori,lokasi_rak,
                     harga_beli,harga_jual,stok,stok_min,tanggal_kadaluarsa,diarsipkan_pada)
                SELECT Id,KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,
                       HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa,@now
                FROM obats WHERE Id=@id;
                """;
            await using (var archiveCommand = new MySqlCommand(archiveSql, connection, (MySqlTransaction)transaction))
            {
                archiveCommand.Parameters.AddWithValue("@id", item.Id);
                archiveCommand.Parameters.AddWithValue("@now", DateTime.Now);
                await archiveCommand.ExecuteNonQueryAsync();
            }

            await using var deleteCommand = new MySqlCommand(
                "DELETE FROM obats WHERE Id = @id;", connection, (MySqlTransaction)transaction);

            deleteCommand.Parameters.AddWithValue("@id", item.Id);
            var affectedRows = await deleteCommand.ExecuteNonQueryAsync();
            if (affectedRows > 0)
                transaction.Commit();
            else
                transaction.Rollback();

            if (affectedRows <= 0)
            {
                await AppDialog.AlertAsync("Hapus Obat", "Data obat tidak ditemukan di database.", "OK");
                return;
            }

            _items.Remove(item);
            RenumberItems();
            ApplySearch(TxtSearch.Text);

            await AppDialog.AlertAsync(
                "Berhasil",
                $"Obat \"{item.NamaObat}\" berhasil dihapus.",
                "OK");
        }
        catch (MySqlException ex)
        {
            await AppDialog.AlertAsync(
                "Gagal Hapus",
                $"Database menolak penghapusan data.\n\n{ex.Message}",
                "OK");
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Gagal Hapus", ex.Message, "OK");
        }
    
    }



    private void RenumberItems()
    {
        for (var i = 0; i < _items.Count; i++)
            _items[i].No = i + 1;
    }

    private static ObatItem? GetItemFromButton(object sender)
    {
        if (sender is not Button button)
            return null;

        return button.BindingContext as ObatItem
               ?? button.CommandParameter as ObatItem;
    }

    private async void OnSimpanClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtKode.Text) ||
            string.IsNullOrWhiteSpace(TxtNama.Text))
        {
            await AppDialog.AlertAsync("Peringatan", "Kode dan nama wajib diisi.", "OK");
            return;
        }

        int.TryParse(TxtStok.Text, out var stok);
        int.TryParse(TxtStokMin.Text, out var stokMin);

        decimal.TryParse(
            TxtHargaBeli.Text?.Replace(",", "."),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var hargaBeli);

        decimal.TryParse(
            TxtHargaJual.Text?.Replace(",", "."),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var hargaJual);

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            var sql = _editId == 0
                ? """
                  INSERT INTO obats
                      (KodeObat, NamaObat, Barcode, Satuan, Kategori, LokasiRak,
                       HargaBeli, HargaJual, Stok, StokMin, TanggalKadaluarsa)
                  VALUES
                      (@kode, @nama, @barcode, @satuan, @kategori, @rak,
                       @hargaBeli, @hargaJual, @stok, @stokMin, @expired);
                  """
                : """
                  UPDATE obats
                  SET KodeObat = @kode,
                      NamaObat = @nama,
                      Barcode = @barcode,
                      Satuan = @satuan,
                      Kategori = @kategori,
                      LokasiRak = @rak,
                      HargaBeli = @hargaBeli,
                      HargaJual = @hargaJual,
                      Stok = @stok,
                      StokMin = @stokMin,
                      TanggalKadaluarsa = @expired
                  WHERE Id = @id;
                  """;

            await using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@kode", TxtKode.Text.Trim());
            command.Parameters.AddWithValue("@nama", TxtNama.Text.Trim());
            command.Parameters.AddWithValue(
                "@barcode",
                string.IsNullOrWhiteSpace(TxtBarcode.Text)
                    ? TxtKode.Text.Trim()
                    : TxtBarcode.Text.Trim());
            command.Parameters.AddWithValue(
                "@satuan",
                string.IsNullOrWhiteSpace(TxtSatuan.Text) ? "Pcs" : TxtSatuan.Text.Trim());
            command.Parameters.AddWithValue(
                "@kategori",
                string.IsNullOrWhiteSpace(TxtKategori.Text) ? "Umum" : TxtKategori.Text.Trim());
            command.Parameters.AddWithValue(
                "@rak",
                string.IsNullOrWhiteSpace(TxtLokasi.Text) ? "-" : TxtLokasi.Text.Trim());
            command.Parameters.AddWithValue("@hargaBeli", hargaBeli);
            command.Parameters.AddWithValue("@hargaJual", hargaJual);
            command.Parameters.AddWithValue("@stok", stok);
            command.Parameters.AddWithValue("@stokMin", stokMin);
            command.Parameters.AddWithValue("@expired", DtpExpired.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (_editId != 0)
                command.Parameters.AddWithValue("@id", _editId);

            await command.ExecuteNonQueryAsync();

            ModalLayout.IsVisible = false;
            await LoadAsync();
        }
        catch (MySqlException ex)
        {
            await AppDialog.AlertAsync("Gagal Simpan", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Gagal Simpan", ex.Message, "OK");
        }
    }

    private void OnBatalClicked(object sender, EventArgs e)
    {
        ModalLayout.IsVisible = false;
    }

    private void ClearForm()
    {
        TxtKode.Text = string.Empty;
        TxtBarcode.Text = string.Empty;
        TxtNama.Text = string.Empty;
        TxtKategori.Text = string.Empty;
        TxtLokasi.Text = string.Empty;
        TxtSatuan.Text = string.Empty;
        TxtStok.Text = string.Empty;
        TxtStokMin.Text = string.Empty;
        TxtHargaBeli.Text = string.Empty;
        TxtHargaJual.Text = string.Empty;
        DtpExpired.Date = DateTime.Today;
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();
            const string sql = "SELECT KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa,KodeKFA,Golongan,BentukSediaan,Kekuatan,Dosis FROM obats ORDER BY NamaObat";
            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string[]>();
            while (await reader.ReadAsync())
                rows.Add(Enumerable.Range(0, reader.FieldCount).Select(i => reader[i] == DBNull.Value ? "" : reader[i].ToString() ?? "").ToArray());
            await ExcelService.SaveAsync($"Data_Obat_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                new[] { "KodeObat","NamaObat","Barcode","Satuan","Kategori","LokasiRak","HargaBeli","HargaJual","Stok","StokMin","TanggalKadaluarsa","KodeKFA","Golongan","BentukSediaan","Kekuatan","Dosis" }, rows);
            await AppDialog.AlertAsync("Export Excel", "Data obat berhasil diekspor ke file Excel (.xlsx).", "OK");
        }
        catch (Exception ex) { await AppDialog.AlertAsync("Export Excel", ex.Message, "OK"); }
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        try
        {
            var rows = await ExcelService.OpenAsync("Pilih file Excel Data Obat");
            if (rows.Count == 0)
            {
                await AppDialog.AlertAsync("Import Excel", "File Excel tidak berisi data.", "OK");
                return;
            }

            // Header fleksibel: file Excel tidak harus sama persis dengan hasil Export.
            // Cukup Nama Obat + (Kode atau Barcode). Kolom lain boleh tidak ada.
            var headerAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["kode"] = new[] { "KodeObat", "Kode Obat", "Kode Barang", "Kode", "Code", "SKU" },
                ["nama"] = new[] { "NamaObat", "Nama Obat", "Nama Barang", "Nama Produk", "Nama", "Obat" },
                ["barcode"] = new[] { "Barcode", "Barcode Obat", "EAN", "EAN13", "UPC" },
                ["satuan"] = new[] { "Satuan", "Unit", "Kemasan" },
                ["kategori"] = new[] { "Kategori", "Kategori Obat", "Jenis" },
                ["rak"] = new[] { "LokasiRak", "Lokasi Rak", "Rak", "Lokasi" },
                ["hb"] = new[] { "HargaBeli", "Harga Beli", "Harga Modal", "HPP", "Modal" },
                ["hj"] = new[] { "HargaJual", "Harga Jual", "Harga Jual Satuan", "Harga" },
                ["stok"] = new[] { "Stok", "Stok Awal", "Jumlah Stok", "Qty", "Jumlah" },
                ["min"] = new[] { "StokMin", "Stok Minimum", "Minimum Stok", "Min Stok" },
                ["exp"] = new[] { "TanggalKadaluarsa", "Tanggal Kadaluarsa", "Tanggal Expired", "Expired", "Kadaluarsa", "Tgl Expired" },
                ["kfa"] = new[] { "KodeKFA", "Kode KFA", "KFA", "KFA Code" },
                ["golongan"] = new[] { "Golongan", "Golongan Obat" },
                ["sediaan"] = new[] { "BentukSediaan", "Bentuk Sediaan", "Sediaan" },
                ["kekuatan"] = new[] { "Kekuatan", "Strength" },
                ["dosis"] = new[] { "Dosis", "Aturan Dosis" }
            };

            var headerRowIndex = -1;
            Dictionary<string, int>? map = null;
            for (var rowIndex = 0; rowIndex < Math.Min(rows.Count, 15); rowIndex++)
            {
                var candidateMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < rows[rowIndex].Length; i++)
                {
                    var normalized = NormalizeHeader(rows[rowIndex][i]);
                    if (!string.IsNullOrWhiteSpace(normalized))
                        candidateMap[normalized] = i;
                }

                int Find(params string[] aliases)
                {
                    foreach (var alias in aliases)
                        if (candidateMap.TryGetValue(NormalizeHeader(alias), out var idx)) return idx;
                    return -1;
                }

                var namaIdx = Find(headerAliases["nama"]);
                var kodeIdx = Find(headerAliases["kode"]);
                var barcodeIdx = Find(headerAliases["barcode"]);
                if (namaIdx >= 0 && (kodeIdx >= 0 || barcodeIdx >= 0))
                {
                    headerRowIndex = rowIndex;
                    map = candidateMap;
                    break;
                }
            }

            if (headerRowIndex < 0 || map is null)
            {
                await AppDialog.AlertAsync("Import Excel",
                    "Header tidak dikenali. Minimal file harus memiliki kolom Nama Obat dan salah satu dari Kode atau Barcode.", "OK");
                return;
            }

            int Col(params string[] aliases)
            {
                foreach (var alias in aliases)
                    if (map.TryGetValue(NormalizeHeader(alias), out var idx)) return idx;
                return -1;
            }

            var kodeCol = Col(headerAliases["kode"]);
            var namaCol = Col(headerAliases["nama"]);
            var barcodeCol = Col(headerAliases["barcode"]);
            var satuanCol = Col(headerAliases["satuan"]);
            var kategoriCol = Col(headerAliases["kategori"]);
            var rakCol = Col(headerAliases["rak"]);
            var hbCol = Col(headerAliases["hb"]);
            var hjCol = Col(headerAliases["hj"]);
            var stokCol = Col(headerAliases["stok"]);
            var minCol = Col(headerAliases["min"]);
            var expCol = Col(headerAliases["exp"]);
            var kfaCol = Col(headerAliases["kfa"]);
            var golonganCol = Col(headerAliases["golongan"]);
            var sediaanCol = Col(headerAliases["sediaan"]);
            var kekuatanCol = Col(headerAliases["kekuatan"]);
            var dosisCol = Col(headerAliases["dosis"]);

            string Cell(string[] row, int index) => index >= 0 && index < row.Length ? (row[index] ?? string.Empty).Trim() : string.Empty;

            decimal Number(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return 0;
                var raw = value.Replace("Rp", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("IDR", "", StringComparison.OrdinalIgnoreCase)
                               .Replace(" ", "").Trim();
                if (decimal.TryParse(raw, NumberStyles.Number, new CultureInfo("id-ID"), out var id)) return id;
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv)) return inv;
                // Excel numeric cells are normally plain invariant numbers.
                var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == ',' || c == '.' || c == '-').ToArray());
                if (cleaned.Contains(',') && cleaned.Contains('.'))
                {
                    cleaned = cleaned.LastIndexOf(',') > cleaned.LastIndexOf('.')
                        ? cleaned.Replace(".", "").Replace(',', '.')
                        : cleaned.Replace(",", "");
                }
                else if (cleaned.Count(c => c == ',') == 1 && cleaned.Split(',')[1].Length <= 2)
                    cleaned = cleaned.Replace(',', '.');
                else if (cleaned.Contains(',')) cleaned = cleaned.Replace(",", "");
                else if (cleaned.Count(c => c == '.') > 1 || (cleaned.Contains('.') && cleaned.Split('.')[1].Length == 3))
                    cleaned = cleaned.Replace(".", "");
                return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0;
            }

            int Integer(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return 0;
                if (decimal.TryParse(value, NumberStyles.Any, new CultureInfo("id-ID"), out var d)) return Math.Max(0, (int)Math.Round(d));
                return int.TryParse(new string(value.Where(char.IsDigit).ToArray()), out var n) ? Math.Max(0, n) : 0;
            }

            string DateValue(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return string.Empty;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial >= 1 && serial <= 100000)
                {
                    try
                    {
                        var date = DateTime.FromOADate(serial);
                        if (date.Year >= 1900 && date.Year <= 2200) return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    catch { }
                }

                var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "MM/dd/yyyy", "M/d/yyyy", "yyyy/MM/dd", "yyyy.MM.dd" };
                if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                    return exact.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (DateTime.TryParse(value.Trim(), new CultureInfo("id-ID"), DateTimeStyles.AllowWhiteSpaces, out var parsed) && parsed.Year >= 1900 && parsed.Year <= 2200)
                    return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                // Jangan pernah memasukkan tanggal rusak seperti 2028-02-373.
                return string.Empty;
            }

            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = connection.BeginTransaction();

            var inserted = 0;
            var updated = 0;
            var skipped = 0;
            var rowNumber = headerRowIndex + 2;
            var generatedCode = await GetNextGeneratedCodeAsync(connection, transaction);

            // KodeObat pada database memang UNIQUE, tetapi file import boleh memiliki
            // kode yang sama untuk beberapa nama obat. Jangan biarkan baris berikutnya
            // menimpa baris sebelumnya. Kode pertama yang memang sudah ada di database
            // dipakai untuk UPDATE; setiap kemunculan berikutnya mendapatkan kode
            // internal baru (mis. OBT007, OBT008, dst.). Barcode tetap boleh sama.
            var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var codeCommand = new MySqlCommand("SELECT KodeObat FROM obats WHERE KodeObat IS NOT NULL AND TRIM(KodeObat) <> '';", connection, transaction))
            await using (var codeReader = await codeCommand.ExecuteReaderAsync())
            {
                while (await codeReader.ReadAsync())
                    existingCodes.Add(codeReader[0]?.ToString()?.Trim() ?? string.Empty);
            }

            var assignedImportCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            async Task<string> CreateUniqueImportCodeAsync()
            {
                while (true)
                {
                    var candidate = $"OBT{generatedCode++:000}";
                    if (existingCodes.Contains(candidate) || assignedImportCodes.Contains(candidate)) continue;
                    return candidate;
                }
            }

            foreach (var row in rows.Skip(headerRowIndex + 1))
            {
                var nama = Cell(row, namaCol);
                var barcode = Cell(row, barcodeCol);
                var kodeAsli = Cell(row, kodeCol);

                if (string.IsNullOrWhiteSpace(nama)) { skipped++; rowNumber++; continue; }

                var kode = kodeAsli;
                if (string.IsNullOrWhiteSpace(kode))
                {
                    // Barcode bukan KodeObat. Jika kode kosong, tetap buat KodeObat
                    // internal agar dua obat dengan barcode yang sama tidak tertimpa.
                    kode = await CreateUniqueImportCodeAsync();
                }
                else if (assignedImportCodes.Contains(kode))
                {
                    // Kode yang sama muncul lagi di Excel: buat kode internal baru.
                    kode = await CreateUniqueImportCodeAsync();
                }

                assignedImportCodes.Add(kode);

                var hb = Number(Cell(row, hbCol));
                var hj = Number(Cell(row, hjCol));
                var stok = Integer(Cell(row, stokCol));
                var stokMin = Integer(Cell(row, minCol));
                var exp = DateValue(Cell(row, expCol));
                var kfa = Cell(row, kfaCol);
                var golongan = Cell(row, golonganCol);
                var sediaan = Cell(row, sediaanCol);
                var kekuatan = Cell(row, kekuatanCol);
                var dosis = Cell(row, dosisCol);
                var satuan = Cell(row, satuanCol);
                var kategori = Cell(row, kategoriCol);
                var rak = Cell(row, rakCol);

                try
                {
                    const string sql = """
INSERT INTO obats (KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa,KodeKFA,Golongan,BentukSediaan,Kekuatan,Dosis)
VALUES (@kode,@nama,@barcode,@satuan,@kategori,@rak,@hb,@hj,@stok,@min,@exp,@kfa,@golongan,@sediaan,@kekuatan,@dosis)
ON DUPLICATE KEY UPDATE
 NamaObat=VALUES(NamaObat),
 Barcode=VALUES(Barcode),
 Satuan=VALUES(Satuan),
 Kategori=VALUES(Kategori),
 LokasiRak=VALUES(LokasiRak),
 HargaBeli=VALUES(HargaBeli),
 HargaJual=VALUES(HargaJual),
 Stok=VALUES(Stok),
 StokMin=VALUES(StokMin),
 TanggalKadaluarsa=VALUES(TanggalKadaluarsa),
 KodeKFA=VALUES(KodeKFA), Golongan=VALUES(Golongan), BentukSediaan=VALUES(BentukSediaan), Kekuatan=VALUES(Kekuatan), Dosis=VALUES(Dosis);
""";
                    await using var command = new MySqlCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("@kode", kode);
                    command.Parameters.AddWithValue("@nama", nama);
                    command.Parameters.AddWithValue("@barcode", string.IsNullOrWhiteSpace(barcode) ? kode : barcode);
                    command.Parameters.AddWithValue("@satuan", string.IsNullOrWhiteSpace(satuan) ? "Pcs" : satuan);
                    command.Parameters.AddWithValue("@kategori", string.IsNullOrWhiteSpace(kategori) ? "Umum" : kategori);
                    command.Parameters.AddWithValue("@rak", string.IsNullOrWhiteSpace(rak) ? "-" : rak);
                    command.Parameters.AddWithValue("@hb", hb);
                    command.Parameters.AddWithValue("@hj", hj);
                    command.Parameters.AddWithValue("@stok", stok);
                    command.Parameters.AddWithValue("@min", stokMin);
                    command.Parameters.AddWithValue("@exp", string.IsNullOrWhiteSpace(exp) ? DBNull.Value : exp);
                    command.Parameters.AddWithValue("@kfa", string.IsNullOrWhiteSpace(kfa) ? DBNull.Value : kfa);
                    command.Parameters.AddWithValue("@golongan", string.IsNullOrWhiteSpace(golongan) ? DBNull.Value : golongan);
                    command.Parameters.AddWithValue("@sediaan", string.IsNullOrWhiteSpace(sediaan) ? DBNull.Value : sediaan);
                    command.Parameters.AddWithValue("@kekuatan", string.IsNullOrWhiteSpace(kekuatan) ? DBNull.Value : kekuatan);
                    command.Parameters.AddWithValue("@dosis", string.IsNullOrWhiteSpace(dosis) ? DBNull.Value : dosis);

                    var existing = new MySqlCommand("SELECT COUNT(*) FROM obats WHERE KodeObat=@kode;", connection, transaction);
                    existing.Parameters.AddWithValue("@kode", kode);
                    var exists = Convert.ToInt32(await existing.ExecuteScalarAsync()) > 0;
                    await command.ExecuteNonQueryAsync();
                    if (exists) updated++; else inserted++;
                }
                catch
                {
                    skipped++;
                }
                rowNumber++;
            }

            await transaction.CommitAsync();
            await LoadAsync();
            await AppDialog.AlertAsync("Import Excel",
                $"Import selesai.\n\nData baru : {inserted}\nData diperbarui : {updated}\nDilewati : {skipped}\n\nKolom yang tidak ada di Excel menggunakan nilai bawaan aplikasi.", "OK");
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Import Excel", "Import gagal:\n" + ex.Message, "OK");
        }
    }

    private static async Task<int> GetNextGeneratedCodeAsync(MySqlConnection connection, MySqlTransaction transaction)
    {
        await using var command = new MySqlCommand("SELECT COALESCE(MAX(CAST(SUBSTR(KodeObat,4) AS INTEGER)),0) FROM obats WHERE KodeObat LIKE 'OBT%';", connection, transaction);
        var max = Convert.ToInt32(await command.ExecuteScalarAsync());
        return Math.Max(1, max + 1);
    }

    private static string NormalizeHeader(string value) =>
        new string((value ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        try { await DataMaintenanceService.BackupObatAsync(); await AppDialog.AlertAsync("Backup", "Backup data obat berhasil dibuat dalam format Excel (.xlsx).", "OK"); }
        catch (Exception ex) { await AppDialog.AlertAsync("Backup", ex.Message, "OK"); }
    }

    private async void OnResetDataClicked(object sender, EventArgs e)
    {
        var ok = await AppDialog.ConfirmAsync("Reset Data Obat", "Data obat aktif akan diarsipkan terlebih dahulu lalu dihapus dari master. Riwayat penjualan dan pembelian tetap dipertahankan. Pastikan backup Excel sudah dibuat.", "Ya, Reset", "Batal");
        if (!ok) return;
        try { var count = await DataMaintenanceService.ResetObatAsync(); await LoadAsync(); await AppDialog.AlertAsync("Reset Selesai", count == 0 ? "Tidak ada data obat aktif untuk direset." : $"{count} obat diarsipkan. Riwayat transaksi tetap aman.", "OK"); }
        catch (Exception ex) { await AppDialog.AlertAsync("Reset Gagal", ex.Message, "OK"); }
    }

    private async void OnBarcodeCompleted(object sender, EventArgs e)
    {
        var barcode = TxtBarcode.Text?.Trim();
        if (string.IsNullOrWhiteSpace(barcode)) return;
        try
        {
            await using var c = new MySqlConnection(MauiProgram.ConnectionString); await c.OpenAsync();
            const string sql = "SELECT KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa FROM obats WHERE Barcode=@b LIMIT 1";
            await using var cmd = new MySqlCommand(sql, c); cmd.Parameters.AddWithValue("@b", barcode);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                TxtKode.Text = r["KodeObat"]?.ToString(); TxtNama.Text = r["NamaObat"]?.ToString(); TxtBarcode.Text = r["Barcode"]?.ToString(); TxtSatuan.Text = r["Satuan"]?.ToString(); TxtKategori.Text = r["Kategori"]?.ToString(); TxtLokasi.Text = r["LokasiRak"]?.ToString();
                TxtHargaBeli.Text = Convert.ToDecimal(r["HargaBeli"]).ToString("0.##", CultureInfo.InvariantCulture); TxtHargaJual.Text = Convert.ToDecimal(r["HargaJual"]).ToString("0.##", CultureInfo.InvariantCulture); TxtStok.Text = r["Stok"]?.ToString(); TxtStokMin.Text = r["StokMin"]?.ToString();
                if (r["TanggalKadaluarsa"] != DBNull.Value && DateTime.TryParse(r["TanggalKadaluarsa"].ToString(), out var exp)) DtpExpired.Date = exp;
                await AppDialog.AlertAsync("Barcode ditemukan", "Informasi obat yang tersimpan dengan barcode tersebut sudah diisi otomatis.", "OK");
            }
            else await AppDialog.AlertAsync("Barcode Baru", "Barcode belum ada di database. Scanner hanya memberikan nomor barcode; data nama, harga, stok, dan kedaluwarsa tetap harus diisi atau diimpor dari master.", "OK");
        }
        catch (Exception ex) { await AppDialog.AlertAsync("Scan Barcode", ex.Message, "OK"); }
    }

    private ObatItem? _barcodeToPrint;

    private void OnPrintBarcodeClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
        {
            _ = AppDialog.AlertAsync("Barcode", "Data barcode tidak ditemukan.", "OK");
            return;
        }
        var item = button.BindingContext as ObatItem;
        if (item is null && button.CommandParameter is ObatItem parameterItem) item = parameterItem;
        if (item is null || string.IsNullOrWhiteSpace(item.Barcode))
        {
            _ = AppDialog.AlertAsync("Barcode", "Barcode obat belum diisi. Isi barcode pada master obat terlebih dahulu.", "OK");
            return;
        }

        LblBarcodeNama.Text = item.NamaObat;
        LblBarcodeKode.Text = $"Kode: {item.KodeObat}";
        LblBarcodeValue.Text = item.Barcode;
        BarcodeGraphicsView.Drawable = new Code128Drawable(item.Barcode);
        _barcodeToPrint = item;
        ModalBarcode.IsVisible = true;
    }

    private async void OnCetakBarcodePreviewClicked(object sender, EventArgs e)
    {
        if (_barcodeToPrint is null) return;
        var body = BrowserPrintService.BarcodeHtml(_barcodeToPrint.NamaObat, _barcodeToPrint.KodeObat, _barcodeToPrint.Barcode);
        var css = @"
.label{width:80mm;min-height:45mm;margin:0 auto 8mm;padding:4mm;text-align:center;page-break-after:always}.product{font-size:14px;font-weight:700}.code{font-size:10px;margin:2mm 0}.barcode{width:100%;height:22mm;display:block}.barcode-text{font:700 11px Arial;margin-top:1mm}
@page{size:80mm auto;margin:3mm}
";
        var ok = await BrowserPrintService.OpenPrintPreviewAsync($"Barcode {_barcodeToPrint.NamaObat}", body, css);
        if (!ok) await AppDialog.AlertAsync("Print Barcode", "Browser tidak dapat dibuka untuk menampilkan Print Preview.", "OK");
    }

    private void OnTutupBarcodeClicked(object sender, EventArgs e)
    {
        ModalBarcode.IsVisible = false;
        _barcodeToPrint = null;
    }

    private async void OnBuatBarcodeManualClicked(object sender, EventArgs e)
    {
        if (Application.Current?.MainPage is MainPage main)
        {
            await main.OpenBarcodeManualAsync();
            return;
        }
        await AppDialog.AlertAsync("Barcode Manual", "Buka menu Barcode Manual dari navigasi utama.", "OK");
    }

=======
            using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();
            using var cmd = new MySqlCommand(@"SELECT Id,KodeObat,NamaObat,Barcode,Satuan,Kategori,Stok,StokMin,HargaBeli,HargaJual,TanggalKadaluarsa
                                               FROM obats ORDER BY NamaObat", c);
            using var r = await cmd.ExecuteReaderAsync();
            int no=1;
            while(await r.ReadAsync())
            {
                var hj=Convert.ToDecimal(r["HargaJual"]);
                _items.Add(new ObatItem {
                    No=no++, Id=Convert.ToInt32(r["Id"]), KodeObat=r["KodeObat"]?.ToString()??"",
                    NamaObat=r["NamaObat"]?.ToString()??"", Barcode=r["Barcode"]?.ToString()??"",
                    Satuan=r["Satuan"]?.ToString()??"", Kategori=r["Kategori"]?.ToString()??"",
                    Stok=Convert.ToInt32(r["Stok"]), StokMin=Convert.ToInt32(r["StokMin"]),
                    HargaBeli=Convert.ToDecimal(r["HargaBeli"]), HargaJual=hj,
                    HargaFormatted="Rp "+hj.ToString("N0"), Expired=r["TanggalKadaluarsa"]==DBNull.Value?null:Convert.ToDateTime(r["TanggalKadaluarsa"])
                });
            }
        } catch(Exception ex) { await DisplayAlert("Error Obat", ex.Message, "OK"); }
    }

    void OnSearchTextChanged(object s, TextChangedEventArgs e)
    {
        var k=e.NewTextValue?.Trim().ToLowerInvariant()??"";
        CvObatList.ItemsSource=string.IsNullOrEmpty(k)?_items:_items.Where(x=>
            x.KodeObat.ToLowerInvariant().Contains(k)||x.NamaObat.ToLowerInvariant().Contains(k)||
            x.Barcode.ToLowerInvariant().Contains(k)||x.Kategori.ToLowerInvariant().Contains(k)).ToList();
    }
    async void OnRefreshClicked(object s, EventArgs e)=>await LoadAsync();

    void OnTambahObatClicked(object s, EventArgs e)
    {
        _editId=0; LblModalTitle.Text="Tambah Obat"; ClearForm(); ModalLayout.IsVisible=true;
    }
    void OnEditClicked(object s, EventArgs e)
    {
        if ((s as Button)?.CommandParameter is not ObatItem x) return;
        _editId=x.Id; LblModalTitle.Text="Edit Obat"; TxtKode.Text=x.KodeObat; TxtBarcode.Text=x.Barcode;
        TxtNama.Text=x.NamaObat; TxtKategori.Text=x.Kategori; TxtLokasi.Text="";
        TxtSatuan.Text=x.Satuan; TxtStok.Text=x.Stok.ToString(); TxtStokMin.Text=x.StokMin.ToString();
        TxtHargaBeli.Text=x.HargaBeli.ToString("0.##",CultureInfo.InvariantCulture); TxtHargaJual.Text=x.HargaJual.ToString("0.##",CultureInfo.InvariantCulture);
        DtpExpired.Date=x.Expired??DateTime.Today; ModalLayout.IsVisible=true;
    }
    async void OnHapusClicked(object s, EventArgs e)
    {
        if ((s as Button)?.CommandParameter is not ObatItem x) return;
        if(!await DisplayAlert("Hapus",$"Hapus obat {x.NamaObat}?","Ya","Batal")) return;
        try { using var c=new MySqlConnection(MauiProgram.ConnectionString); await c.OpenAsync();
            using var cmd=new MySqlCommand("DELETE FROM obats WHERE Id=@id",c); cmd.Parameters.AddWithValue("@id",x.Id); await cmd.ExecuteNonQueryAsync(); await LoadAsync();
        } catch(Exception ex){await DisplayAlert("Gagal Hapus",ex.Message,"OK");}
    }
    async void OnSimpanClicked(object s, EventArgs e)
    {
        if(string.IsNullOrWhiteSpace(TxtKode.Text)||string.IsNullOrWhiteSpace(TxtNama.Text)){await DisplayAlert("Peringatan","Kode dan nama wajib diisi.","OK");return;}
        int.TryParse(TxtStok.Text,out var stok); int.TryParse(TxtStokMin.Text,out var min);
        decimal.TryParse(TxtHargaBeli.Text?.Replace(",","."),NumberStyles.Any,CultureInfo.InvariantCulture,out var hb);
        decimal.TryParse(TxtHargaJual.Text?.Replace(",","."),NumberStyles.Any,CultureInfo.InvariantCulture,out var hj);
        try{
            using var c=new MySqlConnection(MauiProgram.ConnectionString); await c.OpenAsync();
            MySqlCommand cmd;
            if(_editId==0) cmd=new MySqlCommand(@"INSERT INTO obats(KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa)
                VALUES(@kode,@nama,@barcode,@satuan,@kat,@rak,@hb,@hj,@stok,@min,@exp)",c);
            else cmd=new MySqlCommand(@"UPDATE obats SET KodeObat=@kode,NamaObat=@nama,Barcode=@barcode,Satuan=@satuan,Kategori=@kat,LokasiRak=@rak,HargaBeli=@hb,HargaJual=@hj,Stok=@stok,StokMin=@min,TanggalKadaluarsa=@exp WHERE Id=@id",c);
            cmd.Parameters.AddWithValue("@kode",TxtKode.Text.Trim()); cmd.Parameters.AddWithValue("@nama",TxtNama.Text.Trim());
            cmd.Parameters.AddWithValue("@barcode",string.IsNullOrWhiteSpace(TxtBarcode.Text)?TxtKode.Text.Trim():TxtBarcode.Text.Trim());
            cmd.Parameters.AddWithValue("@satuan",string.IsNullOrWhiteSpace(TxtSatuan.Text)?"Pcs":TxtSatuan.Text.Trim());
            cmd.Parameters.AddWithValue("@kat",string.IsNullOrWhiteSpace(TxtKategori.Text)?"Umum":TxtKategori.Text.Trim());
            cmd.Parameters.AddWithValue("@rak",string.IsNullOrWhiteSpace(TxtLokasi.Text)?"-":TxtLokasi.Text.Trim());
            cmd.Parameters.AddWithValue("@hb",hb);cmd.Parameters.AddWithValue("@hj",hj);cmd.Parameters.AddWithValue("@stok",stok);cmd.Parameters.AddWithValue("@min",min);cmd.Parameters.AddWithValue("@exp",DtpExpired.Date);
            if(_editId!=0)cmd.Parameters.AddWithValue("@id",_editId);
            await cmd.ExecuteNonQueryAsync(); ModalLayout.IsVisible=false; await LoadAsync();
        }catch(Exception ex){await DisplayAlert("Gagal Simpan",ex.Message,"OK");}
    }
    void OnBatalClicked(object s,EventArgs e)=>ModalLayout.IsVisible=false;
    void ClearForm(){foreach(var e in new[]{TxtKode,TxtBarcode,TxtNama,TxtKategori,TxtLokasi,TxtSatuan,TxtStok,TxtStokMin,TxtHargaBeli,TxtHargaJual})e.Text="";DtpExpired.Date=DateTime.Today;}

    async void OnExportClicked(object s,EventArgs e)
    {
        try{
            using var c=new MySqlConnection(MauiProgram.ConnectionString);await c.OpenAsync();
            using var cmd=new MySqlCommand("SELECT KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa FROM obats ORDER BY NamaObat",c);
            using var r=await cmd.ExecuteReaderAsync();var sb=new StringBuilder("KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa\n");
            while(await r.ReadAsync()) sb.AppendLine(string.Join(",",Enumerable.Range(0,r.FieldCount).Select(i=>$"\"{r.GetValue(i)?.ToString()?.Replace("\"","\"\"")}\"")));
            await SaveCsvAsync($"Data_Obat_{DateTime.Now:yyyyMMdd_HHmmss}.csv",sb.ToString());
        }catch(Exception ex){await DisplayAlert("Export",ex.Message,"OK");}
    }
    async void OnImportClicked(object s,EventArgs e)
    {
        try{
            var f=await FilePicker.Default.PickAsync(new PickOptions{PickerTitle="Pilih CSV"});
            if(f==null)return;
            using var stream=await f.OpenReadAsync();using var sr=new StreamReader(stream);await sr.ReadLineAsync();
            using var c=new MySqlConnection(MauiProgram.ConnectionString);await c.OpenAsync();int ok=0;
            while(await sr.ReadLineAsync() is string line){
                if(string.IsNullOrWhiteSpace(line))continue;var a=line.Split(',');if(a.Length<10)continue;
                string Clean(string v)=>v.Trim().Trim('"'); 
                using var cmd=new MySqlCommand(@"INSERT INTO obats(KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa)
                    VALUES(@k,@n,@b,@s,@cat,@rak,@hb,@hj,@st,@mn,@ex)
                    ON DUPLICATE KEY UPDATE NamaObat=VALUES(NamaObat),HargaBeli=VALUES(HargaBeli),HargaJual=VALUES(HargaJual),Stok=VALUES(Stok),StokMin=VALUES(StokMin),TanggalKadaluarsa=VALUES(TanggalKadaluarsa)",c);
                cmd.Parameters.AddWithValue("@k",Clean(a[0]));cmd.Parameters.AddWithValue("@n",Clean(a[1]));cmd.Parameters.AddWithValue("@b",Clean(a[2]));cmd.Parameters.AddWithValue("@s",Clean(a[3]));cmd.Parameters.AddWithValue("@cat",Clean(a[4]));cmd.Parameters.AddWithValue("@rak",Clean(a[5]));
                decimal.TryParse(Clean(a[6]),NumberStyles.Any,CultureInfo.InvariantCulture,out var hb);decimal.TryParse(Clean(a[7]),NumberStyles.Any,CultureInfo.InvariantCulture,out var hj);
                int.TryParse(Clean(a[8]),out var st);int.TryParse(Clean(a[9]),out var mn);DateTime.TryParse(a.Length>10?Clean(a[10]):"",out var ex);
                cmd.Parameters.AddWithValue("@hb",hb);cmd.Parameters.AddWithValue("@hj",hj);cmd.Parameters.AddWithValue("@st",st);cmd.Parameters.AddWithValue("@mn",mn);cmd.Parameters.AddWithValue("@ex",ex==default?null:ex);await cmd.ExecuteNonQueryAsync();ok++;
            } await LoadAsync();await DisplayAlert("Import", $"Berhasil memproses {ok} baris.","OK");
        }catch(Exception ex){await DisplayAlert("Import",ex.Message,"OK");}
    }
    async Task SaveCsvAsync(string name,string content){
#if WINDOWS
        var picker=new Windows.Storage.Pickers.FileSavePicker();picker.SuggestedStartLocation=Windows.Storage.Pickers.PickerLocationId.Downloads;picker.FileTypeChoices.Add("CSV",new List<string>{".csv"});picker.SuggestedFileName=name;
        var w=Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;if(w!=null){var hwnd=WinRT.Interop.WindowNative.GetWindowHandle(w);WinRT.Interop.InitializeWithWindow.Initialize(picker,hwnd);}
        var f=await picker.PickSaveFileAsync();if(f!=null)await Windows.Storage.FileIO.WriteTextAsync(f,content);
#else
        var p=Path.Combine(FileSystem.CacheDirectory,name);await File.WriteAllTextAsync(p,content);
#endif
    }
>>>>>>> 7afaaf3961c1cd72c085b84ad7fb4cbabc40b75a
}
