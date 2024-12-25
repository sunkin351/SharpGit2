namespace SharpGit2;

[Flags]
public enum GitApplyFlags : uint
{
    /// <summary>
    /// Don't actually make changes, just test that the patch applies. This is the equivalent of <c>git apply --check</c>.
    /// </summary>
    Check = 1 << 0,
}

/// <summary>
/// Possible application locations for git_apply()
/// </summary>
public enum GitApplyLocationType
{
    /// <summary>
    /// Apply the patch to the workdir, leaving the index untouched. This is the equivalent of <c>git apply</c> with no location argument.
    /// </summary>
    WorkDir = 0,
    /// <summary>
    /// Apply the patch to the index, leaving the working directory untouched. This is the equivalent of <c>git apply --cached</c>.
    /// </summary>
    Index = 1,
    /// <summary>
    /// Apply the patch to both the working directory and the index. This is the equivalent of <c>git apply --index</c>.
    /// </summary>
    Both = 2
}

[Flags]
public enum GitAttributeCheckFlags : uint
{
    /// <summary>
    /// Check attribute flags: Reading values from index and working directory.
    /// <br/><br/>
    /// When checking attributes, it is possible to check attribute files
    /// in both the working directory (if there is one) and the index (if
    /// there is one).  You can explicitly choose where to check and in
    /// which order using the following flags.
    /// <br/><br/>
    /// Core git usually checks the working directory then the index,
    /// except during a checkout when it checks the index first.  It will
    /// use index only for creating archives or for a bare repo (if an
    /// index has been specified for the bare repo).
    /// </summary>
    FileThenIndex = 0,
    ///<inheritdoc cref="FileThenIndex"/>
    IndexThenFile = 1,
    ///<inheritdoc cref="FileThenIndex"/>
    IndexOnly = 1 << 1,

    /// <summary>
    /// Check attribute flags: controlling extended attribute behavior.
    /// 
    /// Normally, attribute checks include looking in the /etc (or system
    /// equivalent) directory for a `gitattributes` file.  Passing this
    /// flag will cause attribute checks to ignore that file.
    /// equivalent) directory for a `gitattributes` file.  Passing the
    /// `GIT_ATTR_CHECK_NO_SYSTEM` flag will cause attribute checks to
    /// ignore that file.
    /// 
    /// Passing the `GIT_ATTR_CHECK_INCLUDE_HEAD` flag will use attributes
    /// from a `.gitattributes` file in the repository at the HEAD revision.
    /// 
    /// Passing the `GIT_ATTR_CHECK_INCLUDE_COMMIT` flag will use attributes
    /// from a `.gitattributes` file in a specific commit.
    /// </summary>
    NoSystem = 1 << 2,
    ///<inheritdoc cref="NoSystem"/>
    IncludeHead = 1 << 3,
    ///<inheritdoc cref="NoSystem"/>
    IncludeCommit = 1 << 4,
}

public enum GitAttributeValueType
{
    /// <summary>
    /// The attribute is not present.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Boolean true value
    /// </summary>
    True,

    /// <summary>
    /// Boolean false value
    /// </summary>
    False,

    /// <summary>
    /// String value
    /// </summary>
    String
}

[Flags]
public enum GitBlameFlags : uint
{
    Normal = 0,
    TrackCopiesSameFile = 1 << 0,
    TrackCopiesSameCommitMoves = 1 << 1,
    TrackCopiesSameCommitCopies = 1 << 2,
    TrackCopiesAnyCommitCopies = 1 << 3,
    FirstParent = 1 << 4,
    UseMailmap = 1 << 5,
    IgnoreWhitespace = 1 << 6,
}

[Flags]
public enum GitBlobFilterFlags : uint
{
    CheckForBinary = 1 << 0,
    NoSystemAttributes = 1 << 1,
    AttributesFromHead = 1 << 2,
    AttributesFromCommit = 1 << 3,
}

/// <summary>
/// Basic type of any Git branch.
/// </summary>
[Flags]
public enum GitBranchType
{
    /// <summary>
    /// Local branch type
    /// </summary>
    Local = 1,
    /// <summary>
    /// Remote branch type
    /// </summary>
    Remote = 2,
    /// <summary>
    /// Any branch type
    /// </summary>
    All = Local | Remote
}

public enum GitCertificateType
{
    None,
    X509,
    HostkeyLibSSH2,
    StringArray
}

public enum GitCertificateSSHType
{
    MD5 = 1,
    SHA1 = 1 << 1,
    SHA256 = 1 << 2,
    SSHRaw = 1 << 3,
}

public enum GitCertificateSSHRawType
{
    Unknown = 0,
    RSA = 1,
    DSS = 2,
    KeyECDSA256 = 3,
    KeyECDSA384 = 4,
    KeyECDSA521 = 5,
    KeyED25519 = 6,
}

/// <summary>
/// Checkout notification flags
/// </summary>
/// <remarks>
/// Checkout will invoke an options notification callback (`notify_cb`) for
/// certain cases - you pick which ones via `notify_flags`:
/// </remarks>
public enum GitCheckoutNotifyFlags : uint
{
    None = 0,
    Conflict = 1 << 0,
    Dirty = 1 << 1,
    Updated = 1 << 2,
    Untracked = 1 << 3,
    Ignored = 1 << 4,
    All = 0xFFFF
}

/// <summary>
/// Checkout behavior flags
/// </summary>
[Flags]
public enum GitCheckoutStrategyFlags : uint
{
    /// <summary>
    /// default is a dry run, no actual updates
    /// </summary>
    None = 0,

    /// <summary>
    /// Allow safe updates that cannot overwrite uncommitted data.
    /// If the uncommitted changes don't conflict with the checked out files,
    /// the checkout will still proceed, leaving the changes intact.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="Force"/>.
    /// <see cref="Force"/> takes precedence over <see cref="Safe"/>.
    /// </remarks>
    Safe = 1,

    /// <summary>
    /// Allow all updates to force working directory to look like index.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="Safe"/>.
    /// <see cref="Force"/> takes precedence over <see cref="Safe"/>.
    /// </remarks>
    Force = 1 << 1,

    /// <summary>
    /// Allow checkout to recreate missing files
    /// </summary>
    RecreateMissing = 1 << 2,

    /// <summary>
    /// Allow checkout to make safe updates even if conflicts are found
    /// </summary>
    AllowConflicts = 1 << 4,

    /// <summary>
    /// Remove untracked files not in index (that are not ignored)
    /// </summary>
    RemoveUntracked = 1 << 5,

    /// <summary>
    /// Remove ignored files not in index
    /// </summary>
    RemoveIgnore = 1 << 6,

