using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitPushUpdate(in Native.GitPushUpdate ptr)
    {
        public string SourceRefname = Git2.GetPooledString(ptr.SourceRefname);
        public string DestinationRefname = Git2.GetPooledString(ptr.DestinationRefname);
        public GitObjectID Source = ptr.Source;
        public GitObjectID Destination = ptr.Destination;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitPushUpdate
    {
        public byte* SourceRefname;
        public byte* DestinationRefname;
        public GitObjectID Source;
        public GitObjectID Destination;

        public void FromManaged(in SharpGit2.GitPushUpdate update)
        {
            SourceRefname = Utf8StringMarshaller.ConvertToUnmanaged(update.SourceRefname);
            DestinationRefname = Utf8StringMarshaller.ConvertToUnmanaged(update.DestinationRefname);
            Source = update.Source;
            Destination = update.Destination;
        }

        public readonly void Free()
        {
            Utf8StringMarshaller.Free(SourceRefname);
            Utf8StringMarshaller.Free(DestinationRefname);
        }
    }
}
