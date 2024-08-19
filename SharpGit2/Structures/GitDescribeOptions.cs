using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitDescribeOptions
    {
        public uint MaxCandidatesTags;
        public GitDescribeStrategy DescribeStrategy;
        public string? Pattern;
        public bool OnlyFollowFirstParent;
        public bool ShowCommitOIDAsFallback;

        public GitDescribeOptions()
        {
            MaxCandidatesTags = 10;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitDescribeOptions
    {
        public uint Version;
        public uint MaxCandidatesTags;
        public GitDescribeStrategy DescribeStrategy;
        public byte* Pattern;
        public int OnlyFollowFirstParent;
        public int ShowCommitOIDAsFallback;

        public GitDescribeOptions()
        {
            Version = 1;
            MaxCandidatesTags = 10;
        }

        public void FromManaged(in SharpGit2.GitDescribeOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            MaxCandidatesTags = options.MaxCandidatesTags;
            DescribeStrategy = options.DescribeStrategy;
            Pattern = Utf8StringMarshaller.ConvertToUnmanaged(options.Pattern);
            OnlyFollowFirstParent = options.OnlyFollowFirstParent ? 1 : 0;
            ShowCommitOIDAsFallback = options.ShowCommitOIDAsFallback ? 1 : 0;
        }

        public void Free()
        {
            Utf8StringMarshaller.Free(Pattern);
        }
    }
}
