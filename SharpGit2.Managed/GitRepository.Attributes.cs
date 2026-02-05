using System.Diagnostics;
using System.Runtime.CompilerServices;
using SharpGit2.Managed.Attributes;
using SharpGit2.Managed.Config;
using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed;

using Path = System.IO.Path;

public partial class GitRepository
{
    internal volatile int AttributeSessionKey;
    private volatile GitAttributeCache? _attributeCache;

    /// <summary>
    /// Gets the attribute cache for this repository, or null if it hasn't been initialized yet.
    /// </summary>
    internal GitAttributeCache? AttributeCacheUnsafe => _attributeCache;

    /// <summary>
    /// Gets the attribute cache for this repository. Triggers lazy initialization when necessary.
    /// </summary>
    internal GitAttributeCache AttributeCache => _attributeCache ?? InitAttributeCache();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private GitAttributeCache InitAttributeCache()
    {
        var cache = new GitAttributeCache(this);

        var config = this.GetConfigSnapshot();

        cache.ConfigAttributeFile = LookupPath(config, GitAttributeCache.AttributeConfig, GitAttributeFile.FileNameXDG);
        cache.ConfigExcludeFile = LookupPath(config, GitAttributeCache.IgnoreConfig, "ignore");

        var returnedValue = Interlocked.CompareExchange(ref _attributeCache, cache, null);
        if (returnedValue == null) // Did we win the race to init the field? null means yes
        {
            this.AddAttributeMacro("binary", "-diff -merge -text -crlf");

            returnedValue = cache;
        }

        return returnedValue;

        static string? LookupPath(GitConfig config, string key, string fallback)
        {
            var entry = config.LookupEntry(key, false);

            string? path = null;

            if (entry is not null)
            {
                string? configValue = entry.Value;

                if (configValue is not null)
                {
                    if (configValue.StartsWith("~/"))
                    {
                        path = SystemDirectory.ExpandHomeDirectoryFile(configValue.AsSpan(2)) ?? configValue;
                    }
                    else
                    {
                        path = configValue;
                    }
                }
            }
            else
            {
                path = SystemDirectory.FindXDGFile(fallback, false);
            }

            return path;
        }
    }

    private List<GitAttributeFile> CollectAttributeFiles(GitAttributeSession? session, in GitAttributeOptions options, string path)
    {
        string? workDir = this.WorkingDirectory;

        GitAttributeCache cache;

        Debug.Assert(!Path.IsPathFullyQualified(path));

        AttributeSetup(session, in options);

        string directory = Path.GetDirectoryName(workDir is null ? Path.GetFullPath(path) : Path.GetFullPath(path, workDir))!;

        List<GitAttributeFile> list = [];
        string attributeFile = this.GetItemPath(GitRepositoryItemType.Info);
        PushAttributeFile(session, list, attributeFile, GitAttributeFile.FileNameInRepo);

        var info = new AttributeWalkUpInfo(this, session, options, workDir, this.GetIndex(), list);

        if (directory is ".")
        {
            info.PushOne("");
        }
        else
        {
            foreach (var wuPath in GitPath.WalkUp(directory, workDir))
            {
                info.PushOne(wuPath.ToString()); // Potentially unnecessary allocation, will need further work to remove
            }
        }

        string? attributeConfigFile = this.AttributeCacheUnsafe?.AttributesFile;

        if (attributeConfigFile is not null)
        {
            PushAttributeFile(session, list, null, attributeConfigFile);
        }

        if ((options.Flags & GitAttributeCheckFlags.NoSystem) == 0)
        {
            var dir = SystemAttributeFile(session);

            if (dir is not null)
            {
                PushAttributeFile(session, list, null, dir);
            }
        }

        return list;
    }

    private void AttributeSetup(GitAttributeSession? session, in GitAttributeOptions options)
    {
        if (session is { InitSetup: true })
            return;

        string? system = SystemAttributeFile(session);
        if (system is not null)
        {
            // preload system attribute file
            PreloadAttributeFile(session, null, system);
        }

        var cache = this.AttributeCache;

        PreloadAttributeFile(session, null, cache.AttributesFile);

        var info = GetItemPath(GitRepositoryItemType.Info);
        PreloadAttributeFile(session, info, GitAttributeFile.FileNameInRepo);

        string? wd = this.WorkingDirectory;
        if (wd is not null)
        {
            PreloadAttributeFile(session, wd, GitAttributeFile.FileName);
        }

        var source = new GitAttributeFile.Source() { Type = GitAttributeFile.SourceType.Index, FileName = GitAttributeFile.FileName };
        PreloadAttributeSource(session, ref source);

        if ((options.Flags & GitAttributeCheckFlags.IncludeHead) != 0)
        {
            source.Type = GitAttributeFile.SourceType.Head;
            PreloadAttributeSource(session, ref source);
        }

        if ((options.Flags & GitAttributeCheckFlags.IncludeCommit) != 0)
        {
            source.Type = GitAttributeFile.SourceType.Commit;
            source.CommitId = options.AttributeCommitId;
            PreloadAttributeSource(session, ref source);
        }

        session?.InitSetup = true;
    }

