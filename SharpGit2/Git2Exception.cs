namespace SharpGit2;

public class Git2Exception : ApplicationException
{
    public GitError ErrorCode { get; }

    public GitErrorClass ErrorClass { get; }

    public Git2Exception(GitError code, GitErrorClass errorClass, string message) : base(message)
    {
        ErrorCode = code;
        ErrorClass = errorClass;
    }
}
