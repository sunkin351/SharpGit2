using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public interface IGitDiffSimilarityMetric
    {
        int FromFile(in GitDiffFile file, string fullPath, out nint out_handle);

        int FromBuffer(in GitDiffFile file, ReadOnlySpan<byte> buffer, out nint out_handle);

        void Free(nint handle);

        int Similarity(nint handleA, nint handleB, out int score);
    }
}

namespace SharpGit2.Native
{
    /// <summary>
    /// Pluggable Similarity Metric
    /// </summary>
    public unsafe struct GitDiffSimilarityMetric
    {
        public delegate* unmanaged[Cdecl]<void**, GitDiffFile*, byte*, nint, int> File;
        public delegate* unmanaged[Cdecl]<void**, GitDiffFile*, byte*, nuint, nint, int> Buffer;
        public delegate* unmanaged[Cdecl]<void*, nint, void> Free;
        public delegate* unmanaged[Cdecl]<int*, void*, void*, nint, int> Similarity;
        public nint Payload;

        public void FromManaged(IGitDiffSimilarityMetric? metric, List<GCHandle> gchandles)
        {
            if (metric is null)
            {
                this = default;
                return;
            }

            var gch = GCHandle.Alloc(metric, GCHandleType.Normal);
            gchandles.Add(gch);

            Payload = (nint)gch;
            File = &FileCallback;
            Buffer = &BufferCallback;
            Free = &FreeCallback;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int FileCallback(void** out_handle, GitDiffFile* file, byte* fullPath, nint payload)
        {
            var callbacks = (IGitDiffSimilarityMetric)((GCHandle)payload).Target!;

            try
            {
                SharpGit2.GitDiffFile _file = new(in *file);
                var path = Git2.GetPooledString(fullPath)!;

                return callbacks.FromFile(in _file, path, out *(nint*)out_handle);
            }
            catch (Git2Exception e)
            {
                return (int)e.ErrorCode;
            }
            catch// (Exception e)
            {
                return -1;
            }
        }


        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int BufferCallback(void** out_handle, GitDiffFile* file, byte* buffer, nuint bufferLength, nint payload)
        {
            if (bufferLength > int.MaxValue)
            {
                GitNativeApi.git_error_set_str(
                    GitErrorClass.Callback,
                    "Buffer length too large to represent with 32-bit signed integer! To get around this, please implement the SharpGit2.Native.GitDiffSimilarityMetric method table yourself.");
                return (int)GitError.Invalid;
            }

            var callbacks = (IGitDiffSimilarityMetric)((GCHandle)payload).Target!;

            try
            {
                SharpGit2.GitDiffFile _file = new(in *file);

                var span = new ReadOnlySpan<byte>(buffer, (int)bufferLength);

                return callbacks.FromBuffer(in _file, span, out *(nint*)out_handle);
            }
            catch (Git2Exception e)
            {
                GitNativeApi.git_error_set_str(e.ErrorClass, e.Message);
                return (int)e.ErrorCode;
            }
            catch (Exception e)
            {
                GitNativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void FreeCallback(void* handle, nint payload)
        {
            var callbacks = (IGitDiffSimilarityMetric)((GCHandle)payload).Target!;

            try
            {
                callbacks.Free((nint)handle);
            }
            catch
            {

            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int SimilarityCallback(int* out_score, void* handleA, void* handleB, nint payload)
        {
            var callbacks = (IGitDiffSimilarityMetric)((GCHandle)payload).Target!;

            try
            {
                return callbacks.Similarity((nint)handleA, (nint)handleB, out *out_score);
            }
            catch (Git2Exception e)
            {
                GitNativeApi.git_error_set_str(e.ErrorClass, e.Message);
                return (int)e.ErrorCode;
            }
            catch (Exception e)
            {
                GitNativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }
    }
}
