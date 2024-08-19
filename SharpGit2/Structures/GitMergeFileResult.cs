namespace SharpGit2
{
    public unsafe struct GitMergeFileResult
    {
        public uint AutoMergeable;
        public string Path;
        public uint Mode;
        public byte* FileContent;
        public nuint ContentLength;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitMergeFileResult
    {
        public uint AutoMergeable;
        public byte* Path;
        public uint Mode;
        public byte* FileContent;
        public nuint ContentLength;
    }
}
