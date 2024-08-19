using System.Runtime.InteropServices;

namespace SharpGit2
{
    public struct GitFilterOptions
    {
        public GitFilterFlags Flags;
        public GitObjectID AttributeCommitId;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitFilterOptions
    {
        public uint Version;
        public GitFilterFlags Flags;
        private nint Reserved;
        public GitObjectID AttributeCommidId;

        public GitFilterOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitFilterOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Flags = options.Flags;
            AttributeCommidId = options.AttributeCommitId;
        }

        public void Free()
        {
        }
    }
}
