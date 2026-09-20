using MySqlConnector;
using System.Collections.ObjectModel;

namespace ApotekApp;

public sealed class SupplierItem
{
    public int Id { get; set; }
    public string NamaSupplier { get; set; } = string.Empty;
    public string Telepon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Alamat { get; set; } = string.Empty;
}

public partial class SupplierPage : ContentPage
{
    private readonly ObservableCollection<SupplierItem> _items = new();
    private int _editId;

    public SupplierPage()
    {
        InitializeComponent();
        BindingContext = this;
        CvSupplierList.ItemsSource = _items;
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
            _items.Clear();

            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT Id, NamaSupplier, Telepon, Alamat, Email
                FROM suppliers
                ORDER BY NamaSupplier;
                """;

            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                _items.Add(new SupplierItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    NamaSupplier = reader["NamaSupplier"]?.ToString() ?? string.Empty,
                    Telepon = reader["Telepon"]?.ToString() ?? string.Empty,
                    Alamat = reader["Alamat"]?.ToString() ?? string.Empty,
                    Email = reader["Email"]?.ToString() ?? string.Empty
                });
            }

            ApplySearch(TxtSearch.Text);
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Supplier", ex.Message, "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearch(e.NewTextValue);
    }

    private void ApplySearch(string? keyword)
    {
        var key = keyword?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(key))
        {
            CvSupplierList.ItemsSource = _items;
            return;
        }

        CvSupplierList.ItemsSource = _items
            .Where(item =>
                item.NamaSupplier.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                item.Telepon.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                item.Email.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                item.Alamat.Contains(key, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadAsync();
    }

    private void OnTambahSupplierClicked(object sender, EventArgs e)
    {
        _editId = 0;
        LblModalTitle.Text = "Tambah Supplier";

        TxtNama.Text = string.Empty;
        TxtTelepon.Text = string.Empty;
        TxtEmail.Text = string.Empty;
        TxtAlamat.Text = string.Empty;

        ModalLayout.IsVisible = true;
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        var item = GetItemFromButton(sender);
        if (item is null)
            return;

        _editId = item.Id;
        LblModalTitle.Text = "Edit Supplier";

        TxtNama.Text = item.NamaSupplier;
        TxtTelepon.Text = item.Telepon;
        TxtEmail.Text = item.Email;
        TxtAlamat.Text = item.Alamat;

        ModalLayout.IsVisible = true;
    }

    private bool _deleteInProgress;

    private async void OnHapusClicked(object sender, EventArgs e)
    {
        await HandleDeleteButtonAsync(sender);
    }

    private async Task HandleDeleteButtonAsync(object sender)
    {
        if (_deleteInProgress || sender is not Button button)
            return;

        _deleteInProgress = true;
        try
        {
            var id = GetIdFromButton(button);
            if (id <= 0)
            {
                await AppDialog.AlertAsync("Hapus Supplier", "ID data tidak ditemukan.", "OK");
                return;
            }

            var item = _items.FirstOrDefault(x => x.Id == id);
            if (item is null)
            {
                await AppDialog.AlertAsync("Hapus Supplier", "Data tidak ditemukan pada daftar.", "OK");
                return;
            }

            await DeleteSupplierAsync(item);
        }
        finally
        {
            _deleteInProgress = false;
        }
    }

    private static int GetIdFromButton(Button button)
    {
        if (button.BindingContext is SupplierItem item)
            return item.Id;

        if (button.CommandParameter is int id)
            return id;

        return int.TryParse(button.CommandParameter?.ToString(), out id) ? id : 0;
    }

    private async Task DeleteSupplierAsync(SupplierItem item)
    {        var confirm = await AppDialog.ConfirmAsync(
            "Konfirmasi Hapus",
            $"Hapus supplier \"{item.NamaSupplier}\"?",
            "Ya, Hapus",
            "Batal");

        if (!confirm)
            return;

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            await using var deleteCommand = new MySqlCommand(
                "DELETE FROM suppliers WHERE Id = @id;",
                connection);

            deleteCommand.Parameters.AddWithValue("@id", item.Id);
            var affectedRows = await deleteCommand.ExecuteNonQueryAsync();

            if (affectedRows <= 0)
            {
                await AppDialog.AlertAsync("Hapus Supplier", "Data supplier tidak ditemukan di database.", "OK");
                return;
            }

            _items.Remove(item);
            await AppDialog.AlertAsync(
                "Berhasil",
                $"Supplier \"{item.NamaSupplier}\" berhasil dihapus.",
                "OK");
        }
        catch (MySqlException ex)
        {
            await AppDialog.AlertAsync("Gagal Hapus", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Gagal Hapus", ex.Message, "OK");
        }
    
    }



    private static SupplierItem? GetItemFromButton(object sender)
    {
        if (sender is not Button button)
            return null;

        return button.BindingContext as SupplierItem
               ?? button.CommandParameter as SupplierItem;
    }

    private async void OnSimpanSupplierClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNama.Text))
        {
            await AppDialog.AlertAsync("Peringatan", "Nama supplier wajib diisi.", "OK");
            return;
        }

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();

            var sql = _editId == 0
                ? """
                  INSERT INTO suppliers (NamaSupplier, Telepon, Alamat, Email)
                  VALUES (@nama, @telepon, @alamat, @email);
                  """
                : """
                  UPDATE suppliers
                  SET NamaSupplier = @nama,
                      Telepon = @telepon,
                      Alamat = @alamat,
                      Email = @email
                  WHERE Id = @id;
                  """;

            await using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@nama", TxtNama.Text.Trim());
            command.Parameters.AddWithValue("@telepon", TxtTelepon.Text?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@alamat", TxtAlamat.Text?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@email", TxtEmail.Text?.Trim() ?? string.Empty);

            if (_editId > 0)
                command.Parameters.AddWithValue("@id", _editId);

            await command.ExecuteNonQueryAsync();

            ModalLayout.IsVisible = false;
            await LoadAsync();

            await AppDialog.AlertAsync(
                "Berhasil",
                _editId == 0
                    ? "Supplier berhasil ditambahkan."
                    : "Supplier berhasil diperbarui.",
                "OK");
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

    private void OnBatalSupplierClicked(object sender, EventArgs e)
    {
        ModalLayout.IsVisible = false;
    }
}
