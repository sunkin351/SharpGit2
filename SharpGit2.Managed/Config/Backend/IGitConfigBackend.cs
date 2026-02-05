using System.Text.RegularExpressions;

namespace SharpGit2.Managed.Config.Backend;

public interface IGitConfigBackend : IDisposable, IEnumerable<GitConfigEntry>
{
    bool IsReadOnly { get; }

    GitConfig Config { get; set; }

    void Open(GitConfigLevel level, GitRepository? repo);

    GitConfigEntry? Get(string key);

    void Set(string key, string value);

    void SetMultiVar(string key, Regex regex, string value);

    bool Delete(string key);

    bool DeleteMultiVar(string key, Regex regex);

    IGitConfigBackend Snapshot();

    void Lock();

    void Unlock(bool success);
}