    /// <summary>
    /// Only update existing files, don't create new ones
    /// </summary>
    UpdateOnly = 1 << 7,

    /// <summary>
    /// Normally checkout updates index entries as it goes; this stops that.
    /// Implies <see cref="DontWriteIndex"/>
    /// </summary>
    DontUpdateIndex = 1 << 8,

    /// <summary>
    /// Don't refresh index/config/etc before doing checkout
    /// </summary>
    NoRefresh = 1 << 9,

    /// <summary>
    /// Allow checkout to skip unmerged files
    /// </summary>
    SkipUnmerged = 1 << 10,

    /// <summary>
    /// For unmerged files, checkout stage 2 from index
    /// </summary>
    UseOurs = 1 << 11,

    /// <summary>
    /// For unmerged files, checkout stage 3 from index
    /// </summary>
    UseTheirs = 1 << 12,

    /// <summary>
    /// Treat pathspec as simple list of exact match file paths
    /// </summary>
    DisablePathSpecMatch = 1 << 13,

    /// <summary>
    /// Recursively checkout submodules with same options
    /// </summary>
    /// <remarks>
    /// NOT YET IMPLEMENTED
    /// </remarks>
    UpdateSubmodules = 1 << 16,

    /// <summary>
    /// Recursively checkout submodules if HEAD moved in super repo
    /// </summary>
    /// <remarks>
    /// NOT YET IMPLEMENTED
    /// </remarks>
    UpdateSubmodulesIfChanged = 1 << 17,

    /// <summary>
    /// Ignore directories in use, they will be left empty
    /// </summary>
    SkipLockedDirectories = 1 << 18,

    /// <summary>
    /// Don't overwrite ignored files that exist in the checkout target
    /// </summary>
    DontOverwriteIgnored = 1 << 19,

    /// <summary>
    /// Write normal merge files for conflicts
    /// </summary>
    ConflictStyleMerge = 1 << 20,

    /// <summary>
    /// Include common ancestor data in diff3 format files for conflicts
    /// </summary>
    ConflictStyleDiff3 = 1 << 21,

    /// <summary>
    /// Don't overwrite existing files or folders
    /// </summary>
    DontRemoveExisting = 1 << 22,

    /// <summary>
    /// Normally checkout writes the index upon completion; this prevents that.
    /// </summary>
    DontWriteIndex = 1 << 23,

    /// <summary>
    /// Show what would be done by a checkout.  Stop after sending
    /// notifications; don't update the working directory or index.
    /// </summary>
    DryRun = 1 << 24,

    /// <summary>
    /// Include common ancestor data in zdiff3 format for conflicts
    /// </summary>
    ConflictStyleZDiff3 = 1 << 25,
}

/// <summary>
/// Options for bypassing the git-aware transport on clone. Bypassing
/// it means that instead of a fetch, libgit2 will copy the object
/// database directory instead of figuring out what it needs, which is
/// faster. If possible, it will hardlink the files to save space.
/// </summary>
public enum GitCloneLocalType
{
    /// <summary>
    /// Auto-detect (default), libgit2 will bypass the git-aware
    /// transport for local paths, but use a normal fetch for
    /// `file://` urls.
    /// </summary>
    LocalAuto,
    /// <summary>
    /// Bypass the git-aware transport even for a `file://` url.
    /// </summary>
    Local,
    /// <summary>
    /// Do no bypass the git-aware transport
    /// </summary>
    NoLocal,
    /// <summary>
    /// Bypass the git-aware transport, but do not try to use
    /// hardlinks.
    /// </summary>
    LocalNoLinks
}

public enum GitConfigLevel
{
    HighestLevel = -1,
    ProgramData = 1,
    System = 2,
    XDG = 3,
    Global = 4,
    Local = 5,
    Worktree = 6,
    App = 7
}

public enum GitConfigMapType
{
    False,
    True,
    Int32,
    String
}
public enum GitCredentialType
{
    UserPassPlainText = 1,
    SSHKey = 1 << 1,
    SSHCustom = 1 << 2,
    Default = 1 << 3,
    SSHInteractive = 1 << 4,
    Username = 1 << 5,
    SSHMemory = 1 << 6,
}

/// <summary>
/// What type of change is described by a git_diff_delta?
/// </summary>
/// <remarks>
/// <see cref="GitDeltaType.Renamed"/> and <see cref="GitDeltaType.Copied"/> will only show up if you run
/// `git_diff_find_similar()` on the diff object.
/// <br/><br/>
/// <see cref="GitDeltaType.TypeChange"/> only shows up given `GIT_DIFF_INCLUDE_TYPECHANGE`
/// in the option flags (otherwise type changes will be split into ADDED /
/// DELETED pairs).
/// </remarks>
public enum GitDeltaType
{
    /// <summary>
    /// No Changes
    /// </summary>
    Unmodified,

    /// <summary>
    /// entry does not exist in old version
    /// </summary>
    Added,

    /// <summary>
    /// entry does not exist in new version
    /// </summary>
    Deleted,

    /// <summary>
    /// entry content changed between old and new
    /// </summary>
    Modified,

    /// <summary>
    /// entry was renamed between old and new
    /// </summary>
    Renamed,

    /// <summary>
    /// entry was copied from another old entry
    /// </summary>
    Copied,

    /// <summary>
    /// entry is ignored item in workdir
    /// </summary>
    Ignored,

    /// <summary>
    /// entry is untracked item in workdir
    /// </summary>
    Untracked,

    /// <summary>
    /// type of entry changed between old and new
    /// </summary>
    TypeChange,

    /// <summary>
    /// entry is unreadable
    /// </summary>
    Unreadable,

    /// <summary>
    /// entry in the index is conflicted
    /// </summary>
    Conflicted
}

public enum GitDescribeStrategy : uint
{
    Default,
    Tags,
    All
}

/// <summary>
/// When producing a binary diff, the binary data returned will be
/// either the deflated full ("literal") contents of the file, or
/// the deflated binary delta between the two sides (whichever is
/// smaller).
/// </summary>
public enum GitDiffBinaryType
{
    /// <summary>
    /// There is no binary delta
    /// </summary>
    None,
    /// <summary>
    /// The binary data is the literal contents of the file
    /// </summary>
    Literal,
    /// <summary>
    /// The binary data is the delta from one side to the other
    /// </summary>
    Delta
}

