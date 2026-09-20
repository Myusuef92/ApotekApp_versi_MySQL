using MySqlConnector;
using ApotekApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApotekApp;

public sealed class UserItem : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NamaLengkap { get; set; } = string.Empty;
    public string Role { get; set; } = "Kasir";
    public string StoredPassword { get; set; } = string.Empty;

    private bool _passwordVisible;
    private string _passwordDisplay = "••••••••";

    public string PasswordDisplay
    {
        get => _passwordDisplay;
        private set { _passwordDisplay = value; OnPropertyChanged(); }
    }

    public string PasswordEyeIcon => _passwordVisible ? "🙈" : "👁";

    public void SetPasswordVisible(bool visible, string? password = null)
    {
        _passwordVisible = visible;
        PasswordDisplay = visible && !string.IsNullOrEmpty(password) ? password : "••••••••";
        OnPropertyChanged(nameof(PasswordEyeIcon));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class UserPage : ContentPage
{
    private readonly ObservableCollection<UserItem> _items = new();
    private int _editId;
    private bool _showPassword;
    private bool _deleteInProgress;

    public UserPage()
    {
        InitializeComponent();
        CvUserList.ItemsSource = _items;
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
                SELECT u.id, u.username, u.nama_lengkap, u.password,
                       COALESCE(r.nama_role, 'Kasir') AS role
                FROM users u
                LEFT JOIN roles r ON r.id = u.role_id
                ORDER BY u.nama_lengkap;
                """;

            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                _items.Add(new UserItem
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Username = reader["username"]?.ToString() ?? string.Empty,
                    NamaLengkap = reader["nama_lengkap"]?.ToString() ?? string.Empty,
                    Role = reader["role"]?.ToString() ?? "Kasir",
                    StoredPassword = reader["password"]?.ToString() ?? string.Empty
                });
            }
            ApplySearch(TxtSearch.Text);
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("User", ex.Message, "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplySearch(e.NewTextValue);

    private void ApplySearch(string? keyword)
    {
        var key = keyword?.Trim() ?? string.Empty;
        CvUserList.ItemsSource = string.IsNullOrEmpty(key)
            ? _items
            : _items.Where(item =>
                item.Username.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                item.NamaLengkap.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                item.Role.Contains(key, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadAsync();

    private void OnTambahUserClicked(object sender, EventArgs e)
    {
        _editId = 0;
        LblModalTitle.Text = "Tambah User";
        TxtUsername.Text = string.Empty;
        TxtNama.Text = string.Empty;
        TxtPassword.Text = string.Empty;
        _showPassword = false;
        TxtPassword.IsPassword = true;
        BtnTogglePassword.Text = "👁";
        CmbRole.SelectedIndex = 2;
        ModalLayout.IsVisible = true;
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        var item = GetItemFromButton(sender);
        if (item is null) return;

        _editId = item.Id;
        LblModalTitle.Text = "Edit User";
        TxtUsername.Text = item.Username;
        TxtNama.Text = item.NamaLengkap;
        TxtPassword.Text = string.Empty;
        _showPassword = false;
        TxtPassword.IsPassword = true;
        BtnTogglePassword.Text = "👁";
        CmbRole.SelectedIndex = Math.Max(0, CmbRole.Items.IndexOf(item.Role));
        ModalLayout.IsVisible = true;
    }

    private async void OnToggleRowPasswordClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not UserItem item)
            return;

        if (item.PasswordDisplay != "••••••••")
        {
            item.SetPasswordVisible(false);
            return;
        }

        if (PasswordService.TryDecryptPassword(item.StoredPassword, out var password))
        {
            item.SetPasswordVisible(true, password);
            return;
        }

        if (PasswordService.IsHashed(item.StoredPassword))
        {
            await AppDialog.AlertAsync(
                "Password Tersimpan",
                $"Password lama untuk user '{item.Username}' sebelumnya disimpan sebagai hash, sehingga tidak dapat ditampilkan. Klik Edit lalu buat password baru; setelah disimpan, password baru dapat dilihat melalui ikon mata.",
                "OK");
            return;
        }

        await AppDialog.AlertAsync("Password", "Password tidak dapat dibaca dari format penyimpanan saat ini. Silakan buat password baru melalui Edit.", "OK");
    }

    private async void OnHapusClicked(object sender, EventArgs e) => await HandleDeleteButtonAsync(sender);

    private async Task HandleDeleteButtonAsync(object sender)
    {
        if (_deleteInProgress || sender is not Button button) return;
        _deleteInProgress = true;
        try
        {
            var id = GetIdFromButton(button);
            var item = _items.FirstOrDefault(x => x.Id == id);
            if (item is null)
            {
                await AppDialog.AlertAsync("Hapus User", "Data user tidak ditemukan.", "OK");
                return;
            }
            await DeleteUserAsync(item);
        }
        finally { _deleteInProgress = false; }
    }

    private static int GetIdFromButton(Button button)
    {
        if (button.CommandParameter is int id) return id;
        return int.TryParse(button.CommandParameter?.ToString(), out id) ? id : 0;
    }

    private async Task DeleteUserAsync(UserItem item)
    {
        var currentUserId = Preferences.Get("CurrentUserId", 0);
        if (item.Id == currentUserId)
        {
            await AppDialog.AlertAsync("Tidak Dapat Dihapus", "Akun yang sedang digunakan tidak boleh dihapus.", "OK");
            return;
        }

        if (!await AppDialog.ConfirmAsync("Konfirmasi Hapus", $"Hapus user \"{item.Username}\"?", "Ya, Hapus", "Batal")) return;

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();
            await using var command = new MySqlCommand("DELETE FROM users WHERE id=@id;", connection);
            command.Parameters.AddWithValue("@id", item.Id);
            if (await command.ExecuteNonQueryAsync() <= 0)
            {
                await AppDialog.AlertAsync("Hapus User", "Data user tidak ditemukan di database.", "OK");
                return;
            }
            _items.Remove(item);
            await AppDialog.AlertAsync("Berhasil", $"User \"{item.Username}\" berhasil dihapus.", "OK");
        }
        catch (Exception ex) { await AppDialog.AlertAsync("Gagal Hapus", ex.Message, "OK"); }
    }

    private static UserItem? GetItemFromButton(object sender) =>
        sender is Button button ? button.CommandParameter as UserItem ?? button.BindingContext as UserItem : null;

    private async void OnSimpanUserClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtUsername.Text) || string.IsNullOrWhiteSpace(TxtNama.Text))
        {
            await AppDialog.AlertAsync("Peringatan", "Username dan nama wajib diisi.", "OK");
            return;
        }

        try
        {
            await using var connection = new MySqlConnection(MauiProgram.ConnectionString);
            await connection.OpenAsync();
            var roleName = CmbRole.SelectedItem?.ToString() ?? "Kasir";

            await using var roleCommand = new MySqlCommand("SELECT id FROM roles WHERE nama_role=@role LIMIT 1;", connection);
            roleCommand.Parameters.AddWithValue("@role", roleName);
            var roleResult = await roleCommand.ExecuteScalarAsync();
            if (roleResult is null) throw new InvalidOperationException("Role yang dipilih tidak tersedia.");
            var roleId = Convert.ToInt32(roleResult);

            string sql;
            if (_editId == 0)
            {
                if (string.IsNullOrWhiteSpace(TxtPassword.Text))
                {
                    await AppDialog.AlertAsync("Peringatan", "Password wajib diisi untuk user baru.", "OK");
                    return;
                }
                sql = "INSERT INTO users(username,password,nama_lengkap,role_id) VALUES(@username,@password,@nama,@role);";
            }
            else if (!string.IsNullOrWhiteSpace(TxtPassword.Text))
            {
                sql = "UPDATE users SET username=@username,password=@password,nama_lengkap=@nama,role_id=@role WHERE id=@id;";
            }
            else
            {
                sql = "UPDATE users SET username=@username,nama_lengkap=@nama,role_id=@role WHERE id=@id;";
            }

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@username", TxtUsername.Text.Trim());
            command.Parameters.AddWithValue("@nama", TxtNama.Text.Trim());
            command.Parameters.AddWithValue("@role", roleId);
            if (_editId > 0) command.Parameters.AddWithValue("@id", _editId);
            if (_editId == 0 || !string.IsNullOrWhiteSpace(TxtPassword.Text))
                command.Parameters.AddWithValue("@password", PasswordService.Encrypt(TxtPassword.Text!.Trim()));

            await command.ExecuteNonQueryAsync();
            ModalLayout.IsVisible = false;
            await LoadAsync();
            await AppDialog.AlertAsync("Berhasil", _editId == 0 ? "User berhasil ditambahkan." : "Data user dan password berhasil diperbarui.", "OK");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await AppDialog.AlertAsync("Gagal Simpan", "Username sudah digunakan. Silakan gunakan username lain.", "OK");
        }
        catch (Exception ex) { await AppDialog.AlertAsync("Gagal Simpan", ex.Message, "OK"); }
    }

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        _showPassword = !_showPassword;
        TxtPassword.IsPassword = !_showPassword;
        BtnTogglePassword.Text = _showPassword ? "🙈" : "👁";
    }

    private void OnBatalModalClicked(object sender, EventArgs e)
    {
        ModalLayout.IsVisible = false;
        _showPassword = false;
        TxtPassword.IsPassword = true;
        BtnTogglePassword.Text = "👁";
    }
}
