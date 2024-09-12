using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public struct GitRebaseOperation
    {
        public GitRebaseOperationType Type;
        public GitObjectID Id;
        public string? Exec;

        public unsafe GitRebaseOperation(in Native.GitRebaseOperation op)
        {
            Type = op.Type;
            Id = op.Id;
            Exec = Utf8StringMarshaller.ConvertToManaged(op.Exec);
        }
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
