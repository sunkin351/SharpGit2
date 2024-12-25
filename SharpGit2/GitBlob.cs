using System.Buffers;
using System.Runtime.InteropServices;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitBlob(Git2.Blob* nativeHandle) : IDisposable, IGitObject<GitBlob>, IGitHandle
{
    public readonly Git2.Blob* NativeHandle = nativeHandle;

    public bool IsNull => this.NativeHandle == null;

    public ref readonly GitObjectID Id
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_blob_id(handle.NativeHandle);
        }
    }

    public byte* RawContent
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_blob_rawcontent(handle.NativeHandle);
        }
    }

    public ulong RawSize
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_blob_rawsize(handle.NativeHandle);
        }
    }

    public bool IsBinary
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_blob_is_binary(handle.NativeHandle) != 0;
        }
    }

    public GitRepository Owner
    {
        get
        {
            var handle = this.ThrowIfNull();

            return new(git_blob_owner(handle.NativeHandle));
        }
    }

    public void Dispose()
    {
        git_blob_free(this.NativeHandle);
    }

    public GitBlob Duplicate()
    {
        var handle = this.ThrowIfNull();

        Git2.Blob* result = null;
        Git2.ThrowIfError(git_blob_dup(&result, handle.NativeHandle));

        return new(result);
    }

    public void Filter(IBufferWriter<byte> buffer, string as_path, in GitBlobFilterOptions options)
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer _buffer = default;
        Native.GitBlobFilterOptions _options = default;
        GitError error;

        try
        {
            _options.FromManaged(in options);

            error = git_blob_filter(&_buffer, handle.NativeHandle, as_path, &_options);
        }
        finally
        {
            _options.Free();
        }

        Git2.ThrowIfError(error);

        try
        {
            _buffer.CopyToBufferWriter(buffer);
        }
        finally
        {
            if (_buffer.Pointer != git_blob_rawcontent(handle.NativeHandle))
                git_buf_dispose(&_buffer);
        }
    }

    public SafeBuffer GetSafeBuffer(bool ownsBlob = false)
    {
        var handle = this.ThrowIfNull();

        return new Buffer(handle, ownsBlob);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ownsBlob"></param>
    /// <returns></returns>
    public Stream GetStream(bool ownsBlob = false)
    {
        var handle = this.ThrowIfNull();

        if (!ownsBlob)
        {
            return new UnmanagedMemoryStream(
                git_blob_rawcontent(handle.NativeHandle),
                checked((long)git_blob_rawsize(handle.NativeHandle)));
        }

        SafeBuffer? buffer = null;
        try
        {
            buffer = handle.GetSafeBuffer(true);
            return new UnmanagedMemoryStream(buffer, 0, checked((long)buffer.ByteLength));
        }
        catch
        {
            buffer?.Dispose();
            throw;
        }
    }

    public bool TryGetContentSpan(out ReadOnlySpan<byte> span)
    {
        var handle = this.ThrowIfNull();

        var length = git_blob_rawsize(handle.NativeHandle);
        if (length <= int.MaxValue)
        {
            span = new ReadOnlySpan<byte>(git_blob_rawcontent(handle.NativeHandle), (int)length);
            return true;
        }

        span = default;
        return false;
    }

    public static bool DataIsBinary(ReadOnlySpan<byte> data)
    {
        fixed (byte* _data = data)
        {
            return git_blob_data_is_binary(_data, (nuint)data.Length);
        }
    }

    public static explicit operator GitBlob(GitObject obj)
    {
        return obj.IsNull || obj.Type == GitObjectType.Blob
            ? new((Git2.Blob*)obj.NativeHandle)
            : throw new InvalidCastException("Git Object is not of type Blob!");
    }

    public static implicit operator GitObject(GitBlob blob)
    {
        return new((Git2.Object*)blob.NativeHandle);
    }

    internal sealed class Buffer : SafeBuffer
    {
        private GitBlob _blob;

        public Buffer(GitBlob blob, bool ownsBlob) : base(ownsBlob)
        {
            _blob = blob;

            this.SetHandle((nint)_blob.RawContent);
            this.Initialize(_blob.RawSize);
        }

        protected override bool ReleaseHandle()
        {
            _blob.Dispose();
            return true;
        }
    }

    Git2.Object* IGitObject<GitBlob>.NativeHandle => (Git2.Object*)this.NativeHandle;

    static GitBlob IGitObject<GitBlob>.FromObjectPointer(Git2.Object* obj)
    {
        return new((Git2.Blob*)obj);
    }

    static GitObjectType IGitObject<GitBlob>.ObjectType => GitObjectType.Blob;
}
