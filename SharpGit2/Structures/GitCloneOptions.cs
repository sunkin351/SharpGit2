using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public struct GitCloneOptions
    {
        public GitCheckoutOptions CheckoutOptions;
        public GitFetchOptions FetchOptions;
        public bool Bare;
        public GitCloneLocalType Local;
        public string CheckoutBranch;
        public RepositoryCreateCallback? RepositoryCallback;
        public RemoteCreateCallback? RemoteCallback;

        public delegate int RepositoryCreateCallback(string path, bool bare, out GitRepository repository);

        public delegate int RemoteCreateCallback(GitRepository repository, string name, string url, out GitRemote remote);
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitCloneOptions
    {
        public uint Version;
        public GitCheckoutOptions CheckoutOptions;
        public GitFetchOptions FetchOptions;
        public int Bare;
        public GitCloneLocalType Local;
        public byte* CheckoutBranch;
        public delegate* unmanaged[Cdecl]<Git2.Repository**, byte*, int, nint, int> RepositoryCallback;
        public nint RepositoryCallbackPayload;
        public delegate* unmanaged[Cdecl]<Git2.Remote**, Git2.Repository*, byte*, byte*, nint, int> RemoteCallback;
        public nint RemoteCallbackPayload;

        public GitCloneOptions()
        {
            Version = 1;
            CheckoutOptions = new();
            FetchOptions = new();
        }

        public void FromManaged(in SharpGit2.GitCloneOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            CheckoutOptions.FromManaged(in options.CheckoutOptions, gchandles);
            FetchOptions.FromManaged(in options.FetchOptions, gchandles);
            Bare = options.Bare ? 1 : 0;
            Local = options.Local;
            CheckoutBranch = Utf8StringMarshaller.ConvertToUnmanaged(options.CheckoutBranch);

            if (options.RepositoryCallback is { } repo_cb)
            {
                var gch = GCHandle.Alloc(repo_cb, GCHandleType.Normal);
                gchandles.Add(gch);

                this.RepositoryCallback = &RepositoryCreateCallback;
                this.RepositoryCallbackPayload = (nint)gch;
            }

            if (options.RemoteCallback is { } remote_cb)
            {
                var gch = GCHandle.Alloc(remote_cb, GCHandleType.Normal);
                gchandles.Add(gch);

                this.RemoteCallback = &RemoteCreateCallback;
                this.RemoteCallbackPayload = (nint)gch;
            }
        }

        /// <summary>
        /// Free unmanaged resources allocated by <see cref="FromManaged"/>
        /// </summary>
        public void Free()
        {
            CheckoutOptions.Free();
            FetchOptions.Free();
            Utf8StringMarshaller.Free(CheckoutBranch);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int RepositoryCreateCallback(Git2.Repository** repo_out, byte* path, int bare, nint payload)
        {
            var callback = (SharpGit2.GitCloneOptions.RepositoryCreateCallback)((GCHandle)payload).Target!;

            try
            {
                string mPath = Git2.GetPooledString(path);

                Debug.Assert(sizeof(GitRepository) == sizeof(void*));

                return callback(mPath, bare != 0, out *(GitRepository*)repo_out);
            }
            catch (Exception e)
            {
                // TODO: Figure out exception propagation here
                GitNativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int RemoteCreateCallback(Git2.Remote** remote_out, Git2.Repository* repo, byte* name, byte* url, nint payload)
        {
            var callback = (SharpGit2.GitCloneOptions.RemoteCreateCallback)((GCHandle)payload).Target!;

            try
            {
                string mName = Git2.GetPooledString(name);
                string mUrl = Git2.GetPooledString(url);

                Debug.Assert(sizeof(GitRemote) == sizeof(void*));

                return callback(new(repo), mName, mUrl, out *(GitRemote*)remote_out);
            }
            catch// (Exception e)
            {
                // TODO: Figure out exception propagation here
                return -1;
            }
        }
    }
}
