using SharpGit2.Native;

namespace SharpGit2
{
    public unsafe struct GitStashSaveOptions
    {
        public GitStashFlags Flags;
        public GitSignature? Stasher;
        public string Message;
        public string[] Paths;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitStashSaveOptions
    {
        public uint Version;
        public GitStashFlags Flags;
        public GitSignature* Stasher;
        public byte* Message;
        public GitStringArray Paths;

        public GitStashSaveOptions()
        {
            Version = 1;
        }
    }
}
