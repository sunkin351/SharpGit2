using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitObjectDatabaseBackendPackOptions
    {
        public GitObjectIDType ObjectIdType;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitObjectDatabaseBackendPackOptions
    {
        public uint Version;
        public GitObjectIDType ObjectIdType;

        public GitObjectDatabaseBackendPackOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitObjectDatabaseBackendPackOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            ObjectIdType = options.ObjectIdType;
        }

        public void Free()
        {
        }
    }
}
