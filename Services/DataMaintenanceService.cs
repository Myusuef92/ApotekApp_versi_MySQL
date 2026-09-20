using MySqlConnector;
using System.Globalization;

namespace ApotekApp.Services;

public static class DataMaintenanceService
{
    public static async Task BackupObatAsync()
    {
        await using var c = new MySqlConnection(MauiProgram.ConnectionString);
        await c.OpenAsync();
        var rows = new List<string[]>();
        const string sql = "SELECT KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa FROM obats ORDER BY NamaObat";
        await using var cmd = new MySqlCommand(sql, c);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            rows.Add(new[] { r["KodeObat"]?.ToString() ?? "", r["NamaObat"]?.ToString() ?? "", r["Barcode"]?.ToString() ?? "", r["Satuan"]?.ToString() ?? "", r["Kategori"]?.ToString() ?? "", r["LokasiRak"]?.ToString() ?? "", Convert.ToDecimal(r["HargaBeli"]).ToString("0.##", CultureInfo.InvariantCulture), Convert.ToDecimal(r["HargaJual"]).ToString("0.##", CultureInfo.InvariantCulture), r["Stok"]?.ToString() ?? "0", r["StokMin"]?.ToString() ?? "0", r["TanggalKadaluarsa"]?.ToString() ?? "" });
        await ExcelService.SaveAsync($"Backup_Obat_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx", new[] { "KodeObat","NamaObat","Barcode","Satuan","Kategori","LokasiRak","HargaBeli","HargaJual","Stok","StokMin","TanggalKadaluarsa" }, rows);
    }

    public static async Task<int> ResetObatAsync()
    {
        await using var c = new MySqlConnection(MauiProgram.ConnectionString);
        await c.OpenAsync();
        await using var tr = c.BeginTransaction();
        int count;
        await using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM obats", c, tr)) count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        if (count == 0) { await tr.CommitAsync(); return 0; }
        // Simpan HPP historis sebelum master obat dihapus.
        await using (var hpp = new MySqlCommand("UPDATE detail_penjualan SET harga_beli=(SELECT COALESCE(HargaBeli,0) FROM obats WHERE obats.Id=detail_penjualan.id_obat) WHERE COALESCE(harga_beli,0)=0;", c, tr))
            await hpp.ExecuteNonQueryAsync();

        const string archive = """
INSERT INTO arsip_obat(obat_id_lama,kode_obat,nama_obat,barcode,satuan,kategori,lokasi_rak,harga_beli,harga_jual,stok,stok_min,tanggal_kadaluarsa,diarsipkan_pada)
SELECT Id,KodeObat,NamaObat,Barcode,Satuan,Kategori,LokasiRak,HargaBeli,HargaJual,Stok,StokMin,TanggalKadaluarsa,NOW() FROM obats;
""";
        await using (var a = new MySqlCommand(archive, c, tr)) await a.ExecuteNonQueryAsync();
        await using (var d = new MySqlCommand("DELETE FROM obats;", c, tr)) await d.ExecuteNonQueryAsync();
        await tr.CommitAsync();
        try { await using var vacuum = new MySqlCommand("OPTIMIZE TABLE obats;", c); await vacuum.ExecuteNonQueryAsync(); } catch (MySqlException) { }
        return count;
    }
}
