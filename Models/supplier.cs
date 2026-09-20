namespace ApotekApp.Models;

public class Supplier
{
    public int Id { get; set; }
    public string NamaSupplier { get; set; } = string.Empty;
    public string Telepon { get; set; } = string.Empty;
    public string Alamat { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}