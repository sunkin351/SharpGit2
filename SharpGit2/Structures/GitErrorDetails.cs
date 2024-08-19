namespace SharpGit2.Native;

public unsafe struct GitErrorDetails
{
    public byte* Message;
    public GitErrorClass Class;
}
