using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApotekApp;

public class StrukItem
{
    public string NamaBarang { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal Harga { get; set; }
    public decimal Subtotal => Qty * Harga;
}

public class StrukService
{
    private readonly string _connString = MauiProgram.ConnectionString;

    public class PengaturanApotek
    {
        public string NamaApotek { get; set; } = "APOTEK SEHAT";
        public string NamaPemilik { get; set; } = "";
        public string Alamat { get; set; } = "";
        public string NoTelepon { get; set; } = "";
        public string SiaSipa { get; set; } = "";
        public string CatatanStruk { get; set; } = "";
        public string UkuranKertas { get; set; } = "58mm";
        public decimal PajakPpn { get; set; } = 0;
    }

    public async Task<PengaturanApotek> GetPengaturanAsync()
    {
        var config = new PengaturanApotek();
        try
        {
            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();

            string query = "SELECT * FROM pengaturan WHERE id = 1 LIMIT 1";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                config.NamaApotek = reader["nama_apotek"]?.ToString() ?? "APOTEK SEHAT";
                config.NamaPemilik = reader["nama_pemilik"]?.ToString() ?? "";
                config.Alamat = reader["alamat"]?.ToString() ?? "";
                config.NoTelepon = reader["no_telepon"]?.ToString() ?? "";
                config.SiaSipa = reader["sia_sipa"]?.ToString() ?? "";
                config.CatatanStruk = reader["catatan_struk"]?.ToString() ?? "";
                config.UkuranKertas = reader["ukuran_kertas"]?.ToString() ?? "58mm";
                config.PajakPpn = Convert.ToDecimal(reader["pajak_ppn"] != DBNull.Value ? reader["pajak_ppn"] : 0);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Gagal memuat setting struk: {ex.Message}");
        }
        return config;
    }

    public async Task<string> GenerateTextStrukAsync(
        string noNota,
        string namaKasir,
        List<StrukItem> items,
        decimal totalBayar,
        decimal tunai,
        decimal kembali)
    {
        var cfg = await GetPengaturanAsync();
        int width = cfg.UkuranKertas == "80mm" ? 48 : 32;
        string line = new string('-', width);

        var sb = new StringBuilder();

        // 1. HEADER PROFIL APOTEK
        sb.AppendLine(CenterText(cfg.NamaApotek.ToUpper(), width));
        if (!string.IsNullOrEmpty(cfg.Alamat))
            sb.AppendLine(CenterText(cfg.Alamat, width));
        if (!string.IsNullOrEmpty(cfg.NoTelepon))
            sb.AppendLine(CenterText($"Telp: {cfg.NoTelepon}", width));
        if (!string.IsNullOrEmpty(cfg.SiaSipa))
            sb.AppendLine(CenterText($"SIPA: {cfg.SiaSipa}", width));

        sb.AppendLine(line);

        // 2. INFORMASI TRANSAKSI
        sb.AppendLine($"No   : {noNota}");
        sb.AppendLine($"Tgl  : {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Kasir: {namaKasir}");
        sb.AppendLine(line);

        // 3. ITEM BARANG
        foreach (var item in items)
        {
            sb.AppendLine(FormatRow(item.NamaBarang, "", width));
            string rincianQty = $"  {item.Qty} x {item.Harga:N0}";
            sb.AppendLine(FormatRow(rincianQty, $"{item.Subtotal:N0}", width));
        }

        sb.AppendLine(line);

        // 4. RINGKASAN TOTAL & PEMBAYARAN
        sb.AppendLine(FormatRow("Total Belanja", $"Rp {totalBayar:N0}", width));
        if (cfg.PajakPpn > 0)
        {
            decimal nominalPpn = totalBayar * (cfg.PajakPpn / 100);
            sb.AppendLine(FormatRow($"PPN ({cfg.PajakPpn:G29}%)", $"Rp {nominalPpn:N0}", width));
        }
        sb.AppendLine(FormatRow("Tunai", $"Rp {tunai:N0}", width));
        sb.AppendLine(FormatRow("Kembali", $"Rp {kembali:N0}", width));

        sb.AppendLine(line);

        // 5. FOOTER / CATATAN
        if (!string.IsNullOrEmpty(cfg.CatatanStruk))
        {
            sb.AppendLine(CenterText(cfg.CatatanStruk, width));
        }
        else
        {
            sb.AppendLine(CenterText("Terima Kasih Atas Kunjungan Anda", width));
            sb.AppendLine(CenterText("Semoga Lekas Sembuh", width));
        }

        return sb.ToString();
    }

    private string CenterText(string text, int width)
    {
        if (text.Length >= width) return text.Substring(0, width);
        int leftPadding = (width - text.Length) / 2;
        return text.PadLeft(leftPadding + text.Length).PadRight(width);
    }

    private string FormatRow(string left, string right, int width)
    {
        int spaces = width - left.Length - right.Length;
        if (spaces < 1) spaces = 1;
        return left + new string(' ', spaces) + right;
    }
}