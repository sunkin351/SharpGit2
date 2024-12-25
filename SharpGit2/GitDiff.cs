using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitDiff(Git2.Diff* nativeHandle) : IDisposable, IGitHandle
{
    public Git2.Diff* NativeHandle { get; } = nativeHandle;

    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_diff_free(NativeHandle);
    }

    public void FindSimilar(in GitDiffFindOptions options)
    {
        var handle = this.ThrowIfNull();

        Native.GitDiffFindOptions nOptions = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_diff_find_similar(handle.NativeHandle, &nOptions);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);
    }

    public void FindSimilar(in Native.GitDiffFindOptions options)
    {
        var handle = this.ThrowIfNull();

        fixed (Native.GitDiffFindOptions* pOptions = &options)
            Git2.ThrowIfError(git_diff_find_similar(handle.NativeHandle, pOptions));
    }

    public void ForEach(IForEachCallbacks callbacks)
    {
        var handle = this.ThrowIfNull();

        CallbackInfo callbackInfo = new() { Callbacks = callbacks };
        GitError error = git_diff_foreach(handle.NativeHandle, &OnFileCallback, &OnBinaryCallback, &OnHunkCallback, &OnLineCallback, (nint)(void*)&callbackInfo);
        callbackInfo.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);
    }

    public GitPatch GetPatch(nuint idx)
    {
        var handle = this.ThrowIfNull();

        Git2.Patch* result = null;
        Git2.ThrowIfError(git_patch_from_diff(&result, handle.NativeHandle, idx));

        return new(result);
    }

    public static void DiffBuffers(
        ReadOnlySpan<char> old_content,
        string? old_as_path,
        ReadOnlySpan<char> new_content,
        string? new_as_path,
        in GitDiffOptions options,
        IForEachCallbacks callbacks)
    {
        byte[] old_array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(old_content));
        try
        {
            var oldWritten = Encoding.UTF8.GetBytes(old_content, old_array);

            byte[] new_array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(new_content));
            try
            {
                var newWritten = Encoding.UTF8.GetBytes(new_content, new_array);

                DiffBuffers(
                    new ReadOnlySpan<byte>(old_array, 0, oldWritten), old_as_path,
                    new ReadOnlySpan<byte>(new_array, 0, newWritten), new_as_path,
                    in options, callbacks);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(new_array);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(old_array);
        }
    }

    public static void DiffBuffers(
        ReadOnlySpan<byte> old_content,
        string? old_as_path,
        ReadOnlySpan<byte> new_content,
        string? new_as_path,
        in GitDiffOptions options,
        IForEachCallbacks callbacks)
    {
        Native.GitDiffOptions nOptions = default;
        CallbackInfo callbackInfo = new() { Callbacks = callbacks };
        List<GCHandle> gchandles = [];
        GitError error;

        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_diff_buffers(
                old_content, old_as_path,
                new_content, new_as_path,
                &nOptions, &OnFileCallback, &OnBinaryCallback, &OnHunkCallback, &OnLineCallback, (nint)(void*)&callbackInfo);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            nOptions.Free();
        }

        callbackInfo.ExceptionInfo?.Throw();

        Git2.ThrowIfError(error);
    }

    public interface IForEachCallbacks
    {
        int OnFile(in GitDiffDelta delta, float progress);

        int OnBinary(in GitDiffDelta delta, in Native.GitDiffBinary binary);

        int OnHunk(in GitDiffDelta delta, in GitDiffHunk hunk);

        int OnLine(in GitDiffDelta delta, in GitDiffHunk hunk, in GitDiffLine line);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnFileCallback(Native.GitDiffDelta* delta, float progress, nint payload)
    {
        ref var info = ref *(CallbackInfo*)payload;

        try
        {
            var mdelta = new GitDiffDelta(in *delta);

            return info.Callbacks.OnFile(in mdelta, progress);
        }
        catch (Exception e)
        {
            info.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
            return -1;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnBinaryCallback(Native.GitDiffDelta* delta, Native.GitDiffBinary* binary, nint payload)
    {
        ref var info = ref *(CallbackInfo*)payload;

        try
        {
            var mdelta = new GitDiffDelta(in *delta);

            return info.Callbacks.OnBinary(in mdelta, in *binary);
        }
        catch (Exception e)
        {
            info.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
            return -1;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnHunkCallback(Native.GitDiffDelta* delta, Native.GitDiffHunk* hunk, nint payload)
    {
        ref var info = ref *(CallbackInfo*)payload;

        try
        {
            var mdelta = new GitDiffDelta(in *delta);
            var mhunk = new GitDiffHunk(in *hunk);

            return info.Callbacks.OnHunk(in mdelta, in mhunk);
        }
        catch (Exception e)
        {
            info.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
            return -1;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnLineCallback(Native.GitDiffDelta* delta, Native.GitDiffHunk* hunk, Native.GitDiffLine* line, nint payload)
    {
        ref var info = ref *(CallbackInfo*)payload;

        try
        {
            var mdelta = new GitDiffDelta(in *delta);
            var mhunk = new GitDiffHunk(in *hunk);
            var mline = new GitDiffLine(in *line);

            return info.Callbacks.OnLine(in mdelta, in mhunk, in mline);
        }
        catch (Exception e)
        {
            info.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
            return -1;
        }
    }

    private ref struct CallbackInfo
    {
        public IForEachCallbacks Callbacks;
        public ExceptionDispatchInfo? ExceptionInfo;
    }
}
