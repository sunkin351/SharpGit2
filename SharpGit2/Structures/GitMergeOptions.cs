using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitMergeOptions
    {
        public GitMergeFlags Flags;
        public uint RenameThreshold;
        public uint TargetLimit;
        public IGitDiffSimilarityMetric? Metric;
        public uint RecursionLimit;
        public string? DefaultDriver;
        public GitMergeFileFavor FileFavor;
        public GitMergeFileFlags FileFlags;

        public GitMergeOptions()
        {
            Flags = GitMergeFlags.FindRenames;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitMergeOptions
    {
        public uint Version;
        public GitMergeFlags Flags;
        public uint RenameThreshold;
        public uint TargetLimit;
        public GitDiffSimilarityMetric* Metric;
        public uint RecursionLimit;
        public byte* DefaultDriver;
        public GitMergeFileFavor FileFavor;
        public GitMergeFileFlags FileFlags;

        public GitMergeOptions()
        {
            Version = 1;
            Flags = GitMergeFlags.FindRenames;
        }

        public void FromManaged(in SharpGit2.GitMergeOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Flags = options.Flags;
            RenameThreshold = options.RenameThreshold;
            TargetLimit = options.TargetLimit;

            if (options.Metric is { } metricCallbacks)
            {
                Metric = (GitDiffSimilarityMetric*)NativeMemory.AllocZeroed((nuint)sizeof(GitDiffSimilarityMetric));

                Metric->FromManaged(metricCallbacks, gchandles);
            }

            RecursionLimit = options.RecursionLimit;
            DefaultDriver = Utf8StringMarshaller.ConvertToUnmanaged(options.DefaultDriver);
            FileFavor = options.FileFavor;
            FileFlags = options.FileFlags;
        }

        /// <summary>
        /// Free unmanaged resources allocated by <see cref="FromManaged"/>
        /// </summary>
        public void Free()
        {
            NativeMemory.Free(this.Metric);
            Utf8StringMarshaller.Free(this.DefaultDriver);
        }
    }
}
