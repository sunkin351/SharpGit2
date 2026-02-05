using CommunityToolkit.HighPerformance.Buffers;

namespace SharpGit2.Managed.Worktree;

public sealed class GitWorktree
{
    internal readonly string Gitlink_Path;

    private WeakReference<GitRepository>? _worktreeRepository;

    internal static string? ReadLink(string @base, string file)
    {
        string result;

        using (var buffer = new ArrayPoolBufferWriter<char>())
        {
            try
            {
                File.ReadAllText(Path.Combine(@base, file), buffer);
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            result = buffer.WrittenSpan.TrimEnd().ToString();
        }

        if (result.StartsWith("./") || result.StartsWith("../"))
        {
            result = Path.GetFullPath(result, @base);
            if (OperatingSystem.IsWindows())
            {
                result = result.Replace('\\', '/');
            }
        }

        return result;
    }
}
