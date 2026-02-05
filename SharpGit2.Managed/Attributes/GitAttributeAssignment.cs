namespace SharpGit2.Managed.Attributes;

internal class GitAttributeAssignment : IComparable<GitAttributeAssignment>
{
    public string Name;
    public uint NameHash;
    public string Value;

    public int CompareTo(GitAttributeAssignment? other)
    {
        var hash = this.NameHash;
        var otherHash = other!.NameHash;

        if (hash < otherHash)
            return -1;

        if (hash > otherHash)
            return 1;

        return string.Compare(this.Name, other.Name);
    }
}
