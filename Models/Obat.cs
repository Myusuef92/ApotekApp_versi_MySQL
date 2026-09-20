using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApotekApp.Models;

[Table("obats")]
public class Obat
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("KodeObat")]
    public string KodeObat { get; set; } = string.Empty;

    [Column("NamaObat")]
    public string NamaObat { get; set; } = string.Empty;

    [Column("Barcode")]
    public string? Barcode { get; set; }

    [Column("Satuan")]
    public string? Satuan { get; set; }

    [Column("Kategori")]
    public string? Kategori { get; set; }

    [Column("LokasiRak")]
    public string? LokasiRak { get; set; }

    [Column("HargaBeli")]
    public decimal HargaBeli { get; set; }

    [Column("HargaJual")]
    public decimal HargaJual { get; set; }

    [Column("Stok")]
    public int Stok { get; set; }

    [Column("StokMin")]
    public int StokMin { get; set; }

    [Column("TanggalKadaluarsa")]
    public DateTime TanggalKadaluarsa { get; set; }

    // Field legacy; tidak disimpan langsung karena database versi 3.5.x tidak memiliki kolom ini.
    [NotMapped]
    public DateTime? TanggalExpired { get; set; }

    // Field legacy; relasi kategori belum digunakan oleh database aktif.
    [NotMapped]
    public int? KategoriObatId { get; set; }

    [Column("Gambar")]
    public string? Gambar { get; set; }

    [Column("KodeKFA")] public string? KodeKFA { get; set; }
    [Column("Golongan")] public string? Golongan { get; set; }
    [Column("BentukSediaan")] public string? BentukSediaan { get; set; }
    [Column("Kekuatan")] public string? Kekuatan { get; set; }
    [Column("Dosis")] public string? Dosis { get; set; }
    [Column("IsAktif")] public bool IsAktif { get; set; } = true;
}