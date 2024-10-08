using System.Buffers;
using System.Runtime.InteropServices;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitBlob(Git2.Blob* nativeHandle) : IDisposable
{
    public readonly Git2.Blob* NativeHandle = nativeHandle;

    public ref readonly GitObjectID Id => ref *git_blob_id(this.NativeHandle);

    public byte* RawContent => git_blob_rawcontent(this.NativeHandle);

    public ulong RawSize => git_blob_rawsize(this.NativeHandle);

    public bool IsBinary => git_blob_is_binary(this.NativeHandle) != 0;

    public GitRepository Owner => new(git_blob_owner(this.NativeHandle));

    public void Dispose()
    {
        git_blob_free(NativeHandle);
    }

    public GitBlob Duplicate()
    {
        Git2.Blob* result;
        Git2.ThrowIfError(git_blob_dup(&result, this.NativeHandle));

        return new(result);
    }

    public void Filter(IBufferWriter<byte> buffer, string as_path, in GitBlobFilterOptions options)
    {
        Native.GitBuffer _buffer = default;
        Native.GitBlobFilterOptions _options = default;
        GitError error;

        try
        {
            _options.FromManaged(in options);

            error = git_blob_filter(&_buffer, this.NativeHandle, as_path, &_options);
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
            if (_buffer.Pointer != this.RawContent)
                git_buf_dispose(&_buffer);
        }
    }

    public SafeBuffer GetSafeBuffer(bool ownsBlob = false)
    {
        return new Buffer(this, ownsBlob);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ownsBlob"></param>
    /// <returns></returns>
    public Stream GetStream(bool ownsBlob = false)
    {
        if (!ownsBlob)
        {
            return new UnmanagedMemoryStream((byte*)this.RawContent, checked((long)this.RawSize));
        }

        SafeBuffer? buffer = null;
        try
        {
            buffer = this.GetSafeBuffer(true);
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
        var length = RawSize;
        if (length <= int.MaxValue)
        {
            span = new ReadOnlySpan<byte>(RawContent, (int)length);
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
        return obj.Type == GitObjectType.Blob
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
}
