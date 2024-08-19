using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitRevertOptions
    {
        public uint Mainline;
        public GitMergeOptions MergeOptions;
        public GitCheckoutOptions CheckoutOptions;

        public GitRevertOptions()
        {
            MergeOptions = new();
            CheckoutOptions = new();
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitRevertOptions
    {
        public uint Version;
        public uint Mainline;
        public GitMergeOptions MergeOptions;
        public GitCheckoutOptions CheckoutOptions;

        public GitRevertOptions()
        {
            Version = 1;
            MergeOptions = new();
            CheckoutOptions = new();
        }

        public void FromManaged(in SharpGit2.GitRevertOptions options, List<GCHandle> gchandles)
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
