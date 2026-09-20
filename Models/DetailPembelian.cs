using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApotekApp.Models;

public class DetailPembelian
{
    [Key]
    public int IdDetailPembelian { get; set; }

    public int PembelianId { get; set; }
    [ForeignKey("PembelianId")]
    public virtual Pembelian? Pembelian { get; set; }

    public int ObatId { get; set; }
    [ForeignKey("ObatId")]
    public virtual Obat? Obat { get; set; }

    public int Jumlah { get; set; }

    public decimal HargaBeli { get; set; }

    public decimal Subtotal => Jumlah * HargaBeli;
}