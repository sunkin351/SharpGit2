using System.Runtime.InteropServices;

namespace SharpGit2
{
    /// <summary>
    /// An options structure for querying attributes.
    /// </summary>
    public struct GitAttributeOptions
    {
        public GitAttributeCheckFlags Flags;

        /// <summary>
        /// The commit to load attributes from, when
        /// <see cref="GitAttributeCheckFlags.IncludeCommit"/> is specified.
        /// </summary>
        public GitObjectID AttributeCommitID;
    }
}

namespace SharpGit2.Native
{
    /// <summary>
    /// An options structure for querying attributes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GitAttributeOptions
    {
        public uint Version;
        public GitAttributeCheckFlags Flags;
        private void* Reserved;
        public GitObjectID AttributeCommitID;

        public GitAttributeOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitAttributeOptions options)
        {
            Version = 1;
            Flags = options.Flags;
            Reserved = null;
            AttributeCommitID = options.AttributeCommitID;
        }

        public void Free()
        {
        }
    }
}
