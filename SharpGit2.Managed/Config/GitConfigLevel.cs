namespace SharpGit2.Managed.Config;

public enum GitConfigLevel
{
    HighestLevel = -1,
    ProgramData = 1,
    System = 2,
    XDG = 3,
    Global = 4,
    Local = 5,
    Worktree = 6,
    App = 7
}
