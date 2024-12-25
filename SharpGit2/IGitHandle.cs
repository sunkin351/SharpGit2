namespace SharpGit2;

/// <summary>
/// A helper interface for a specific generic function
/// </summary>
public unsafe interface IGitHandle : IDisposable
{
    /// <summary>
    /// Is the native object null?
    /// </summary>
    bool IsNull { get; }
}
