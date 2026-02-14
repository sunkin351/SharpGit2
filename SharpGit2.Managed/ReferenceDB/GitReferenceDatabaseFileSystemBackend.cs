using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using CommunityToolkit.HighPerformance.Buffers;

using SharpGit2.Managed.Config;
using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed.ReferenceDB;

/// <summary>
/// The default reference database backend, storing reference information on disk in the classic Git format.
/// </summary>
internal sealed class GitReferenceDatabaseFileSystemBackend : IGitReferenceDatabaseBackend
{
    [Flags]
    private enum PackRefFlags
    {
        HasPeel = 1,
        WasLoose = 1 << 1,
        CannotPeel = 1 << 2,
        Shadowed = 1 << 3
    }
    
    private enum Peeling
    {
        None,
        Standard,
        Full
    }

    [StructLayout(LayoutKind.Auto)]
    private struct PackRef : IComparable<PackRef>
    {
        public GitObjectID Oid;
        public GitObjectID Peel;
        public PackRefFlags Flags;
        public string Name;
        
        public int CompareTo(PackRef other)
        {
            return this.Name.CompareTo(other.Name, StringComparison.Ordinal);
        }
    }

    private const string PackedRefsFile = "packed-refs";

    private readonly GitRepository _repository;
    private string? _gitPath;
    private string _commonPath;
    private GitObjectIDType _objectIdType;
    private bool _fsync, _sorted;
    private Peeling _peelingMode;
    private GitIteratorFlags _iteratorFlags;
    private readonly Lock _lock;
    private char[]? _packedRefsData;
    private int _packedRefsDataLength;
    private DateTime _packedRefsStamp;
    private SortedCache _referenceCache;
    
    public GitReferenceDatabaseFileSystemBackend(GitRepository repo)
    {
        _repository = repo;
        _objectIdType = repo.ObjectIdType;

        if (repo.RepositoryPath != null)
        {
            this._gitPath = SetupNamespace(repo, repo.RepositoryPath);
        }

        if (repo.CommonDirectory != null)
        {
            this._commonPath = SetupNamespace(repo, repo.CommonDirectory);
        }

        _referenceCache = new SortedCache(Path.Combine(_commonPath, PackedRefsFile));

        if (repo.ConfigMapLookup(GitConfigMapItem.IgnoreCase) != 0)
        {
            _iteratorFlags |= GitIteratorFlags.IgnoreCase;
        }

        if (repo.ConfigMapLookup(GitConfigMapItem.Precompose) != 0)
        {
            _iteratorFlags |= GitIteratorFlags.PrecomposeUnicode;
        }

        _fsync = GitRepository.FSyncGitDir || repo.ConfigMapLookup(GitConfigMapItem.FSyncObjectFiles) != 0;

        _iteratorFlags |= GitIteratorFlags.DescendSymlinks;
    }

    private bool _disposed = false;
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
            return;

