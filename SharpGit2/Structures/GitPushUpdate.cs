using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitPushUpdate
    {
        public string SourceRefname;
        public string DestinationRefname;
        public GitObjectID Source;
        public GitObjectID Destination;

        public GitPushUpdate(in Native.GitPushUpdate ptr)
        {
            SourceRefname = Utf8StringMarshaller.ConvertToManaged(ptr.SourceRefname)!;
            DestinationRefname = Utf8StringMarshaller.ConvertToManaged(ptr.DestinationRefname)!;
            Source = ptr.Source;
            Destination = ptr.Destination;
        }
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

        public void Free()
        {
            Utf8StringMarshaller.Free(SourceRefname);
            Utf8StringMarshaller.Free(DestinationRefname);
        }
    }
}
