namespace SharpGit2.Managed;

[Flags]
public enum GitIndexCapabilities
{
    FromOwner = -1,

    IgnoreCase = 1,
    NoFilemode = 2,
    NoSymlinks = 4,
}

public sealed class GitIndex
{
    internal GitIndex(string path, GitObjectIDType type)
    {
        throw new NotImplementedException();
    }

    public ref readonly GitIndexEntry this[int index]
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    internal int FindPosition(string path, int stage)
    {
        // git_index__find_pos()
        throw new NotImplementedException();
    }

    internal void SetCapabilities(GitIndexCapabilities capabilities)
    {
        throw new NotImplementedException();
    }
}

public struct GitIndexEntry
{
    public GitObjectID Id;
}