using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public struct GitApplyOptions
    {
        public ICallbacks Callbacks;
        public GitApplyFlags Flags;

        public interface ICallbacks
        {
            int OnDelta(in GitDiffDelta delta);
            int OnHunk(in GitDiffHunk hunk);
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitApplyOptions
    {
        public uint Version;
        public delegate* unmanaged[Cdecl]<GitDiffDelta*, nint, int> DeltaCallback;
        public delegate* unmanaged[Cdecl]<GitDiffHunk*, nint, int> HunkCallback;
        public nint Payload;
        public GitApplyFlags Flags;

        public GitApplyOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitApplyOptions options, List<GCHandle> gchandles)
        {
            Version = 1;

            if (options.Callbacks is { } callbacks)
            {
                var gch = GCHandle.Alloc(callbacks, GCHandleType.Normal);
                gchandles.Add(gch);

                Payload = (nint)gch;
                DeltaCallback = &OnDelta;
                HunkCallback = &OnHunk;
            }

            Flags = options.Flags;
        }

        /// <summary>
        /// Free unmanaged resources allocated by <see cref="FromManaged"/>
        /// </summary>
        public void Free()
        {
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnDelta(GitDiffDelta* delta, nint payload)
        {
            var callbacks = (SharpGit2.GitApplyOptions.ICallbacks)((GCHandle)payload).Target!;

            try
            {
                SharpGit2.GitDiffDelta mDelta = new(in *delta);

                return callbacks.OnDelta(in mDelta);
            }
            catch (Exception e)
            {
                NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnHunk(GitDiffHunk* hunk, nint payload)
        {
            var callbacks = (SharpGit2.GitApplyOptions.ICallbacks)((GCHandle)payload).Target!;

            try
            {
                return callbacks.OnHunk(in *hunk);
            }
            catch (Exception e)
            {
                NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }
    }
}