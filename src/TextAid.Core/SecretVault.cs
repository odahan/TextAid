using System.Security.Cryptography;
using System.Text;

namespace TextAid.Core;

/// <summary>Provides encrypted per-user storage for connection secrets.</summary>
public interface ISecretVault
{
    /// <summary>Stores a secret under an opaque reference.</summary>
    void SetSecret(string reference, string secret);

    /// <summary>Attempts to retrieve a secret without exposing persistence details.</summary>
    bool TryGetSecret(string reference, out string secret);

    /// <summary>Removes a stored secret when it exists.</summary>
    void RemoveSecret(string reference);
}

/// <summary>Stores individual secrets under the user's application-data directory using CurrentUser DPAPI.</summary>
public sealed class DpapiSecretVault : ISecretVault
{
    private readonly string directory;

    /// <summary>Creates a vault in the default TextAid user-profile directory.</summary>
    public DpapiSecretVault() : this(Path.Combine(UserConfiguration.GetUserDataDirectory(), "secrets")) { }

    /// <summary>Creates a vault in a supplied directory, primarily for deterministic tests.</summary>
    public DpapiSecretVault(string directory)
    {
        this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }

    /// <inheritdoc />
    public void SetSecret(string reference, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows DPAPI is required for TextAid secrets.");
        Directory.CreateDirectory(directory);
        byte[] protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetPath(reference), protectedBytes);
    }

    /// <inheritdoc />
    public bool TryGetSecret(string reference, out string secret)
    {
        secret = string.Empty;
        if (string.IsNullOrWhiteSpace(reference)) return false;
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            string path = GetPath(reference);
            if (!File.Exists(path)) return false;
            byte[] clearBytes = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
            secret = Encoding.UTF8.GetString(clearBytes);
            return !string.IsNullOrWhiteSpace(secret);
        }
        catch (CryptographicException) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    /// <inheritdoc />
    public void RemoveSecret(string reference)
    {
        string path = GetPath(reference);
        if (File.Exists(path)) File.Delete(path);
    }

    private string GetPath(string reference)
    {
        string safeReference = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reference)));
        return Path.Combine(directory, $"{safeReference}.secret");
    }
}
