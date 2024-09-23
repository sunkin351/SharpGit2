namespace SharpGit2.Tests;

public unsafe class RepositoryTests
{
    private readonly string? _repoPath;

    public RepositoryTests()
    {
        _repoPath = GitRepository.Discover(Environment.CurrentDirectory); // Discover the repo this test resides in
    }

    [Fact]
    public void OpenTest()
    {
        using var repo = GitRepository.Open(_repoPath);

        Assert.True(repo.NativeHandle != null);
    }
}