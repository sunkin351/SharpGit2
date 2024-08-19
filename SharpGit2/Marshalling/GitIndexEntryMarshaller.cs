using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2.Marshalling;

[CustomMarshaller(typeof(GitIndexEntry), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
internal unsafe static class GitIndexEntryMarshaller
{
    public ref struct ManagedToUnmanagedIn
    {
        public static int BufferSize => 128;

        private Utf8StringMarshaller.ManagedToUnmanagedIn _pathMarshaller;
        private Native.GitIndexEntry _structure;

        public void FromManaged(GitIndexEntry entry, Span<byte> buffer)
        {
            _pathMarshaller.FromManaged(entry.Path, buffer);

            _structure.CTime = entry.CTime;
            _structure.MTime = entry.MTime;
            _structure.Dev = entry.Dev;
            _structure.Ino = entry.Ino;
            _structure.Mode = entry.Mode;
            _structure.UID = entry.UID;
            _structure.GID = entry.GID;
            _structure.FileSize = entry.FileSize;
            _structure.ID = entry.ID;
            _structure.Flags = entry._flags;
            _structure.FlagsExtended = entry._flagsExtended;
            _structure.Path = _pathMarshaller.ToUnmanaged();
        }

        public Native.GitIndexEntry* ToUnmanaged()
        {
            return (Native.GitIndexEntry*)Unsafe.AsPointer(ref _structure);
        }

        public void Free()
        {
            _pathMarshaller.Free();
        }
    }
}