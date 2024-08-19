using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2
{
    public unsafe struct GitRebaseOptions
    {
        public bool Quiet;
        public bool InMemory;
        public string? RewriteNotesRef;
        public GitMergeOptions MergeOptions;
        public GitCheckoutOptions CheckoutOptions;
        public CreateCommitCallback? CreateCommit;

        public delegate int CreateCommitCallback(
            in GitSignature author,
            in GitSignature committer,
            string message,
            GitTree tree,
            ReadOnlySpan<GitCommit> parents,
            out GitObjectID out_id);
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitRebaseOptions
    {
        public uint Version;
        public int Quiet;
        public int InMemory;
        public byte* RewriteNotesRef;
        public GitMergeOptions MergeOptions;
        public GitCheckoutOptions CheckoutOptions;
        public delegate* unmanaged[Cdecl]<GitObjectID*, GitSignature*, GitSignature*, byte*, byte*, Git2.Tree*, nuint, Git2.Commit**, nint, int> CreateCommit;
        private nint Reserved; // deprecated/obsolete field
        public nint Payload;

        public GitRebaseOptions()
        {
            Version = 1;
            MergeOptions = new();
            CheckoutOptions = new();
        }

        public void FromManaged(in SharpGit2.GitRebaseOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Quiet = options.Quiet ? 1 : 0;
            InMemory = options.InMemory ? 1 : 0;
            RewriteNotesRef = Utf8StringMarshaller.ConvertToUnmanaged(options.RewriteNotesRef);
            MergeOptions.FromManaged(in options.MergeOptions, gchandles);
            CheckoutOptions.FromManaged(in options.CheckoutOptions, gchandles);

            if (options.CreateCommit is { } createCommit)
            {
                var gch = GCHandle.Alloc(createCommit, GCHandleType.Normal);
                gchandles.Add(gch);

                CreateCommit = &CreateCommitNativeCallback;
                Payload = (nint)gch;
            }
        }

        public void Free()
        {
            Utf8StringMarshaller.Free(RewriteNotesRef);
            MergeOptions.Free();
            CheckoutOptions.Free();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int CreateCommitNativeCallback(
            GitObjectID* out_id,
            GitSignature* author,
            GitSignature* committer,
            byte* message_encoding,
            byte* message,
            Git2.Tree* tree,
            nuint parent_count,
            Git2.Commit** parents,
            nint payload)
        {
            var callback = (SharpGit2.GitRebaseOptions.CreateCommitCallback)((GCHandle)payload).Target!;

            try
            {
                var encoding = Utf8StringMarshaller.ConvertToManaged(message_encoding) is string encodingName
                    ? Encoding.GetEncoding(encodingName)
                    : Encoding.UTF8;

                if (!encoding.IsSingleByte)
                {
                    NativeApi.git_error_set_str(GitErrorClass.Callback, "Multi-byte code unit string encodings are not supported! (utf-16/32 and the like)");
                    return (int)GitError.NotSupported;
                }

                string messageStr = encoding.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(message));

                SharpGit2.GitSignature mAuthor = new(in *author),
                    mCommitter = new(in *committer);

                Debug.Assert(sizeof(GitCommit) == sizeof(void*));

                return callback(
                    in mAuthor,
                    in mCommitter,
                    messageStr,
                    new(tree),
                    new ReadOnlySpan<GitCommit>(parents, checked((int)parent_count)),
                    out *out_id);
            }
            catch (Exception e)
            {
                NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return e is ArgumentException ? (int)GitError.Invalid : -1;
            }
        }
    }
}
