using JetBrains.Annotations;

namespace SharpGit2.Managed;

[PublicAPI]
public static class GitConfigMapValues
{
    public const int SafeCrlfFalse = 0;
    public const int SafeCrlfFail = 1;
    public const int SafeCrlfWarn = 2;

    public const int AutoCrlfFalse = 0;
    public const int AutoCrlfTrue = 1;
    public const int AutoCrlfInput = 2;
    public const int AutoCrlfDefault = AutoCrlfFalse;

    public const int Eol_Unset = 0,
        Eol_CRLF = 1,
        Eol_LF = 2;
}