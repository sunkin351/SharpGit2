
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    [Flags]
    public enum GitRemoteCallbackRegisterFlags
    {
        OnSidebandProgress = 1 << 0,
        OnCompletion = 1 << 1,
        GetCredentials = 1 << 2,
        OnCertificateCheck = 1 << 3,
        OnTransferProgress = 1 << 4,
        OnUpdateTips = 1 << 5,
        OnPackProgress = 1 << 6,
        OnPushTransferProgress = 1 << 7,
        OnPushUpdateReference = 1 << 8,
        OnPushNegotiation = 1 << 9,
        GetTransport = 1 << 10,
        OnRemoteReady = 1 << 11,
    }

    public interface IGitRemoteCallbacks
    {
        GitRemoteCallbackRegisterFlags CallbacksToRegister { get; }

        /// <summary>
        /// Callback for messages received by the transport.
        /// </summary>
        /// <param name="message">The message from the transport</param>
        /// <returns>
        /// A negative value to cancel the network operation, or 0 to continue.
        /// </returns>
        /// <remarks>
        /// Textual progress from the remote. Text send over the
        /// progress side-band will be passed to this function (this is
        /// the 'counting objects' output).
        /// </remarks>
        int OnSidebandProgress(ReadOnlySpan<byte> message)
        {
            return 0;
        }

        /// <summary>
        /// Completion is called when different parts of the download
        /// process are done (currently unused).
        /// </summary>
        int OnCompletion(GitRemoteCompletionType type)
        {
            return 0;
        }

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

        /// <summary>
        /// Type for progress callbacks during indexing.  Return a value less
        /// than zero to cancel the indexing or download.
        /// </summary>
        /// <param name="gitIndexerProgress">Structure containing information about the state of the transfer</param>
        /// <returns>
        /// Negative value to cancel the indexing or download, 0 to proceed.
        /// </returns>
        /// <remarks>
        /// During the download of new data, this will be regularly
        /// called with the current count of progress done by the
        /// indexer.
        /// </remarks>
        int OnTransferProgress(in GitIndexerProgress gitIndexerProgress)
        {
            return 0;
        }

        /// <summary>
        /// Each time a reference is updated locally, this function
        /// will be called with information about it.
        /// </summary>
        /// <param name="refName"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>
        /// Less than 0 to cancel operation, 0 to continue
        /// </returns>
        int OnUpdateTips(string? refName, in GitObjectID a, in GitObjectID b)
        {
            return 0;
        }

        /// <summary>
        /// Function to call with progress information
        /// during pack building.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="current"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        /// <remarks>
        /// Be aware that this is called inline with pack building operations,
        /// so performance may be affected.
        /// </remarks>
        int OnPackProgress(int stage, uint current, uint total)
        {
            return 0;
        }

        /// <summary>
        /// Function to call with progress information during the
        /// upload portion of a push.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="total"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <remarks>
        /// Be aware that this is called inline with pack building operations,
        /// so performance may be affected.
        /// </remarks>
        int OnPushTransferProgress(uint current, uint total, nuint bytes)
        {
            return 0;
        }

        /// <summary>
        /// Callback used to inform of the update status from the remote.
        /// </summary>
        /// <param name="refname">refname specifying to the remote ref</param>
        /// <param name="status">status message sent from the remote</param>
        /// <returns>
        /// 0 on success, otherwise an error (negative value, see <see cref="GitError"/>)
        /// </returns>
        /// <remarks>
        /// Called for each updated reference on push.
        /// If <paramref name="status"/> is not <see langword="null"/>,
        /// the update was rejected by the remote server
        /// and `status` contains the reason given.
        /// </remarks>
        int OnPushUpdateReference(string refname, string? status)
        {
            return 0;
        }

        /// <summary>
        /// Called once between the negotiation step and the upload. It
        /// provides information about what updates will be performed.
        /// </summary>
        /// <param name="updates">
        /// an array containing the updates which will be sent
        /// as commands to the destination.
        /// </param>
        /// <returns></returns>
        int OnPushNegotiation(GitPushUpdate[] updates)
        {
            return 0;
        }

        /// <summary>
        /// Provide the transport to use for this operation.
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="transport"></param>
        /// <returns></returns>
        int GetTransport(GitRemote remote, out GitTransport transport)
        {
            // returning a null transport without error is valid,
            // and libgit2 will come up with something itself or error out
            transport = default;
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gitRemote"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        int OnRemoteReady(GitRemote gitRemote, GitDirection direction)
        {
            return 0;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitRemoteCallbacks
    {
        public uint Version;
        public delegate* unmanaged[Cdecl]<byte*, int, nint, int> SidebandProgress;
        public delegate* unmanaged[Cdecl]<GitRemoteCompletionType, nint, int> Completion;
        public delegate* unmanaged[Cdecl]<Git2.Credential**, byte*, byte*, GitCredentialType, nint, int> Credentials;
        public delegate* unmanaged[Cdecl]<Git2.Certificate*, int, byte*, nint, int> CertificateCheck;
        public delegate* unmanaged[Cdecl]<GitIndexerProgress*, nint, int> TransferProgress;
        public delegate* unmanaged[Cdecl]<byte*, GitObjectID*, GitObjectID*, nint, int> UpdateTips;
        public delegate* unmanaged[Cdecl]<int, uint, uint, nint, int> PackProgress;
        public delegate* unmanaged[Cdecl]<uint, uint, nuint, nint, int> PushTransferProgress;
        public delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> PushUpdateReference;
        public delegate* unmanaged[Cdecl]<GitPushUpdate**, nuint, nint, int> PushNegotiation;
        public delegate* unmanaged[Cdecl]<Git2.Transport**, Git2.Remote*, nint, int> Transport;
        public delegate* unmanaged[Cdecl]<Git2.Remote*, GitDirection, nint, int> RemoteReady;
        public nint Payload;
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private nint Reserved;

        public GitRemoteCallbacks()
        {
            Version = 1;
        }

        public void FromManaged(IGitRemoteCallbacks? callbacks, List<GCHandle> gchandles)
        {
            Version = 1;

            if (callbacks is null)
                return;

            var gch = GCHandle.Alloc(callbacks, GCHandleType.Normal);
            gchandles.Add(gch);

            Payload = (nint)gch;
            SetDefaultCallbacks(callbacks);
        }

        private static bool HasFlag(GitRemoteCallbackRegisterFlags flags, GitRemoteCallbackRegisterFlags flags2)
        {
            return (flags & flags2) == flags2;
        }

        internal void SetDefaultCallbacks(IGitRemoteCallbacks callbacks)
        {
            var flags = callbacks.CallbacksToRegister;

            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnSidebandProgress))
                SidebandProgress = &OnSidebandProgress;
            
            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnCompletion))
                Completion = &OnCompletion;
            
            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.GetCredentials))
                Credentials = &OnGetCredentials;
            
            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnTransferProgress))
                TransferProgress = &OnTransferProgress;

            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnUpdateTips))
                UpdateTips = &OnUpdateTips;

            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnPackProgress))
                PackProgress = &OnPackProgress;

            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnPushTransferProgress))
                PushTransferProgress = &OnPushTransferProgress;

            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnPushUpdateReference))
                PushUpdateReference = &OnPushUpdateReference;

            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnPushNegotiation))
                PushNegotiation = &OnPushNegotiation;

            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.GetTransport))
                Transport = &GetTransport;

            if (HasFlag(flags, GitRemoteCallbackRegisterFlags.OnRemoteReady))
                RemoteReady = &OnRemoteReady;
        }

        /// <summary>
        /// Free unmanaged resources allocated by <see cref="FromManaged"/>
        /// </summary>
        public readonly void Free()
        {
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnSidebandProgress(byte* message, int messageLength, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                return callbacks.OnSidebandProgress(new ReadOnlySpan<byte>(message, messageLength));
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnCompletion(GitRemoteCompletionType type, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                return callbacks.OnCompletion(type);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnGetCredentials(Git2.Credential** out_credential, byte* url, byte* usernameFromUrl, GitCredentialType allowedTypes, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                string mUrl = Git2.GetPooledString(url);
                string? mUsernameFromUrl = usernameFromUrl == null ? null : Git2.GetPooledString(usernameFromUrl);

                Debug.Assert(sizeof(GitCredential) == sizeof(void*)); // Ensure struct size assumption

                return callbacks.GetCredentials(mUrl, mUsernameFromUrl, allowedTypes, out *(GitCredential*)out_credential);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnCertificateCheck(GitCertificateBase* certificate, int isValid, byte* host, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                var cert = GitCertificate.FromUnmanaged(certificate);

                string mHost = Git2.GetPooledString(host)!;

                return callbacks.OnCertificateCheck(cert, isValid != 0, mHost);
            }
            catch (NotSupportedException)
            {
                return (int)GitError.NotSupported;
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnTransferProgress(GitIndexerProgress* stats, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                return callbacks.OnTransferProgress(in *stats);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnUpdateTips(byte* refname, GitObjectID* a, GitObjectID* b, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                var refName = Git2.GetPooledString(refname);

                return callbacks.OnUpdateTips(refName, in *a, in *b);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnPackProgress(int stage, uint current, uint total, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                return callbacks.OnPackProgress(stage, current, total);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnPushTransferProgress(uint current, uint total, nuint bytes, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                return callbacks.OnPushTransferProgress(current, total, bytes);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnPushUpdateReference(byte* refname, byte* status, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                string mRefName = Git2.GetPooledString(refname)!;
                string? mStatus = status == null ? null : Git2.GetPooledString(status);

                return callbacks.OnPushUpdateReference(mRefName, mStatus);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnPushNegotiation(GitPushUpdate** updates, nuint update_count, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                int count = checked((int)update_count);
                var array = new SharpGit2.GitPushUpdate[count];

                for (int i = 0; i < count; ++i)
                {
                    array[i] = new(in *updates[i]);
                }

                return callbacks.OnPushNegotiation(array);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int GetTransport(Git2.Transport** transport_out, Git2.Remote* owner, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                Debug.Assert(sizeof(GitTransport) == sizeof(nint));

                return callbacks.GetTransport(new(owner), out *(GitTransport*)transport_out);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnRemoteReady(Git2.Remote* remote, GitDirection direction, nint payload)
        {
            var callbacks = (IGitRemoteCallbacks)((GCHandle)payload).Target!;

            try
            {
                return callbacks.OnRemoteReady(new(remote), direction);
            }
            catch// (Exception e)
            {
                return -1;
            }
        }
    }
}
