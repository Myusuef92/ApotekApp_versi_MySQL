using System.ComponentModel.DataAnnotations.Schema;

namespace ApotekApp;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Alias untuk PasswordHash
    public string PasswordHash
    {
        get => Password;
        set => Password = value;
    }

    public string NamaLengkap { get; set; } = string.Empty;
    public int RoleId { get; set; }

    // Properti utama nama role
    [NotMapped]
    public string NamaRole { get; set; } = string.Empty;

    // Alias RoleName untuk LoginPage
    [NotMapped]
    public string RoleName
    {
        get => NamaRole;
        set => NamaRole = value;
    }

    // Alias Role untuk UserPage (Memperbaiki Error CS1061)
    [NotMapped]
    public string Role
    {
        get => NamaRole;
        set => NamaRole = value;
    }
}