namespace SharpGit2;

public unsafe readonly partial struct ObjectDatabaseHandle(nint handle)
{
    internal readonly nint NativeHandle = handle;


}
