using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitCommitCreateOptions
    {
        public bool AllowEmptyCommit;
        public GitSignature? Author;
        public GitSignature? Committer;
        public string? MessageEncoding;
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitCommitCreateOptions
    {
        public uint Version;
        private uint _allowEmptyCommit;
        public GitSignature* Author;
        public GitSignature* Committer;
        public byte* MessageEncoding;

        public GitCommitCreateOptions()
        {
            Version = 1;
        }

        public bool AllowEmptyCommit
        {
            readonly get => (_allowEmptyCommit & 1) != 0;
            set => _allowEmptyCommit = value ? 1u : 0;
        }

        public void FromManaged(in SharpGit2.GitCommitCreateOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            AllowEmptyCommit = options.AllowEmptyCommit;
            Author = GitSignature.FromManaged(options.Author);
            Committer = GitSignature.FromManaged(options.Committer);
            MessageEncoding = Utf8StringMarshaller.ConvertToUnmanaged(options.MessageEncoding);
        }

        public void Free()
        {
            if (Author != null)
            {
                Author->Free();
                NativeMemory.Free(Author);
            }

            if (Committer != null)
            {
                Committer->Free();
                NativeMemory.Free(Committer);
            }

            Utf8StringMarshaller.Free(MessageEncoding);
        }
    }
}
