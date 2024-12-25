using System.Runtime.InteropServices;

namespace SharpGit2
{
    /// <summary>
    /// Worktree add options structure
    /// </summary>
    public unsafe struct GitWorktreeAddOptions
    {
        /// <summary>
        /// Lock newly created worktree
        /// </summary>
        public bool Lock;
        /// <summary>
        /// 
        /// </summary>
        public bool CheckoutExisting;
        /// <summary>
        /// Reference to use for the new worktree HEAD
        /// </summary>
        public GitReference Ref;
        /// <summary>
        /// Options for the checkout.
        /// </summary>
        public GitCheckoutOptions CheckoutOptions;
    }
}

namespace SharpGit2.Native
{
    /// <summary>
    /// Worktree add options structure
    /// </summary>
    public unsafe struct GitWorktreeAddOptions
    {
        public uint Version;

        /// <summary>
        /// Lock newly created worktree
        /// </summary>
        public int Lock;

        //public int CheckoutExisting;
        /// <summary>
        /// Reference to use for the new worktree HEAD
        /// </summary>
        public Git2.Reference* Ref;
        /// <summary>
        /// Options for the checkout.
        /// </summary>
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
            //CheckoutExisting = options.CheckoutExisting ? 1 : 0;
            Ref = options.Ref.NativeHandle;
            CheckoutOptions.FromManaged(in options.CheckoutOptions, gchandles);
        }

        public void Free()
        {
            CheckoutOptions.Free();
        }
    }
}