[Flags]
public enum GitDiffFindFlags
{
    FindByConfig = 0,
    FindRenames = 1 << 0,
    FindRenamesFromRewrites = 1 << 1,
    FindCopies = 1 << 2,
    FindCopiesFromUnmodified = 1 << 3,
    FindRewrites = 1 << 4,
    BreakRewrites = 1 << 5,
    FindAndBreakRewrites = FindRewrites | BreakRewrites,
    FindForUntracked = 1 << 6,
    FindAll = 0xff,

    IgnoreLeadingWhitespace = 0,
    IgnoreWhitespace = 1 << 12,
    DontIgnoreWhitespace = 1 << 13,
    ExactMatchOnly = 1 << 14,
    BreakRewritesForRenamesOnly = 1 << 15,
    RemoveUnmodified = 1 << 16,
}

/// <summary>
/// Flags for the delta object and the file objects on each side.
/// </summary>
/// <remarks>
/// These flags are used for both the `flags` value of the `git_diff_delta`
/// and the flags for the `git_diff_file` objects representing the old and
/// new sides of the delta.  Values outside of this public range should be
/// considered reserved for internal or future use.
/// </remarks>
[Flags]
public enum GitDiffFlags : uint
{
    /// <summary>
    /// file(s) treated as binary data
    /// </summary>
    Binary = 1,

    /// <summary>
    /// file(s) treated as text data
    /// </summary>
    NotBinary = 1 << 1,

    /// <summary>
    /// <see cref="GitDiffFile.Id"/> is known correct
    /// </summary>
    ValidID = 1 << 2,

    /// <summary>
    /// file exists at this side of the delta
    /// </summary>
    Exists = 1 << 3,

    /// <summary>
    /// file size value is known correct
    /// </summary>
    ValidSize = 1 << 4,
}

[Flags]
public enum GitDiffFormatEmailFlags
{
    None = 0,
    ExcludeSubjectPatchMarker = 1,
}

public enum GitDiffFormatType : uint
{
    Patch = 1,
    PatchHeader = 2,
    Raw = 3,
    NameOnly = 4,
    NameStatus = 5,
    PatchID = 6
}

public enum GitDiffLineType : byte
{
    Context = (byte)' ',
    Addition = (byte)'+',
    Deletion = (byte)'-',
    ContextEOFNL = (byte)'=',
    AddEOFNL = (byte)'>',
    DeleteEOFNL = (byte)'<',
    FileHDR = (byte)'F',
    HunkHDR = (byte)'H',
    Binary = (byte)'B'
}

/// <summary>
/// Flags for diff options.  A combination of these flags can be passed
/// in via the `flags` value in the `git_diff_options`.
/// </summary>
[Flags]
public enum GitDiffOptionFlags : uint
{
    /// <summary>
    /// Normal diff, the default
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Reverse the sides of the diff
    /// </summary>
    Reverse = 1 << 0,
    /// <summary>
    /// Include ignored files in the diff
    /// </summary>
    IncludeIgnored = 1 << 1,
    /// <summary>
    /// Even with <see cref="IncludeIgnored"/>, an entire ignored directory
    /// will be marked with only a single entry in the diff; this flag
    /// adds all files under the directory as IGNORED entries, too.
    /// </summary>
    RecurseIgnoredDirectories = 1 << 2,
    /// <summary>
    /// Include untracked files in the diff
    /// </summary>
    IncludeUntracked = 1 << 3,
    /// <summary>
    /// Even with <see cref="IncludeUntracked"/>, an entire untracked
	/// directory will be marked with only a single entry in the diff
	/// (a la what core Git does in `git status`); this flag adds *all*
	/// files under untracked directories as UNTRACKED entries, too.
    /// </summary>
    RecurseUntrackedDirectories = 1 << 4,
    /// <summary>
    /// Include unmodified files in the diff
    /// </summary>
    IncludeUnmodified = 1 << 5,
    /// <summary>
    /// Normally, a type change between files will be converted into a
    /// DELETED record for the old and an ADDED record for the new; this
    /// options enabled the generation of TYPECHANGE delta records.
    /// </summary>
    IncludeTypeChange = 1 << 6,
    /// <summary>
    /// Even with <see cref="IncludeTypeChange"/>, blob->tree changes still
    /// generally show as a DELETED blob.  This flag tries to correctly
    /// label blob->tree transitions as TYPECHANGE records with new_file's
    /// mode set to tree.  Note: the tree SHA will not be available.
    /// </summary>
    IncludeTypeChangeTrees = 1 << 7,
    /// <summary>
    /// Ignore file mode changes
    /// </summary>
    IgnoreFileMode = 1 << 8,
    /// <summary>
    /// Treat all submodules as unmodified
    /// </summary>
    IgnoreSubmodules = 1 << 9,
    /// <summary>
    /// Use case insensitive filename comparisons
    /// </summary>
    IgnoreCase = 1 << 10,
    /// <summary>
    /// May be combined with <see cref="IgnoreCase"/> to specify that a file
    /// that has changed case will be returned as an add/delete pair.
    /// </summary>
    IncludeCaseChange = 1 << 11,
    /// <summary>
    /// If the pathspec is set in the diff options, this flags indicates
    /// that the paths will be treated as literal paths instead of
    /// fnmatch patterns.  Each path in the list must either be a full
    /// path to a file or a directory.  (A trailing slash indicates that
    /// the path will _only_ match a directory).  If a directory is
    /// specified, all children will be included.
    /// </summary>
    DisablePathSpecMatch = 1 << 12,
    /// <summary>
    /// Disable updating of the `binary` flag in delta records.  This is
    /// useful when iterating over a diff if you don't need hunk and data
    /// callbacks and want to avoid having to load file completely.
    /// </summary>
    SkipBinaryCheck = 1 << 13,
    /// <summary>
    /// When diff finds an untracked directory, to match the behavior of
    /// core Git, it scans the contents for IGNORED and UNTRACKED files.
    /// If *all* contents are IGNORED, then the directory is IGNORED; if
    /// any contents are not IGNORED, then the directory is UNTRACKED.
    /// This is extra work that may not matter in many cases.  This flag
    /// turns off that scan and immediately labels an untracked directory
    /// as UNTRACKED (changing the behavior to not match core Git).
    /// </summary>
    EnableFastUntrackedDirectories = 1 << 14,
    /// <summary>
    /// When diff finds a file in the working directory with stat
    /// information different from the index, but the OID ends up being the
    /// same, write the correct stat information into the index.  Note:
    /// without this flag, diff will always leave the index untouched.
    /// </summary>
    UpdateIndex = 1 << 15,
    /// <summary>
    /// Include unreadable files in the diff
    /// </summary>
    IncludeUnreadable = 1 << 16,
    /// <summary>
    /// Include unreadable files in the diff
    /// </summary>
    IncludeUnreadableAsUntracked = 1 << 17,
    /// <summary>
    /// Use a heuristic that takes indentation and whitespace into account
    /// which generally can produce better diffs when dealing with ambiguous
    /// diff hunks.
    /// </summary>
    IndentHeuristic = 1 << 18,
    /// <summary>
    /// Ignore blank lines
    /// </summary>
    IgnoreBlankLines = 1 << 19,
    /// <summary>
    /// Treat all files as text, disabling binary attributes and detection
    /// </summary>
    ForceText = 1 << 20,
    /// <summary>
    /// Treat all files as binary, disabling text diffs
    /// </summary>
    ForceBinary = 1 << 21,
    /// <summary>
    /// Ignore all whitespace
    /// </summary>
    IgnoreWhitespace = 1 << 22,
    /// <summary>
    /// Ignore changes in amount of whitespace
    /// </summary>
    IgnoreWhitespaceChange = 1 << 23,
    /// <summary>
    /// Ignore whitespace at end of line
    /// </summary>
    IgnoreWhitespaceEOL = 1 << 24,
    /// <summary>
    /// When generating patch text, include the content of untracked
    /// files.  This automatically turns on <see cref="IncludeUntracked"/> but
    /// it does not turn on <see cref="RecurseUntrackedDirectories"/>. Add that
    /// flag if you want the content of every single UNTRACKED file.
    /// </summary>
    ShowUntrackedContent = 1 << 25,
    /// <summary>
    /// When generating output, include the names of unmodified files if
    /// they are included in the git_diff.  Normally these are skipped in
    /// the formats that list files (e.g. name-only, name-status, raw).
    /// Even with this, these will not be included in patch format.
    /// </summary>
    ShowUnmodified = 1 << 26,
    /// <summary>
    /// Use the "patience diff" algorithm
    /// </summary>
    Patience = 1 << 28,
    /// <summary>
    /// Take extra time to find minimal diff
    /// </summary>
    Minimal = 1 << 29,
    /// <summary>
    /// Include the necessary deflate / delta information so that `git-apply`
    /// can apply given diff information to binary files.
    /// </summary>
    ShowBinary = 1 << 30,
}

