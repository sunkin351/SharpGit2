using JetBrains.Annotations;

namespace SharpGit2.Managed;

[Flags, PublicAPI]
public enum GitIndexCapabilities
{
    FromOwner = -1,

    IgnoreCase = 1,
    NoFileMode = 2,
    NoSymlinks = 4,
}

[PublicAPI]
public sealed class GitIndex : IDisposable
{
    internal GitIndex(string path, GitObjectIDType type)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
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

[PublicAPI]
public sealed class GitIndexEntry
{
    public GitObjectID Id;
}