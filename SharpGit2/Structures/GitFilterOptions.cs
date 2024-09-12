using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SharpGit2
{
    public struct GitFilterOptions
    {
        public GitFilterFlags Flags;
        public GitObjectID AttributeCommitId;
    }
}

#pragma warning disable CS0169

namespace SharpGit2.Native
{
    public unsafe struct GitFilterOptions
    {
        public uint Version;
        public GitFilterFlags Flags;
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "This is a native padding field, reserved for future use")]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private void* Reserved;
        public GitObjectID AttributeCommitId;

        public GitFilterOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitFilterOptions options)
        {
            Version = 1;
            Flags = options.Flags;
            AttributeCommitId = options.AttributeCommitId;
        }

        public void Free()
        {
        }
    }
}
