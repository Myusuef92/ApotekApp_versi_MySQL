using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApotekApp.Models;

public class Pembelian
{
    [Key]
    public int IdPembelian { get; set; }

    [Required]
    public string NoFaktur { get; set; } = string.Empty;

    public DateTime TanggalPembelian { get; set; } = DateTime.Now;

    public decimal TotalHarga { get; set; }

    public string? Catatan { get; set; }

    // Relasi ke Supplier
    public int SupplierId { get; set; }
    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    // Navigation Property
    public virtual List<DetailPembelian> DetailPembelians { get; set; } = new();
}