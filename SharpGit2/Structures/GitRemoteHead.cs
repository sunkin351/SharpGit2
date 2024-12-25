namespace SharpGit2
{
    public unsafe struct GitRemoteHead(in Native.GitRemoteHead native)
    {
        public bool Local = native.Local != 0;
        public GitObjectID OID = native.OID;
        public GitObjectID LOID = native.LOID;
        public string Name = Git2.GetPooledString(native.Name)!;
        public string SymRefTarget = Git2.GetPooledString(native.SymRefTarget)!;
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
