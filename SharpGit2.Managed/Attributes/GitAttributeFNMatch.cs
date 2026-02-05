namespace SharpGit2.Managed.Attributes;

internal struct GitAttributeFNMatch
{
    public string Pattern;
    public string? ContainingDir;
    public GitAttributeFNMatchFlags Flags;

    public bool IsMatch(in GitAttributePath path)
    {
        ReadOnlySpan<char> relpath = path.RelativePath;

        ReadOnlySpan<char> filename;
        WildMatch.Flags flags = 0;

        if (ContainingDir is not null)
        {
            if (!relpath.StartsWith(ContainingDir, (Flags & GitAttributeFNMatchFlags.IgnoreCase) != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return false;

            relpath = relpath.Slice(ContainingDir.Length);
        }

        if ((this.Flags & GitAttributeFNMatchFlags.IgnoreCase) != 0)
            flags |= WildMatch.Flags.CaseFold;

        if ((this.Flags & GitAttributeFNMatchFlags.FullPath) != 0)
        {
            filename = relpath;
            flags |= WildMatch.Flags.PathName;
        }
        else
        {
            filename = path.Basename;
        }

        if ((this.Flags & GitAttributeFNMatchFlags.Directory) != 0 && !path.IsDirectory)
        {
            if ((this.Flags & GitAttributeFNMatchFlags.Ignore) == 0 || path.Basename == relpath)
                return false;

            if (relpath.Equals(this.Pattern, (this.Flags & GitAttributeFNMatchFlags.IgnoreCase) != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return false;

            return WildMatch.Match(this.Pattern, relpath, flags) == WildMatch.Result.Match;
        }
        else
        {
            return WildMatch.Match(this.Pattern, filename, flags) == WildMatch.Result.Match;
        }
    }
}
