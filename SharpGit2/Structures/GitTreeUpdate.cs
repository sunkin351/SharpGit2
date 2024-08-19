using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharpGit2.Marshalling;

namespace SharpGit2
{
    [NativeMarshalling(typeof(GitTreeUpdateMarshaller))]
    public struct GitTreeUpdate
    {
        public GitTreeUpdateType Action;
        public GitObjectID Id;
        public GitFileMode FileMode;
        public string Path;
    }

    namespace Native
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct GitTreeUpdate
        {
            public GitTreeUpdateType Action;
            public GitObjectID Id;
            public GitFileMode FileMode;
            public byte* Path;
        }
    }
}