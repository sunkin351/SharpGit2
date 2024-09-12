using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGit2
{
    public unsafe struct GitDiffHunk
    {
        public int OldStart;
        public int OldLines;
        public int NewStart;
        public int NewLines;
        public string? Header;

        public GitDiffHunk(in Native.GitDiffHunk hunk)
        {
            OldStart = hunk.OldStart;
            OldLines = hunk.OldLines;
            NewStart = hunk.NewStart;
            NewLines = hunk.NewLines;

            ReadOnlySpan<byte> header = hunk.Header;
            Header = Encoding.UTF8.GetString(header.Slice(0, (int)hunk.HeaderLength));
        }
    }
}

namespace SharpGit2.Native
{
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

            return Encoding.UTF8.GetString(span.Slice(0, (int)HeaderLength));
        }

        [InlineArray(128)]
        public struct HeaderMemory
        {
            public byte Element;
        }
    }
}
