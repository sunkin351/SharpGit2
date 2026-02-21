using System.Collections.Immutable;
using System.Text;

using SharpGit2.Managed.ObjectDB;

namespace SharpGit2.Managed;

public sealed class GitTree : GitObject
{
    private GitOdbObject OdbObject;
    private ImmutableArray<GitTreeEntry> Entries;

    internal override GitObjectType ObjectType => GitObjectType.Tree;

    internal override int ObjectSize => throw new NotImplementedException();

    internal GitTree(in GitObjectID oid, GitRepository repository) : base(in oid, repository)
    {
    }

    public ref readonly GitTreeEntry GetEntryByPath(string path)
    {
        throw new NotImplementedException();
    }

    internal void Parse(GitOdbObject obj, GitObjectIDType type)
    {
        // git_tree__parse()
        throw new NotImplementedException();
    }

    internal void Parse(ReadOnlySpan<char> data, GitObjectIDType type)
    {
        // git_tree__parse_raw()
        throw new NotImplementedException();
    }

    internal void WriteIndex(ref GitObjectID oid, GitIndex index, GitRepository repo)
    {
        // git_tree__write_index()
        throw new NotImplementedException();
    }

    public sealed class Builder
    {
        public GitRepository Repository { get; }
        private Dictionary<string, GitTreeEntry> Map;
        private StringBuilder WriteCache;

    }
}

public struct GitTreeEntry
{
    internal FileAttributes Attributes;
    internal GitObjectID Oid;
    internal string Filename;

    internal readonly bool IsTree
    {
        get => (Attributes & FileAttributes.Directory) != 0;
    }
}