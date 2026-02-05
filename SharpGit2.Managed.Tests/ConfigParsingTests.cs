using SharpGit2.Managed.Config.Backend;

namespace SharpGit2.Managed.Tests;

public class ConfigParsingTests
{
    [Fact]
    public void SimpleTest()
    {
        using var reader = new StringReader("""
            [core "hwnw"]
            eof = true # this is a test comment
            my-var = xkcd ; this is another comment

            [core]
            worktree = C:/XK\
            CD/Hello-World
            """);

        using var parser = new GitConfigParser(reader, "./internal.config");

        var data = new Dictionary<string, Dictionary<string, string?>>()
        {
            { "core.hwnw", new() { { "eof", "true" }, { "my-var", "xkcd" } } },
            { "core", new() { { "worktree", "C:/XKCD/Hello-World" } } }
        };

        var callbacks = new SimpleTest_Callbacks(data);

        parser.Parse(callbacks);

        callbacks.AssertAllEncountered();
    }

    private class SimpleTest_Callbacks : GitConfigParser.ICallbacks
    {
        private readonly Dictionary<string, Dictionary<string, string?>> _expected;
        private readonly HashSet<string> _encountered = [];
        private string? lastSection;

        public SimpleTest_Callbacks(Dictionary<string, Dictionary<string, string?>> expected)
        {
            _expected = expected;
        }

        public void Clear()
        {
            _encountered.Clear();
            lastSection = null;
        }

        public void AssertAllEncountered()
        {
            foreach (var key in _expected.SelectMany(x => (IEnumerable<string>)[x.Key, .. x.Value.Keys.Select(y => $"{x.Key}.{y}")]))
            {
                Assert.Contains(key, _encountered);
            }
        }

        public void OnComment(GitConfigParser parser, ReadOnlySpan<char> line)
        {
        }

        public void OnEndOfFile(GitConfigParser parser, string currentSection)
        {
        }

        public void OnSection(GitConfigParser parser, string currentSection, ReadOnlySpan<char> line)
        {
            Assert.Contains(currentSection, _expected);

            _encountered.Add(currentSection);
            lastSection = currentSection;
        }

        public void OnVariable(GitConfigParser parser, string currentSection, string variableName, string? variableValue, ReadOnlySpan<char> line)
        {
            Assert.Contains(currentSection, _encountered);
            Assert.Equal(lastSection, currentSection);
            Assert.True(_expected.TryGetValue(currentSection, out var x));

            Assert.Contains(variableName, x);
            Assert.Equal(x[variableName], variableValue);

            Assert.True(_encountered.Add($"{currentSection}.{variableName}"));
        }
    }

    [Theory]
    [InlineData(["\nhello \twor\b\"ld!\\", "\\nhello \\twor\\b\\\"ld!\\\\"])]
    [InlineData(["Hello World!", "Hello World!"])]
    public void CharacterEscapeTest(string value, string expected)
    {
        Assert.Equal(expected, GitConfigFileBackend.EscapeValue(value));
    }
}
