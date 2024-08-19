using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGit2;

public unsafe struct GitDiffHunk
{
    public int OldStart;
    public int OldLines;
    public int NewStart;
    public int NewLines;
    public nuint HeaderLength;
    public HeaderMemory Header;

    public readonly string GetHeader()
    {
        ReadOnlySpan<byte> span = Header;

        int idx = span.IndexOf((byte)0);

        return Encoding.UTF8.GetString(idx >= 0 ? span[..idx] : span);
    }

    [InlineArray(128)]
    public struct HeaderMemory
    {
        public byte Element;
    }
}
