using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Microsoft.Maui.Storage;
using Microsoft.Maui.LifecycleEvents;
#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
#endif
using ApotekApp.Data;
using ApotekApp.Services;

namespace ApotekApp;

public static class MauiProgram
{
    public const string DatabaseName = DatabaseConfig.DatabaseName;
    public static string ConnectionString => DatabaseConfig.ConnectionString;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
#if WINDOWS
            .ConfigureMauiHandlers(handlers =>
            {
                PickerHandler.Mapper.AppendToMapping("ForceLightDropdown", (handler, view) =>
                {
                    var combo = handler.PlatformView;
                    combo.RequestedTheme = ElementTheme.Light;
                    combo.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray);
                    combo.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
                    combo.BorderBrush = null;
                    combo.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                    combo.MaxDropDownHeight = 360;
                    combo.MinWidth = 180;
                    combo.HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                    combo.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                    combo.Padding = new Microsoft.UI.Xaml.Thickness(10, 0, 10, 0);
                });
            })
#endif
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows => windows.OnWindowCreated(window => window.ExtendsContentIntoTitleBar = true));
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Pastikan database MySQL sudah tersedia sebelum EF Core melakukan AutoDetect versi server.
        EnsureDatabase();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString)),
            ServiceLifetime.Transient);
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ObatPage>();
        builder.Services.AddTransient<SupplierPage>();
        builder.Services.AddTransient<UserPage>();
        builder.Services.AddTransient<TransaksiPage>();
        builder.Services.AddTransient<PembelianPage>();
        builder.Services.AddTransient<LaporanPage>();
        builder.Services.AddTransient<SettingPage>();
        builder.Services.AddTransient<PelangganPage>();
        builder.Services.AddTransient<ResepPage>();
        builder.Services.AddTransient<StokPage>();
        builder.Services.AddTransient<BarcodeManualPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    private static void EnsureDatabase()
    {
        try
        {
            // phpMyAdmin biasanya berjalan di atas MySQL/MariaDB pada port 3306.
            // Database dibuat otomatis jika user database memiliki hak CREATE DATABASE.
            using (var server = new MySqlConnection(DatabaseConfig.ServerConnectionString))
            {
                server.Open();
                using var create = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;", server);
                create.ExecuteNonQuery();
            }

            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            string[] statements =
            {
                "CREATE TABLE IF NOT EXISTS roles (id INT NOT NULL AUTO_INCREMENT, nama_role VARCHAR(100) NOT NULL, PRIMARY KEY(id), UNIQUE KEY uq_roles_nama_role(nama_role)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS users (id INT NOT NULL AUTO_INCREMENT, username VARCHAR(100) NOT NULL, password TEXT NOT NULL, nama_lengkap VARCHAR(150) NOT NULL, role_id INT NOT NULL, PRIMARY KEY(id), UNIQUE KEY uq_users_username(username), KEY idx_users_role(role_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS obats (Id INT NOT NULL AUTO_INCREMENT, KodeObat VARCHAR(100) NOT NULL, NamaObat VARCHAR(255) NOT NULL, Barcode VARCHAR(100) NULL, Satuan VARCHAR(50) NOT NULL DEFAULT 'Pcs', Kategori VARCHAR(100) NOT NULL DEFAULT 'Umum', LokasiRak VARCHAR(100) NOT NULL DEFAULT '-', HargaBeli DECIMAL(18,2) NOT NULL DEFAULT 0, HargaJual DECIMAL(18,2) NOT NULL DEFAULT 0, Stok INT NOT NULL DEFAULT 0, StokMin INT NOT NULL DEFAULT 0, TanggalKadaluarsa VARCHAR(50) NULL, Gambar LONGTEXT NULL, KodeKFA VARCHAR(100) NULL, Golongan VARCHAR(100) NULL, BentukSediaan VARCHAR(100) NULL, Kekuatan VARCHAR(100) NULL, Dosis VARCHAR(100) NULL, IsAktif TINYINT(1) NOT NULL DEFAULT 1, PRIMARY KEY(Id), UNIQUE KEY uq_obats_kode(KodeObat), KEY idx_obats_nama(NamaObat), KEY idx_obats_barcode(Barcode)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS suppliers (Id INT NOT NULL AUTO_INCREMENT, NamaSupplier VARCHAR(200) NOT NULL, Telepon VARCHAR(50) NOT NULL DEFAULT '', Alamat TEXT NOT NULL, Email VARCHAR(150) NOT NULL DEFAULT '', PRIMARY KEY(Id), KEY idx_suppliers_nama(NamaSupplier)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS penjualan (id INT NOT NULL AUTO_INCREMENT, no_nota VARCHAR(100) NOT NULL, tanggal DATETIME NOT NULL, total DECIMAL(18,2) NOT NULL DEFAULT 0, diskon DECIMAL(18,2) NOT NULL DEFAULT 0, grand_total DECIMAL(18,2) NOT NULL DEFAULT 0, bayar DECIMAL(18,2) NOT NULL DEFAULT 0, kembali DECIMAL(18,2) NOT NULL DEFAULT 0, user_id INT NULL, PRIMARY KEY(id), UNIQUE KEY uq_penjualan_no_nota(no_nota), KEY idx_penjualan_tanggal(tanggal)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS detail_penjualan (id INT NOT NULL AUTO_INCREMENT, no_nota VARCHAR(100) NOT NULL, id_obat INT NOT NULL, nama_obat VARCHAR(255) NULL, harga DECIMAL(18,2) NOT NULL, harga_beli DECIMAL(18,2) NOT NULL DEFAULT 0, qty INT NOT NULL, subtotal DECIMAL(18,2) NOT NULL, PRIMARY KEY(id), KEY idx_detail_penjualan_nota(no_nota), KEY idx_detail_penjualan_obat(id_obat)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS pembelians (IdPembelian INT NOT NULL AUTO_INCREMENT, NoFaktur VARCHAR(100) NOT NULL, TanggalPembelian DATETIME NOT NULL, TotalHarga DECIMAL(18,2) NOT NULL DEFAULT 0, Catatan TEXT NULL, SupplierId INT NOT NULL, nama_supplier VARCHAR(200) NULL, PRIMARY KEY(IdPembelian), KEY idx_pembelian_tanggal(TanggalPembelian), KEY idx_pembelian_supplier(SupplierId)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS detail_pembelians (IdDetailPembelian INT NOT NULL AUTO_INCREMENT, PembelianId INT NOT NULL, ObatId INT NOT NULL, nama_obat VARCHAR(255) NULL, Jumlah INT NOT NULL, HargaBeli DECIMAL(18,2) NOT NULL DEFAULT 0, PRIMARY KEY(IdDetailPembelian), KEY idx_detail_pembelian_pembelian(PembelianId), KEY idx_detail_pembelian_obat(ObatId)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS pengaturan (id INT NOT NULL, nama_apotek VARCHAR(200) NOT NULL DEFAULT 'APOTEK SEHAT', nama_pemilik VARCHAR(200) NULL, alamat TEXT NULL, no_telepon VARCHAR(50) NULL, sia_sipa VARCHAR(150) NULL, catatan_struk TEXT NULL, logo_path TEXT NULL, logo_data LONGBLOB NULL, ukuran_kertas VARCHAR(20) NOT NULL DEFAULT '58mm', pajak_ppn DECIMAL(8,2) NOT NULL DEFAULT 0, organization_id VARCHAR(150) NULL, satusehat_client_id VARCHAR(200) NULL, satusehat_client_secret TEXT NULL, backup_folder TEXT NULL, PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS arsip_obat (id INT NOT NULL AUTO_INCREMENT, obat_id_lama INT NOT NULL, kode_obat VARCHAR(100) NULL, nama_obat VARCHAR(255) NULL, barcode VARCHAR(100) NULL, satuan VARCHAR(50) NULL, kategori VARCHAR(100) NULL, lokasi_rak VARCHAR(100) NULL, harga_beli DECIMAL(18,2) NULL, harga_jual DECIMAL(18,2) NULL, stok INT NULL, stok_min INT NULL, tanggal_kadaluarsa VARCHAR(50) NULL, diarsipkan_pada DATETIME NOT NULL, PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS aset_modal (id INT NOT NULL AUTO_INCREMENT, nama VARCHAR(200) NOT NULL, jenis VARCHAR(100) NOT NULL, nilai DECIMAL(18,2) NOT NULL, tanggal DATETIME NOT NULL, keterangan TEXT NULL, PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS pelanggan (id INT NOT NULL AUTO_INCREMENT, no_rm VARCHAR(100) NOT NULL, nama VARCHAR(200) NOT NULL, nik VARCHAR(50) NULL, jenis_kelamin VARCHAR(30) NULL, tanggal_lahir VARCHAR(50) NULL, telepon VARCHAR(50) NULL, alamat TEXT NULL, alergi TEXT NULL, catatan TEXT NULL, satusehat_id VARCHAR(150) NULL, aktif TINYINT(1) NOT NULL DEFAULT 1, PRIMARY KEY(id), UNIQUE KEY uq_pelanggan_rm(no_rm), KEY idx_pelanggan_nama(nama)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS resep (id INT NOT NULL AUTO_INCREMENT, no_resep VARCHAR(100) NOT NULL, tanggal DATETIME NOT NULL, pelanggan_id INT NULL, nama_dokter VARCHAR(200) NULL, sip_dokter VARCHAR(100) NULL, status VARCHAR(50) NOT NULL DEFAULT 'Menunggu', jenis_resep VARCHAR(100) NOT NULL DEFAULT 'Resep Umum', catatan TEXT NULL, user_id INT NULL, PRIMARY KEY(id), UNIQUE KEY uq_resep_no(no_resep), KEY idx_resep_tanggal(tanggal), KEY idx_resep_pelanggan(pelanggan_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS resep_detail (id INT NOT NULL AUTO_INCREMENT, resep_id INT NOT NULL, obat_id INT NULL, nama_obat VARCHAR(255) NOT NULL, qty INT NOT NULL DEFAULT 1, aturan_pakai VARCHAR(255) NULL, dosis VARCHAR(100) NULL, catatan TEXT NULL, substitusi TINYINT(1) NOT NULL DEFAULT 0, PRIMARY KEY(id), KEY idx_resep_detail_resep(resep_id), KEY idx_resep_detail_obat(obat_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS mutasi_stok (id INT NOT NULL AUTO_INCREMENT, tanggal DATETIME NOT NULL, obat_id INT NOT NULL, jenis VARCHAR(50) NOT NULL, qty_masuk INT NOT NULL DEFAULT 0, qty_keluar INT NOT NULL DEFAULT 0, stok_setelah INT NOT NULL DEFAULT 0, referensi VARCHAR(200) NULL, batch VARCHAR(100) NULL, expired VARCHAR(50) NULL, harga_satuan DECIMAL(18,2) NOT NULL DEFAULT 0, user_id INT NULL, keterangan TEXT NULL, PRIMARY KEY(id), KEY idx_mutasi_tanggal(tanggal), KEY idx_mutasi_obat(obat_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS audit_log (id INT NOT NULL AUTO_INCREMENT, waktu DATETIME NOT NULL, user_id INT NULL, modul VARCHAR(100) NOT NULL, aksi VARCHAR(100) NOT NULL, referensi VARCHAR(200) NULL, detail TEXT NULL, PRIMARY KEY(id), KEY idx_audit_waktu(waktu)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS sesi_kasir (id INT NOT NULL AUTO_INCREMENT, user_id INT NOT NULL, buka DATETIME NOT NULL, tutup DATETIME NULL, saldo_awal DECIMAL(18,2) NOT NULL DEFAULT 0, saldo_akhir DECIMAL(18,2) NULL, status VARCHAR(30) NOT NULL DEFAULT 'OPEN', catatan TEXT NULL, PRIMARY KEY(id), KEY idx_sesi_user(user_id), KEY idx_sesi_status(status)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                "CREATE TABLE IF NOT EXISTS batch_obat (id INT NOT NULL AUTO_INCREMENT, obat_id INT NOT NULL, no_batch VARCHAR(100) NOT NULL, tanggal_kadaluarsa VARCHAR(50) NULL, qty INT NOT NULL DEFAULT 0, harga_beli DECIMAL(18,2) NOT NULL DEFAULT 0, supplier_id INT NULL, PRIMARY KEY(id), KEY idx_batch_obat(obat_id), KEY idx_batch_supplier(supplier_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;"
            };

            foreach (var sql in statements)
            {
                using var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            Exec(conn, "INSERT IGNORE INTO roles(id,nama_role) VALUES (1,'Admin'),(2,'Apoteker'),(3,'Kasir');");
            Exec(conn, "INSERT IGNORE INTO users(id,username,password,nama_lengkap,role_id) VALUES (1,'admin','$PBKDF2-SHA256$120000$kNB7OAelShFbo+PGGcBIyA==$E/x5yXC92aG15Cz49cgy4dc3JgmFU3fPnNtPXJcRKbs=','Administrator',1);");
            MigratePlaintextPasswords(conn);
            Exec(conn, "INSERT IGNORE INTO pengaturan(id,nama_apotek,alamat,no_telepon,ukuran_kertas,pajak_ppn) VALUES(1,'APOTEK SEHAT','Jl. Kesehatan No. 1','0812-3456-7890','58mm',0);");

            using var dateCmd = new MySqlCommand("SELECT Id,TanggalKadaluarsa FROM obats WHERE TanggalKadaluarsa IS NOT NULL AND TRIM(TanggalKadaluarsa) <> '';", conn);
            using var dateReader = dateCmd.ExecuteReader();
            var badIds = new List<int>();
            while (dateReader.Read())
            {
                var text = dateReader["TanggalKadaluarsa"]?.ToString()?.Trim() ?? "";
                if (!DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out _) &&
                    !DateTime.TryParse(text, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out _))
                    badIds.Add(Convert.ToInt32(dateReader["Id"]));
            }
            dateReader.Close();
            foreach (var id in badIds)
            {
                using var fix = new MySqlCommand("UPDATE obats SET TanggalKadaluarsa=NULL WHERE Id=@id;", conn);
                fix.Parameters.AddWithValue("@id", id);
                fix.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[MYSQL INIT] " + ex);
            throw new InvalidOperationException("Koneksi MySQL gagal. Pastikan MySQL/MariaDB aktif dan konfigurasi DatabaseConfig.cs benar.\n\n" + ex.Message, ex);
        }
    }

    private static void MigratePlaintextPasswords(MySqlConnection conn)
    {
        using var read = new MySqlCommand("SELECT id,password FROM users;", conn);
        using var reader = read.ExecuteReader();
        var updates = new List<(int Id, string Hash)>();
        while (reader.Read())
        {
            var id = Convert.ToInt32(reader["id"]);
            var password = reader["password"]?.ToString() ?? "";
            if (!PasswordService.IsHashed(password) && !PasswordService.IsEncrypted(password) && !string.IsNullOrEmpty(password))
                updates.Add((id, PasswordService.Encrypt(password)));
        }
        reader.Close();
        foreach (var item in updates)
        {
            using var update = new MySqlCommand("UPDATE users SET password=@password WHERE id=@id;", conn);
            update.Parameters.AddWithValue("@password", item.Hash);
            update.Parameters.AddWithValue("@id", item.Id);
            update.ExecuteNonQuery();
        }
    }

    private static void EnsureColumn(MySqlConnection c, string table, string column, string definition)
    {
        using var check = new MySqlCommand(@"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@table AND COLUMN_NAME=@column;", c);
        check.Parameters.AddWithValue("@table", table);
        check.Parameters.AddWithValue("@column", column);
        if (Convert.ToInt32(check.ExecuteScalar()) > 0) return;
        using var alter = new MySqlCommand($"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition};", c);
        alter.ExecuteNonQuery();
    }

    private static void Exec(MySqlConnection c, string sql)
    {
        using var cmd = new MySqlCommand(sql, c);
        cmd.ExecuteNonQuery();
    }
}
