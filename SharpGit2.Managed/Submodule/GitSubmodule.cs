namespace SharpGit2.Managed.Submodule;

public enum GitSubmoduleUpdateType
{
    Default = 0,
    Checkout,
    Rebase,
    Merge,
    None
}

public enum GitSubmoduleIgnoreType
{
    Unspecified = -1,
    None = 1,
    Untracked,
    Dirty,
    All
}

public enum GitSubmoduleRecurseType
{
    No = 0,
    Yes,
    OnDemand
}

public sealed class GitSubmodule
{
    private string Name;
    private string Path;
    private string Url;
    private string Branch;

    private GitSubmoduleUpdateType Update;
    private GitSubmoduleUpdateType UpdateDefault;
    private GitSubmoduleIgnoreType Ignore;
    private GitSubmoduleIgnoreType IgnoreDefault;
    private GitSubmoduleRecurseType Recurse;
    private GitSubmoduleRecurseType RecurseDefault;

    private GitRepository Repository;
    private uint Flags;
    private GitObjectID HeadOID;
    private GitObjectID IndexOID;
    private GitObjectID WorkDirectoryOID;

    internal GitSubmodule(GitRepository repo)
    {
        throw new NotImplementedException();
    }

    public GitRepository Open()
    {
        throw new NotImplementedException();
    }
}
