using System.Runtime.InteropServices;

using SharpGit2.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitPushOptions
    {
        public uint PackBuilderParallelism;
        public IGitRemoteCallbacks? Callbacks;
        public GitProxyOptions ProxyOptions;
        public GitRemoteRedirectType FollowRedirects;
        public string[] CustomHeaders;
        public string[] RemotePushOptions;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitPushOptions
    {
        public uint Version;
        public uint PackBuilderParallelism;
        public GitRemoteCallbacks Callbacks;
        public GitProxyOptions ProxyOptions;
        public GitRemoteRedirectType FollowRedirects;
        public GitStringArray CustomHeaders;
        public GitStringArray RemotePushOptions;

        public void FromManaged(in SharpGit2.GitPushOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            PackBuilderParallelism = options.PackBuilderParallelism;
            Callbacks.FromManaged(options.Callbacks, gchandles);
            ProxyOptions.FromManaged(in options.ProxyOptions, gchandles);
            FollowRedirects = options.FollowRedirects;
            CustomHeaders = StringArrayMarshaller.ConvertToUnmanaged(options.CustomHeaders);
            RemotePushOptions = StringArrayMarshaller.ConvertToUnmanaged(options.RemotePushOptions);
        }

        /// <summary>
        /// Free unmanaged resources allocated by <see cref="FromManaged"/>
        /// </summary>
        public void Free()
        {
            Callbacks.Free();
            ProxyOptions.Free();
            StringArrayMarshaller.Free(CustomHeaders);
            StringArrayMarshaller.Free(RemotePushOptions);
        }
    }
}