public enum GitDiffStatsFormat
{
    None = 0,
    Full = 1 << 0,
    Short = 1 << 1,
    Number = 1 << 2,
    IncludeSummary = 1 << 3,
}

/// <summary>
/// Direction of the connection.
/// </summary>
/// <remarks>
/// We need this because we need to know whether we should call
/// git-upload-pack or git-receive-pack on the remote end when get_refs
/// gets called.
/// </remarks>
public enum GitDirection
{
    Fetch,
    Push
}

public enum GitEmailCreateFlags
{
    Default = 0,
    OmitNumbers = 1,
    AlwaysNumbers = 1 << 1,
    NoRenames = 1 << 2,
}

public enum GitErrorClass
{
    None = 0,
    NoMemory,
    OS,
    Invalid,
    Reference,
    ZLib,
    Repository,
    Config,
    Regex,
    ObjectDB,
    Index,
    Object,
    Net,
    Tag,
    Tree,
    Indexer,
    SSL,
    Submodule,
    Thread,
    Stash,
    Checkout,
    FetchHead,
    Merge,
    SSH,
    Filter,
    Revert,
    Callback,
    CherryPick,
    Describe,
    Rebase,
    FileSystem,
    Patch,
    Worktree,
    SHA,
    HTTP,
    Internal,
    Grafts,
}

public enum GitFetchPrune
{
    Unspecified,
    Prune,
    NoPrune
}

[Flags]
public enum GitFeatures
{
    /// <summary>
    /// If set, libgit2 was built thread-aware and can be safely used from
    /// multiple threads.
    /// </summary>
    Threads = 1,

    /// <summary>
    /// If set, libgit2 was built with and linked against a TLS implementation.
    /// Custom TLS streams may still be added by the user to support HTTPS
    /// regardless of this.
    /// </summary>
    HTTPS = 1 << 1,

    /// <summary>
    /// If set, libgit2 was built with and linked against libssh2. A custom
    /// transport may still be added by the user to support libssh2 regardless
    /// of this.
    /// </summary>
    SSH = 1 << 2,

    /// <summary>
    /// If set, libgit2 was built with support for sub-second resolution in file
    /// modification times.
    /// </summary>
    NSEC = 1 << 3,
}

public enum GitFetchDepth
{
    Full = 0,
    Unshallow = 2147483647,
}

public enum GitFetchPruneType
{
    Unspecified,
    Prune,
    NoPrune,
}

public enum GitFileMode
{
    Unreadable = 0,
    Tree = 0x4000,
    Blob = 0x81A4,
    BlobExecutable = 0x81ED,
    Link = 0xA000,
    Commit = 0xE000,
}

[Flags]
public enum GitFilterFlags
{
    Default = 0,
    AllowUnsafe = 1,
    NoSystemAttributes = 1 << 1,
    AttributesFromHead = 1 << 2,
    AttributesFromCommit = 1 << 3,
}

public enum GitFilterMode
{
    ToWorktree = 0,
    Smudge = ToWorktree,
    ToObjectDB = 1,
    Clean = ToObjectDB,
}

[Flags]
public enum GitIndexEntryFlags
{
    Extended = 1,
    Valid = 2,
}

public enum GitIndexStageFlags
{
    /// <summary>
    /// Match any index stage.
    /// </summary>
    /// <remarks>
    /// Some index APIs take a stage to match; pass this value to match
    /// any entry matching the path regardless of stage.
    /// </remarks>
    Any = -1,

    /// <summary>
    /// A normal staged file in the index.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// The ancestor side of a conflict.
    /// </summary>
    Ancestor = 1,

    /// <summary>
    /// The "ours" side of a conflict.
    /// </summary>
    Ours = 2,

    /// <summary>
    /// The "theirs" side of a conflict.
    /// </summary>
    Theirs = 3
}

[Flags]
public enum GitIndexEntryExtendedFlags
{
    UpToDate = 1 << 2,
    IntentToAdd = 1 << 13,
    SkipWorktree = 1 << 14,
}

[Flags]
public enum GitIndexAddOptions
{
    Default = 0,
    Force = 1,
    DisablePathspecMatch = 1 << 1,
    CheckPathspec = 1 << 2
}

/// <summary>
/// Capabilities of system that affect index actions.
/// </summary>
[Flags]
public enum GitIndexCapabilities
{
    IgnoreCase = 1,
    NoFilemode = 2,
    NoSymlinks = 4,
    FromOwner = -1
}

