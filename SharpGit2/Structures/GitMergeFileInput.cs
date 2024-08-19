namespace SharpGit2
{
    public struct GitMergeFileInput
    {
        public string FileContent;
        public string Path;
        public uint Mode;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitMergeFileInput
    {
        public uint Version;
        public byte* FileContent;
        public nuint ContentLength;
        public byte* Path;
        public uint Mode;

        public GitMergeFileInput()
        {
            Version = 1;
        }
    }
}
