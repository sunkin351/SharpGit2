namespace SharpGit2.Native;

public unsafe struct GitObjectDatabaseWritePack
{
    public Git2.ObjectDatabaseBackend* Backend;
    public delegate* unmanaged[Cdecl]<GitObjectDatabaseWritePack*, void*, nuint, GitIndexerProgress*, int> Append;
    public delegate* unmanaged[Cdecl]<GitObjectDatabaseWritePack*, GitIndexerProgress*, int> Commit;
    public delegate* unmanaged[Cdecl]<GitObjectDatabaseWritePack*, void> Free;
}