/// <summary>
/// The results of `git_merge_analysis` indicate the merge opportunities.
/// </summary>
[Flags]
public enum GitMergeAnalysisResult
{
    /// <summary>
    /// No merge is possible.  (Unused.)
    /// </summary>
    None,
    /// <summary>
    /// A "normal" merge; both HEAD and the given merge input have diverged
    /// from their common ancestor.  The divergent commits must be merged.
    /// </summary>
    Normal = 1,
    /// <summary>
    /// All given merge inputs are reachable from HEAD, meaning the
    /// repository is up-to-date and no merge needs to be performed.
    /// </summary>
    UpToDate = 1 << 1,
    /// <summary>
    /// The given merge input is a fast-forward from HEAD and no merge
    /// needs to be performed.  Instead, the client can check out the
    /// given merge input.
    /// </summary>
    FastForward = 1 << 2,
    /// <summary>
    /// The HEAD of the current repository is "unborn" and does not point to
    /// a valid commit.  No merge can be performed, but the caller may wish
    /// to simply set HEAD to the target commit(s).
    /// </summary>
    Unborn = 1 << 3,
}

/// <summary>
/// Merge file favor options for `git_merge_options` instruct the file-level
/// merging functionality how to deal with conflicting regions of the files.
/// </summary>
public enum GitMergeFileFavor
{
    /// <summary>
    /// When a region of a file is changed in both branches, a conflict
    /// will be recorded in the index so that `git_checkout` can produce
    /// a merge file with conflict markers in the working directory.
    /// This is the default.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// When a region of a file is changed in both branches, the file
    /// created in the index will contain the "ours" side of any conflicting
    /// region.  The index will not record a conflict.
    /// </summary>
    Ours,
    /// <summary>
    /// When a region of a file is changed in both branches, the file
    /// created in the index will contain the "theirs" side of any conflicting
    /// region.  The index will not record a conflict.
    /// </summary>
    Thiers,
    /// <summary>
    /// When a region of a file is changed in both branches, the file
    /// created in the index will contain each unique line from each side,
    /// which has the result of combining both files.  The index will not
    /// record a conflict.
    /// </summary>
    Union,
}

[Flags]
public enum GitMergeFileFlags : uint
{
    Default = 0,
    /// <summary>
    /// Create standard conflicted merge files
    /// </summary>
    StyleMerge = 1,
    /// <summary>
    /// Create diff3-style files
    /// </summary>
    StyleDiff3 = 1 << 1,
    /// <summary>
    /// Condense non-alphanumeric regions for simplified diff file
    /// </summary>
    SimplifyAlphaNumeric = 1 << 2,
    /// <summary>
    /// Ignore all whitespace
    /// </summary>
    IgnoreWhitespace = 1 << 3,
    /// <summary>
    /// Ignore changes in amount of whitespace
    /// </summary>
    IgnoreWhitespaceChange = 1 << 4,
    /// <summary>
    /// Ignore whitespace at end of line
    /// </summary>
    IgnoreWhitespaceEOL = 1 << 5,
    /// <summary>
    /// Use the "patience diff" algorithm
    /// </summary>
    DiffPatience = 1 << 6,
    /// <summary>
    /// Take extra time to find minimal diff
    /// </summary>
    DiffMinimal = 1 << 7,
    /// <summary>
    /// Create zdiff3 ("zealous diff3")-style files
    /// </summary>
    StyleZDiff3 = 1 << 8,
    /// <summary>
    /// Do not produce file conflicts when common regions have
    /// changed; keep the conflict markers in the file and accept
    /// that as the merge result.
    /// </summary>
    AcceptConflicts = 1 << 9,
}

/// <summary>
/// Flags for `git_merge` options.  A combination of these flags can be
/// passed in via the `flags` value in <see cref="GitMergeOptions"/> `git_merge_options`.
/// </summary>
[Flags]
public enum GitMergeFlags : uint
{
    /// <summary>
    /// Detect renames that occur between the common ancestor and the "ours" side or the common ancestor and the "theirs" side.
    /// This will enable the ability to merge between a modified and renamed file.
    /// </summary>
    FindRenames = 1,
    /// <summary>
    /// If a conflict occurs, exit immediately instead of attempting to continue resolving conflicts.
    /// The merge operation will fail with <see cref="GitError.MergeConflict"/> and no index will be returned.
    /// </summary>
    FailOnConflict = 1 << 1,
    /// <summary>
    /// Do not write the REUC extension on the generated index
    /// </summary>
    SkipREUC = 1 << 2,
    /// <summary>
    /// If the commits being merged have multiple merge bases, do not build a recursive merge base (by merging the multiple merge bases),
    /// instead simply use the first base. This flag provides a similar merge base to <c>git-merge-resolve</c>.
    /// </summary>
    NoRecursive = 1 << 3,
    /// <summary>
    /// Treat this merge as if it is to produce the virtual base of a recursive merge.
    /// This will ensure that there are no conflicts, any conflicting regions will keep conflict markers in the merge result.
    /// </summary>
    VirtualBase = 1 << 4,
}

/// <summary>
/// The user's stated preference for merges.
/// </summary>
[Flags]
public enum GitMergePreference
{
    /// <summary>
    /// No configuration was found that suggests a preferred behavior for
    /// merge.
    /// </summary>
    None = 0,
    /// <summary>
    /// There is a `merge.ff=false` configuration setting, suggesting that
    /// the user does not want to allow a fast-forward merge.
    /// </summary>
    NoFastForward = 1,
    /// <summary>
    /// There is a `merge.ff=only` configuration setting, suggesting that
    /// the user only wants fast-forward merges.
    /// </summary>
    FastForwardOnly = 2,
}

[Flags]
public enum GitObjectDatabaseBackendLooseFlags : uint
{
    LooseFSync = 1 << 0,
}

/// <summary>
/// Flags controlling the behavior of ODB lookup operations
/// </summary>
[Flags]
public enum GitObjectDatabaseLookupFlags : uint
{
    /// <summary>
    /// Don't call `git_odb_refresh` if the lookup fails. Useful when doing
    /// a batch of lookup operations for objects that may legitimately not
    /// exist. When using this flag, you may wish to manually call
    /// `git_odb_refresh` before processing a batch of objects.
    /// </summary>
    NoRefresh = 1 << 0,
}

public enum GitObjectDatabaseStreamMode
{
    ReadOnly = 1 << 1,
    WriteOnly = 1 << 2,
    ReadWrite = ReadOnly | WriteOnly,
}

public enum GitPackBuilderStageType
{
    AddingObjects = 0,
    Deltafication = 1,
}

