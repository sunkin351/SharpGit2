using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitWorktreeAddOptions
    {
        public bool Lock;
        public bool CheckoutExisting;
        public GitReference Ref;
        public GitCheckoutOptions CheckoutOptions;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitWorktreeAddOptions
    {
        public uint Version;
        public int Lock;
        public int CheckoutExisting;
        public Git2.Reference* Ref;
        public GitCheckoutOptions CheckoutOptions;

        public GitWorktreeAddOptions()
        {
            Version = 1;
            CheckoutOptions = new();
        }

        public void FromManaged(in SharpGit2.GitWorktreeAddOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Lock = options.Lock ? 1 : 0;
            CheckoutExisting = options.CheckoutExisting ? 1 : 0;
            Ref = options.Ref.NativeHandle;
            CheckoutOptions.FromManaged(in options.CheckoutOptions, gchandles);
        }

        public void Free()
        {
            CheckoutOptions.Free();
        }
    }
}
