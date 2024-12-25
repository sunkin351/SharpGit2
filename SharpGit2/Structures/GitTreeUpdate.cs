using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharpGit2.Marshalling;

namespace SharpGit2
{
    /// <summary>
    /// An action to perform during the update of a tree
    /// </summary>
    [NativeMarshalling(typeof(GitTreeUpdateMarshaller))]
    public struct GitTreeUpdate
    {
        /// <summary>
        /// Update action. If it's an removal, only the path is looked at
        /// </summary>
        public GitTreeUpdateType Action;
        /// <summary>
        /// The entry's id
        /// </summary>
        public GitObjectID Id;
        /// <summary>
        /// The filemode/kind of object
        /// </summary>
        public GitFileMode FileMode;
        /// <summary>
        /// The full path from the root tree
        /// </summary>
        public string Path;
    }

    namespace Native
    {
        /// <summary>
        /// An action to perform during the update of a tree
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct GitTreeUpdate
        {
            /// <summary>
            /// Update action. If it's an removal, only the path is looked at
            /// </summary>
            public GitTreeUpdateType Action;
            /// <summary>
            /// The entry's id
            /// </summary>
            public GitObjectID Id;
            /// <summary>
            /// The filemode/kind of object
            /// </summary>
            public GitFileMode FileMode;
            /// <summary>
            /// The full path from the root tree
            /// </summary>
            public byte* Path;
        }
    }
}