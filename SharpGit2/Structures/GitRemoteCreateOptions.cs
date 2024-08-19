using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitRemoteCreateOptions
    {
        public GitRepository Repository;
        public string Name;
        public string FetchSpec;
        public GitRemoteCreateFlags Flags;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitRemoteCreateOptions
    {
        public uint Version;
        public Git2.Repository* Repository;
        public byte* Name;
        public byte* FetchSpec;
        public GitRemoteCreateFlags Flags;

        public GitRemoteCreateOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitRemoteCreateOptions options)
        {
            Version = 1;
            Repository = options.Repository.NativeHandle;
            Name = Utf8StringMarshaller.ConvertToUnmanaged(options.Name);
            FetchSpec = Utf8StringMarshaller.ConvertToUnmanaged(options.FetchSpec);
            Flags = options.Flags;
        }

        public void Free()
        {
            Utf8StringMarshaller.Free(Name);
            Utf8StringMarshaller.Free(FetchSpec);
        }
    }
}
