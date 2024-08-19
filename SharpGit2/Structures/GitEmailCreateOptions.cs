namespace SharpGit2
{
    public unsafe struct GitEmailCreateOptions
    {
        public GitEmailCreateFlags Flags;
        public GitDiffOptions DiffOptions;
        public GitDiffFindOptions DiffFindOptions;
        public string? SubjectPrefix;
        public nuint StartNumber;
        public nuint RerollNumber;

        public GitEmailCreateOptions()
        {
            DiffOptions = new();
            DiffFindOptions = new();
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitEmailCreateOptions
    {
        public uint Version;
        public GitEmailCreateFlags Flags;
        public GitDiffOptions DiffOptions;
        public GitDiffFindOptions DiffFindOptions;
        public byte* SubjectPrefix;
        public nuint StartNumber;
        public nuint RerollNumber;

        public GitEmailCreateOptions()
        {
            Version = 1;
            DiffOptions = new();
            DiffFindOptions = new();
        }
    }
}
