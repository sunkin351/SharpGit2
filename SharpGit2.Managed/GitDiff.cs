using System.Collections;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using JetBrains.Annotations;

namespace SharpGit2.Managed;

[PublicAPI]
internal enum GitDiffOrigin
{
    Unknown = 0,
    Generated,
    Parsed
}

/// <summary>
/// When producing a binary diff, the binary data returned will be
/// either the deflated full ("literal") contents of the file, or
/// the deflated binary delta between the two sides (whichever is
/// smaller).
/// </summary>
[PublicAPI]
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

[Flags, PublicAPI]
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
[Flags, PublicAPI]
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

[Flags, PublicAPI]
public enum GitDiffFormatEmailFlags
{
    None = 0,
    ExcludeSubjectPatchMarker = 1,
}

[PublicAPI]
public enum GitDiffFormatType : uint
{
    Patch = 1,
    PatchHeader = 2,
    Raw = 3,
    NameOnly = 4,
    NameStatus = 5,
    PatchID = 6
}

[PublicAPI]
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
[Flags, PublicAPI]
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

[PublicAPI]
public enum GitDiffStatsFormat
{
    None = 0,
    Full = 1 << 0,
    Short = 1 << 1,
    Number = 1 << 2,
    IncludeSummary = 1 << 3,
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
[PublicAPI]
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

[PublicAPI]
public struct GitDiffFile
{
    public GitObjectID Id;
    public string? Path;
    public long Size;
    public GitDiffFlags Flags;
    public GitFileMode Mode;
    public ushort IdAbbreviation;
}

[PublicAPI]
public sealed class GitDiffDelta
{
    public GitDeltaType Status { get; }

    public GitDiffFlags Flags { get; }

    public ushort Similarity { get; }

    public ushort FileCount { get; }

    public readonly GitDiffFile OldFile, NewFile;

    private string? GetPath()
    {
        string? path = OldFile.Path;

        if (path is null || this.Status is GitDeltaType.Added or GitDeltaType.Renamed or GitDeltaType.Copied)
        {
            path = NewFile.Path;
        }

        return path;
    }

    internal sealed class Comparer : IComparer<GitDiffDelta>
    {
        public static readonly Comparer CaseSensitive = new(StringComparison.Ordinal);
        public static readonly Comparer CaseInsensitive = new(StringComparison.OrdinalIgnoreCase);

        private readonly StringComparison comparison;

        private Comparer(StringComparison comparison)
        {
        }

        public int Compare(GitDiffDelta? x, GitDiffDelta? y)
        {
            if (x is null)
            {
                return y is null ? 0 : 1;
            }
            else if (y is null)
            {
                return -1;
            }

            int cmp = string.Compare(x.GetPath(), y.GetPath(), comparison);

            return cmp != 0 ? cmp : (int)x.Status - (int)y.Status;
        }
    }
}

[PublicAPI]
public sealed class GitDiff : IEnumerable<GitDiffDelta>
{
    private readonly GitDiffOrigin Type;
    private readonly ImmutableArray<GitDiffDelta> _deltas;

    public IEnumerator<GitDiffDelta> GetEnumerator()
    {
        if (_deltas.IsDefault || _deltas.Length == 0)
            return Enumerable.Empty<GitDiffDelta>().GetEnumerator();

        return ((IEnumerable<GitDiffDelta>)ImmutableCollectionsMarshal.AsArray(_deltas)!).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    private struct PatchIDArgs
    {
        public GitDiff diff;
        public HashAlgorithm HashContext;
        public GitObjectID Result;
        public GitObjectIDType ObjectIDType;
        public int FirstFile;
    }
}
