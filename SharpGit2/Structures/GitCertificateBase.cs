namespace SharpGit2
{
    public unsafe abstract class GitCertificate
    {
        public abstract Native.GitCertificateBase* ToUnmanaged();

        public abstract void Free(Native.GitCertificateBase* cert);

        public static GitCertificate? FromUnmanaged(Native.GitCertificateBase* unmanagedPtr)
        {
            switch (unmanagedPtr->CertificateType)
            {
                case GitCertificateType.None:
                    return null;
                case GitCertificateType.HostkeyLibSSH2:
                {
                    var data = (Native.GitCertificateHostKey*)unmanagedPtr;

                    return new GitCertificateHostKey()
                    {
                        Type = data->Type,
                        HashMD5 = data->HashMD5,
                        HashSHA1 = data->HashSHA1,
                        HashSHA256 = data->HashSHA256,
                        RawType = data->RawType,
                        HostKey = data->HostKeyLength == 0 ? null : new ReadOnlySpan<byte>(data->HostKey, checked((int)data->HostKeyLength)).ToArray()
                    };
                }
                case GitCertificateType.X509:
                {
                    var data = (Native.GitCertificateX509*)unmanagedPtr;

                    return new GitCertificateX509()
                    {
                        Data = data->Length == 0 ? null : new ReadOnlySpan<byte>(data->Data, checked((int)data->Length)).ToArray()
                    };
                }
                default:
                    throw new NotSupportedException($"The {unmanagedPtr->CertificateType} certificate type is not supported!");
            }
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitCertificateBase
    {
        public GitCertificateType CertificateType;
    }
}
