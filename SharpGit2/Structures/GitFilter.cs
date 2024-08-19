namespace SharpGit2.Native;

public unsafe struct GitFilter
{
    public uint Version;
    public byte* Attributes;
    public delegate* unmanaged[Cdecl]<GitFilter*, int> Initialize;
    public delegate* unmanaged[Cdecl]<GitFilter*, void> Shutdown;

    public GitFilter()
    {
        Version = 1;
    }
}
