namespace SharpGit2.Managed;

public sealed class GitTag : GitObject
{

    internal override GitObjectType ObjectType => GitObjectType.Tag;
    
    internal override int ObjectSize => throw new NotImplementedException();
    
    internal GitTag(in GitObjectID oid, GitRepository repository) : base(in oid, repository)
    {
    }

    public ref readonly GitObjectID Target => throw new NotImplementedException();
}