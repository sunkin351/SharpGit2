namespace SharpGit2.Managed.Internal;

[Flags]
internal enum GitIteratorFlags
{
    IgnoreCase = 1,
    DontIgnoreCase = 1 << 1,
    IncludeTrees = 1 << 2,
    DontAutoExpand = 1 << 3,
    PrecomposeUnicode = 1 << 4,
    DontPrecomposeUnicode = 1 << 5,
    IncludeConflicts = 1 << 6,
    DescendSymlinks = 1 << 7,
    IncludeHash = 1 << 8,
}