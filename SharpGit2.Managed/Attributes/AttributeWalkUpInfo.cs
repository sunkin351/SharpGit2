namespace SharpGit2.Managed.Attributes;

internal struct AttributeWalkUpInfo
{
    public GitRepository Repo;
    public GitAttributeSession? Session;
    public GitAttributeOptions options;
    public string WorkingDirectory;
    public GitIndex? Index;
    public ValueList<GitAttributeFile> Files;
}
