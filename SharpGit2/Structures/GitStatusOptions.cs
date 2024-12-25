using SharpGit2.Marshalling;

namespace SharpGit2
{
    /// <summary>
    /// Options to control how libgit2 will issue callbacks
    /// </summary>
    public unsafe struct GitStatusOptions
    {
        /// <summary>
        /// Controls which files to scan and in what order. The default is <see cref="GitStatusShow.IndexAndWorkDir"/>.
        /// </summary>
        public GitStatusShow Show;
        /// <summary>
        /// Adjusts the behavior of file status enumeration. Defaults to <see cref="GitStatusOptionFlags.Defaults"/>.
        /// </summary>
        public GitStatusFlags Flags;
        /// <summary>
        /// An array of path patterns to match (using fnmatch-style matching), or just an array of paths to match exactly
        /// if <see cref="GitStatusOptionFlags.DisablePathSpecMatch"/> is specified in <see cref="Flags"/>.
        /// </summary>
        public string[] PathSpec;
        /// <summary>
        /// The tree to be used for comparison to the working directory and index. Defaults to HEAD.
        /// </summary>
        public GitTree Baseline;
        /// <summary>
        /// Threshold above which similar files will be considered renames. This is equivalent to the -M option. Defaults to 50.
        /// </summary>
        public ushort RenameThreshold;

        /// <summary>
        /// Initialize all fields to the given values
        /// </summary>
        /// <param name="show"></param>
        /// <param name="flags"></param>
        /// <param name="pathspec"></param>
        /// <param name="baseline"></param>
        /// <param name="renameThreshold"></param>
        public GitStatusOptions(
            GitStatusShow show = default,
            GitStatusFlags flags = default,
            string[] pathspec = default!,
            GitTree baseline = default,
            ushort renameThreshold = 0)
            : this()
        {
            Show = show;
            Flags = flags;
            PathSpec = pathspec;
            Baseline = baseline;
            RenameThreshold = renameThreshold;
        }
    }
}

namespace SharpGit2.Native
{
    /// <summary>
    /// Options to control how libgit2 will enumerate file status'
    /// </summary>
    public unsafe struct GitStatusOptions
    {
        /// <summary>
        /// The struct version
        /// </summary>
        public uint Version;
        /// <summary>
        /// Controls which files to scan and in what order. The default is <see cref="GitStatusShow.IndexAndWorkDir"/>.
        /// </summary>
        public GitStatusShow Show;
        /// <summary>
        /// Adjusts the behavior of file status enumeration. Defaults to <see cref="GitStatusOptionFlags.Defaults"/>.
        /// </summary>
        public GitStatusFlags Flags;
        /// <summary>
        /// An array of path patterns to match (using fnmatch-style matching), or just an array of paths to match exactly
        /// if <see cref="GitStatusOptionFlags.DisablePathSpecMatch"/> is specified in <see cref="Flags"/>.
        /// </summary>
        public GitStringArray PathSpec;
        /// <summary>
        /// The tree to be used for comparison to the working directory and index. Defaults to HEAD.
        /// </summary>
        public Git2.Tree* Baseline;
        /// <summary>
        /// Threshold above which similar files will be considered renames.
        /// This is equivalent to the -M option. Defaults to 50.
        /// </summary>
        public ushort RenameThreshold;

        /// <summary>
        /// Parameterless constructor, initializes all fields to default values.
        /// </summary>
        public GitStatusOptions()
        {
            Version = 1;
        }

        /// <summary>
        /// Initialize all fields to the given values
        /// </summary>
        /// <param name="show">Controls which files to scan and in what order. The default is <see cref="GitStatusShow.IndexAndWorkDir"/>.</param>
        /// <param name="flags">Adjusts the behavior of file status enumeration. Defaults to <see cref="GitStatusOptionFlags.Defaults"/>.</param>
        /// <param name="pathspec">
        /// An array of path patterns to match (using fnmatch-style matching), or just an array of paths to match exactly
        /// if <see cref="GitStatusOptionFlags.DisablePathSpecMatch"/> is specified in <see cref="Flags"/>.
        /// </param>
        /// <param name="baseline"></param>
        /// <param name="renameThreshold"></param>
        public GitStatusOptions(
            GitStatusShow show = default,
            GitStatusFlags flags = default,
            GitStringArray pathspec = default,
            Git2.Tree* baseline = null,
            ushort renameThreshold = 0)
            : this()
        {
            Show = show;
            Flags = flags;
            PathSpec = pathspec;
            Baseline = baseline;
            RenameThreshold = renameThreshold;
        }

        /// <summary>
        /// Populate this unmanaged structure with the managed value from <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The managed value to use</param>
        public void FromManaged(in SharpGit2.GitStatusOptions options)
        {
            Version = 1;
            Show = options.Show;
            Flags = options.Flags;
            PathSpec = StringArrayMarshaller.ConvertToUnmanaged(options.PathSpec);
            Baseline = options.Baseline.NativeHandle;
            RenameThreshold = options.RenameThreshold;
        }

        /// <summary>
        /// Free any unmanaged/native resources allocated by <see cref="FromManaged(in SharpGit2.GitStatusOptions)"/>
        /// </summary>
        public void Free()
        {
            StringArrayMarshaller.Free(PathSpec);
        }
    }
}
