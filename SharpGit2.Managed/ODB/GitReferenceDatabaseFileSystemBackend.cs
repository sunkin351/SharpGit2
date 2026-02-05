using System.Buffers;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using SharpGit2.Managed.Internal;
using TerraFX.Interop.Windows;

namespace SharpGit2.Managed.ODB;

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

    private GitRepository _repository;
    private string? _gitPath;
    private string _commonPath;
    private GitObjectIDType _objectIdType;
    private bool _fsync, _sorted;
    private Peeling _peelingMode;
    private GitIteratorFlags _iteratorFlags;
    private readonly Lock _lock;
    private DateTime _packedRefsStamp;
    private SortedCache _referenceCache;
    
    public GitReferenceDatabaseFileSystemBackend(GitRepository repo)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public IEnumerable<GitReference> EnumerateReferences(string glob)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> EnumerateReferenceNames(string glob)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void ReflogDelete(string name)
    {
        throw new NotImplementedException();
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
        var oidHexSize = _objectIdType.HashSize * 2;

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

            string text = File.ReadAllText(cache.Path);
            
            cache.Clear(false);

            int scan = 0;
            while (scan < text.Length && text[scan] == '#')
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
            catch
            {
                goto parseFailed;
            }

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

    private Span<char> PackedSetPeelingMode(Span<char> data)
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

            this.LooseLookupToPackFile(referenceName);
        }
    }

    private string LooseParseSymbolic(ReadOnlySpan<char> fileContent, string filename)
    {
        if (fileContent.Length <= Constants.GitSymRef.Length || !fileContent.StartsWith(Constants.GitSymRef))
        {
            ThrowCorruptedLooseReference(filename);
        }

        var referenceName = fileContent.Slice(Constants.GitSymRef.Length);

        if (!GitReference.IsReferenceNameValid(referenceName)) // Sanity check
        {
            ThrowCorruptedLooseReference(filename);
        }
        
        return Utilities.GetPooledString(referenceName);
    }

    private static bool IsPerWorktreeRef(ReadOnlySpan<char> referenceName)
    {
        return !referenceName.StartsWith("refs/")
               || referenceName.StartsWith("refs/bisect/")
               || referenceName.StartsWith("refs/worktree/")
               || referenceName.StartsWith("refs/rewritten/");
    }

    private GitReference? LooseLookup(string referenceName)
    {
        string referenceDir = IsPerWorktreeRef(referenceName) ? _gitPath! : _commonPath;

        using var fileContentBuffer = new ArrayPoolBufferWriter<char>();
        
        LooseReadBuffer(fileContentBuffer, referenceDir, referenceName);

        var fileContent = fileContentBuffer.WrittenSpan;
        
        if (fileContent.StartsWith(Constants.GitSymRef))
        {
            fileContent = fileContent.TrimEnd();
            
            string target = LooseParseSymbolic(fileContent, referenceName);

            return new GitReference(referenceName, target);
        }
        else
        {
            var oid = LooseParseObjectID(referenceName, fileContent, _objectIdType);
            
            return new GitReference(referenceName, oid, null);
        }
    }

    private class SortedCache(string path)
    {
        public readonly ReaderWriterLockSlim Lock = new();

        private readonly OrderedDictionary<string, PackRef> _map = [];
        public string Path { get; } = path;
        public DateTime FileTimeStamp;

        public int Count => _map.Count;
        
        public SortedCache Copy(bool @lock)
        {
            var newCache =  new SortedCache(this.Path);
            
            if (@lock)
                this.Lock.EnterReadLock();
            try
            {
                foreach (var item in _map.Values)
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
                _map.Clear();
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

            _map[value.Name] = value;
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
                return _map.ContainsKey(key);
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
        
        public PackRef this[int pos] => _map.GetAt(pos).Value;

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