/// <summary>
/// Options controlling how pathspec match should be executed
/// </summary>
[Flags]
public enum GitPathSpecFlags : uint
{
    Default = 0,

    /// <summary>
    /// Forces match to ignore case. Otherwise match will
    /// use native case sensitivity of platform filesystem
    /// </summary>
    IgnoreCase = 1,

    /// <summary>
    /// Forces case sensitive match. Otherwise match will
    /// use native case sensitivity of platform filesystem
    /// </summary>
    UseCase = 1 << 1,

    /// <summary>
    /// Disables glob patterns and just uses simple
    /// string comparison for matching
    /// </summary>
    NoGlob = 1 << 2,

    /// <summary>
    /// This means the match functions return error code <see cref="GitError.NotFound"/>
    /// if no matches are found; otherwise no matches is still success (return 0) but
    /// <see cref="GitNativeApi.git_pathspec_match_list_entrycount(Git2.PathSpecMatchList*)"/>
    /// will indicate 0 matches.
    /// </summary>
    NoMatchError = 1 << 3,

    /// <summary>
    /// This means that the <see cref="GitPathSpecMatchList"/>
    /// should track which patterns matched which files so that at the end of
    /// the match we can identify patterns that did not match any files.
    /// </summary>
    FindFailures = 1 << 4,

    /// <summary>
    /// This means that the <see cref="GitPathSpecMatchList"/>
    /// does not need to keep the actual matching filenames.  Use this to
    /// just test if there were any matches at all or in combination with
    /// <see cref="FindFailures"/> to validate a pathspec.
    /// </summary>
    FailuresOnly = 1 << 5,
}

/// <summary>
/// The type of proxy to use.
/// </summary>
public enum GitProxyType
{
    /// <summary>
    /// Do not attempt to connect through a proxy
    /// </summary>
    /// <remarks>
    /// If built against libcurl, it itself may attempt to connect
    /// to a proxy if the environment variables specify it.
    /// </remarks>
    None,
    /// <summary>
    /// Try to auto-detect the proxy from the git configuration.
    /// </summary>
    Auto,
    /// <summary>
    /// Connect via the URL given in the options
    /// </summary>
    Specified,
}

public enum GitRebaseOperationType
{
    Pick = 0,
    Reword,
    Edit,
    Squash,
    Fixup,
    Exec,
}

public enum GitRemoteAutoTagOption
{
    DownloadTagsUnspecified,
    DownloadTagsAuto,
    DownloadTagsNone,
    DownloadTagsAll,
}

public enum GitRemoteCompletionType
{
    Download,
    Indexing,
    Error
}

/// <summary>
/// Remote creation options flags
/// </summary>
[Flags]
public enum GitRemoteCreateFlags
{
    /// <summary>
    /// Ignore the repository apply.insteadOf configuration
    /// </summary>
    SkipInsteadOf = 1 << 0,
    /// <summary>
    /// Don't build a fetchspec from the name if none is set
    /// </summary>
    SkipDefaultFetchSpec = 1 << 1,
}

/// <summary>
/// Remote redirection settings; whether redirects to another host
/// are permitted.  By default, git will follow a redirect on the
/// initial request (`/info/refs`), but not subsequent requests.
/// </summary>
public enum GitRemoteRedirectType
{
    /// <summary>
    /// Do not follow any off-site redirects at any stage of
    /// the fetch or push.
    /// </summary>
    None = 1 << 0,
    /// <summary>
    /// Allow off-site redirects only upon the initial request.
    /// This is the default.
    /// </summary>
    Initial = 1 << 1,
    /// <summary>
    /// Allow redirects at any stage in the fetch or push.
    /// </summary>
    All = 1 << 2,
}

[Flags]
public enum GitRemoteUpdateFlags : uint
{
    FetchHead = 1,
    ReportUnchanged = 1 << 1
}

public enum GitRepositoryState
{
    None,
    Merge,
    Revert,
    RevertSequence,
    CherryPick,
    CherryPickSequence,
    Bisect,
    Rebase,
    RebaseInteractive,
    RebaseMerge,
    ApplyMailbox,
    ApplyMailboxOrRebase
}

[Flags]
public enum GitRepositoryInitFlags : uint
{
    /// <summary>
    /// Create a bare repository with no working directory.
    /// </summary>
    Bare = 1,
    /// <summary>
    /// Return <see cref="GitError.Exists"/> if the repo_path appears to already be a git repository.
    /// </summary>
    NoReinit = 1 << 1,
    /// <summary>
    /// Normally a "/.git/" will be appended to the repo path for
    /// non-bare repos (if it is not already there), but passing this flag
    /// prevents that behavior.
    /// </summary>
    NoDotGitFolder = 1 << 2,
    /// <summary>
    /// Make the repo_path (and workdir_path) as needed. Init is always willing
    /// to create the ".git" directory even without this flag. This flag tells
    /// init to create the trailing component of the repo and workdir paths
    /// as needed.
    /// </summary>
    MakeDirectory = 1 << 3,
    /// <summary>
    /// Recursively make all components of the repo
    /// and workdir paths as necessary.
    /// </summary>
    MakePath = 1 << 4,
    /// <summary>
    /// libgit2 normally uses internal templates to initialize a new repo.
    /// This flags enables external templates, looking the "template_path" from
    /// the options if set, or the `init.templatedir` global config if not,
    /// or falling back on "/usr/share/git-core/templates" if it exists.
    /// </summary>
    ExternalTemplate = 1 << 5,
    /// <summary>
    /// If an alternate workdir is specified, use relative paths for the gitdir
    /// and core.worktree.
    /// </summary>
    RelativeGitlink = 1 << 6
}

public enum GitRepositoryInitMode : uint
{
    /// <summary>
    /// Use permissions configured by umask - the default.
    /// </summary>
    SharedUmask = 0,
    /// <summary>
    /// Use "--shared=group" behavior, chmod'ing the new repo to be group
    /// writable and "g+sx" for sticky group assignment.
    /// </summary>
    SharedGroup = 2775,
    /// <summary>
    /// Use "--shared=all" behavior, adding world readability.
    /// </summary>
    SharedAll = 2777
}

public enum GitRepositoryItemType
{
    GitDir,
    WorkDir,
    CommonDir,
    Index,
    Objects,
    Refs,
    PackedRefs,
    Remotes,
    Config,
    Info,
    Hooks,
    Logs,
    Modules,
    WorkTrees,
    WorkTreeConfig,
    _Last
}

