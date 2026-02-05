namespace SharpGit2.Managed;

public partial class GitRepository
{
    private const string ObjectsInfoDir = Constants.ObjectsDir + "info/";
    private const string ObjectsPackDir = Constants.ObjectsDir + "pack/";

    private const string HooksDir = "hooks/";
    private const UnixFileMode HooksDirMode = (UnixFileMode)(7 * 64 + 7 * 8 + 7); //0777

    private const string HooksReadmeFile = HooksDir + "README.sample";
    private const UnixFileMode HooksReadmeMode = (UnixFileMode)(7 * 64 + 7 * 8 + 7); //0777

    private const string HooksReadmeContent = """
        #!/bin/sh
        #
        # Place appropriately named executable hook scripts into this directory
        # to intercept various actions that git takes.  See `git help hooks` for
        # more information.

        """;

    private const string InfoDir = "info/";
    private const UnixFileMode InfoDirMode = (UnixFileMode)(7 * 64 + 7 * 8 + 7); //0777

    private const string InfoExcludeFile = InfoDir + "exclude";
    private const UnixFileMode InfoExcludeMode = (UnixFileMode)(6 * 64 + 6 * 8 + 6); //0666

    private const string InfoExcludeContent = """
        # File patterns to ignore; see `git help ignore` for more information.
        # Lines that start with '#' are comments.

        """;

    private const string DescFile = "description";
    private const UnixFileMode DescMode = (UnixFileMode)(6 * 64 + 6 * 8 + 6); //0666

    private const string DescContent = """
        Unnamed repository; edit this file 'description' to name the repository.

        """;

    private static readonly (string Path, UnixFileMode Mode, string? Content)[] RepoTemplates = [
        (ObjectsInfoDir, Constants.ObjectsDirMode, null),
        (ObjectsPackDir, Constants.ObjectsDirMode, null),
        (Constants.RefsHeadsDir, Constants.RefsDirMode, null),
        (Constants.RefsTagsDir, Constants.RefsDirMode, null),
        (HooksDir, HooksDirMode, null),
        (InfoDir, InfoDirMode, null),
        (DescFile, DescMode, DescContent.ReplaceLineEndings("\n")),
        (HooksReadmeFile, HooksReadmeMode, HooksReadmeContent.ReplaceLineEndings("\n")),
        (InfoExcludeFile, InfoExcludeMode, InfoExcludeContent.ReplaceLineEndings("\n"))
    ];
}
