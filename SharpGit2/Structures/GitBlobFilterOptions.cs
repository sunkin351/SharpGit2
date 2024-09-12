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
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "This is a native padding field, reserved for future use")]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private void* Reserved;
        public GitObjectID AttributeCommitId;

        public GitBlobFilterOptions()
        {
            Version = 1;
            Flags = GitBlobFilterFlags.CheckForBinary;
        }
    }
}

