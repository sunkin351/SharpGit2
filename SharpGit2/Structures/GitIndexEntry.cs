using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

using SharpGit2.Marshalling;

namespace SharpGit2
{
    /// <summary>
    /// In-memory representation of a file entry in the index.
    /// </summary>
    /// <remarks>
    /// This is a public structure that represents a file entry in the index.
    /// The meaning of the fields corresponds to core Git's documentation (in
    /// "Documentation/technical/index-format.txt").
    /// <br/><br/>
    /// The <c>flags</c> field consists of a number of bit fields which can be
    /// accessed via the first set of `GIT_INDEX_ENTRY_...` bitmasks below.
    /// These flags are all read from and persisted to disk.
    /// <br/><br/>
    /// The flags_extended field also has a number of bit fields which can be
    /// accessed via the later GIT_INDEX_ENTRY_... bitmasks. Some of these
    /// flags are read from and written to disk, but some are set aside for
    /// in-memory only reference.
    /// <br/><br/>
    /// Note that the time and size fields are truncated to 32 bits. This is
    /// enough to detect changes, which is enough for the index to function as a
    /// cache, but it should not be taken as an authoritative source for that data.
    /// </remarks>
    [NativeMarshalling(typeof(GitIndexEntryMarshaller))]
    public unsafe class GitIndexEntry
    {
        public GitIndexTime CTime { get; set; }
        public GitIndexTime MTime { get; set; }
        public uint Dev { get; set; }
        public uint Ino { get; set; }
        public uint Mode { get; set; }
        public uint UID { get; set; }
        public uint GID { get; set; }
        public uint FileSize { get; set; }

        public GitObjectID ID;

        internal ushort _flags;
        internal ushort _flagsExtended;

        public string? Path { get; set; }

        public GitIndexEntry()
        {
        }

        internal GitIndexEntry(Native.GitIndexEntry* ptr)
        {
            CTime = ptr->CTime;
            MTime = ptr->MTime;
            Dev = ptr->Dev;
            Ino = ptr->Ino;
            Mode = ptr->Mode;
            UID = ptr->UID;
            GID = ptr->GID;
            FileSize = ptr->FileSize;
            ID = ptr->ID;
            _flags = ptr->Flags;
            _flagsExtended = ptr->FlagsExtended;

            this.Path = Utf8StringMarshaller.ConvertToManaged(ptr->Path);
        }

        public ushort FlagsExtended
        {
            get => _flagsExtended;
            set => _flagsExtended = value;
        }

        /// <summary>
        /// Flags name bitfield
        /// </summary>
        public uint FlagsName
        {
            get => _flags & 0x0fffu;
            set
            {
                _flags = (ushort)((_flags & ~0x0fffu) | (value & 0x0fff));
            }
        }

        /// <summary>
        /// Flags stage bitfield
        /// </summary>
        public GitIndexStageFlags StageFlags
        {
            get => (GitIndexStageFlags)((_flags & 0x3000u) >> 12);
            set
            {
                ThrowIfInvalidFlags(value);

                _flags = (ushort)((_flags & ~0x3000u) | ((uint)value << 12));
            }
        }

        /// <summary>
        /// Flags flags bitfield
        /// </summary>
        public GitIndexEntryFlags Flags
        {
            get => (GitIndexEntryFlags)(_flags >> 14);
            set
            {
                ThrowIfInvalidFlags(value);

                _flags = (ushort)((_flags & ~0xc000u) | ((uint)value << 14));
            }
        }

        public bool IsConflict => this.StageFlags > 0;

        /// <summary>
        /// Construct Flags field from bitfields
        /// </summary>
        /// <param name="flags">2 bit field</param>
        /// <param name="stage">2 bit field</param>
        /// <param name="name">12 bit field</param>
        public void SetFlags(GitIndexEntryFlags flags, GitIndexStageFlags stage, uint name)
        {
            ThrowIfInvalidFlags(flags);
            ThrowIfInvalidFlags(stage);

            uint _flags = (uint)flags << 14;
            uint _stage = (uint)stage << 12;
            this._flags = (ushort)(_flags | _stage | (name & 0x0fff));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfInvalidFlags(GitIndexStageFlags flags, [CallerArgumentExpression(nameof(flags))] string? paramName = null)
        {
            if ((uint)flags > 3u)
            {
                ThrowInvalidFlags(paramName!, flags);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfInvalidFlags(GitIndexEntryFlags flags, [CallerArgumentExpression(nameof(flags))] string? paramName = null)
        {
            if ((uint)flags > 3u)
            {
                ThrowInvalidFlags(paramName!, flags);
            }
        }

        private static void ThrowInvalidFlags<T>(string paramName, T flags)
        {
            throw new ArgumentOutOfRangeException(paramName, flags, "Invalid flag value(s)!");
        }
    }

    namespace Native
    {
        public unsafe struct GitIndexEntry
        {
            public GitIndexTime CTime;
            public GitIndexTime MTime;

            public uint Dev;
            public uint Ino;
            public uint Mode;
            public uint UID;
            public uint GID;
            public uint FileSize;

            public GitObjectID ID;

            public ushort Flags;
            public ushort FlagsExtended;

            internal byte* Path;
        }
    }
}