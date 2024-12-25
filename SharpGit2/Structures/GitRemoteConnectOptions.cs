using System.Runtime.InteropServices;

using SharpGit2.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitRemoteConnectOptions
    {
        public IGitRemoteCallbacks? Callbacks;
        public GitProxyOptions ProxyOptions = new();
        public GitRemoteRedirectType FollowRedirects;
        public string[]? CustomHeaders;

        public GitRemoteConnectOptions()
        {
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitRemoteConnectOptions
    {
        public uint Version;
        public GitRemoteCallbacks Callbacks;
        public GitProxyOptions ProxyOptions;
        public GitRemoteRedirectType FollowRedirects;
        public GitStringArray CustomHeaders;

        public GitRemoteConnectOptions()
        {
            Version = 1;
            Callbacks = new();
            ProxyOptions = new();
        }

        public void FromManaged(in SharpGit2.GitRemoteConnectOptions options, List<GCHandle> gchandles)
        {
            Callbacks.FromManaged(options.Callbacks, gchandles);
            ProxyOptions.FromManaged(options.ProxyOptions, gchandles);
            FollowRedirects = options.FollowRedirects;
            CustomHeaders = StringArrayMarshaller.ConvertToUnmanaged(options.CustomHeaders);
        }

        /// <summary>
        /// Free unmanaged resources allocated by <see cref="FromManaged"/>
        /// </summary>
        public void Free()
        {
            Callbacks.Free();
            ProxyOptions.Free();
            StringArrayMarshaller.Free(CustomHeaders);
        }
    }
}
