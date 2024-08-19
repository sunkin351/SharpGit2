using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitDiffFile
    {
        public GitObjectID Id;
        public string Path;
        public ulong Size;
        public GitDiffFlags Flags;
        public ushort Mode;
        public ushort IdAbbrev;

        public GitDiffFile(in Native.GitDiffFile ptr)
        {
            Id = ptr.Id;
            Path = Utf8StringMarshaller.ConvertToManaged(ptr.Path)!;
            Size = ptr.Size;
            Flags = ptr.Flags;
            Mode = ptr.Mode;
            IdAbbrev = ptr.IdAbbrev;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitDiffFile
    {
        public GitObjectID Id;
        public byte* Path;
        public ulong Size;
        public GitDiffFlags Flags;
        public ushort Mode;
        public ushort IdAbbrev;
    }
}
