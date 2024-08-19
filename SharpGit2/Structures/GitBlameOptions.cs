using System.Runtime.InteropServices;

namespace SharpGit2
{
    public struct GitBlameOptions
    {
        public GitBlameFlags Flags;
        public ushort MinMatchCharacters;
        public GitObjectID NewestCommit;
        public GitObjectID OldestCommit;
        public nuint MinLine;
        public nuint MaxLine;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitBlameOptions
    {
        public uint Version;
        public GitBlameFlags Flags;
        public ushort MinMatchCharacters;
        public GitObjectID NewestCommit;
        public GitObjectID OldestCommit;
        public nuint MinLine;
        public nuint MaxLine;

        public GitBlameOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitBlameOptions options, List<GCHandle> gchandles)
        {
            Version = 1;

            Flags = options.Flags;
            MinMatchCharacters = options.MinMatchCharacters;
            NewestCommit = options.NewestCommit;
            OldestCommit = options.OldestCommit;
            MinLine = options.MinLine;
            MaxLine = options.MaxLine;
        }

        public void Free()
        {
        }
    }
}