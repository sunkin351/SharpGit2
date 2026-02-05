namespace SharpGit2.Managed.Config;

public class GitConfigEntry
{
    public string Name { get; }

    public string? Value { get; }

    public string BackendType { get; }

    public string OriginPath { get; }

    public uint IncludeDepth { get; }

    public GitConfigLevel Level { get; }


    protected GitConfigEntry(string name, string? value, string backendType, string originPath, uint includeDepth, GitConfigLevel level)
    {
        this.Name = name;
        this.Value = value;
        this.BackendType = backendType;
        this.OriginPath = originPath;
        this.IncludeDepth = includeDepth;
        this.Level = level;
    }

}
