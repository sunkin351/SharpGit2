using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SharpGit2.Managed;

internal static class Constants
{
    public const string attribute_internal_true = "[internal]__TRUE__";
    public const string attribute_internal_false = "[internal]__FALSE__";
    public const string attribute_internal_unset = "[internal]__UNSET__";

    public const long MaxAttributeFileSize = 100 * 1024 * 1024;

    public const int GitRepositoryMaxVersion = 1;

    public static char PathListSeparator => OperatingSystem.IsWindows() ? ';' : ':';

    public const string DotGit = ".git";
    
    public const string GitDirectoryShortname = "GIT~1";

    public const UnixFileMode FileModeAllPermissions = (UnixFileMode)(7 * 64 + 7 * 8 + 7); // 0777
    public const UnixFileMode FileModeReadWritePermissions = (UnixFileMode)(6 * 64 + 6 * 8 + 6); // 0666

    public const string GitHeadFile = "HEAD";
    public const string GitDirFile = "gitdir";

    public const string ObjectsDir = "objects/";
    public const UnixFileMode ObjectsDirMode = FileModeAllPermissions;

    public const string RefsDir = "refs/";
    public const string RefsHeadsDir = RefsDir + "heads/";
    public const string RefsTagsDir = RefsDir + "tags/";
    public const string RefsRemotesDir = RefsDir + "remotes/";
    public const string RefsNotesDir = RefsDir + "notes/";
    public const string ReflogDir = "logs/";
    
    public const UnixFileMode RefsDirMode = FileModeAllPermissions;
    public const UnixFileMode RefsFileMode = FileModeReadWritePermissions;

    public const UnixFileMode ReflogDirMode = FileModeAllPermissions;
    public const UnixFileMode ReflogMode = FileModeReadWritePermissions;

    public const string RenamedRefFile = RefsDir + "RENAMED-REF";

    public const string GitFileContentPrefix = "gitdir:";
    public const string GitSymRef = "ref: ";

    public static readonly Encoding UTF8NoBOM = new UTF8Encoding(false);
    public const int RepositoryVersionDefault = 0;

    public const UnixFileMode DefaultFileMode = FileModeReadWritePermissions;

    public const string GitDefaultBranch = "master";

    public const int GitAbbrevDefault = 7;

    public const string GitRemoteOrigin = "origin";
    public const int GitAutoCRLFDefault = 0;

    public const int MaxReferenceNestingLevel = 10;
    public const int DefaultReferenceNestingLevel = 5;

    internal static readonly GitObjectID EmptyBlobSHA1 = new GitObjectID(
        GitObjectIDType.SHA1,
        [0xe6, 0x9d, 0xe2, 0x9b, 0xb2, 0xd1, 0xd6, 0x43, 0x4b, 0x8b,
            0x29, 0xae, 0x77, 0x5a, 0xd8, 0xc2, 0xe4, 0x8c, 0x53, 0x91]);
    
    internal static readonly GitObjectID EmptyTreeSHA1 = new GitObjectID(
        GitObjectIDType.SHA1,
        [0x4b, 0x82, 0x5d, 0xc6, 0x42, 0xcb, 0x6e, 0xb9, 0xa0, 0x60,
            0xe5, 0x4b, 0xf8, 0xd6, 0x92, 0x88, 0xfb, 0xee, 0x49, 0x04]);

    [DoesNotReturn]
    internal static void ThrowWindowsPlatformNotSupported()
    {
        throw new PlatformNotSupportedException("Only Windows kernel version 6.1+ is supported! (Windows 7+)");
    }
}
