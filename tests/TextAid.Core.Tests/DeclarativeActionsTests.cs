using System.Text.Json;
using TextAid.Core;

namespace TextAid.Core.Tests;

public sealed class DeclarativeActionsTests
{
    [Fact]
    public void Render_ReplacesTextOnceAndLeavesInjectedBracesLiteral()
    {
        string result = TemplateRenderer.Render("Start <TEXT>{{text}}</TEXT> End", "Bonjour {{unknown}}");

        Assert.Equal("Start <TEXT>Bonjour {{unknown}}</TEXT> End", result);
    }

    [Theory]
    [InlineData("{{language}}")]
    [InlineData("{{text")]
    [InlineData("text }}")]
    public void ValidateTemplate_RejectsUnsupportedOrMalformedPlaceholders(string template)
    {
        Assert.Throws<InvalidOperationException>(() => TemplateRenderer.ValidateTemplate(template));
    }

    [Fact]
    public void Load_CreatesAndValidatesAllBuiltInActions()
    {
        using var directory = new TemporaryDirectory();
        IReadOnlyList<ActionDefinition> actions = new ActionLoader(actionsDirectory: directory.Path).Load();

        Assert.Equal(9, actions.Count);
        Assert.Contains(actions, action => action.Id == "translate" && action.IsReserved);
        Assert.Contains(actions, action => action.Id == "answer-this-mail" && action.AskForUserInstructions);
        Assert.All(actions, action => Assert.True(File.Exists(System.IO.Path.Combine(directory.Path, $"{action.Id}.json"))));
    }

    [Fact]
    public void Load_ExposesValidUserActionWithoutRecompilation()
    {
        using var directory = new TemporaryDirectory();
        var loader = new ActionLoader(actionsDirectory: directory.Path);
        _ = loader.Load();
        var action = new ActionDefinition("polish", "Polish", true, "Polish the input text.", "local-default", null, "Unchanged", false);
        File.WriteAllText(System.IO.Path.Combine(directory.Path, "polish.json"), JsonSerializer.Serialize(action));

        IReadOnlyList<ActionDefinition> actions = loader.Load();

        Assert.Contains(actions, candidate => candidate.Id == "polish" && candidate.DisplayName == "Polish");
    }

    [Fact]
    public void BuiltInDisplayName_UsesTheUiResolverButNeverOverridesAUserName()
    {
        Func<string, string?>? previous = BuiltInActionCatalog.DisplayNameResolver;
        try
        {
            BuiltInActionCatalog.DisplayNameResolver = id => id == "translate" ? "Traduire" : null;

            var builtIn = new ActionDefinition("translate", null, true, "Translate {{text}}", "local-default", null, "Unchanged", false, true);
            var customized = builtIn with { DisplayNameOverride = "My Translate" };

            Assert.Equal("Traduire", builtIn.DisplayName);
            Assert.Equal("My Translate", customized.DisplayName);
        }
        finally
        {
            BuiltInActionCatalog.DisplayNameResolver = previous;
        }
    }

    [Fact]
    public void Load_RejectsAChangedReservedTranslateAction()
    {
        using var directory = new TemporaryDirectory();
        var loader = new ActionLoader(actionsDirectory: directory.Path);
        _ = loader.Load();
        string path = System.IO.Path.Combine(directory.Path, "translate.json");
        ActionDefinition translate = JsonSerializer.Deserialize<ActionDefinition>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })! with { PromptTemplate = "Do something else." };
        File.WriteAllText(path, JsonSerializer.Serialize(translate));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => loader.Load());

        Assert.Equal("The reserved Translate action cannot be modified.", exception.Message);
    }

    [Fact]
    public void Delete_RemovesAnOrdinaryActionAndRetainsAValidSet()
    {
        using var directory = new TemporaryDirectory();
        var loader = new ActionLoader(actionsDirectory: directory.Path);
        _ = loader.Load();
        loader.Save(new ActionDefinition("polish", "Polish", true, "Process {{text}}", "local-default", null, "Unchanged", false));

        loader.Delete("polish");

        Assert.DoesNotContain(loader.Load(), action => action.Id == "polish");
        Assert.False(File.Exists(Path.Combine(directory.Path, "polish.json")));
    }

    [Fact]
    public void Delete_RejectsTheReservedTranslateAction()
    {
        using var directory = new TemporaryDirectory();
        var loader = new ActionLoader(actionsDirectory: directory.Path);
        _ = loader.Load();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => loader.Delete("translate"));

        Assert.Equal("The reserved Translate action cannot be deleted.", exception.Message);
        Assert.Contains(loader.Load(), action => action.Id == "translate");
    }

    [Fact]
    public void Validate_RejectsAnInvalidOutputLanguage()
    {
        ActionDefinition action = new("custom", null, true, "Process {{text}}", "local-default", null, "not-a-language", false);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => ActionValidator.Validate(action, new HashSet<string>(["local-default"])));

        Assert.Equal("Action 'custom' has an invalid output-language default.", exception.Message);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TextAid.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