[Flags]
public enum GitRepositoryOpenFlags : uint
{
    /// <summary>
    /// Only open the repository if it can be immediately found in the
    /// start_path. Do not walk up from the start_path looking at parent
    /// directories.
    /// </summary>
    NoSearch = 1,

    /// <summary>
    /// Unless this flag is set, open will not continue searching across
    /// filesystem boundaries (i.e. when `st_dev` changes from the `stat`
    /// system call).  For example, searching in a user's home directory at
    /// "/home/user/source/" will not return "/.git/" as the found repo if
    /// "/" is a different filesystem than "/home".
    /// </summary>
    CrossFileSystem = 1 << 1,

    /// <summary>
    /// Open repository as a bare repo regardless of core.bare config, and
    /// defer loading config file for faster setup.
    /// Unlike <seealso cref="GitRepository.Open(string, GitRepositoryOpenFlags, string?)"/>, this can follow gitlinks.
    /// </summary>
    Bare = 1 << 2,

    /// <summary>
    /// Do not check for a repository by appending /.git to the start_path;
    /// only open the repository if start_path itself points to the git
    /// directory.
    /// </summary>
    NoDotGit = 1 << 3,

    /// <summary>
    /// Find and open a git repository, respecting the environment variables
    /// used by the git command-line tools.
    /// 
    /// If set, `git_repository_open_ext` will ignore the other flags and
    /// the `ceiling_dirs` argument, and will allow a NULL `path` to use
    /// `GIT_DIR` or search from the current directory.
    /// 
    /// The search for a repository will respect $GIT_CEILING_DIRECTORIES and
    /// $GIT_DISCOVERY_ACROSS_FILESYSTEM.  The opened repository will
    /// respect $GIT_INDEX_FILE, $GIT_NAMESPACE, $GIT_OBJECT_DIRECTORY, and
    /// $GIT_ALTERNATE_OBJECT_DIRECTORIES.
    /// 
    /// In the future, this flag will also cause <see cref="GitRepository.Open(string?, GitRepositoryOpenFlags, string?)"/>
    /// to respect $GIT_WORK_TREE and $GIT_COMMON_DIR; currently,
    /// <see cref="GitRepository.Open(string?, GitRepositoryOpenFlags, string?)"/>
    /// with this flag will error out if either
    /// $GIT_WORK_TREE or $GIT_COMMON_DIR is set.
    /// </summary>
    FromEnvironment = 1 << 4
}

public enum GitResetType
{
    Soft = 1,
    Mixed = 2,
    Hard = 3,
}

public enum GitRevSpecType
{
    Single = 1,
    Range = 1 << 1,
    MergeBase = 1 << 2,
}

[Flags]
public enum GitSortType
{
    None = 0,
    Topological = 1,
    Time = 1 << 1,
    Reverse = 1 << 2,
}

[Flags]
public enum GitStashApplyFlags
{
    Default = 0,
    ReinstateIndex = 1,
}

public enum GitStashApplyProgressType
{
    None = 0,
    LoadingStash,
    AnalyzeIndex,
    AnalyzeModified,
    AnalyzeUntracked,
    CheckoutUntracked,
    CheckoutModified,
    Done
}

/// <summary>
/// Stash Flags
/// </summary>
public enum GitStashFlags
{
    /// <summary>
    /// No options, default
    /// </summary>
    Default = 0,
    /// <summary>
    /// All changes already added to the index are left intact in
    /// the working directory
    /// </summary>
    KeepIndex = 1,
    /// <summary>
    /// All untracked files are also stashed and then cleaned up
    /// from the working directory
    /// </summary>
    IncludeUntracked = 1 << 1,
    /// <summary>
    /// All ignored files are also stashed and then cleaned up from
    /// the working directory
    /// </summary>
    IncludeIgnore = 1 << 2,
    /// <summary>
    /// All changes in the index and working directory are left intact
    /// </summary>
    KeepAll = 1 << 3,
}

/// <summary>
/// Status flags for a single file.
/// </summary>
/// <remarks>
/// A combination of these values will be returned to indicate the status of
/// a file.  Status compares the working directory, the index, and the
/// current HEAD of the repository.  The <c>GitStatusFlags.Index*</c> set of flags
/// represents the status of file in the index relative to the HEAD, and the
/// <c>GitStatusFlags.WorkingTree*</c> set of flags represent the status of the file in the
/// working directory relative to the index.
/// </remarks>
[Flags]
public enum GitStatusFlags
{
    Current = 0,

    IndexNew = 1,
    IndexModified = 1 << 1,
    IndexDeleted = 1 << 2,
    IndexRenamed = 1 << 3,
    IndexTypeChange = 1 << 4,

    WorkingTreeNew = 1 << 7,
    WorkingTreeModified = 1 << 8,
    WorkingTreeDeleted = 1 << 9,
    WorkingTreeTypeChange = 1 << 10,
    WorkingTreeRenamed = 1 << 11,
    WorkingTreeUnreadable = 1 << 12,

    Ignored = 1 << 14,
    Conflicted = 1 << 15
}

