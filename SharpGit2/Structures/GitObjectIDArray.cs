namespace SharpGit2.Native;

public unsafe struct GitObjectIDArray
{
    public GitObjectID* Ids;
    public nuint Count;
}
