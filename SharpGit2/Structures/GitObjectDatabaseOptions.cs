using System.Runtime.InteropServices;

namespace SharpGit2
{
    public struct GitObjectDatabaseOptions
    {
        public GitObjectIDType ObjectIdType;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitObjectDatabaseOptions
    {
        public uint Version;
        public GitObjectIDType ObjectIdType;

        public GitObjectDatabaseOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitObjectDatabaseOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            ObjectIdType = options.ObjectIdType;
        }

        public void Free()
        {
        }
    }
}
