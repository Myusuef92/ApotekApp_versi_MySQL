namespace ApotekApp.Models;

public class Role
{
    public int Id { get; set; }

    public string NamaRole { get; set; } = string.Empty;

    // Relasi ke User
    public ICollection <User> Users { get; set; } = new List <User>();
}