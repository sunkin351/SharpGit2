namespace SharpGit2;

[Flags]
public enum GitReferenceFormat
{
    Normal,
    AllowOneLevel = 1,
    RefSpecPattern = 2,
    RefSpecShorthand = 4
}
