using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitRebaseOperation(in Native.GitRebaseOperation op)
    {
        public GitRebaseOperationType Type = op.Type;
        public GitObjectID Id = op.Id;
        public string? Exec = op.Exec == null ? null : Git2.GetPooledString(op.Exec);
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitRebaseOperation
    {
        public GitRebaseOperationType Type;
        public GitObjectID Id;
        public byte* Exec;
    }
}
