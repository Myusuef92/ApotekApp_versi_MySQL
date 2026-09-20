using ApotekApp.Models;

namespace ApotekApp.Models;

public static class UserSession
{
    public static int UserId { get; set; }
    public static string NamaUser { get; set; } = string.Empty;
    public static string Role { get; set; } = "Kasir";

    // Properti UserRole yang menyebabkan error CS0117 jika belum dideklarasikan
    public static Role? UserRole { get; set; }

    // Helper logika peran
    public static bool IsAdmin =>
        UserRole?.NamaRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ??
        Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    public static bool IsKasir =>
        UserRole?.NamaRole?.Equals("Kasir", StringComparison.OrdinalIgnoreCase) ??
        Role.Equals("Kasir", StringComparison.OrdinalIgnoreCase);
}