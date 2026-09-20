using MySqlConnector;
using ApotekApp.Services;

namespace ApotekApp;

public partial class LoginPage : ContentPage
{
    private readonly string _connString = MauiProgram.ConnectionString;

    public LoginPage() => InitializeComponent();

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = TxtUsername?.Text?.Trim() ?? "";
        string password = TxtPassword?.Text ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Peringatan", "Username dan Password wajib diisi.", "OK");
            return;
        }

        try
        {
            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();

            const string sql = @"SELECT u.id, u.username, u.nama_lengkap, u.password, 
                                        COALESCE(r.nama_role,'Kasir') AS role
                                 FROM users u
                                 LEFT JOIN roles r ON r.id = u.role_id
                                 WHERE u.username=@username LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                await DisplayAlert("Login Gagal", "Username atau Password salah.", "OK");
                return;
            }

            string stored = reader["password"]?.ToString() ?? "";
            if (!PasswordService.Verify(password, stored))
            {
                await DisplayAlert("Login Gagal", "Username atau Password salah.", "OK");
                return;
            }

            // Legacy installations may still contain plaintext passwords. Upgrade
            // the matching account immediately after a successful login.
            if (!PasswordService.IsHashed(stored) && !PasswordService.IsEncrypted(stored))
            {
                await using var upgrade = new MySqlCommand("UPDATE users SET password=@password WHERE id=@id", conn);
                upgrade.Parameters.AddWithValue("@password", PasswordService.Encrypt(password));
                upgrade.Parameters.AddWithValue("@id", Convert.ToInt32(reader["id"]));
                await upgrade.ExecuteNonQueryAsync();
            }

            Preferences.Set("CurrentUserId", Convert.ToInt32(reader["id"]));
            Preferences.Set("CurrentUserName", reader["nama_lengkap"]?.ToString() ?? username);
            Preferences.Set("CurrentUsername", username);
            Preferences.Set("CurrentUserRole", reader["role"]?.ToString() ?? "Kasir");

            if (Application.Current != null)
                Application.Current.MainPage = new NavigationPage(new MainPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Koneksi Database", 
                "Tidak dapat terhubung ke database MySQL/MariaDB.\n\n" + ex.Message, "OK");
        }
    }

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        TxtPassword.IsPassword = !TxtPassword.IsPassword;
    }
}
