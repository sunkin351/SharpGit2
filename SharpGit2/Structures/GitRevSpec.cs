using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SharpGit2
{
    public unsafe struct GitRevSpec
    {
        public GitObject From;
        public GitObject To;
        public GitRevSpecType Flags;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitRevSpec
    {
        public Git2.Object* From;
        public Git2.Object* To;
        public GitRevSpecType Flags;

        public void FromManaged(in SharpGit2.GitRevSpec spec)
        {
            this = Unsafe.As<SharpGit2.GitRevSpec, GitRevSpec>(ref Unsafe.AsRef(in spec));

            Debug.Assert(From == spec.From.NativeHandle
                && To == spec.To.NativeHandle
                && Flags == spec.Flags);
        }

        public void Free()
        {
        }
    }
}
