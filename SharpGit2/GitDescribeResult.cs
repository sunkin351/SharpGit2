using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitDescribeResult(Git2.DescribeResult* nativeHandle) : IDisposable
{
    public readonly Git2.DescribeResult* NativeHandle = nativeHandle;

    public void Dispose()
    {
        git_describe_result_free(this.NativeHandle);
    }

    public string Format(in GitDescribeFormatOptions options)
    {
        GitError error;
        Native.GitBuffer buffer = default;

        Native.GitDescribeFormatOptions _options = default;
        try
        {
            _options.FromManaged(in options);

            error = git_describe_format(&buffer, this.NativeHandle, &_options);
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
