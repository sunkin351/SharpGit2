using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitStashApplyOptions
    {
        public GitStashApplyFlags Flags;
        public GitCheckoutOptions CheckoutOptions;
        public ProgressCallback? Progress;

        public delegate int ProgressCallback(GitStashApplyProgressType progress);
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitStashApplyOptions
    {
        public uint Version;
        public GitStashApplyFlags Flags;
        public GitCheckoutOptions CheckoutOptions;
        public delegate* unmanaged[Cdecl]<GitStashApplyProgressType, nint, int> ProgressCallback;
        public nint ProgressPayload;

        public GitStashApplyOptions()
        {
            Version = 1;
            CheckoutOptions = new();
        }

        public void FromManaged(in SharpGit2.GitStashApplyOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Flags = options.Flags;
            CheckoutOptions.FromManaged(in options.CheckoutOptions, gchandles);

            if (options.Progress is { } progress)
            {
                var gch = GCHandle.Alloc(progress, GCHandleType.Normal);
                gchandles.Add(gch);

                ProgressCallback = &OnProgress;
                ProgressPayload = (nint)gch;
            }
        }

        public void Free()
        {
            CheckoutOptions.Free();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnProgress(GitStashApplyProgressType type, nint payload)
        {
            var callback = (SharpGit2.GitStashApplyOptions.ProgressCallback)((GCHandle)payload).Target!;

            try
            {
                return callback(type);
            }
            catch (Exception e)
            {
                // TODO: Figure out exception propagation here
                NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }
    }
}