    internal static string? SystemAttributeFile(GitAttributeSession? session)
    {
        if (session is null)
        {
            return SystemDirectory.FindSystemFile(GitAttributeFile.FileNameSystem, false);
        }

        if (!session.InitSysDir)
        {
            session.SysDir = SystemDirectory.FindSystemFile(GitAttributeFile.FileNameSystem, false);
            session.InitSysDir = true;
        }

        return session.SysDir;
    }

    private void PreloadAttributeFile(GitAttributeSession? session, string? @base, string filename)
    {
        if (filename is null)
            return;

        var source = new GitAttributeFile.Source(GitAttributeFile.SourceType.File, filename, @base);

        PreloadAttributeSource(session, in source);
    }

    private void PreloadAttributeSource(GitAttributeSession? session, in GitAttributeFile.Source source)
    {
        this.AttributeCache.Get(session, in source, GitAttributeFile.ParseBuffer, true);
    }

    private void PushAttributeFile(GitAttributeSession? session, List<GitAttributeFile> list, string? @base, string filename)
    {
        var source = new GitAttributeFile.Source(GitAttributeFile.SourceType.File, filename, @base);

        PushAttributeSource(session, list, in source, true);
    }

    private void PushAttributeSource(GitAttributeSession? session, List<GitAttributeFile> list, in GitAttributeFile.Source source, bool allow_macros)
    {
        var file = this.AttributeCache.Get(session, in source, GitAttributeFile.ParseBuffer, allow_macros);

        if (file != null)
        {
            list.Add(file);
        }
    }

    private struct AttributeWalkUpInfo
    {
        public GitRepository Repo { get; }
        public GitAttributeSession? Session { get; }
        public GitAttributeOptions Options { get; }
        public string? WorkingDirectory { get; }
        public GitIndex? Index { get; }
        public List<GitAttributeFile> Files { get; }

        [InlineArray(5)]
        private struct SourcesArray
        {
            public GitAttributeFile.SourceType _element;
        }

        private int _sourceCount;
        private SourcesArray _sources;

        public AttributeWalkUpInfo(GitRepository repo, GitAttributeSession? session, GitAttributeOptions options, string? workingDirectory, GitIndex? index, List<GitAttributeFile> files)
        {
            Repo = repo;
            Session = session;
            Options = options;
            WorkingDirectory = workingDirectory;
            Index = index;
            Files = files;

            _sourceCount = DecideSources(_sources);
        }

        public void PushOne(string path)
        {
            Span<GitAttributeFile.SourceType> src = _sources;

            src = src.Slice(0, _sourceCount);

            bool allow_macros = WorkingDirectory is not null && path == WorkingDirectory;

            for (int i = 0; i < src.Length; ++i)
            {
                var source = new GitAttributeFile.Source(src[i], GitAttributeFile.FileName, path);

                if (source.Type == GitAttributeFile.SourceType.Commit)
                {
                    source.CommitId = Options.AttributeCommitId;
                }

                Repo.PushAttributeSource(Session, Files, in source, allow_macros);
            }
        }

        private int DecideSources(Span<GitAttributeFile.SourceType> sources)
        {
            var flags = Options.Flags;
            var hasWd = WorkingDirectory is not null;
            var hasIndex = Index is not null;

            int count = 0;

            switch (flags & (GitAttributeCheckFlags.IndexThenFile | GitAttributeCheckFlags.IndexOnly))
            {
                case GitAttributeCheckFlags.FileThenIndex:
                    if (hasWd)
                        sources[count++] = GitAttributeFile.SourceType.File;
                    if (hasIndex)
                        sources[count++] = GitAttributeFile.SourceType.Index;

                    break;
                case GitAttributeCheckFlags.IndexThenFile:
                    if (hasIndex)
                        sources[count++] = GitAttributeFile.SourceType.Index;
                    if (hasWd)
                        sources[count++] = GitAttributeFile.SourceType.File;

                    break;

                case GitAttributeCheckFlags.IndexOnly:
                    if (hasIndex)
                        sources[count++] = GitAttributeFile.SourceType.Index;

                    break;
            }

            if ((flags & GitAttributeCheckFlags.IncludeHead) != 0)
            {
                sources[count++] = GitAttributeFile.SourceType.Head;
            }

            if ((flags & GitAttributeCheckFlags.IncludeCommit) != 0)
            {
                sources[count++] = GitAttributeFile.SourceType.Commit;
            }

            return count;
        }
    }
}
