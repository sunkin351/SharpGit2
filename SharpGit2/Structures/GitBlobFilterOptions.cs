namespace SharpGit2
{
    public unsafe struct GitBlobFilterOptions
    {
        public GitBlobFilterFlags Flags;
        public GitObjectID AttributeCommitId;

        public GitBlobFilterOptions()
        {
            Flags = GitBlobFilterFlags.CheckForBinary;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitBlobFilterOptions
    {
        public uint Version;
        public GitBlobFilterFlags Flags;
        private void* Reserved;
        public GitObjectID AttributeCommitId;

        public GitBlobFilterOptions()
        {
            Version = 1;
            Flags = GitBlobFilterFlags.CheckForBinary;
        }
    }
}

