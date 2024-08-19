namespace SharpGit2
{
    public unsafe struct GitStatusEntry
    {
        public GitStatusFlags Flags;
        public GitDiffDelta HeadToIndex;
        public GitDiffDelta IndexToWorkingDirectory;

        public GitStatusEntry(in Native.GitStatusEntry entry)
        {
            Flags = entry.Flags;
            HeadToIndex = new(in *entry.HeadToIndex);
            IndexToWorkingDirectory = new(in *entry.IndexToWorkingDirectory);
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitStatusEntry
    {
        public GitStatusFlags Flags;
        public GitDiffDelta* HeadToIndex;
        public GitDiffDelta* IndexToWorkingDirectory;
    }
}
