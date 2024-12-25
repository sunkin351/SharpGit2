using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitDiffFile(in Native.GitDiffFile ptr)
    {
        public GitObjectID Id = ptr.Id;
        public string Path = Git2.GetPooledString(ptr.Path);
        public ulong Size = ptr.Size;
        public GitDiffFlags Flags = ptr.Flags;
        public ushort Mode = ptr.Mode;
        public ushort IdAbbrev = ptr.IdAbbrev;
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
