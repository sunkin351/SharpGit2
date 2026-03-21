using JetBrains.Annotations;

namespace SharpGit2.Managed;

[PublicAPI]
public sealed class GitBlob : GitObject
{
    internal override GitObjectType ObjectType => GitObjectType.Blob;

    internal override int ObjectSize => throw new NotImplementedException();

    internal GitBlob(in GitObjectID oid, GitRepository repository) : base(in oid, repository)
    {
    }

    public long RawSize { get; }

    public bool TryGetSpan(out ReadOnlySpan<byte> span)
    {
        throw new NotImplementedException();
    }

    public Stream GetContentStream()
    {
        throw new NotImplementedException();
    }
}
