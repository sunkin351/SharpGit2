namespace SharpGit2
{
    public unsafe struct GitDiffDelta
    {
        public GitDeltaType Status;
        public GitDiffFlags Flags;
        public ushort Similarity;
        public ushort FileCount;
        public GitDiffFile OldFile;
        public GitDiffFile NewFile;

        public GitDiffDelta(in Native.GitDiffDelta native)
        {
            Status = native.Status;
            Flags = native.Flags;
            Similarity = native.Similarity;
            FileCount = native.FileCount;
            OldFile = new(in native.OldFile);
            NewFile = new(in native.NewFile);
        }
    }
}

namespace SharpGit2.Native
{
    public struct GitDiffDelta
    {
        public GitDeltaType Status;
        public GitDiffFlags Flags;
        public ushort Similarity;
        public ushort FileCount;
        public GitDiffFile OldFile;
        public GitDiffFile NewFile;
    }
}
