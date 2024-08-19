using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitCherrypickOptions
    {
        public uint Mainline;
        public GitMergeOptions MergeOptions;
        public GitCheckoutOptions CheckoutOptions;

        public GitCherrypickOptions()
        {
            MergeOptions = new();
            CheckoutOptions = new();
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitCherrypickOptions
    {
        public uint Version;
        public uint Mainline;
        public GitMergeOptions MergeOptions;
        public GitCheckoutOptions CheckoutOptions;

        public GitCherrypickOptions()
        {
            Version = 1;
            Mainline = 0;
            MergeOptions = new();
            CheckoutOptions = new();
        }

        public void FromManaged(in SharpGit2.GitCherrypickOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Mainline = options.Mainline;
            MergeOptions.FromManaged(in options.MergeOptions, gchandles);
            CheckoutOptions.FromManaged(in options.CheckoutOptions, gchandles);
        }

        public void Free()
        {
            MergeOptions.Free();
            CheckoutOptions.Free();
        }
    }
}
