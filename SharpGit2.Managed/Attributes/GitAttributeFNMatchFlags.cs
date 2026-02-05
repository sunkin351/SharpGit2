namespace SharpGit2.Managed.Attributes;

[Flags]
internal enum GitAttributeFNMatchFlags
{
    Negate = 1,
    Directory = 1 << 1,
    FullPath = 1 << 2,
    Macro = 1 << 3,
    Ignore = 1 << 4,
    HasWild = 1 << 5,
    AllowSpace = 1 << 6,
    IgnoreCase = 1 << 7,
    MatchAll = 1 << 8,
    AllowNegation = 1 << 9,
    AllowMacro = 1 << 10,

    _IncomingMask = AllowSpace | AllowNegation | AllowMacro
}
