using System.Runtime.InteropServices;

namespace SharpGit2
{
    public unsafe struct GitObjectDatabaseBackendLooseOptions
    {
        public GitObjectDatabaseBackendLooseFlags Flags;
        public int CompressionLevel;
        public uint DirectoryMode;
        public uint FileMode;
        public GitObjectIDType ObjectIdType;

        public GitObjectDatabaseBackendLooseOptions()
        {
            CompressionLevel = -1;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitObjectDatabaseBackendLooseOptions
    {
        public uint Version;
        public GitObjectDatabaseBackendLooseFlags Flags;
        public int CompressionLevel;
        public uint DirectoryMode;
        public uint FileMode;
        public GitObjectIDType ObjectIdType;

        public GitObjectDatabaseBackendLooseOptions()
        {
            Version = 1;
            CompressionLevel = -1;
        }

        public void FromManaged(in SharpGit2.GitObjectDatabaseBackendLooseOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Flags = options.Flags;
            CompressionLevel = options.CompressionLevel;
            DirectoryMode = options.DirectoryMode;
            FileMode = options.FileMode;
            ObjectIdType = options.ObjectIdType;
        }

        public void Free()
        {
        }
    }
}
