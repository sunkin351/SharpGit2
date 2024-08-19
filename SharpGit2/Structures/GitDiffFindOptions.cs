using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitDiffFindOptions
    {
        public GitDiffFindFlags Flags;
        public ushort RenameThreshold;
        public ushort RenameFromRewriteThreshold;
        public ushort CopyThreshold;
        public ushort BreakRewriteThreshold;
        public nuint RenameLimit;
        public IGitDiffSimilarityMetric? Metric;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitDiffFindOptions
    {
        public uint Version;
        public GitDiffFindFlags Flags;
        public ushort RenameThreshold;
        public ushort RenameFromRewriteThreshold;
        public ushort CopyThreshold;
        public ushort BreakRewriteThreshold;
        public nuint RenameLimit;
        public GitDiffSimilarityMetric* Metric;

        public GitDiffFindOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitDiffFindOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Flags = options.Flags;
            RenameThreshold = options.RenameThreshold;
            RenameFromRewriteThreshold = options.RenameFromRewriteThreshold;
            CopyThreshold = options.CopyThreshold;
            BreakRewriteThreshold = options.BreakRewriteThreshold;
            RenameLimit = options.RenameLimit;

            if (options.Metric is { } metric)
            {
                Metric = (GitDiffSimilarityMetric*)NativeMemory.AllocZeroed((nuint)sizeof(GitDiffSimilarityMetric));

                Metric->FromManaged(metric, gchandles);
            }
        }

        public void Free()
        {
            NativeMemory.Free(Metric);
        }
    }
}
