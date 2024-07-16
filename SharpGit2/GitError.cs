﻿namespace Server.Scripting.Git;

public enum GitError : int
{
    /// <summary>
    /// No Error
    /// </summary>
    OK = 0,

    /// <summary>
    /// Generic Error
    /// </summary>
    Error = -1,

    /// <summary>
    /// Requested object could not be found
    /// </summary>
    NotFound = -3,

    /// <summary>
    /// Object exists preventing operation
    /// </summary>
    Exists = -4,

    /// <summary>
    /// More than one object matches
    /// </summary>
    Ambiguous = -5,

    /// <summary>
    /// Output buffer too short to hold data
    /// </summary>
    BufferTooShort = -6,

    /// <summary>
    /// GIT_EUSER is a special error that is never generated by libgit2
    /// code. You can return it from a callback (e.g to stop an iteration)
    /// to know that it was generated by the callback and not by libgit2.
    /// </summary>
    User = -7,

    /// <summary>
    /// Operation not allowed on bare repository
    /// </summary>
    BareRepo = -8,

    /// <summary>
    /// HEAD refers to branch with no commits
    /// </summary>
    UnbornBranch = -9,

    /// <summary>
    /// Merge in progress prevented operation
    /// </summary>
    Unmerged = -10,

    /// <summary>
    /// Reference was not fast-forwardable
    /// </summary>
    NonFastForeward = -11,

    /// <summary>
    /// Name/ref spec was not in a valid format
    /// </summary>
    InvalidSpec = -12,

    /// <summary>
    /// Checkout conflicts prevented operation
    /// </summary>
    Conflict = -13,

    /// <summary>
    /// Lock file prevented operation
    /// </summary>
    Locked = -14,

    /// <summary>
    /// Reference value does not match expected
    /// </summary>
    Modified = -15,

    /// <summary>
    /// Authentication error
    /// </summary>
    Authentication = -16,

    /// <summary>
    /// Server certificate is invalid
    /// </summary>
    Certificate = -17,

    /// <summary>
    /// Patch/merge has already been applied
    /// </summary>
    Applied = -18,

    /// <summary>
    /// The requested peel operation is not possible
    /// </summary>
    Peel = -19,

    /// <summary>
    /// Unexpected EOF
    /// </summary>
    EOF = -20,

    /// <summary>
    /// Invalid Operation or Input
    /// </summary>
    Invalid = -21,

    /// <summary>
    /// Uncommitted changes in index prevented operation
    /// </summary>
    Uncommitted = -22,

    /// <summary>
    /// The operation is not valid for a directory
    /// </summary>
    Directory = -23,

    /// <summary>
    /// A merge conflict exists and cannot continue
    /// </summary>
    MergeConflict = -24,

    /// <summary>
    /// A user-configured callback refused to act
    /// </summary>
    PassThrough = -30,

    /// <summary>
    /// Signals the end of iteration with an iterator
    /// </summary>
    IterationOver = -31,

    /// <summary>
    /// Internal only
    /// </summary>
    Retry = -32,

    /// <summary>
    /// Hashsum mismatch in object
    /// </summary>
    Mismatch = -33,

    /// <summary>
    /// Unsaved changes in the index would be overwritten
    /// </summary>
    IndexDirty = -34,

    /// <summary>
    /// Patch application failed
    /// </summary>
    ApplyFail = -35,

    /// <summary>
    /// The object is not owned by the current user
    /// </summary>
    Owner = -36,

    /// <summary>
    /// The operation timed out
    /// </summary>
    TimeOut = -37,

    /// <summary>
    /// There were no changes
    /// </summary>
    Unchanged = -38,

    /// <summary>
    /// An option is not supported
    /// </summary>
    NotSupported = -39,

    /// <summary>
    /// The subject is read-only
    /// </summary>
    ReadOnly = -40,
}