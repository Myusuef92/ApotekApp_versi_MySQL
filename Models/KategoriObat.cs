namespace ApotekApp.Models;

public class KategoriObat
{
    public int Id { get; set; }
        public string NamaKategori { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public bool Aktif { get; set; } = true;
        // Relasi ke Obat
    public ICollection<Obat> Obat { get; set; } = new List<Obat>();
}