namespace SharpGit2.Managed.Tests;

public class GitReferenceNameTests
{
    [Theory]
    [InlineData(null, GitReferenceFormat.Normal)]
    [InlineData("", GitReferenceFormat.Normal)]
    [InlineData("master", GitReferenceFormat.Normal)]
    [InlineData("^", GitReferenceFormat.AllowOneLevel)]
    public void InvalidNamesShouldThrow(string? referenceName, GitReferenceFormat formatFlags)
    {
        Assert.Throws<ArgumentException>(() => GitReference.ThrowIfInvalidReferenceName(referenceName, formatFlags));
    }
}