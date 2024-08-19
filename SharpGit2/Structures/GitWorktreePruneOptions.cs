namespace SharpGit2
{
    public unsafe struct GitWorktreePruneOptions
    {
        public GitWorktreePruneFlags Flags;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitWorktreePruneOptions
    {
        public uint Version;
        public GitWorktreePruneFlags Flags;

        public GitWorktreePruneOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitWorktreePruneOptions options)
        {
            Version = 1;
            Flags = options.Flags;
        }

        public void Free()
        {
        }
    }
}
