namespace SharpGit2
{
    /// <summary>
    /// A status entry, providing the differences between the file as it exists in HEAD and the index,
    /// and providing the differences between the index and the working directory.
    /// </summary>
    public unsafe struct GitStatusEntry
    {
        /// <summary>
        /// The status flags for this file.
        /// </summary>
        public GitStatusFlags Flags;

        /// <summary>
        /// Detailed information about the differences between the file in HEAD and the file in the Index.
        /// </summary>
        public GitDiffDelta HeadToIndex;

        /// <summary>
        /// Detailed information about the differences between the file in the Index and the file in the working directory.
        /// </summary>
        public GitDiffDelta IndexToWorkingDirectory;

        /// <summary>
        /// Conversion constructor from the native structure
        /// </summary>
        /// <param name="entry">The native entry</param>
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
    /// <summary>
    /// A status entry, providing the differences between the file as it exists in HEAD and the index,
    /// and providing the differences between the index and the working directory.
    /// </summary>
    public unsafe struct GitStatusEntry
    {
        /// <summary>
        /// The status flags for this file.
        /// </summary>
        public GitStatusFlags Flags;

        /// <summary>
        /// Detailed information about the differences between the file in HEAD and the file in the Index.
        /// </summary>
        public GitDiffDelta* HeadToIndex;

        /// <summary>
        /// Detailed information about the differences between the file in the Index and the file in the working directory.
        /// </summary>
        public GitDiffDelta* IndexToWorkingDirectory;
    }
}
