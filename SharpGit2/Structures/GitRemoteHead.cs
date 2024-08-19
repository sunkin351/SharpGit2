namespace SharpGit2
{
    public unsafe struct GitRemoteHead
    {
        public bool Local;
        public GitObjectID OID;
        public GitObjectID LOID;
        public string Name;
        public string SymRefTarget;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitRemoteHead
    {
        public int Local;
        public GitObjectID OID;
        public GitObjectID LOID;
        public byte* Name;
        public byte* SymRefTarget;
    }
}
