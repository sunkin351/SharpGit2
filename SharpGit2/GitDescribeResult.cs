using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitDescribeResult(Git2.DescribeResult* nativeHandle) : IDisposable, IGitHandle
{
    public readonly Git2.DescribeResult* NativeHandle = nativeHandle;

    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_describe_result_free(this.NativeHandle);
    }

    public string Format(in GitDescribeFormatOptions options)
    {
        var handle = this.ThrowIfNull();

        GitError error;
        Native.GitBuffer buffer = default;

        Native.GitDescribeFormatOptions _options = default;
        try
        {
            _options.FromManaged(in options);

            error = git_describe_format(&buffer, handle.NativeHandle, &_options);
        }
        finally
        {
            _options.Free();
        }

        Git2.ThrowIfError(error);

        try
        {
            return buffer.AsString();
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }
}
