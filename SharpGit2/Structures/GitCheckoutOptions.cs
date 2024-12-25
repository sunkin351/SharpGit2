using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using SharpGit2.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitCheckoutOptions
    {
        public GitCheckoutStrategyFlags CheckoutStrategy;
        public bool DisableFilters;
        public uint DirectoryMode;
        public uint FileMode;
        public int FileOpenFlags;
        public GitCheckoutNotifyFlags NotifyFlags;
        public NotifyCallbackType? NotifyCallback;
        public ProgressCallbackType? ProgressCallback;
        public string[]? Paths;
        public GitTree Baseline;
        public GitIndex BaselineIndex;
        public string? TargetDirectory;
        public string? AncestorLabel;
        public string? OurLabel;
        public string? TheirLabel;
        public PerformanceDataCallbackType? PerfDataCallback;

        public GitCheckoutOptions()
        {
            CheckoutStrategy = GitCheckoutStrategyFlags.Safe;
        }

        public delegate int NotifyCallbackType(GitCheckoutNotifyFlags why, string path, in GitDiffFile baseline, in GitDiffFile target, in GitDiffFile workDir);

        public delegate void ProgressCallbackType(string path, nuint completedSteps, nuint totalSteps);

        public delegate void PerformanceDataCallbackType(in GitCheckoutPerformanceData perfData);
    }
}

namespace SharpGit2.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GitCheckoutOptions
    {
        public uint Version;
        public GitCheckoutStrategyFlags CheckoutStrategy;
        public int DisableFilters;
        public uint DirectoryMode;
        public uint FileMode;
        public int FileOpenFlags;
        public GitCheckoutNotifyFlags NotifyFlags;
        public delegate* unmanaged[Cdecl]<GitCheckoutNotifyFlags, byte*, GitDiffFile*, GitDiffFile*, GitDiffFile*, nint, int> NotifyCallback;
        public nint NotifyPayload;
        public delegate* unmanaged[Cdecl]<byte*, nuint, nuint, nint, void> ProgressCallback;
        public nint ProgressPayload;
        public GitStringArray Paths;
        public Git2.Tree* Baseline;
        public Git2.Index* BaselineIndex;
        public byte* TargetDirectory;
        public byte* AncestorLabel;
        public byte* OurLabel;
        public byte* TheirLabel;
        public delegate* unmanaged[Cdecl]<GitCheckoutPerformanceData*, nint, void> PerfDataCallback;
        public nint PerfDataPayload;

        public GitCheckoutOptions()
        {
            Version = 1;
            CheckoutStrategy = GitCheckoutStrategyFlags.Safe;
        }

        public void FromManaged(in SharpGit2.GitCheckoutOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            CheckoutStrategy = options.CheckoutStrategy;
            DisableFilters = options.DisableFilters ? 1 : 0;
            DirectoryMode = options.DirectoryMode;
            FileMode = options.FileMode;
            FileOpenFlags = options.FileOpenFlags;
            NotifyFlags = options.NotifyFlags;

            if (options.NotifyCallback is { } notify) // is not null
            {
                var gch = GCHandle.Alloc(notify, GCHandleType.Normal);
                gchandles.Add(gch);

                NotifyPayload = (nint)gch;
                NotifyCallback = &Notify;
            }

            if (options.ProgressCallback is { } progress) // is not null
            {
                var gch = GCHandle.Alloc(progress, GCHandleType.Normal);
                gchandles.Add(gch);

                ProgressPayload = (nint)gch;
                ProgressCallback = &Progress;
            }

            Paths = StringArrayMarshaller.ConvertToUnmanaged(options.Paths);
            Baseline = options.Baseline.NativeHandle;
            BaselineIndex = options.BaselineIndex.NativeHandle;
            TargetDirectory = Utf8StringMarshaller.ConvertToUnmanaged(options.TargetDirectory);
            AncestorLabel = Utf8StringMarshaller.ConvertToUnmanaged(options.AncestorLabel);
            OurLabel = Utf8StringMarshaller.ConvertToUnmanaged(options.OurLabel);
            TheirLabel = Utf8StringMarshaller.ConvertToUnmanaged(options.TheirLabel);

            if (options.PerfDataCallback is { } perfdata)
            {
                var gch = GCHandle.Alloc(perfdata, GCHandleType.Normal);
                gchandles.Add(gch);

                PerfDataPayload = (nint)gch;
                PerfDataCallback = &PerformanceData;
            }
        }

        /// <summary>
        /// Free unmanaged resources allocated by <see cref="FromManaged"/>
        /// </summary>
        public readonly void Free()
        {
            StringArrayMarshaller.Free(Paths);
            Utf8StringMarshaller.Free(TargetDirectory);
            Utf8StringMarshaller.Free(AncestorLabel);
            Utf8StringMarshaller.Free(OurLabel);
            Utf8StringMarshaller.Free(TheirLabel);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int Notify(GitCheckoutNotifyFlags why, byte* path, GitDiffFile* baseline, GitDiffFile* target, GitDiffFile* workDir, nint payload)
        {
            var callback = (SharpGit2.GitCheckoutOptions.NotifyCallbackType)((GCHandle)payload).Target!;

            try
            {
                string _path = Git2.GetPooledString(path);

                SharpGit2.GitDiffFile mBaseline = new(in *baseline),
                    mTarget = new(in *target),
                    mWorkDir = new(in *workDir);

                return callback(why, _path, mBaseline, mTarget, mWorkDir);
            }
            catch// (Exception e)
            {
                // TODO: Figure out exception propagation here
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Progress(byte* path, nuint completedSteps, nuint totalSteps, nint payload)
        {
            var callback = (SharpGit2.GitCheckoutOptions.ProgressCallbackType)((GCHandle)payload).Target!;

            try
            {
                string mPath = Git2.GetPooledString(path);

                callback(mPath, completedSteps, totalSteps);
            }
            catch// (Exception e)
            {
                // TODO: Figure out exception propagation here
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void PerformanceData(GitCheckoutPerformanceData* perfData, nint payload)
        {
            var callback = (SharpGit2.GitCheckoutOptions.PerformanceDataCallbackType)((GCHandle)payload).Target!;

            try
            {
                callback(in *perfData);
            }
            catch// (Exception e)
            {
                // TODO: Figure out exception propagation here
            }
        }
    }
}
