using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
