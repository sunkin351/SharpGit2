using System.Runtime.InteropServices;

namespace SharpGit2
{
    public sealed class GitCertificateX509 : GitCertificate
    {
        public byte[]? Data;

        public override unsafe Native.GitCertificateBase* ToUnmanaged()
        {
            var memory = (Native.GitCertificateX509*)NativeMemory.Alloc((nuint)sizeof(Native.GitCertificateX509));
            
            *memory = new();

            try
            {
                if (Data is byte[] { Length: > 0 } data)
                {
                    memory->Data = NativeMemory.Alloc((nuint)data.Length);
                    memory->Length = (nuint)data.Length;

                    new ReadOnlySpan<byte>(data).CopyTo(new Span<byte>(memory->Data, data.Length));
                }

                return (Native.GitCertificateBase*)memory;
            }
            catch
            {
                NativeMemory.Free(memory->Data);
                NativeMemory.Free(memory);
                throw;
            }
        }

        public override unsafe void Free(Native.GitCertificateBase* cert)
        {
            if (cert == null)
                return;

            if (cert->CertificateType != GitCertificateType.X509)
                throw new InvalidOperationException();

            var trueCert = (Native.GitCertificateX509*)cert;

            NativeMemory.Free(trueCert->Data);
            NativeMemory.Free(trueCert);
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitCertificateX509
    {
        public GitCertificateBase Parent;
        public void* Data;
        public nuint Length;

        public GitCertificateX509()
        {
            Parent.CertificateType = GitCertificateType.X509;
        }
    }
}
