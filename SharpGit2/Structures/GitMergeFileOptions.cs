using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitMergeFileOptions
    {
        public string? AncestorLabel;
        public string? OurLabel;
        public string? TheirLabel;
        public GitMergeFileFavor Favor;
        public GitMergeFileFlags Flags;
        public ushort MarkerSize;

        public GitMergeFileOptions()
        {
            MarkerSize = 7;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitMergeFileOptions
    {
        public uint Version;
        public byte* AncestorLabel;
        public byte* OurLabel;
        public byte* TheirLabel;
        public GitMergeFileFavor Favor;
        public GitMergeFileFlags Flags;
        public ushort MarkerSize;

        public GitMergeFileOptions()
        {
            Version = 1;
            MarkerSize = 7;
        }

        public void FromManaged(in SharpGit2.GitMergeFileOptions options, List<GCHandle> gchandles)
        {
            AncestorLabel = Utf8StringMarshaller.ConvertToUnmanaged(options.AncestorLabel);
            OurLabel = Utf8StringMarshaller.ConvertToUnmanaged(options.OurLabel);
            TheirLabel = Utf8StringMarshaller.ConvertToUnmanaged(options.TheirLabel);
            Favor = options.Favor;
            Flags = options.Flags;
            MarkerSize = options.MarkerSize;
        }

        public void Free()
        {
            Utf8StringMarshaller.Free(AncestorLabel);
            Utf8StringMarshaller.Free(OurLabel);
            Utf8StringMarshaller.Free(TheirLabel);
        }
    }
}