        if (_packedRefsData != null)
        {
            ArrayPool<char>.Shared.Return(_packedRefsData);
            _packedRefsData = null;
            _packedRefsDataLength = 0;
        }
    }

    public void Initialize(string? headTarget, UnixFileMode mode, ReferenceDatabaseBackendInitFlags flags)
    {
        if ((flags & ReferenceDatabaseBackendInitFlags.IsWorktree) == 0)
        {
            var dmode = mode switch
            {
                0 => Constants.FileModeAllPermissions,
                GitRepository.InitSharedGroup => (UnixFileMode)(7 * 64 + 7 * 8 + 5),
                GitRepository.InitSharedAll => (UnixFileMode)(7 * 64 + 7 * 8 + 7),
                _ => mode
            };

            FileSystemHelpers.CreateDirectory(Path.Combine(_gitPath, Constants.RefsHeadsDir), dmode);
            FileSystemHelpers.CreateDirectory(Path.Combine(_gitPath, Constants.RefsTagsDir), dmode);
        }

        if (headTarget != null)
        {
            string headFilePath = Path.Combine(_gitPath, Constants.GitHeadFile);
            
            if ((flags & ReferenceDatabaseBackendInitFlags.ForceHead) != 0 || !File.Exists(headFilePath))
            {
                File.WriteAllText(headFilePath, $"{Constants.GitSymRef}{headTarget}\n");
            }
        }
    }

    public bool Exists(string referenceName)
    {
        var refPath = LoosePath(_gitPath, referenceName);

        if (File.Exists(refPath))
            return true;
        
        this.PackedReload();

        return _referenceCache.Contains(referenceName);
    }

    public GitReference? Lookup(string referenceName)
    {
        GitReference? reference = this.LooseLookup(referenceName);
        
        if (reference == null)
        {
            this.PackedReload();
            
            if (_referenceCache.TryLookup(referenceName, out var entry))
            {
                reference = new GitReference(referenceName, entry.Oid, entry.Peel);
            }
        }

        return reference;
    }

    // TODO: Iterate on these enumerators more
    private struct EnumerableCommon
    {
        public readonly GitReferenceDatabaseFileSystemBackend Backend;
        public readonly SortedCache Cache;
        public readonly string? Glob;
        public readonly List<string> Loose = new();

        public EnumerableCommon(GitReferenceDatabaseFileSystemBackend backend, string? glob)
        {
            Backend = backend;
            Glob = glob;
            
            string? pathPrefix = OptimizePrefix(glob);
            
            this.LoadPaths(backend._commonPath, false, pathPrefix);

            if (backend._repository.IsWorktree)
            {
                this.LoadPaths(backend._gitPath!, true, pathPrefix);
            }
            
            backend.PackedReload();
            Cache = backend._referenceCache.Copy(true);
        }

        private void LoadPaths(string rootPath, bool worktree, string? pathPrefix)
        {
            string searchRootPath = pathPrefix != null ? Path.Combine(rootPath, pathPrefix) : rootPath;

            foreach (string path in Directory.EnumerateFiles(searchRootPath, "*", SearchOption.AllDirectories))
            {
                if (path.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
                    continue;

                string referenceName = Path.GetRelativePath(rootPath, path);

                if (OperatingSystem.IsWindows())
                    referenceName = referenceName.Replace('\\', '/');

                if (worktree)
                {
                    if (!IsPerWorktreeRef(referenceName))
                        continue;
                }
                else if (this.Backend._repository.IsWorktree && IsPerWorktreeRef(referenceName))
                {
                    continue;
                }

                if (this.Glob != null && WildMatch.Match(this.Glob, referenceName, 0) != WildMatch.Result.Match)
                    continue;
                
                referenceName = Utilities.GetPooledString(referenceName); // deduplicate if possible
                
                this.Loose.Add(referenceName);
            }
        }
    }

    public IEnumerable<GitReference> EnumerateReferences(string? glob)
    {
        return new ReferenceEnumerable(this, glob);
    }

    private sealed class ReferenceEnumerable(GitReferenceDatabaseFileSystemBackend backend, string? glob) : IEnumerable<GitReference>
    {
        private EnumerableCommon _common = new(backend, glob);

        public IEnumerator<GitReference> GetEnumerator()
        {
            var seenNames = new HashSet<string>(_common.Loose.Count);
            
            foreach (var name in _common.Loose)
            {
                if (_common.Backend.LooseLookup(name) is {} reference)
                {
                    seenNames.Add(name);
                
                    yield return reference;
                }
            }

            foreach (var (name, entry) in _common.Cache.Map)
            {
                if (seenNames.Contains(name))
                    continue;
                
                if (_common.Glob != null && WildMatch.Match(_common.Glob, name, 0) != WildMatch.Result.Match)
                {
                    continue;
                }

                yield return new GitReference(name, entry.Oid, entry.Peel);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    public IEnumerable<string> EnumerateReferenceNames(string? glob)
    {
        return new ReferenceNameEnumerable(this, glob);
    }

    private sealed class ReferenceNameEnumerable : IEnumerable<string>
    {
        private EnumerableCommon _common;

        public ReferenceNameEnumerable(GitReferenceDatabaseFileSystemBackend backend, string? glob)
        {
            _common = new EnumerableCommon(backend, glob);
        }
        
        public IEnumerator<string> GetEnumerator()
        {
            var seenNames = new HashSet<string>(_common.Loose.Count);
            
            foreach (var name in _common.Loose)
            {
                if (_common.Backend.LooseLookup_Exists(name))
                {
                    seenNames.Add(name);
                
                    yield return name;
                }
            }

            foreach (var name in _common.Cache.Map.Keys)
            {
                if (seenNames.Contains(name))
                    continue;
                
                if (_common.Glob != null && WildMatch.Match(_common.Glob, name, 0) != WildMatch.Result.Match)
                {
                    continue;
                }

                yield return name;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    private static readonly SearchValues<char> _optimizePrefix_SearchCharacters = SearchValues.Create("?*[\\");

    private static string? OptimizePrefix(string? glob)
    {
        if (string.IsNullOrEmpty(glob))
            return "refs/";

        ReadOnlySpan<char> span = glob;

        var idx = span.IndexOfAny(_optimizePrefix_SearchCharacters);

        int lastSep = (idx < 0 ? span : span.Slice(0, idx)).LastIndexOf('/');

        if (lastSep < 0)
            return "refs/";

        ReadOnlySpan<char> prefix = span.Slice(0, lastSep + 1);
        
        return prefix.StartsWith("refs/") ? prefix.ToString() : $"refs/{prefix}";
    }

    public void Write(GitReference reference, bool force, GitSignature who, string? message)
    {
        throw new NotImplementedException();
    }

    public void Write(GitReference reference, bool force, GitSignature who, string? message, string old_target)
    {
        throw new NotImplementedException();
    }

    public void Write(GitReference reference, bool force, GitSignature who, string? message, in GitObjectID old_target)
    {
        throw new NotImplementedException();
    }

    public GitReference Rename(string old_name, string new_name, bool force, GitSignature who, string? message)
    {
        throw new NotImplementedException();
    }

    public void Delete(string referenceName)
    {
        throw new NotImplementedException();
    }

    public void Delete(string referenceName, string old_target)
    {
        throw new NotImplementedException();
    }

    public void Delete(string referenceName, in GitObjectID old_target)
    {
        throw new NotImplementedException();
    }

    public void Compress()
    {
        throw new NotImplementedException();
    }

    public bool HasLog(string refname)
    {
        throw new NotImplementedException();
    }

    public void EnsureLog(string refname)
    {
        throw new NotImplementedException();
    }

    private string ReflogPath(string referenceName)
    {
        throw new NotImplementedException();
    }

    public GitReferenceLog ReflogRead(string refname)
    {
        throw new NotImplementedException();
    }

    public void ReflogWrite(GitReferenceLog reflog)
    {
        throw new NotImplementedException();
    }

    public void ReflogRename(string old_name, string new_name)
    {
        string normalized = GitReference.NormalizeReferenceName(new_name, GitReferenceFormat.AllowOneLevel);

        string oldPath = Path.Combine(_repository.RepositoryPath, Constants.ReflogDir, old_name);

        if (!Path.Exists(oldPath))
            return;
        
        string newPath = Path.Combine(_repository.RepositoryPath, Constants.ReflogDir, normalized);
        
        //var tempPath = LoosePath()
        throw new NotImplementedException();
    }

    public void ReflogDelete(string name)
    {
        var path = this.ReflogPath(name);

        // If a reference was moved downwards, eg refs/heads/br2 -> refs/heads/br2/new-name,
        // refs/heads/br2 does exist, but it's a directory. That's a valid situation.
        // Proceed only if it's a file.
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        
        this.PruneRefs(name, Constants.ReflogDir);
    }

    public object Lock(string refname)
    {
        throw new NotImplementedException();
    }

    public void Unlock(
        object payload,
        bool success,
        bool update_reflog,
        GitReference? reference,
        GitSignature? who,
        string? message)
    {
        throw new NotImplementedException();
    }

    public void Unlock(
        object payload,
        int success,
        bool update_reflog,
        GitReference? reference,
        GitSignature? who,
        string? message)
    {
        throw new NotImplementedException();
    }

    private static string SetupNamespace(GitRepository repo, string @in)
    {
        if (repo.Namespace == null)
            return @in;

        StringBuilder path = new(@in);
        if (!Path.EndsInDirectorySeparator(@in))
            path.Append('/');
        
        ReadOnlySpan<char> namespaceStr = repo.Namespace;
        foreach (var range in namespaceStr.Split('/'))
        {
            (int start, int length) = range.GetOffsetAndLength(namespaceStr.Length);

            if (length == 0)
                continue;
            
            path.Append("refs/namespaces/").Append(namespaceStr.Slice(start, length)).Append('/');
        }

        string pathStr = path.ToString();
        
        FileSystemHelpers.CreateDirectory(pathStr + "refs", Constants.FileModeAllPermissions);

        return pathStr;
    }

    private static string LoosePath(string @base, string referenceName)
    {
        string path = GitPath.PosixJoin(@base, referenceName);

        if (!GitPath.ValidateStringLengthWithSuffix(path, ".lock".Length))
        {
            throw new PathTooLongException();
        }

        return path;
    }

    private static string ReflogPath(GitRepository repo, string referenceName)
    {
        return LoosePath(
            GitPath.PosixJoin(
                referenceName == Constants.GitHeadFile
                    ? repo.RepositoryPath
                    : repo.CommonDirectory,
                Constants.ReflogDir),
            referenceName);
    }

    private void PackedReload()
    {
        int oidHexSize = _objectIdType.HexSize;

        if (_gitPath == null)
            return;

        var cache = _referenceCache;
        cache.Lock.EnterWriteLock();
        try
        {
            // TODO: potentially move this outside of the write lock (Needs more research)
            var currentWriteTime = File.GetLastWriteTimeUtc(cache.Path);
            if (currentWriteTime == cache.FileTimeStamp)
                return;

            string text;
            using (var stream = File.OpenRead(cache.Path))
            using (var reader = new StreamReader(stream))
            {
                currentWriteTime = File.GetLastWriteTimeUtc(stream.SafeFileHandle); // Possible to have changed in between now and the previous call
                text = reader.ReadToEnd();
            }
            
            cache.Clear(false);

            int scan = 0;
            while ((uint)scan < (uint)text.Length && text[scan] == '#')
            {
                scan = text.IndexOf('\n', scan);
                
                if (scan < 0)
                    goto parseFailed;
                
                scan += 1;
            }

            try
            {
                while (scan < text.Length)
                {
                    PackRef packRef = default;
                    
                    packRef.Oid = GitObjectID.Parse(text.AsSpan(scan, oidHexSize));
                    scan += oidHexSize;
                
                    if (text[scan++] != ' ')
                        goto parseFailed;

                    int eol = text.IndexOf('\n', scan);

                    if (eol < 0)
                        goto parseFailed;

                    int endOfReferenceName = text[eol - 1] == '\r' ? eol - 1 : eol;

                    // Interesting that the original implementation didn't sanity check these
                    packRef.Name = Utilities.GetPooledString(text.AsSpan(scan..endOfReferenceName));

                    scan = eol + 1;

                    if ((uint)scan < (uint)text.Length && text[scan] == '^')
                    {
                        scan += 1;
                        packRef.Peel = GitObjectID.Parse(text.AsSpan(scan, oidHexSize));
                        scan += oidHexSize;

                        if (scan < text.Length)
                        {
                            scan = text.IndexOf('\n', scan);
                            if (scan < 0)
                                goto parseFailed;
                            scan += 1;
                        }

                        packRef.Flags |= PackRefFlags.HasPeel;
                    }
                    else if (_peelingMode == Peeling.Full || (_peelingMode == Peeling.Standard && packRef.Name.StartsWith(Constants.RefsTagsDir)))
                    {
                        packRef.Flags |= PackRefFlags.CannotPeel;
                    }
                    
                    cache.InsertOrUpdate(packRef);
                }
            }
            catch (Exception e)
            {
                if (e is NullReferenceException or IndexOutOfRangeException)
                    throw; // Bug in the code, let that pass as itself
                
                goto parseFailed;
            }

            cache.FileTimeStamp = currentWriteTime;
            return;
        
        parseFailed:
            cache.Clear(false);
            throw new Git2Exception("Corrupted packed references file!");
        }
        finally
        {
            cache.Lock.ExitWriteLock();
        }
    }

    private ReadOnlySpan<char> PackedSetPeelingMode(ReadOnlySpan<char> data)
    {
        const string traits_header = "# pack-refs with:";

        var peelingMode = Peeling.None;
        
        if (data.StartsWith(traits_header))
        {
            const string sorted = " sorted ", peeled = " peeled ", fully_peeled = " fully-peeled ";

            data = data.Slice(traits_header.Length);

            int eol = data.IndexOf('\n');
            if (eol < 0)
                return null;

            var dataTmp = data[..eol];
            
            if (dataTmp.Contains(fully_peeled, StringComparison.Ordinal))
            {
                peelingMode = Peeling.Full;
            }
            else if (dataTmp.Contains(peeled, StringComparison.Ordinal))
            {
                peelingMode = Peeling.Standard;
            }
            
            _sorted = dataTmp.Contains(sorted, StringComparison.Ordinal);
            data = data[(eol + 1)..];
        }

        _peelingMode = peelingMode;
        return data;
    }

    private static GitObjectID LooseParseObjectID(string filename, ReadOnlySpan<char> fileContent, GitObjectIDType oidType)
    {
        GitObjectID result = default;
        int hexSize = oidType.HexSize;
        
        if (fileContent.Length < hexSize
            || !GitObjectID.TryParse(fileContent.Slice(0, hexSize), out result, oidType)
            || (fileContent.Length > hexSize && !char.IsWhiteSpace(fileContent[hexSize])))
        {
            ThrowCorruptedLooseReference(filename);
        }

        return result;
    }


    private static ReadOnlySpan<char> LooseParseSymbolic(ReadOnlySpan<char> fileContent, string filename)
    {
        if (fileContent.Length <= Constants.GitSymRef.Length || !fileContent.StartsWith(Constants.GitSymRef))
        {
            ThrowCorruptedLooseReference(filename);
        }

        var referenceName = fileContent.Slice(Constants.GitSymRef.Length);

        if (!GitReference.IsReferenceNameValid(referenceName, GitReferenceFormat.Normal)) // Sanity check
        {
            ThrowCorruptedLooseReference(filename);
        }
        
        return referenceName;
    }
    
    private static void LooseReadBuffer(IBufferWriter<char> buffer, string @base, string path)
    {
        File.ReadAllText(LoosePath(@base, path), buffer);
    }

    private void LooseLookupToPackFile(string name)
    {
        using var buffer = new ArrayPoolBufferWriter<char>();

        try
        {
            LooseReadBuffer(buffer, _gitPath, name);
        }
        catch
        {
             // if we fail to load the loose reference, assume someone changed
             // the filesystem under us and skip it...
             return;
        }

        var fileData = buffer.WrittenSpan;

        // skip symbolic links
        if (fileData.StartsWith(Constants.GitSymRef))
            return;

        var packref = new PackRef()
        {
            Name = name,
            Flags = PackRefFlags.WasLoose,
            Oid = LooseParseObjectID(name, fileData, _objectIdType)
        };

        var cache = _referenceCache;
        
        cache.Lock.EnterWriteLock();
        try
        {
            cache.InsertOrUpdate(packref);
        }
        finally
        {
            cache.Lock.ExitWriteLock();
        }
    }

    private void PackedLoadLoose()
    {
        foreach (string path in Directory.EnumerateFiles(Path.Combine(_gitPath, Constants.RefsDir), "*", SearchOption.AllDirectories)
                 /*new FileSystemEnumerable<string>(Path.Combine(_gitPath, Constants.RefsDir), (ref entry) => entry.ToFullPath())
            {
               ShouldIncludePredicate = (ref entry) => !entry.IsDirectory,
               ShouldRecursePredicate = (ref entry) => true
            }*/)
        {
            if (path.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
                continue;

            string referenceName = Path.GetRelativePath(_gitPath, path);

            if (OperatingSystem.IsWindows())
                referenceName = referenceName.Replace('\\', '/');

            this.LooseLookupToPackFile(Utilities.GetPooledString(referenceName));
        }
    }

    private static bool IsPerWorktreeRef(ReadOnlySpan<char> referenceName)
    {
        return !referenceName.StartsWith("refs/")
               || referenceName.StartsWith("refs/bisect/")
               || referenceName.StartsWith("refs/worktree/")
               || referenceName.StartsWith("refs/rewritten/");
    }

    private bool LooseLookup_Exists(string referenceName)
    {
        // The original implementation validated the file if it existed,
        // whether it returned a git reference object or not.
        string referenceDir = IsPerWorktreeRef(referenceName) ? _gitPath! : _commonPath;

        using var fileContentBuffer = new ArrayPoolBufferWriter<char>();

        try
        {
            LooseReadBuffer(fileContentBuffer, referenceDir, referenceName);
        }
        catch (IOException e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }

        var fileContent = fileContentBuffer.WrittenSpan;
        
        // Validate file content
        if (fileContent.StartsWith(Constants.GitSymRef))
        {
            LooseParseSymbolic(fileContent.TrimEnd(), referenceName);
        }
        else
        {
            LooseParseObjectID(referenceName, fileContent, _objectIdType);
        }

        return true;
    }
    
    private GitReference? LooseLookup(string referenceName)
    {
        string referenceDir = IsPerWorktreeRef(referenceName) ? _gitPath! : _commonPath;

        using var fileContentBuffer = new ArrayPoolBufferWriter<char>();

        try
        {
            LooseReadBuffer(fileContentBuffer, referenceDir, referenceName);
        }
        catch (IOException e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }

        var fileContent = fileContentBuffer.WrittenSpan;
        
        if (fileContent.StartsWith(Constants.GitSymRef))
        {
            string target = Utilities.GetPooledString(LooseParseSymbolic(fileContent.TrimEnd(), referenceName));

            return new GitReference(referenceName, target);
        }
        else
        {
            var oid = LooseParseObjectID(referenceName, fileContent, _objectIdType);
            
            return new GitReference(referenceName, oid, null);
        }
    }

    private void PruneRefs(string referenceName, string? prefix)
    {
        referenceName = GitPath.SquashSlashes(referenceName);
        
        string relativePath;

        if (referenceName.StartsWith(Constants.RefsHeadsDir))
        {
            relativePath = Constants.RefsHeadsDir;
        }
        else if (referenceName.StartsWith(Constants.RefsTagsDir))
        {
            relativePath = Constants.RefsTagsDir;
        }
        else if (referenceName.StartsWith(Constants.RefsRemotesDir))
        {
            relativePath = Constants.RefsRemotesDir;
        }
        else
        {
            return;
        }
        
        string basePath = prefix != null ? Path.Combine(_commonPath, prefix, relativePath) : Path.Combine(_commonPath, relativePath);
        
        GitPath.ValidatePathLength(null, basePath);

        try
        {
            Directory.Delete(Path.Join(basePath, referenceName.AsSpan(relativePath.Length)), true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private class SortedCache(string path)
    {
        public readonly ReaderWriterLockSlim Lock = new();

        public readonly OrderedDictionary<string, PackRef> Map = [];
        public string Path { get; } = path;
        public DateTime FileTimeStamp;

        public int Count => Map.Count;
        
        public SortedCache Copy(bool @lock)
        {
            var newCache =  new SortedCache(this.Path);
            
            if (@lock)
                this.Lock.EnterReadLock();
            try
            {
                foreach (var item in Map.Values)
                {
                    newCache.InsertOrUpdate(item);
                }

                return newCache;
            }
            finally
            {
                if (@lock)
                    this.Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Lock cache and load backing file into a buffer.
        /// <br/><br/>
        /// This grabs a write lock on the cache then looks at the modification time and size of the file on disk.
        /// <br/><br/>
        /// If the file appears to have changed, this loads the file contents into the buffer and returns a positive
        /// value leaving the cache locked. The caller should parse the file content, update the cache as needed, then
        /// release the lock. NOTE: In this case, the caller MUST unlock the cache.
        /// </summary>
        /// <param name="buffer"></param>
        /// <exception cref="NotImplementedException"></exception>
        public bool LockAndLoad(IBufferWriter<byte> buffer)
        {
            bool shouldUnlock = true;
            this.Lock.EnterWriteLock();

            try
            {
                using var handle = File.OpenHandle(this.Path, FileMode.Open, options: FileOptions.SequentialScan);

                var current = File.GetLastWriteTimeUtc(handle);
                if (current == FileTimeStamp)
                    return false;

                shouldUnlock = false;
                FileTimeStamp = current;

                // These are limited to int.MaxValue bytes because they are backed by singular arrays.
                // Assuming all other implementations use a more sophisticated buffering scheme that
                // buffers beyond int.MaxValue efficiently.
                if (buffer is ArrayBufferWriter<byte> or ArrayPoolBufferWriter<byte>)
                {
                    long length = RandomAccess.GetLength(handle);
                    if (unchecked((ulong)length) > int.MaxValue)
                    {
                        throw new Git2Exception($"Unable to load file larger than {int.MaxValue} bytes!");
                    }
                }

                long position = 0;
                int read;
                while ((read = RandomAccess.Read(handle, buffer.GetSpan(256), position)) > 0)
                {
                    buffer.Advance(read);
                    position += read;
                }

                return true;
            }
            catch
            {
                shouldUnlock = true;
                throw;
            }
            finally
            {
                if (shouldUnlock)
                    this.Lock.ExitWriteLock();
            }
        }

        public void Updated()
        {
            throw new NotImplementedException();
        }

        public void Clear(bool wlock)
        {
            if (wlock)
                this.Lock.EnterWriteLock();
            try
            {
                Map.Clear();
            }
            finally
            {
                if (wlock)
                    this.Lock.ExitWriteLock();
            }
        }

        public void InsertOrUpdate(PackRef value)
        {
            Debug.Assert(this.Lock.IsWriteLockHeld);

            Map[value.Name] = value;
        }

        public void RemoveAt(int pos)
        {
            throw new NotImplementedException();
        }

        public bool Contains(string key)
        {
            this.Lock.EnterReadLock();
            try
            {
                return Map.ContainsKey(key);
            }
            finally
            {
                this.Lock.ExitReadLock();
            }
        }

        public PackRef Lookup(string key)
        {
            throw new NotImplementedException();
        }

        public bool TryLookup(string key, out PackRef value)
        {
            this.Lock.EnterReadLock();
            try
            {
                return Map.TryGetValue(key, out value);
            }
            finally
            {
                this.Lock.ExitReadLock();
            }
        }
        
        public PackRef this[int pos] => Map.GetAt(pos).Value;

        public int LookupIndex(string key)
        {
            throw new NotImplementedException();
        }

    }

    private static void ThrowCorruptedLooseReference(string? filename)
    {
        throw new Git2Exception($"Corrupted loose reference file: {filename}");
    }
    
    private struct PackedReferenceParser
    {
        private readonly StreamReader _reader;
        private char[] _buffer;
        private int _position, _buffered;
        
        public PackedReferenceParser(StreamReader reader)
        {
            _reader = reader;
            _buffer = new char[256];
            _position = 0;
            _buffered = 0;
        }
        
        public ReadOnlySpan<char> BufferedText => _buffer.AsSpan()[_position.._buffered];

        public void SkipCommentLines()
        {
            
            
        }

        private bool BufferMore()
        {
            int pos = _position, countBuffered = _buffered - pos;
            char[] buffer = _buffer;
            
            if (pos > 0)
            {
                Array.Copy(buffer, pos, buffer, 0, countBuffered);
            }
            else if (_buffered >= buffer.Length)
            {
                return true;
            }

            int read = _reader.Read(buffer.AsSpan(countBuffered));

            _position = 0;
            _buffered = countBuffered + read;
            return read > 0;
        }
    }
}