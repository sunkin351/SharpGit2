using System.Runtime.InteropServices;

using SharpGit2.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitFetchOptions
    {
        public int Version;
        public IGitRemoteCallbacks? Callbacks;
        public GitFetchPrune Prune;
        public GitRemoteUpdateFlags UpdateFetchhead;
        public GitRemoteAutoTagOption DownloadTags;
        public GitProxyOptions ProxyOptions;
        public int Depth;
        public GitRemoteRedirectType FollowRedirects;
        public string[]? CustomHeaders;

        public GitFetchOptions()
        {
            Version = 1;
            ProxyOptions = new();
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitFetchOptions
    {
        public int Version;
        public GitRemoteCallbacks Callbacks;
        public GitFetchPrune Prune;
        public GitRemoteUpdateFlags UpdateFetchhead;
        public GitRemoteAutoTagOption DownloadTags;
        public GitProxyOptions ProxyOptions;
        public int Depth;
        public GitRemoteRedirectType FollowRedirects;
        public GitStringArray CustomHeaders;

        public GitFetchOptions()
        {
            Version = 1;
            Callbacks = new();
            UpdateFetchhead = GitRemoteUpdateFlags.FetchHead;
            ProxyOptions = new();
        }

        public void FromManaged(in SharpGit2.GitFetchOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Callbacks.FromManaged(options.Callbacks, gchandles);
            Prune = options.Prune;
            UpdateFetchhead = options.UpdateFetchhead;
            DownloadTags = options.DownloadTags;
            ProxyOptions.FromManaged(in options.ProxyOptions, gchandles);
            Depth = options.Depth;
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
