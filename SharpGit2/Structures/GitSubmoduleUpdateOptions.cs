using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitSubmoduleUpdateOptions
    {
        public GitCheckoutOptions CheckoutOptions;
        public GitFetchOptions FetchOptions;
        public bool AllowFetch;

        public GitSubmoduleUpdateOptions()
        {
            CheckoutOptions = new();
            FetchOptions = new();
            AllowFetch = true;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitSubmoduleUpdateOptions
    {
        public uint Version;
        public GitCheckoutOptions CheckoutOptions;
        public GitFetchOptions FetchOptions;
        public int AllowFetch;

        public GitSubmoduleUpdateOptions()
        {
            Version = 1;
            CheckoutOptions = new();
            FetchOptions = new();
            AllowFetch = 1;
        }

        public void FromManaged(in SharpGit2.GitSubmoduleUpdateOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            CheckoutOptions.FromManaged(in options.CheckoutOptions, gchandles);
            FetchOptions.FromManaged(in options.FetchOptions, gchandles);
            AllowFetch = options.AllowFetch ? 1 : 0;
        }

        public void Free()
        {
            CheckoutOptions.Free();
            FetchOptions.Free();
        }
    }
}
