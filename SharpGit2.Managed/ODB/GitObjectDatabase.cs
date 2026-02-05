using System.Diagnostics;
using SharpGit2.Managed.Cache;
using SharpGit2.Managed.CommitGraph;

namespace SharpGit2.Managed.ODB;

internal enum GitObjectDatabaseCapabilities
{
    FromOwner = -1,
}

public sealed class GitObjectDatabase
{
    private readonly Lock Lock = new();

    internal readonly GitObjectIDType Options_OID_Type;

    private readonly List<object> _backends = [];

    private readonly GitCache _ownCache;
    private readonly GitCommitGraph _cgraph;
    private bool _doFsync;

    internal GitObjectDatabase(GitObjectIDType type)
    {
        Debug.Assert(type != default);

        Options_OID_Type = type;
    }

    public void AddDiskAlternate(string alt)
    {
        throw new NotImplementedException();
    }

    internal void SetCaps(GitObjectDatabaseCapabilities caps)
    {
        throw new NotImplementedException();
    }

    internal void AddDefaultBackends(string objectsDir, bool asAlternates, int alternateDepth)
    {
        throw new NotImplementedException();
    }


}
