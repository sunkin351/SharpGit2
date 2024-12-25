using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitDescribeFormatOptions
    {
        public uint AbbreviatedSize;
        public bool AlwaysUseLongFormat;
        public string? DirtySuffix;

        public GitDescribeFormatOptions()
        {
            AbbreviatedSize = 7;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitDescribeFormatOptions
    {
        public uint Version;
        public uint AbbreviatedSize;
        public int AlwaysUseLongFormat;
        public byte* DirtySuffix;

        public GitDescribeFormatOptions()
        {
            Version = 1;
            AbbreviatedSize = 7;
        }

        public void FromManaged(in SharpGit2.GitDescribeFormatOptions options)
        {
            Version = 1;
            AbbreviatedSize = options.AbbreviatedSize;
            AlwaysUseLongFormat = options.AlwaysUseLongFormat ? 1 : 0;
            DirtySuffix = Utf8StringMarshaller.ConvertToUnmanaged(options.DirtySuffix);
        }

        public void Free()
        {
            Utf8StringMarshaller.Free(DirtySuffix);
        }
    }
}
