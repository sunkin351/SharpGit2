using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2
{
    using static SharpGit2.Native.GitCertificateHostKey;

    public sealed class GitCertificateHostKey : GitCertificate
    {
        public GitCertificateSSHType Type;
        public MD5HashMemory HashMD5;
        public SHA1HashMemory HashSHA1;
        public SHA256HashMemory HashSHA256;
        public GitCertificateSSHRawType RawType;
        public byte[]? HostKey;

        public GitCertificateHostKey()
        {
        }

        public override unsafe Native.GitCertificateBase* ToUnmanaged()
        {
            var memory = (Native.GitCertificateHostKey*)NativeMemory.Alloc((nuint)sizeof(Native.GitCertificateHostKey));
            
            // Initialize memory to a valid value, mainly zero
            *memory = new();

            try
            {
                memory->Type = this.Type;
                memory->HashMD5 = this.HashMD5;
                memory->HashSHA1 = this.HashSHA1;
                memory->HashSHA256 = this.HashSHA256;
                memory->RawType = this.RawType;

                if (this.HostKey is byte[] key)
                {
                    memory->HostKeyLength = (nuint)key.Length;
                    memory->HostKey = (byte*)NativeMemory.Alloc((nuint)key.Length);

                    new ReadOnlySpan<byte>(key).CopyTo(new Span<byte>(memory->HostKey, key.Length));
                }

                return (Native.GitCertificateBase*)memory;
            }
            catch
            {
                NativeMemory.Free(memory->HostKey);
                NativeMemory.Free(memory);
                throw;
            }
        }

        public override unsafe void Free(Native.GitCertificateBase* cert)
        {
            if (cert->CertificateType != GitCertificateType.HostkeyLibSSH2)
                throw new InvalidOperationException();

            var trueCert = (Native.GitCertificateHostKey*)cert;

            NativeMemory.Free(trueCert->HostKey);
            NativeMemory.Free(trueCert);
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitCertificateHostKey
    {
        public GitCertificateBase Parent; // C Equivilent of base class/type
        public GitCertificateSSHType Type;
        public MD5HashMemory HashMD5;
        public SHA1HashMemory HashSHA1;
        public SHA256HashMemory HashSHA256;
        public GitCertificateSSHRawType RawType;
        public byte* HostKey;
        public nuint HostKeyLength;

        public GitCertificateHostKey()
        {
            Parent.CertificateType = GitCertificateType.HostkeyLibSSH2;
        }

        [InlineArray(16)]
        public struct MD5HashMemory
        {
            public byte Element;
        }

        [InlineArray(20)]
        public struct SHA1HashMemory
        {
            public byte Element;
        }

        [InlineArray(32)]
        public struct SHA256HashMemory
        {
            public byte Element;
        }
    }
}
