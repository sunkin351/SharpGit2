namespace SharpGit2.Managed.Attributes;

internal sealed class GitAttributeSession
{
    public int Key;
    public bool InitSetup, InitSysDir;
    public string? SysDir, Tmp;

    public GitAttributeSession(GitRepository repo)
    {
        Key = Interlocked.Increment(ref repo.AttributeSessionKey);
    }

    public void GetMany(GitRepository repo, in GitAttributeOptions options, string path, Span<string> names, Span<string> values_out)
    {
        ArgumentNullException.ThrowIfNull(repo);
        ArgumentNullException.ThrowIfNull(path);

        ArgumentOutOfRangeException.ThrowIfNotEqual(values_out.Length, names.Length);

        if (names.IsEmpty)
            return;


    }
}
