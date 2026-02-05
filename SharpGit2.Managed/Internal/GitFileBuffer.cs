using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace SharpGit2.Managed.Internal;

internal sealed class GitFileBuffer : IDisposable
{
    public const int DeflateShift = 7;

    [Flags]
    public enum Flags
    {
        HashSHA1 = 1 << 0,
        HashSHA256 = 1 << 1,
        Append = 1 << 2,
        CreateLeadingDirectories = 1 << 3,
        Temporary = 1 << 4,
        DoNotBuffer = 1 << 5,
        FSync = 1 << 6,

    }

    internal const int DigestType_SHA1 = 1;
    internal const int DigestType_SHA256 = 2;

    public string? PathOriginal { get; }
    public string PathLock { get; }

    private readonly IncrementalHash? _hashState;

    private readonly FileStream _fileStream;
    private readonly Stream _writeStream;

    public bool DidRename { get; private set; }
    private bool _disposed = false;

    [ThreadStatic]
    private static FileStreamOptions? _lockFileOptions;

    public GitFileBuffer(string path, Flags flags, UnixFileMode mode, ZLibCompressionOptions? compressionOptions = null, int bufferSize = WriteBufferSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if ((flags & Flags.HashSHA1) != 0)
        {
            this._hashState = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        }
        else if ((flags & Flags.HashSHA256) != 0)
        {
            this._hashState = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        }

        if ((flags & Flags.Temporary) != 0)
        {
            this._fileStream = FileSystemHelpers.CreateTemporary(path, mode, out var tempPath, options: FileOptions.DeleteOnClose);
            this.PathLock = tempPath;
        }
        else
        {
            var resolvedPath = ResolveSymlink(path);

            this.PathOriginal = resolvedPath;
            this.PathLock = resolvedPath + ".lock";

            if (Directory.Exists(resolvedPath))
            {
                throw new InvalidOperationException($"Path '{this.PathOriginal}' is a directory!");
            }
            
            // inlined `lock_file()`
            if ((flags & Flags.CreateLeadingDirectories) != 0)
            {
                var parent = Path.GetDirectoryName(this.PathLock);

                Debug.Assert(parent != null);

                FileSystemHelpers.CreateDirectory(parent, FileSystemHelpers.AllPerms);
            }

            var options = _lockFileOptions ??= new()
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.ReadWrite,
                Share = FileShare.None,
                Options = FileOptions.DeleteOnClose
            };

            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = mode;

            try
            {
                this._fileStream = new FileStream(this.PathLock, options);
            }
            catch (IOException e)
            {
                var message = Path.Exists(this.PathLock)
                    ? $"Failed to lock file '{this.PathLock}' for writing!"
                    : $"Failed to create locked file '{this.PathLock}'!";

                throw new Git2OSException(message, e);
            }

            if ((flags & Flags.Append) != 0 && Path.Exists(this.PathOriginal))
            {
                Debug.Assert(compressionOptions == null, "Compression is being used with append!"); // assert that compression isn't being used with append

                using var source = File.OpenRead(this.PathOriginal);

                var hashState = this._hashState;

                if (hashState == null)
                {
                    source.CopyTo(_fileStream);
                }
                else
                {
                    byte[] array = ArrayPool<byte>.Shared.Rent(128 * 1024);
                    try
                    {
                        var destination = _fileStream;
                        int read;

                        while ((read = source.Read(array, 0, array.Length)) > 0)
                        {
                            destination.Write(array, 0, read);
                            hashState.AppendData(array, 0, read);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(array);
                    }
                }
                
            }
        }

        this._writeStream = compressionOptions != null
            ? new DeflateStream(this._fileStream, compressionOptions, leaveOpen: true)
            : this._fileStream;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        if (_writeStream != _fileStream)
        {
            _writeStream.Dispose();
        }

        _fileStream.Dispose(); // Automatically deletes the file, as `DeleteOnClose` was passed in it's creation
        _hashState?.Dispose();

        _disposed = true;
    }

    public Stream GetWriteStream()
    {
        if (_hashState == null)
            return _writeStream;

        return new HashingStream(_writeStream, _hashState);
    }

    public void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _writeStream.Write(buffer);
        _hashState?.AppendData(buffer);
    }

    public int Hash(Span<byte> hashOut)
    {
        if (_hashState == null)
            throw new InvalidOperationException($"Hashing was not enabled on this {nameof(GitFileBuffer)}!");

        return _hashState.GetCurrentHash(hashOut);
    }

    public void Commit()
    {
        this.Commit(this.PathOriginal!, default);
    }

    public void Commit(Span<byte> checksum)
    {
        this.Commit(this.PathOriginal!, checksum);
    }

    public void Commit(string targetPath)
    {
        this.Commit(targetPath, default);
    }

    public void Commit(string targetPath, Span<byte> checksum)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        if (!checksum.IsEmpty)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(checksum.Length, _hashState?.HashLengthInBytes ?? 0);
        }

        if (_writeStream is not FileStream)
            _writeStream.Flush();

        using (var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var temp = _fileStream;

            temp.Position = 0;
            temp.CopyTo(stream);
        }

        this.DidRename = true;

        _writeStream.Dispose();
        _fileStream.Dispose();

        if (!checksum.IsEmpty && _hashState != null)
        {
            _hashState.GetCurrentHash(checksum);
        }

        _hashState?.Dispose();
        _disposed = true;
    }

    public (DateTime, long) Stats()
    {
        if (_fileStream.SafeFileHandle is { IsClosed: false } handle)
        {
            return (File.GetLastWriteTimeUtc(handle), _fileStream.Length);
        }

        using (handle = File.OpenHandle(this.PathOriginal!, FileMode.Open, FileAccess.Read))
        {
            return (File.GetLastWriteTimeUtc(handle), RandomAccess.GetLength(handle));
        }
    }

    private static string ResolveSymlink(string path)
    {
        return File.ResolveLinkTarget(path, true)?.FullName ?? path;
    }

    private const int WriteBufferSize = 4096 * 2;
    private const int MaxSymlinkDepth = 5;
}
