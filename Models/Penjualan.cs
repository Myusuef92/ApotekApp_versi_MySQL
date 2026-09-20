using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace ApotekApp.Models;

[Table("penjualan")]
public class Penjualan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, StringLength(50)]
    [Column("no_nota")]
    public string NoNota { get; set; } = string.Empty;

    [Column("tanggal")]
    public DateTime Tanggal { get; set; } = DateTime.Now;

    [Column("total")]
    public decimal TotalHarga { get; set; }

    [Column("bayar")]
    public decimal Bayar { get; set; }

    [Column("kembali")]
    public decimal Kembali { get; set; }

    [Column("diskon")]
    public decimal Diskon { get; set; }

    [Column("grand_total")]
    public decimal GrandTotal { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [NotMapped] public string TanggalFormatted => Tanggal.ToString("dd MMM yyyy HH:mm", new CultureInfo("id-ID"));
    [NotMapped] public string TotalHargaFormatted => TotalHarga.ToString("C0", new CultureInfo("id-ID"));
    [NotMapped] public string BayarFormatted => Bayar.ToString("C0", new CultureInfo("id-ID"));
    [NotMapped] public string KembaliFormatted => Kembali.ToString("C0", new CultureInfo("id-ID"));
}
