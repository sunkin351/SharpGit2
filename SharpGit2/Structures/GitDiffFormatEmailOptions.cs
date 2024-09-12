using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2
{
    public unsafe struct GitDiffFormatEmailOptions
    {
        public GitDiffFormatEmailFlags Flags;
        public nuint PatchNumber;
        public nuint TotalPatches;
        public GitObjectID? Id;
        public string? Summary;
        public string? Body;
        public GitSignature? Author;

        public GitDiffFormatEmailOptions()
        {
            PatchNumber = 1;
            TotalPatches = 1;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitDiffFormatEmailOptions
    {
        public uint Version;
        public GitDiffFormatEmailFlags Flags;
        public nuint PatchNumber;
        public nuint TotalPatches;
        public GitObjectID* Id;
        public byte* Summary;
        public byte* Body;
        public GitSignature* Author;

        public GitDiffFormatEmailOptions()
        {
            Version = 1;
            PatchNumber = 1;
            TotalPatches = 1;
        }
    }
}
