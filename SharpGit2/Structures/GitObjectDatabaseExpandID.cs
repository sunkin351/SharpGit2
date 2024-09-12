namespace SharpGit2
{
    /// <summary>
    /// The information about object IDs to query in `git_odb_expand_ids`,
    /// which will be populated upon return.
    /// </summary>
    public unsafe struct GitObjectDatabaseExpandID
    {
        /// <summary>
        /// The object ID to expand
        /// </summary>
        public GitObjectID Id;
        /// <summary>
        /// The length of the object ID (in nibbles, or packets of 4 bits; the
        /// number of hex characters)
        /// </summary>
        public ushort Length;
        /// <summary>
        /// The (optional) type of the object to search for; leave as <see langword="default"/> or set
        /// to <see cref="GitObjectType.Any"/> to query for any object matching the ID.
        /// </summary>
        public GitObjectType Type;
    }
}
