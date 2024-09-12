namespace SharpGit2.Native;

public unsafe struct GitConfigMap
{
    public GitConfigMapType Type;
    public byte* StringMatch;
    public int MapValue;
}