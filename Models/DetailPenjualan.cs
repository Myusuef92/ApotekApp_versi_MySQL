using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApotekApp.Models;

[Table("detail_penjualan")]
public class DetailPenjualan
{
    [Key, Column("id")] public int Id { get; set; }
    [Column("no_nota")] public string NoNota { get; set; } = string.Empty;
    [Column("id_obat")] public int ObatId { get; set; }
    [Column("nama_obat")] public string? NamaObat { get; set; }
    [Column("qty")] public int Jumlah { get; set; }
    [Column("harga")] public decimal HargaSatuan { get; set; }
    [Column("harga_beli")] public decimal HargaBeli { get; set; }
    [Column("subtotal")] public decimal Subtotal { get; set; }
    [NotMapped] public Penjualan? Penjualan { get; set; }
    [NotMapped] public Obat? Obat { get; set; }
}
