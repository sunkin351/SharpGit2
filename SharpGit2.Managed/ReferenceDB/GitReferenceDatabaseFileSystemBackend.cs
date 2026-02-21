using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    private struct PackRef
    {
        public GitObjectID Oid;
        public GitObjectID Peel;
        public PackRefFlags Flags;
        public string Name;
        
        public void Write(StreamWriter writer)
        {
            Span<char> buffer = stackalloc char[GitObjectID.MaxHexSize];

            bool success = this.Oid.TryFormat(buffer, out int written);
            Debug.Assert(success);
            
            writer.Write(buffer.Slice(0, written));
            writer.Write(' ');
            writer.Write(this.Name);
            writer.Write('\n');

            if ((this.Flags & PackRefFlags.HasPeel) != 0)
            {
                success = this.Peel.TryFormat(buffer, out written);
                Debug.Assert(success);
                
                writer.Write('^');
                writer.Write(buffer.Slice(0, written));
                writer.Write('\n');
            }
        }
    }

    private const string PackedRefsFile = "packed-refs";
    private const UnixFileMode PackedRefsFileMode = Constants.DefaultFileMode;

    private readonly GitRepository _repository;
    private string? _gitPath;
    private string _commonPath;
    private GitObjectIDType _objectIdType;
    private bool _fsync, _sorted;
    private Peeling _peelingMode;
    private GitIteratorFlags _iteratorFlags;
    private readonly Lock _lock = new();
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

    private static string OptimizePrefix(string? glob)
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
        this.ThrowIfReferencePathNotAvailable(reference.ReferenceName, null, force);

        using var @lock = this.LooseLock(reference.ReferenceName);
        
        this.WriteTail(reference, @lock, true, who, message);
    }

    public void Write(GitReference reference, bool force, GitSignature who, string? message, string old_target)
    {
        this.ThrowIfReferencePathNotAvailable(reference.ReferenceName, null, force);

        using var @lock = this.LooseLock(reference.ReferenceName);
        
        this.WriteTail(reference, @lock, true, who, message, old_target);
    }

    public void Write(GitReference reference, bool force, GitSignature who, string? message, in GitObjectID old_target)
    {
        this.ThrowIfReferencePathNotAvailable(reference.ReferenceName, null, force);

        using var @lock = this.LooseLock(reference.ReferenceName);
        
        this.WriteTail(reference, @lock, true, who, message, in old_target);
    }

    public GitReference Rename(string old_name, string new_name, bool force, GitSignature who, string? message)
    {
        this.ThrowIfReferencePathNotAvailable(new_name, old_name, force);

        var oldReference = this.Lookup(old_name) ?? throw new Git2ReferenceException($"Original reference '{old_name}' does not exist!");
        GitReference newReference;
        
        using (var @lock = this.LooseLock(oldReference.ReferenceName))
        {
            newReference = oldReference.WithReferenceName(new_name);
        
            this.DeleteTail(@lock, old_name);
        }
        
        using var lock2 = this.LooseLock(newReference.ReferenceName);

        this.ReflogRename(old_name, new_name);

        this.ReflogAppend(newReference, newReference.DirectTarget, null, who, message);

        LooseCommit(lock2, newReference);

        return newReference;
    }

    public void Delete(string referenceName)
    {
        using var _lock = this.LooseLock(referenceName);
        
        this.ReflogDelete(referenceName);
        
        this.DeleteTail(_lock, referenceName);
    }

    public void Delete(string referenceName, string old_target)
    {
        using var @lock = this.LooseLock(referenceName);
        
        this.ReflogDelete(referenceName);
        
        this.DeleteTail(@lock, referenceName, old_target);
    }

    public void Delete(string referenceName, in GitObjectID old_target)
    {
        using var @lock = this.LooseLock(referenceName);
        
        this.ReflogDelete(referenceName);
        
        this.DeleteTail(@lock, referenceName, in old_target);
    }

    public void Compress()
    {
        this.PackedReload();
        this.PackedLoadLoose();
        this.PackedWrite();
    }

    public object Lock(string referenceName)
    {
        return this.LooseLock(referenceName);
    }

    public void Unlock(
        object payload,
        int success,
        bool update_reflog,
        GitReference? reference,
        GitSignature signature,
        string? message)
    {
        using var _lock = (GitFileBuffer)payload;

        switch (success)
        {
            case 2: // specific success code
                Debug.Assert(reference != null);
                this.DeleteTail(_lock, reference.ReferenceName);
                break;
            case not 0: // general success
                Debug.Assert(reference != null);
                this.WriteTail(reference, _lock, update_reflog, signature, message);
                break;
        }
    }

    private void WriteTail(GitReference reference, GitFileBuffer file, bool updateReflog, GitSignature signature, string? message)
    {
        bool shouldNotUpdate = reference.ReferenceType switch
        {
            GitReferenceType.Direct => DoesOldValueMatch(reference.ReferenceName, in reference.DirectTarget),
            GitReferenceType.Symbolic => DoesOldValueMatch(reference.ReferenceName, reference.SymbolicTarget!)
        };

        if (shouldNotUpdate) // Do not update when old and new value match
            return;

        if (updateReflog)
        {
            if (_repository.ReferenceDatabase.ShouldWriteReferenceLog(reference))
            {
                this.ReflogAppend(reference, null, null, signature, message);
                MaybeAppendHead(reference, signature, message);
            }
        }

        LooseCommit(file, reference);

        void MaybeAppendHead(GitReference reference, GitSignature who, string? message)
        {
            if (!_repository.ReferenceDatabase.ShouldWriteHeadReferenceLog(reference))
                return;

            if (!_repository.References.TryNameToId(reference.ReferenceName, out GitObjectID oldId))
                oldId = default;

            var head = _repository.References.Lookup(Constants.GitHeadFile);
        
            this.ReflogAppend(head, oldId, reference.DirectTarget, who, message);
        }
    }
    
    private void WriteTail(GitReference reference, GitFileBuffer file, bool updateReflog, GitSignature signature, string? message, string oldSymbolicTarget)
    {
        if (!DoesOldValueMatch(reference.ReferenceName, oldSymbolicTarget))
            throw new Git2ReferenceException("Old reference value does not match!");
        
        this.WriteTail(reference, file, updateReflog, signature, message);
    }
    
    private void WriteTail(GitReference reference, GitFileBuffer file, bool updateReflog, GitSignature signature, string? message, in GitObjectID oldDirectTarget)
    {
        if (!DoesOldValueMatch(reference.ReferenceName, in oldDirectTarget))
            throw new Git2ReferenceException("Old reference value does not match!");
        
        this.WriteTail(reference, file, updateReflog, signature, message);
    }
    
    private void DeleteTail(GitFileBuffer file, string referenceName)
    {
        this.PackedDelete(referenceName);
        this.LooseDelete(referenceName);
        
        this.PruneRefs(referenceName, null);
    }

    private void DeleteTail(GitFileBuffer file, string referenceName, string oldSymbolicTarget)
    {
        if (!this.DoesOldValueMatch(referenceName, oldSymbolicTarget))
            throw new Git2ReferenceException("Old reference value does not match!");
        
        this.DeleteTail(file, referenceName);
    }
    
    private void DeleteTail(GitFileBuffer file, string referenceName, in GitObjectID oldDirectTarget)
    {
        if (!this.DoesOldValueMatch(referenceName, in oldDirectTarget))
            throw new Git2ReferenceException("Old reference value does not match!");
        
        this.DeleteTail(file, referenceName);
    }

    private bool DoesOldValueMatch(string referenceName, in GitObjectID oldTarget)
    {
        var reference = this.Lookup(referenceName);

        if (reference == null)
            return oldTarget.IsZero;

        if (reference.ReferenceType != GitReferenceType.Direct)
            return false;

        return reference.DirectTarget == oldTarget;
    }

    private bool DoesOldValueMatch(string referenceName, string oldTarget)
    {
        var reference = this.Lookup(referenceName);

        if (reference == null)
            return false;

        if (reference.ReferenceType != GitReferenceType.Direct)
            return false;

        return reference.SymbolicTarget == oldTarget;
    }

    private void ThrowIfReferencePathNotAvailable(string newName, string? oldName, bool force)
    {
        this.PackedReload();

        if (!force && this.Exists(newName))
        {
            throw new Git2ReferenceException(
                $"Failed to write reference '{newName}': a reference with that name already exists.");
        }
        
        _referenceCache.Lock.EnterReadLock();
        try
        {
            foreach (var refName in _referenceCache.Keys)
            {
                if (!IsReferenceAvailable(oldName, newName, refName))
                {
                    throw new Git2ReferenceException($"Path to reference '{newName}' collides with an existing path");
                }
            }
        }
        finally
        {
            _referenceCache.Lock.ExitReadLock();
        }
    }

    private bool IsReferenceAvailable(string? oldName, string newName, string thisRef)
    {
        if (oldName == null || oldName != thisRef)
        {
            var (compareLength, lead) = thisRef.Length < newName.Length ? (thisRef.Length, newName) : (newName.Length, thisRef);

            if (newName.AsSpan(0, compareLength).SequenceEqual(thisRef.AsSpan(0, compareLength))
                && (uint)compareLength < (uint)lead.Length
                && lead[compareLength] == '/')
            {
                return false;
            }
        }

        return true;
    }

    #region Reference Logs
    
    public bool HasLog(string refname)
    {
        return File.Exists(this.ReflogPath(refname));
    }

    private static readonly FileStreamOptions _ensureLog_Options = new()
    {
        Mode = FileMode.OpenOrCreate,
        Access = FileAccess.ReadWrite,
        Share = FileShare.ReadWrite,
        UnixCreateMode = OperatingSystem.IsWindows() ? null : Constants.ReflogMode
    };
        
    public void EnsureLog(string refname)
    {
        var path = this.ReflogPath(refname);

        if (File.Exists(path))
            return;
        
        FileSystemHelpers.CreateDirectory(Path.GetDirectoryName(path)!, Constants.ReflogDirMode);
        
        try
        {
            File.Open(path, _ensureLog_Options).Dispose();
        }
        catch (IOException e) when (e.FileInUse) // custom extension to simplify check
        {
            // The file is in use by another process/handle. As it's already created, we have nothing to do.
        }
    }

    public GitReferenceLog ReflogRead(string referenceName)
    {
        var log = new GitReferenceLog(referenceName, _objectIdType);

        var logPath = this.ReflogPath(referenceName);

        StreamReader reader;
        try
        {
            reader = new StreamReader(logPath);
        }
        catch (FileNotFoundException)
        {
            this.EnsureLog(referenceName);
            return log;
        }

        using var parser = new GitParser(reader);

        while (parser.AdvanceToNextLine())
        {
            GitReferenceLogEntry entry = default;

            if (!parser.AdvanceObjectId(_objectIdType, out entry.ObjectID_Old)
                || !parser.AdvanceExpected(" ")
                || !parser.AdvanceObjectId(_objectIdType, out entry.ObjectID_Current))
                continue;

            var remaining = parser.GetRemainingLine();
            int end = remaining.IndexOfAny('\t', '\n');
            if (end < 0)
                end = remaining.Length;

            if (!GitSignature.TryParse(remaining.Slice(0, end), out entry.Committer))
                continue;

            parser.AdvanceCount(end);

            if (remaining[end] == '\t')
            {
                parser.AdvanceCount(1);

                remaining = parser.GetRemainingLine().TrimEnd('\n');

                entry.Message = remaining.ToString();
            }
            
            log.Entries.Add(entry);
        }
        
        return log;
    }

    public void ReflogWrite(GitReferenceLog reflog)
    {
        using GitFileBuffer _lock = this.LockReflog(reflog.ReferenceName);

        using (var writer = new StreamWriter(_lock.GetWriteStream(), leaveOpen: true))
        {
            foreach (var entry in reflog.Entries)
            {
                SerializeReflogEntry(writer, in entry.ObjectID_Old, in entry.ObjectID_Current, entry.Committer, entry.Message);
            }
        }
        
        _lock.Commit();
    }

    public void ReflogRename(string old_name, string new_name)
    {
        string normalized = GitReference.NormalizeReferenceName(new_name, GitReferenceFormat.AllowOneLevel);

        string repoPath = _repository.RepositoryPath;
        
        string oldPath = Path.Combine(repoPath, Constants.ReflogDir, old_name);

        if (!Path.Exists(oldPath))
            return;
        
        string newPath = Path.Combine(repoPath, Constants.ReflogDir, normalized);
        
        string tempPath = LoosePath(Path.Combine(repoPath,  Constants.ReflogDir), "temp_reflog");

        FileSystemHelpers.MakeTemporary(ref tempPath, Constants.ReflogMode);

        File.Move(oldPath, tempPath, true); // We created the file destination, reserving the name, so overwrite it
        try
        {
            if (Directory.Exists(newPath))
            {
                if (!FileSystemHelpers.DeleteDirectoryRecursive(newPath))
                    throw new Git2Exception("Cannot replace directory that contains existing reference logs");
            }
            else
            {
                var parent = Path.GetDirectoryName(newPath);
                if (!Directory.Exists(parent))
                {
                    FileSystemHelpers.CreateDirectory(parent, Constants.ReflogDirMode);
                }
            }

            try
            {
                File.Move(tempPath, newPath);
            }
            catch (Exception e)
            {
                throw new Git2OSException($"Failed to rename reflog for '{new_name}'", e);
            }
        }
        catch
        {
            // attempt to revert if something goes wrong
            File.Move(tempPath, oldPath);
            throw;
        }
    }

    public void ReflogDelete(string name)
    {
        string path = this.ReflogPath(name);

        // If a reference was moved downwards, e.g. refs/heads/br2 -> refs/heads/br2/new-name,
        // refs/heads/br2 does exist, but it's a directory. That's a valid situation.
        // Proceed only if it's a file.
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        
        this.PruneRefs(name, Constants.ReflogDir);
    }
    
    private string ReflogPath(string referenceName)
    {
        return ReflogPath(_repository, referenceName);
    }

    private static string ReflogPath(GitRepository repo, string referenceName)
    {
        return LoosePath(
            Path.Combine(
                referenceName == Constants.GitHeadFile
                    ? repo.RepositoryPath
                    : repo.CommonDirectory!,
                Constants.ReflogDir),
            referenceName);
    }

    private GitFileBuffer LockReflog(string referenceName)
    {
        if (!GitPath.PathStringIsValid(_repository, referenceName, 0, GitPath.DefaultValidation))
        {
            throw new Git2Exception($"Invalid reference name '{referenceName}'");
        }
        
        string logPath = ReflogPath(_repository, referenceName);

        if (!File.Exists(logPath))
        {
            throw new Git2Exception($"Log file for reference '{referenceName}' doesn't exist");
        }

        return new GitFileBuffer(logPath, 0, Constants.ReflogMode);
    }

    private void ReflogAppend(
        GitReference reference,
        GitObjectID? oldId,
        GitObjectID? newId,
        GitSignature who,
        string? message)
    {
        var repo = _repository;

        bool isSymbolic = reference.ReferenceType == GitReferenceType.Symbolic;

        if (isSymbolic
            && reference.ReferenceName != Constants.GitHeadFile
            && !(oldId.HasValue && newId.HasValue))
            return;

        GitObjectID old_id = oldId ?? repo.References.NameToId(reference.ReferenceName),
            new_id = default;

        if (newId.HasValue)
        {
            new_id = newId.GetValueOrDefault();
        }
        else if (!isSymbolic)
        {
            new_id = reference.DirectTarget;
        }
        else
        {
            if (!repo.References.TryNameToId(reference.SymbolicTarget!, out new_id))
            {
                return;
            }
        }

        string path = ReflogPath(repo, reference.ReferenceName);

        if (Directory.Exists(path))
        {
            // If the new branch matches part of the namespace of a previously deleted branch,
            // there maybe an obsolete/unused directory (or directory hierarchy) in the way.
            if (!FileSystemHelpers.DeleteDirectoryRecursive(path))
            {
                throw new Git2ReferenceException(
                    $"Cannot create reflog at '{reference.ReferenceName}', there are reflogs beneath that folder");
            }
        }
        else
        {
            string parent = Path.GetDirectoryName(path)!;
            FileSystemHelpers.CreateDirectory(parent, Constants.FileModeAllPermissions);
        }

        using var appendStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None);
        
        using (var writer = new StreamWriter(appendStream, leaveOpen: true))
        {
            SerializeReflogEntry(writer, in old_id, in new_id, who, message);
        }

        if (_fsync)
            appendStream.Flush(true);
    }
    
    internal static void SerializeReflogEntry(
        TextWriter writer,
        in GitObjectID oid_old,
        in GitObjectID oid_new,
        GitSignature committer,
        string? message)
    {
        Span<char> span = stackalloc char[128];

        bool success = oid_old.TryFormat(span, out int written);
        Debug.Assert(success);

        writer.Write(span.Slice(0, written));
        writer.Write(' ');
        
        success = oid_new.TryFormat(span, out written);
        Debug.Assert(success);
        
        writer.Write(span.Slice(0, written));
        writer.Write(' ');
        
        if (committer.TryFormat(span, out written, "tc"))
        {
            writer.Write(span.Slice(0, written));
        }
        else
        {
            writer.Write(committer.ToString("tc", null));
        }

        if (message != null)
        {
            writer.Write('\t');
            
            var messageSpan = message.AsSpan().TrimEnd();

            if (!messageSpan.Contains('\n'))
            {
                // avoid a copy if unnecessary
                writer.Write(messageSpan);
            }
            else if (messageSpan.Length <= span.Length)
            {
                messageSpan.Replace(span, '\n', ' ');
                
                writer.Write(span.Slice(0, messageSpan.Length));
            }
            else
            {
                char[] array = ArrayPool<char>.Shared.Rent(messageSpan.Length);
                try
                {
                    messageSpan.Replace(array, '\n', ' ');

                    writer.Write(array, 0, messageSpan.Length);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(array);
                }
            }
        }
        
        writer.Write('\n');
    }
    
    // This is marked `internal` to allow unit testing
    internal static void SerializeReflogEntry(
        IBufferWriter<char> buffer,
        in GitObjectID oid_old,
        in GitObjectID oid_new,
        GitSignature committer,
        string? message)
    {
        Span<char> span = buffer.GetSpan(GitObjectID.MaxHexSize * 2 + 2);

        bool success = oid_old.TryFormat(span, out int written);
        Debug.Assert(success);

        int toAdvance = written;
        span[toAdvance] = ' ';
        toAdvance += 1;

        success = oid_new.TryFormat(span.Slice(toAdvance), out written);
        Debug.Assert(success);
            
        toAdvance += written;
        span[toAdvance] = ' ';
            
        buffer.Advance(toAdvance + 1);

        if (committer.TryFormat(buffer.GetSpan(64), out written, "tc"))
        {
            buffer.Advance(written);
        }
        else
        {
            buffer.Write(committer.ToString(true));
        }

        if (message != null)
        {
            var messageSpan = message.AsSpan().TrimEnd();

            var destination = buffer.GetSpan(messageSpan.Length + 2);

            destination[0] = '\t';
            messageSpan.Replace(destination[1..], '\n', ' ');
            destination[messageSpan.Length + 1] = '\n';
                
            buffer.Advance(messageSpan.Length + 2);
        }
        else
        {
            buffer.GetSpan(1)[0] = '\n';
            buffer.Advance(1);
        }
    }

    #endregion

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
        
        // Creates the entire path leading up to the directory, as well as the directory itself
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
            using (var stream = new FileStream(cache.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
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

    private void PackedWrite()
    {
        var cache = _referenceCache;

        cache.Lock.EnterWriteLock();
        try
        {
            cache.ValidateStructure();

            using var file = new GitFileBuffer(cache.Path, _fsync ? GitFileBuffer.Flags.FSync : 0, PackedRefsFileMode);

            using (var writer = new StreamWriter(file.GetWriteStream(), leaveOpen: true)) // UTF-8 by default
            {
                // packed refs header
                // Is not in fact required, but may as well include it.
                writer.Write("# pack-refs with: peeled fully-peeled sorted \n");
                
                foreach (var key in cache.Keys)
                {
                    ref var packref = ref CollectionsMarshal.GetValueRefOrNullRef(cache.Map, key);

                    if (Unsafe.IsNullRef(ref packref)) // catch a bug and report it as what it truly is. A missing 
                        throw new KeyNotFoundException();

                    this.PackedFindPeel(ref packref);

                    packref.Write(writer);
                }
            }

            file.Commit();
            
            this.PackedRemoveLoose();
            
            cache.Updated();
        }
        finally
        {
            cache.Lock.ExitWriteLock();
        }
    }

    private void PackedDelete(string referenceName)
    {
        this.PackedReload();

        bool found = false;
        
        var cache = _referenceCache;
        cache.Lock.EnterWriteLock();
        try
        {
            if (cache.Map.Remove(referenceName))
            {
                cache.Keys.Remove(referenceName);
                found = true;
            }
        }
        finally
        {
            cache.Lock.ExitWriteLock();
        }
        
        if (found)
            this.PackedWrite();
    }

    private void PackedFindPeel(ref PackRef packref)
    {
        if ((packref.Flags & PackRefFlags.HasPeel) != 0 || (packref.Flags & PackRefFlags.CannotPeel) != 0)
            return;
        
        var obj = _repository.Objects.Lookup(in packref.Oid);

        if (obj.ObjectType == GitObjectType.Tag)
        {
            var tag = (GitTag)obj;

            packref.Peel = tag.Target;
            packref.Flags |= PackRefFlags.HasPeel;
        }
    }

    private void PackedRemoveLoose()
    {
        Debug.Assert(_referenceCache.Lock.IsWriteLockHeld);

        foreach (string referenceName in _referenceCache.Map.Keys)
        {
            ref var packref = ref CollectionsMarshal.GetValueRefOrNullRef(_referenceCache.Map, referenceName);
            Debug.Assert(!Unsafe.IsNullRef(ref packref));
            
            if ((packref.Flags & PackRefFlags.WasLoose) == 0)
                continue;

            GitFileBuffer @lock;
            try
            {
                @lock = this.LooseLock(packref.Name);
            }
            catch (IOException e)
            {
                // Likely being modified ahead of us, let that continue.
                continue;
            }

            try
            {
                using var fileData = new ArrayPoolBufferWriter<char>();
                try
                {
                    File.ReadAllText(@lock.PathOriginal!, fileData);
                }
                catch (FileNotFoundException)
                {
                    // Someone beat us to deleting the file
                    continue;
                }

                if (fileData.WrittenSpan.StartsWith(Constants.GitSymRef))
                {
                    // This became a symbolic reference between us packing and trying to delete it, so ignore it.
                    continue;
                }

                var oid = LooseParseObjectID(@lock.PathOriginal!, fileData.WrittenSpan, _objectIdType);

                if (oid != packref.Oid)
                    continue; // If the ref moved since we packed it, we must not delete it

                try
                {
                    File.Delete(@lock.PathOriginal!);
                }
                catch
                {
                    // if we fail to remove a single file, this is *not* good,
                    // but we should keep going and remove as many as possible.
                    // If we fail to remove, the ref is still in the old state, so
                    // we haven't lost information.
                }
            }
            finally
            {
                @lock.Dispose();
            }
        }
    }
    
    private GitFileBuffer LooseLock(string referenceName)
    {
        if (!_repository.IsPathStringValid(referenceName, 0, GitPath.DefaultValidation))
        {
            throw new Git2Exception($"Invalid reference name '{referenceName}'");
        }

        string baseDir = IsPerWorktreeRef(referenceName) ? _gitPath! : _commonPath;

        var refPath = LoosePath(baseDir, referenceName);

        if (Directory.Exists(refPath))
        {
            if (!FileSystemHelpers.DeleteDirectoryRecursive(refPath))
                throw new Git2ReferenceException(
                    $"Cannot lock ref '{referenceName}', there are refs beneath that folder");
        }

        var flags = GitFileBuffer.Flags.CreateLeadingDirectories;
        if (_fsync)
            flags |= GitFileBuffer.Flags.FSync;

        return new GitFileBuffer(refPath, flags, Constants.RefsFileMode);
    }

    private bool LooseDelete(string referenceName)
    {
        try
        {
            File.Delete(LoosePath(_commonPath, referenceName));
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    private static void LooseCommit(GitFileBuffer file, GitReference reference)
    {
        using (var writer = new StreamWriter(file.GetWriteStream(), leaveOpen: true))
        {
            switch (reference.ReferenceType)
            {
                case GitReferenceType.Direct:
                    Span<char> oid = stackalloc char[GitObjectID.MaxHexSize];

                    bool success = reference.DirectTarget.TryFormat(oid, out int written);
                    Debug.Assert(success);
                    
                    writer.Write(oid.Slice(0, written));
                    break;
                case GitReferenceType.Symbolic:
                    writer.Write(Constants.GitSymRef);
                    writer.Write(reference.SymbolicTarget);
                    break;
                default:
                    throw new Git2Exception("Internal error!");
            }

            writer.Write('\n');
        }
        
        file.Commit();
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

        public readonly Dictionary<string, PackRef> Map = [];
        public readonly SortedSet<string> Keys = [];
        
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

        public void Updated()
        {
            FileTimeStamp = File.GetLastWriteTimeUtc(this.Path);
        }

        public void Clear(bool wlock)
        {
            if (wlock)
                this.Lock.EnterWriteLock();
            try
            {
                Map.Clear();
                Keys.Clear();
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

            Keys.Add(value.Name);
            Map[value.Name] = value;
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
        
        [Conditional("DEBUG")]
        public void ValidateStructure()
        {
            var keys = new HashSet<string>(Keys);
            
            keys.SymmetricExceptWith(Map.Keys);

            if (keys.Count > 0)
                throw new Exception("An internal bug has been found.");
        }
    }

    private static void ThrowCorruptedLooseReference(string? filename)
    {
        throw new Git2Exception($"Corrupted loose reference file: {filename}");
    }
}