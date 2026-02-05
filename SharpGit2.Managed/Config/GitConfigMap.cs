namespace SharpGit2.Managed.Config;

public enum GitConfigMapType
{
    False,
    True,
    Int32,
    String
}

public struct GitConfigMap
{
    public GitConfigMapType Type;
    public string StringMatch;
    public int MapValue;
}
