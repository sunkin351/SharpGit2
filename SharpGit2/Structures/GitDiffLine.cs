using System.Runtime.InteropServices;
using System.Text;

namespace SharpGit2
{
    public unsafe struct GitDiffLine
    {
        public GitDiffLineType Origin;
        public int OldLineNumber;
        public int NewLineNumber;
        public int LineCount;
        public string Content;
        public long ContentOffset;

        public GitDiffLine(in Native.GitDiffLine line)
        {
            Origin = line.Origin;
            OldLineNumber = line.OldLineNumber;
            NewLineNumber = line.NewLineNumber;
            LineCount = line.LineCount;
            Content = Encoding.UTF8.GetString(line.ContentSpan);
            ContentOffset = line.ContentOffset;
        }
    }
}

namespace SharpGit2.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GitDiffLine
    {
        public GitDiffLineType Origin;
        public int OldLineNumber;
        public int NewLineNumber;
        public int LineCount;
        public nuint ContentLength;
        public long ContentOffset;
        public byte* Content;

        public readonly ReadOnlySpan<byte> ContentSpan => new(Content, checked((int)ContentLength));
    }
}
