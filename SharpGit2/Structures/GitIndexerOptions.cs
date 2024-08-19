using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitIndexerOptions
    {
        public ProgressCallback? Progress;
        public bool Verify;

        public delegate int ProgressCallback(in GitIndexerProgress progress);
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitIndexerOptions
    {
        public uint Version;

#if GIT_EXPERIMENTAL_SHA256
        public uint Mode;
        public Git2.ObjectDatabase* ObjectDatabase;
#endif

        public delegate* unmanaged[Cdecl]<GitIndexerProgress*, nint, int> Progress;
        public nint ProgressPayload;
        public byte Verify;

        public GitIndexerOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitIndexerOptions options, List<GCHandle> gchandles)
        {
            Version = 1;

            if (options.Progress is { } progress)
            {
                var gch = GCHandle.Alloc(progress, GCHandleType.Normal);
                gchandles.Add(gch);

                Progress = &OnProgress;
                ProgressPayload = (nint)gch;
            }

            Verify = (byte)(options.Verify ? 1 : 0);
        }

        public void Free()
        {
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnProgress(GitIndexerProgress* progress, nint payload)
        {
            var callbacks = (SharpGit2.GitIndexerOptions.ProgressCallback)((GCHandle)payload).Target!;

            try
            {
                return callbacks(in *progress);
            }
            catch (Exception e)
            {
                NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }
    }
}
