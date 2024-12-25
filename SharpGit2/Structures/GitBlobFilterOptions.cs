using System.Diagnostics.CodeAnalysis;

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

#pragma warning disable CS0169

namespace SharpGit2.Native
{
    public unsafe struct GitBlobFilterOptions
    {
        public uint Version;
        public GitBlobFilterFlags Flags;
        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "This is a native padding field, reserved for future use")]
        private void* Reserved;
        public GitObjectID AttributeCommitId;

        public GitBlobFilterOptions()
        {
            Version = 1;
            Flags = GitBlobFilterFlags.CheckForBinary;
        }

        public void FromManaged(in SharpGit2.GitBlobFilterOptions options)
        {
            Version = 1;
            Flags = options.Flags;
            Reserved = null;
            AttributeCommitId = options.AttributeCommitId;
        }

        public void Free()
        {
        }
    }
}

