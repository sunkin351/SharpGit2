using System.Diagnostics;
using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed.Attributes;

internal ref struct GitAttributePath
{
    public ReadOnlySpan<char> FullPath;
    public ReadOnlySpan<char> RelativePath;
    public ReadOnlySpan<char> Basename;
    public bool IsDirectory;

    public GitAttributePath(string path, string? @base, bool? dirFlag)
    {
        string fullPath = PathUtilities.JoinUnrooted(path, @base, out int root);

        Debug.Assert(root >= 0);

        FullPath = fullPath.AsSpan().TrimEnd('/');
        RelativePath = fullPath.AsSpan(root).TrimStart('/');
        Basename = Path.GetFileName(RelativePath);
        if (Basename.IsEmpty)
            Basename = RelativePath;

        if (dirFlag.HasValue)
        {
            IsDirectory = dirFlag.GetValueOrDefault();
        }
        else
        {
            IsDirectory = Directory.Exists(fullPath);
        }
    }

}
