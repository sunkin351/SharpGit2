using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitRepositoryInitOptions
    {
        public GitRepositoryInitFlags Flags;
        public GitRepositoryInitMode Mode;
        public string? WorkingDirectory;
        public string? Description;
        public string? TemplatePath;
        public string? InitialHead;
        public string? OriginUrl;

#if GIT_EXPERIMENTAL_SHA256
        public GitObjectIDType ObjectIDType;
#endif
    }

    namespace Native
    {
        public unsafe struct GitRepositoryInitOptions
        {
            public uint Version;
            public GitRepositoryInitFlags Flags;
            public GitRepositoryInitMode Mode;
            public byte* WorkingDirectory;
            public byte* Description;
            public byte* TemplatePath;
            public byte* InitialHead;
            public byte* OriginUrl;

#if GIT_EXPERIMENTAL_SHA256
            public GitObjectIDType ObjectIDType;
#endif

            public GitRepositoryInitOptions()
            {
                Version = 1;
            }

            public void FromManaged(in SharpGit2.GitRepositoryInitOptions options)
            {
                Version = 1;
                Flags = options.Flags;
                Mode = options.Mode;
                WorkingDirectory = Utf8StringMarshaller.ConvertToUnmanaged(options.WorkingDirectory);
                Description = Utf8StringMarshaller.ConvertToUnmanaged(options.Description);
                TemplatePath = Utf8StringMarshaller.ConvertToUnmanaged(options.TemplatePath);
                InitialHead = Utf8StringMarshaller.ConvertToUnmanaged(options.InitialHead);
                OriginUrl = Utf8StringMarshaller.ConvertToUnmanaged(options.OriginUrl);

#if GIT_EXPERIMENTAL_SHA256
                ObjectIDType = options.ObjectIDType;
#endif
            }

            /// <summary>
            /// Free unmanaged resources allocated by <see cref="FromManaged"/>
            /// </summary>
            public void Free()
            {
                Utf8StringMarshaller.Free(WorkingDirectory);
                Utf8StringMarshaller.Free(Description);
                Utf8StringMarshaller.Free(TemplatePath);
                Utf8StringMarshaller.Free(InitialHead);
                Utf8StringMarshaller.Free(OriginUrl);
            }
        }
    }
}
