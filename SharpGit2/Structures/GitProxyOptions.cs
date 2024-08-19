using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitProxyOptions
    {
        public GitProxyType Type;
        public string? Url;
        public ICallbacks? Callbacks;

        public interface ICallbacks
        {
            /// <summary>
            /// Credential acquisition callback.
            /// This will be called if the remote host requires
            /// authentication in order to connect to it.
            /// </summary>
            /// <param name="url">
            /// The resource for which we are demanding a credential.
            /// </param>
            /// <param name="usernameFromUrl">
            /// The username that was embedded in a "user\@host"
            /// remote url, or NULL if not included.
            /// </param>
            /// <param name="allowedTypes">
            /// A bitmask stating which credential types are OK to return.
            /// </param>
            /// <param name="credentials">
            /// The newly created credential object.
            /// </param>
            /// <returns>
            /// 0 for success, less than 0 to indicate an error, greater than 0 to indicate no credential was acquired. (Negative values given meaning in <see cref="GitError"/>)
            /// </returns>
            /// <remarks>
            /// Returning <see cref="GitError.PassThrough"/> will make libgit2 behave as if this method wasn't given to it.
            /// </remarks>
            int GetCredentials(string url, string? usernameFromUrl, GitCredentialType allowedTypes, out GitCredential credentials)
            {
                Unsafe.SkipInit(out credentials);
                return (int)GitError.PassThrough;
            }

            /// <summary>
            /// Callback for the user's custom certificate checks.
            /// </summary>
            /// <param name="certificate">The host certificate</param>
            /// <param name="valid">
            /// Whether the libgit2 checks (OpenSSL or WinHTTP) think
            /// this certificate is valid
            /// </param>
            /// <param name="host">Hostname of the host libgit2 connected to</param>
            /// <returns>
            /// 0 to proceed with the connection, less than 0 to fail the connection,
            /// or greater than 0 to indicate that the callback refused to act and that
            /// the existing validity determination should be honored
            /// </returns>
            /// <remarks>
            /// If cert verification fails, this will be called to let the
            /// user make the final decision of whether to allow the
            /// connection to proceed.
            /// </remarks>
            int OnCertificateCheck(GitCertificate? certificate, bool valid, string host)
            {
                return 1; // Accept libgit2's validity decision
            }
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitProxyOptions
    {
        public uint Version;
        public GitProxyType Type;
        public byte* Url;
        public delegate* unmanaged[Cdecl]<Git2.Credential**, byte*, byte*, GitCredentialType, nint, int> Credentials;
        public delegate* unmanaged[Cdecl]<GitCertificateBase*, int, byte*, nint, int> CertificateCheck;
        public nint Payload;

        public GitProxyOptions()
        {
            Version = 1;
        }

        public void FromManaged(in SharpGit2.GitProxyOptions options, List<GCHandle> gchandles)
        {
            Version = 1;
            Type = options.Type;
            Url = Utf8StringMarshaller.ConvertToUnmanaged(options.Url);

            if (options.Callbacks is { } callbacks)
            {
                var gch = GCHandle.Alloc(callbacks, GCHandleType.Normal);
                gchandles.Add(gch);

                Credentials = &OnGetCredentials;
                CertificateCheck = &OnCertificateCheck;
                Payload = (nint)gch;
            }
        }

        /// <summary>
        /// Free unmanaged resources allocated by <see cref="FromManaged"/>
        /// </summary>
        public void Free()
        {
            Utf8StringMarshaller.Free(Url);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnGetCredentials(Git2.Credential** out_credential, byte* url, byte* usernameFromUrl, GitCredentialType allowedTypes, nint payload)
        {
            var callbacks = (SharpGit2.GitProxyOptions.ICallbacks)((GCHandle)payload).Target!;

            try
            {
                string mUrl = Utf8StringMarshaller.ConvertToManaged(url)!;
                string? mUsernameFromUrl = Utf8StringMarshaller.ConvertToManaged(usernameFromUrl);

                Debug.Assert(sizeof(GitCredential) == sizeof(void*)); // Ensure struct size assumption

                return callbacks.GetCredentials(mUrl, mUsernameFromUrl, allowedTypes, out *(GitCredential*)out_credential);
            }
            catch (Exception e)
            {
                NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnCertificateCheck(GitCertificateBase* certificate, int isValid, byte* host, nint payload)
        {
            var callbacks = (SharpGit2.GitProxyOptions.ICallbacks)((GCHandle)payload).Target!;

            try
            {
                var cert = GitCertificate.FromUnmanaged(certificate);

                string mHost = Utf8StringMarshaller.ConvertToManaged(host)!;

                return callbacks.OnCertificateCheck(cert, isValid != 0, mHost);
            }
            catch (Exception e)
            {
                NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                return e is NotSupportedException ? (int)GitError.NotSupported : -1;
            }
        }
    }
}
