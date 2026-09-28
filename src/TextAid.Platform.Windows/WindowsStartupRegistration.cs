using Microsoft.Win32;

namespace TextAid.Platform.Windows;

/// <summary>Represents the current user's single TextAid startup registration.</summary>
public interface IWindowsStartupRegistration
{
    /// <summary>Returns whether Windows will start TextAid when the current user signs in.</summary>
    bool IsEnabled();

    /// <summary>Creates or removes only TextAid's current-user startup registration.</summary>
    void SetEnabled(bool enabled);
}

/// <summary>Stores a single named value in the current user's Windows Run registry key.</summary>
public interface ICurrentUserRunRegistry
{
    /// <summary>Reads the named value from the current user's Run key.</summary>
    string? GetValue(string name);

    /// <summary>Creates or replaces the named value in the current user's Run key.</summary>
    void SetValue(string name, string command);

    /// <summary>Deletes the named value from the current user's Run key.</summary>
    void DeleteValue(string name);
}

/// <summary>Provides the per-user Windows startup option without requiring elevation.</summary>
public sealed class WindowsStartupRegistration : IWindowsStartupRegistration
{
    /// <summary>The only Run-key value owned by TextAid.</summary>
    public const string ValueName = "TextAid";
    private readonly ICurrentUserRunRegistry registry;
    private readonly string command;

    /// <summary>Creates a registration for the currently running TextAid executable.</summary>
    public WindowsStartupRegistration()
        : this(new CurrentUserRunRegistry(), Environment.ProcessPath ?? throw new InvalidOperationException("TextAid could not determine its executable path."))
    {
    }

    /// <summary>Creates a registration with an injectable registry boundary for deterministic tests.</summary>
    public WindowsStartupRegistration(ICurrentUserRunRegistry registry, string executablePath)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        if (string.IsNullOrWhiteSpace(executablePath)) throw new ArgumentException("An executable path is required.", nameof(executablePath));
        command = $"\"{executablePath}\"";
    }

    /// <inheritdoc />
    public bool IsEnabled() => string.Equals(registry.GetValue(ValueName), command, StringComparison.Ordinal);

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            registry.SetValue(ValueName, command);
            if (!IsEnabled()) throw new InvalidOperationException("Windows did not retain TextAid's startup registration.");
            return;
        }

        registry.DeleteValue(ValueName);
        if (registry.GetValue(ValueName) is not null) throw new InvalidOperationException("Windows did not remove TextAid's startup registration.");
    }
}

/// <summary>Reads and writes only the current user's Run key.</summary>
public sealed class CurrentUserRunRegistry : ICurrentUserRunRegistry
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <inheritdoc />
    public string? GetValue(string name)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(name) as string;
    }

    /// <inheritdoc />
    public void SetValue(string name, string command)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Windows could not open the current-user startup registration.");
        key.SetValue(name, command, RegistryValueKind.String);
    }

    /// <inheritdoc />
    public void DeleteValue(string name)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
