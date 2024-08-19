using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2;

public enum GitObjectIDType : byte
{
    SHA1 = 1,
#if GIT_EXPERIMENTAL_SHA256
    SHA256 = 2
#endif
}

[StructLayout(LayoutKind.Sequential)]
public struct GitObjectID
{
#if GIT_EXPERIMENTAL_SHA256
    public GitObjectIDType Type;
    public IdArray Id;

    [InlineArray(32)]
#else
    public IdArray Id;

    [InlineArray(20)]
#endif
    public struct IdArray
    {
        public byte Element;
    }
}
