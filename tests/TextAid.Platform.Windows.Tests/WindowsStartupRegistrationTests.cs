using TextAid.Platform.Windows;

namespace TextAid.Platform.Windows.Tests;

public sealed class WindowsStartupRegistrationTests
{
    [Fact]
    public void EnablingCreatesOnlyTextAidCurrentUserRegistration()
    {
        var registry = new FakeCurrentUserRunRegistry();
        var startup = new WindowsStartupRegistration(registry, @"C:\Program Files\TextAid\TextAid.exe");

        startup.SetEnabled(true);

        Assert.True(startup.IsEnabled());
        Assert.Equal("\"C:\\Program Files\\TextAid\\TextAid.exe\"", registry.Values[WindowsStartupRegistration.ValueName]);
        Assert.Equal(WindowsStartupRegistration.ValueName, Assert.Single(registry.Values).Key);
    }

    [Fact]
    public void DisablingRemovesOnlyTextAidRegistration()
    {
        var registry = new FakeCurrentUserRunRegistry();
        registry.Values[WindowsStartupRegistration.ValueName] = "old TextAid command";
        registry.Values["Another application"] = "other.exe";
        var startup = new WindowsStartupRegistration(registry, @"C:\TextAid\TextAid.exe");

        startup.SetEnabled(false);

        Assert.False(startup.IsEnabled());
        Assert.DoesNotContain(WindowsStartupRegistration.ValueName, registry.Values.Keys);
        Assert.Equal("other.exe", registry.Values["Another application"]);
    }

    [Fact]
    public void RegistrationFailureIsPropagatedForRecoverableUiFeedback()
    {
        var startup = new WindowsStartupRegistration(new FailingCurrentUserRunRegistry(), @"C:\TextAid\TextAid.exe");

        Assert.Throws<UnauthorizedAccessException>(() => startup.SetEnabled(true));
    }

    private sealed class FakeCurrentUserRunRegistry : ICurrentUserRunRegistry
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);
        public string? GetValue(string name) => Values.GetValueOrDefault(name);
        public void SetValue(string name, string command) => Values[name] = command;
        public void DeleteValue(string name) => Values.Remove(name);
    }

    private sealed class FailingCurrentUserRunRegistry : ICurrentUserRunRegistry
    {
        public string? GetValue(string name) => null;
        public void SetValue(string name, string command) => throw new UnauthorizedAccessException();
        public void DeleteValue(string name) => throw new UnauthorizedAccessException();
    }
}