/// <summary>
/// Flags to control what files are included in a status enumeration.
/// </summary>
[Flags]
public enum GitStatusOptionFlags
{
    /// <summary>
    /// Says that callbacks should be called on untracked files. These will only
    /// be called if the workdir files are included in the status "show" option.
    /// </summary>
    IncludeUntracked = 1,
    /// <summary>
    /// Says that ignored files get callbacks. Again, these callbacks will only
    /// be made if the workdir files are included in the status "show" option.
    /// </summary>
    IncludeIgnored = 1 << 1,
    /// <summary>
    /// Indicates that callback should be made even on unmodified files.
    /// </summary>
    IncludeUnmodified = 1 << 2,
    /// <summary>
    /// Indicates that submodules should be skipped. This only applies if there
    /// are no pending typechanges to the submodule (either from or to another type).
    /// </summary>
    ExcludeSubmodules = 1 << 3,
    /// <summary>
    /// Indicates that all files in untracked directories should be included.
    /// Normally if an entire directory is new, then just the top-level directory
    /// is included (with a trailing slash on the entry name). This flag says to
    /// include all of the individual files in the directory instead.
    /// </summary>
    RecurseUntrackedDirectories = 1 << 4,
    /// <summary>
    /// Indicates that the given path should be treated as a literal path,
    /// and not as a pathspec pattern.
    /// </summary>
    DisablePathSpecMatch = 1 << 5,
    /// <summary>
    /// Indicates that the contents of ignored directories should be included in the status.
    /// This is like doing <c>git ls-files -o -i --exclude-standard</c> with core git.
    /// </summary>
    RecurseIgnoredDirectories = 1 << 6,
    /// <summary>
    /// Indicates that rename detection should be processed between the head and the
    /// index and enables the <see cref="GitStatusFlags.IndexRenamed"/> as a possible status flag.
    /// </summary>
    RenamesHeadToIndex = 1 << 7,
    /// <summary>
    /// Indicates that rename detection should be run between the index and the working directory
    /// and enabled <see cref="GitStatusFlags.WorkingTreeRenamed"/> as a possible status flag.
    /// </summary>
    RenamesIndexToWorkDirectory = 1 << 8,
    /// <summary>
    /// Overrides the native case sensitivity for the file system and forces the output to be in case-sensitive order.
    /// </summary>
    SortCaseSensitively = 1 << 9,
    /// <summary>
    /// Overrides the native case sensitivity for the file system and forces the output to be in case-insensitive order.
    /// </summary>
    SortCaseInsensitively = 1 << 10,
    /// <summary>
    /// Indicates that rename detection should include rewritten files.
    /// </summary>
    RenamesFromRewrites = 1 << 11,
    /// <summary>
    /// Bypasses the default status behavior of doing a "soft" index reload (i.e. reloading the index data if the file on disk has been modified outside libgit2).
    /// </summary>
    NoRefresh = 1 << 12,
    /// <summary>
    /// Tells libgit2 to refresh the stat cache in the index for files that are unchanged but have out of date stat einformation in the index.
    /// It will result in less work being done on subsequent calls to get status. This is mutually exclusive with the NO_REFRESH option.
    /// </summary>
    UpdateIndex = 1 << 13,
    /// <summary>
    /// Normally files that cannot be opened or read are ignored as these are often transient files;
    /// this option will return unreadable files as <see cref="GitStatusFlags.WorkingTreeUnreadable"/>
    /// </summary>
    IncludeUnreadable = 1 << 14,
    /// <summary>
    /// Unreadable files will be detected and given the status untracked instead of unreadable.
    /// </summary>
    IncludeUnreadableAsUntracked = 1 << 15,

    /// <summary>
    /// The default flags
    /// </summary>
    Defaults = IncludeIgnored | IncludeUntracked | RecurseUntrackedDirectories
}

/// <summary>
/// Select the files on which to report status.
/// </summary>
/// <remarks>
/// With git_status_foreach_ext(), this will control which changes get callbacks. <br/>
/// With git_status_list_new(), these will control which changes are included in the list.
/// </remarks>
public enum GitStatusShow
{
    /// <summary>
    /// The default. This roughly matches <c>git status --porcelain</c> regarding which files are included and in what order.
    /// </summary>
    IndexAndWorkDir,
    /// <summary>
    /// Only gives status based on HEAD to index comparison, not looking at working directory changes.
    /// </summary>
    IndexOnly,
    /// <summary>
    /// Only gives status based on index to working directory comparison, not comparing the index to the HEAD.
    /// </summary>
    WorkDirOnly,
}

public enum GitSubmoduleIgnoreType
{
    Unspecified = -1,
    None = 1,
    Untracked = 2,
    Dirty = 3,
    All = 4
}

public enum GitSubmoduleRecurseType
{
    No = 0,
    Yes = 1,
    OnDemand = 2,
}

[Flags]
public enum GitSubmoduleStatusFlags
{
    InHead = 1,
    InIndex = 1 << 1,
    InConfig = 1 << 2,
    InWorkingDirectory = 1 << 3,
    IndexAdded = 1 << 4,
    IndexDeleted = 1 << 5,
    IndexModified = 1 << 6,
    WorkingDirectoryUninitialized = 1 << 7,
    WorkingDirectoryAdded = 1 << 8,
    WorkingDirectoryDeleted = 1 << 9,
    WorkingDirectoryModified = 1 << 10,
    WorkingDirectoryIndexModified = 1 << 11,
    WorkingDirectoryWorkingDirectoryModified = 1 << 12,
    WorkingDirectoryUntracked = 1 << 13,
}

public static class GitSubmoduleStatusFlagsExtensions
{
    const GitSubmoduleStatusFlags GIT_SUBMODULE_STATUS__IN_FLAGS = (GitSubmoduleStatusFlags)0x000Fu;
    const GitSubmoduleStatusFlags GIT_SUBMODULE_STATUS__INDEX_FLAGS = (GitSubmoduleStatusFlags)0x0070u;
    const GitSubmoduleStatusFlags GIT_SUBMODULE_STATUS__WD_FLAGS = (GitSubmoduleStatusFlags)0x3F80u;

    public static bool IsUnmodified(this GitSubmoduleStatusFlags flags)
    {
        return (flags & ~GIT_SUBMODULE_STATUS__IN_FLAGS) == 0;
    }

    public static bool IsIndexUnmodified(this GitSubmoduleStatusFlags flags)
    {
        return (flags & GIT_SUBMODULE_STATUS__INDEX_FLAGS) == 0;
    }

    public static bool IsWorkingDirectoryUnmodified(this GitSubmoduleStatusFlags flags)
    {
        const GitSubmoduleStatusFlags check = GIT_SUBMODULE_STATUS__WD_FLAGS
            & ~GitSubmoduleStatusFlags.WorkingDirectoryUninitialized;

        return (flags & check) == 0;
    }

    public static bool IsWorkingDirectoryDirty(this GitSubmoduleStatusFlags flags)
    {
        const GitSubmoduleStatusFlags check = GitSubmoduleStatusFlags.IndexModified
            | GitSubmoduleStatusFlags.WorkingDirectoryWorkingDirectoryModified
            | GitSubmoduleStatusFlags.WorkingDirectoryUntracked;

        return (flags & check) != 0;
    }
}

public enum GitSubmoduleUpdateType
{
    Default = 0,
    Checkout = 1,
    Rebase = 2,
    Merge = 3,
    None = 4,
}

public enum GitTraceLevel
{
    None = 0,
    Fatal,
    Error,
    Warn,
    Info,
    Debug,
    Trace
}

public enum GitTreeUpdateType
{
    UpdateOrInsert,
    Remove
}

public enum GitTreeWalkMode
{
    PreOrder,
    PostOrder
}

/// <summary>
/// Flags which can be passed to git_worktree_prune to alter its
/// behavior.
/// </summary>
[Flags]
public enum GitWorktreePruneFlags
{
    /// <summary>
    /// Prune working tree even if working tree is valid
    /// </summary>
    Valid = 1,
    /// <summary>
    /// Prune working tree even if it is locked
    /// </summary>
    Locked = 1 << 1,
    /// <summary>
    /// Prune checked out working tree
    /// </summary>
    WorkingTree = 1 << 2,
}