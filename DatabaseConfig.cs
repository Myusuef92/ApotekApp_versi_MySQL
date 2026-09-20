namespace ApotekApp;

/// <summary>
/// Konfigurasi koneksi MySQL/MariaDB untuk aplikasi Apotek.
/// Jika XAMPP/WAMP memakai root tanpa password, nilai default di bawah sudah siap dipakai.
/// Untuk server lain, ubah Server, Port, User dan Password lalu rebuild aplikasi.
/// </summary>
public static class DatabaseConfig
{
    public const string DatabaseName = "apotek_db";
    public const string Server = "127.0.0.1";
    public const uint Port = 3306;
    public const string User = "root";
    public const string Password = "";

    public static string ServerConnectionString =>
        $"Server={Server};Port={Port};User ID={User};Password={Password};CharSet=utf8mb4;SslMode=None;Allow User Variables=True;";

    public static string ConnectionString =>
        $"Server={Server};Port={Port};Database={DatabaseName};User ID={User};Password={Password};CharSet=utf8mb4;SslMode=None;Allow User Variables=True;Connection Timeout=10;Default Command Timeout=30;";
}
