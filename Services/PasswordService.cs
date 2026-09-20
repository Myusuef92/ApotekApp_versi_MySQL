using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Storage;

namespace ApotekApp.Services;

/// <summary>
/// Password helper for the local desktop application.
/// New passwords are stored reversibly (encrypted) because the User module
/// intentionally supports the administrator's eye/reveal feature. Existing
/// PBKDF2 hashes remain supported for backward compatibility.
/// </summary>
public static class PasswordService
{
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const string HashPrefix = "$PBKDF2-SHA256$";
    private const string EncryptedPrefix = "$AESGCM$";
    private const string KeyPreference = "ApotekApp.PasswordEncryptionKey.v1";
    private static readonly byte[] AppKeySeed = SHA256.HashData(Encoding.UTF8.GetBytes("ApotekApp-Client-Release-4.2.1-Password-Viewer"));

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password tidak boleh kosong.", nameof(password));
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{HashPrefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    /// <summary>Stores a new password in an authenticated encrypted format so it can be revealed by an authorized local admin.</summary>
    public static string Encrypt(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password tidak boleh kosong.", nameof(password));

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(password);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(GetEncryptionKey(), 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return $"{EncryptedPrefix}{Convert.ToBase64String(nonce)}${Convert.ToBase64String(tag)}${Convert.ToBase64String(cipher)}";
    }

    public static bool TryDecryptPassword(string stored, out string password)
    {
        password = string.Empty;
        if (!IsEncrypted(stored)) return false;
        var parts = stored.Split('$', StringSplitOptions.None);
        if (parts.Length != 5) return false;
        try
        {
            var nonce = Convert.FromBase64String(parts[2]);
            var tag = Convert.FromBase64String(parts[3]);
            var cipher = Convert.FromBase64String(parts[4]);
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(GetEncryptionKey(), 16);
            aes.Decrypt(nonce, cipher, tag, plain);
            password = Encoding.UTF8.GetString(plain);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored)) return false;

        if (IsEncrypted(stored))
            return TryDecryptPassword(stored, out var plain) &&
                   CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(plain));

        if (!stored.StartsWith(HashPrefix, StringComparison.Ordinal))
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(stored));

        var parts = stored.Split('$', StringSplitOptions.None);
        if (parts.Length != 5 || !int.TryParse(parts[2], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }

    public static bool IsHashed(string value) => !string.IsNullOrEmpty(value) && value.StartsWith(HashPrefix, StringComparison.Ordinal);
    public static bool IsEncrypted(string value) => !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

    private static byte[] GetEncryptionKey()
    {
        // Keep the encryption key local to this installation. This is deliberately
        // separate from the database so the password value is not stored as plain text.
        var existing = Preferences.Get(KeyPreference, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            try
            {
                var key = Convert.FromBase64String(existing);
                if (key.Length == 32) return key;
            }
            catch { }
        }

        var generated = RandomNumberGenerator.GetBytes(32);
        // Mix in an app-specific seed so a corrupted/empty preference never produces a predictable key.
        var mixed = HMACSHA256.HashData(AppKeySeed, generated);
        Preferences.Set(KeyPreference, Convert.ToBase64String(mixed));
        return mixed;
    }
}
