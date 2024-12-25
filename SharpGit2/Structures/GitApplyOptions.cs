using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2
{
    /// <summary>
    /// Apply options structure
    /// </summary>
    public struct GitApplyOptions
    {
        public ICallbacks Callbacks;
        /// <summary>
        /// Behavioral flags for the Apply operation
        /// </summary>
        public GitApplyFlags Flags;

        /// <summary>
        /// Callbacks interface 
        /// </summary>
        public interface ICallbacks
        {
            int OnDelta(in GitDiffDelta delta);
            int OnHunk(in GitDiffHunk hunk);
        }
    }
}

namespace SharpGit2.Native
{
    /// <summary>
    /// Apply options structure
    /// </summary>
    public unsafe struct GitApplyOptions
    {
        /// <summary>
        /// The struct version
        /// </summary>
        public uint Version;
        /// <summary>
        /// When applying a patch, callback that will be made per delta (file).
        /// </summary>
        public delegate* unmanaged[Cdecl]<GitDiffDelta*, nint, int> DeltaCallback;
        /// <summary>
        /// When applying a patch, callback that will be made per hunk.
        /// </summary>
        public delegate* unmanaged[Cdecl]<GitDiffHunk*, nint, int> HunkCallback;
        /// <summary>
        /// Payload passed to both delta_cb and hunk_cb.
        /// </summary>
        public nint Payload;
        /// <summary>
        /// Behavioral flags for the Apply operation
        /// </summary>
        public GitApplyFlags Flags;

        /// <summary>
        /// Initialize the structure to it's default values
        /// </summary>
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
        /// Free any unmanaged/native resources allocated by <see cref="FromManaged(in SharpGit2.GitApplyOptions, List{GCHandle})"/>
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
                GitNativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnHunk(GitDiffHunk* hunk, nint payload)
        {
            var callbacks = (SharpGit2.GitApplyOptions.ICallbacks)((GCHandle)payload).Target!;

            try
            {
                var mHunk = new SharpGit2.GitDiffHunk(in *hunk);

                return callbacks.OnHunk(in mHunk);
            }
            catch (Exception e)
            {
                GitNativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }
    }
}