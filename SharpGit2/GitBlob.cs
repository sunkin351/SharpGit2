using System.Runtime.InteropServices;

namespace SharpGit2;

public unsafe readonly struct GitBlob(Git2.Blob* nativeHandle) : IDisposable
{
    public readonly Git2.Blob* NativeHandle = nativeHandle;

    public void* RawContent => NativeApi.git_blob_rawcontent(NativeHandle);
    public ulong RawSize => NativeApi.git_blob_rawsize(NativeHandle);

    public void Dispose()
    {
        NativeApi.git_blob_free(NativeHandle);
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
