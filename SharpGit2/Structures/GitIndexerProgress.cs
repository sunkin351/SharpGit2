using System.Runtime.InteropServices;

namespace SharpGit2;

[StructLayout(LayoutKind.Sequential)]
public struct GitIndexerProgress
{
    public uint TotalObjects;
    public uint IndexedObjects;
    public uint ReceivedObjects;
    public uint LocalObjects;
    public uint TotalDeltas;
    public uint IndexedDeltas;
    public nuint ReceivedBytes;
}
