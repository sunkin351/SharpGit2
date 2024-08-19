namespace SharpGit2.Native;

public unsafe struct GitObjectDatabaseStream
{
    public Git2.ObjectDatabaseBackend* Backend;
    public uint Mode;
    public void* HashContext;

#if GIT_EXPERIMENTAL_SHA256
    public GitObjectIDType ObjectIdType;
#endif

    public ulong DeclaredSize;
    public ulong ReceivedBytes;

    public delegate* unmanaged[Cdecl]<GitObjectDatabaseStream*, byte*, nuint, int> Read;
    public delegate* unmanaged[Cdecl]<GitObjectDatabaseStream*, byte*, nuint, int> Write;
    public delegate* unmanaged[Cdecl]<GitObjectDatabaseStream*, GitObjectID*, int> FinalizeWrite;
    public delegate* unmanaged[Cdecl]<GitObjectDatabaseStream*, void> Free;
}
