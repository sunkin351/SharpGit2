using System.Collections.Immutable;

namespace SharpGit2.Managed;

public sealed class GitCommit : GitObject
{
    internal override GitObjectType ObjectType => GitObjectType.Commit;

    internal override int ObjectSize => throw new NotImplementedException();
    
    public ImmutableArray<GitObjectID> Parents { get; }
    
    public string Summary { get; }
    
    public GitSignature Committer { get; }
    

    internal GitCommit(in GitObjectID oid, GitRepository repository) : base(in oid, repository)
    {
    }

    public GitTree GetTree()
    {
        throw new NotImplementedException();
    }
}
