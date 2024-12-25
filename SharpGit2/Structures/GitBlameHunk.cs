using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2
{
    public unsafe struct GitBlameHunk
    {
        public nuint LinesInHunk;
        public GitObjectID FinalCommitId;
        public nuint FinalStartLineNumber;
        public GitSignature? FinalSignature;
        public GitObjectID OriginalCommitId;
        public string? OriginalPath;
        public nuint OriginalStartLineNumber;
        public GitSignature? OriginalSignature;
        public byte Boundary;

        public GitBlameHunk(in Native.GitBlameHunk hunk)
        {
            LinesInHunk = hunk.LinesInHunk;
            FinalCommitId = hunk.FinalCommitId;
            FinalStartLineNumber = hunk.FinalStartLineNumber;
            FinalSignature = hunk.FinalSignature != null ? new GitSignature(in *hunk.FinalSignature) : null;
            OriginalCommitId = hunk.OriginalCommitId;

            var nativeOPath = hunk.OriginalPath;

            OriginalPath = nativeOPath == null ? null : Git2.GetPooledString(nativeOPath);

            OriginalStartLineNumber = hunk.OriginalStartLineNumber;
            OriginalSignature = hunk.OriginalSignature != null ? new GitSignature(in *hunk.OriginalSignature) : null;
            Boundary = hunk.Boundary;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitBlameHunk
    {
        public nuint LinesInHunk;
        public GitObjectID FinalCommitId;
        public nuint FinalStartLineNumber;
        public GitSignature* FinalSignature;
        public GitObjectID OriginalCommitId;
        public byte* OriginalPath;
        public nuint OriginalStartLineNumber;
        public GitSignature* OriginalSignature;
        public byte Boundary;
    }
}
