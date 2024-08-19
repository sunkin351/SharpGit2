using SharpGit2.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitStatusOptions
    {
        public GitStatusShow Show;
        public GitStatusFlags Flags;
        public string[] PathSpec;
        public GitTree Baseline;
        public ushort RenameThreshold;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitStatusOptions
    {
        public uint Version;
        public GitStatusShow Show;
        public GitStatusFlags Flags;
        public GitStringArray PathSpec;
        public Git2.Tree* Baseline;
        public ushort RenameThreshold;

        public GitStatusOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitStatusOptions options)
        {
            Version = 1;
            Show = options.Show;
            Flags = options.Flags;
            PathSpec = StringArrayMarshaller.ConvertToUnmanaged(options.PathSpec);
            Baseline = options.Baseline.NativeHandle;
            RenameThreshold = options.RenameThreshold;
        }

        public void Free()
        {
            StringArrayMarshaller.Free(PathSpec);
        }
    }
}
