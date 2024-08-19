using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using SharpGit2.Marshalling;

namespace SharpGit2
{
    /// <summary>
    /// Structure describing options about how the diff should be executed.
    /// </summary>
    /// <remarks>
    /// Setting all values of the structure to zero will yield the default
    /// values.  Similarly, passing NULL for the options structure will
    /// give the defaults.  The default values are marked below.
    /// </remarks>
    public unsafe struct GitDiffOptions
    {
        public GitDiffOptionFlags Flags;
        public GitSubmoduleIgnoreType IgnoreSubmodules;
        public string[]? PathSpec;
        public ICallbacks? Callbacks;
        public uint ContextLines;
        public uint InterhunkLines;
        public GitObjectType OidType;
        public ushort IdAbbrev;
        public long MaxSize;
        public string? OldPrefix;
        public string? NewPrefix;

        public GitDiffOptions()
        {
            IgnoreSubmodules = GitSubmoduleIgnoreType.Unspecified;
            ContextLines = 3;
        }

        public interface ICallbacks
        {
            /// <summary>
            /// Diff notification callback function
            /// </summary>
            /// <param name="diffSoFar">The diff being generated</param>
            /// <param name="deltaToAdd"></param>
            /// <param name="matchedPathSpec"></param>
            /// <returns>
            /// Less than 0 to abort the diff process;
            /// Greater than 0 to prevent the delta from being added, but continue the diff process;
            /// 0 to add the delta and continue the diff process
            /// </returns>
            /// <remarks>
            /// The callback will be called for each file, just before the `git_diff_delta`
            /// gets inserted into the diff.
            /// </remarks>
            int OnNotify(GitDiff diffSoFar, in GitDiffDelta deltaToAdd, string? matchedPathSpec);

            /// <summary>
            /// Diff progress callback
            /// </summary>
            /// <param name="diffSoFar">The diff being generated</param>
            /// <param name="oldPath">The path to the old file or NULL</param>
            /// <param name="newPath">The path to the new file or NULL</param>
            /// <returns>Non-zero to abort the diff</returns>
            /// <remarks>
            /// Called before each file comparison.
            /// </remarks>
            int OnProgress(GitDiff diffSoFar, string? oldPath, string? newPath);
        }
    }

    namespace Native
    {
        public unsafe struct GitDiffOptions
        {
            public uint Version;
            public GitDiffOptionFlags Flags;
            public GitSubmoduleIgnoreType IgnoreSubmodules;
            public GitStringArray PathSpec;
            public delegate* unmanaged[Cdecl]<Git2.Diff*, GitDiffDelta*, byte*, nint, int> NotifyCallback;
            public delegate* unmanaged[Cdecl]<Git2.Diff*, byte*, byte*, nint, int> ProgressCallback;
            public nint Payload;
            public uint ContextLines;
            public uint InterhunkLines;
            public GitObjectType OidType;
            public ushort IdAbbrev;
            public long MaxSize;
            public byte* OldPrefix;
            public byte* NewPrefix;

            public GitDiffOptions()
            {
                Version = 1;

                // Non-Zero Default values
                IgnoreSubmodules = GitSubmoduleIgnoreType.Unspecified;
                ContextLines = 3;
            }

            public void MarshalFrom(in SharpGit2.GitDiffOptions options, List<GCHandle> handles)
            {
                Version = 1;
                Flags = options.Flags;
                IgnoreSubmodules = options.IgnoreSubmodules;
                PathSpec = StringArrayMarshaller.ConvertToUnmanaged(options.PathSpec);
                ContextLines = options.ContextLines;
                InterhunkLines = options.InterhunkLines;
                OidType = options.OidType;
                IdAbbrev = options.IdAbbrev;
                MaxSize = options.MaxSize;
                OldPrefix = Utf8StringMarshaller.ConvertToUnmanaged(options.OldPrefix);
                NewPrefix = Utf8StringMarshaller.ConvertToUnmanaged(options.NewPrefix);

                if (options.Callbacks is { } callbacks)
                {
                    var handle = GCHandle.Alloc(callbacks, GCHandleType.Normal);

                    handles.Add(handle);

                    NotifyCallback = &Notify;
                    ProgressCallback = &Progress;
                    Payload = (nint)handle;
                }
            }

            public void Free()
            {
                StringArrayMarshaller.Free(PathSpec);
                Utf8StringMarshaller.Free(OldPrefix);
                Utf8StringMarshaller.Free(NewPrefix);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
            private static int Notify(Git2.Diff* diffSoFar, GitDiffDelta* deltaToAdd, byte* matchedPathSpec, nint payload)
            {
                var callbackObj = (SharpGit2.GitDiffOptions.ICallbacks)((GCHandle)payload).Target!;

                try
                {
                    var delta = new SharpGit2.GitDiffDelta(in *deltaToAdd);
                    var pathSpec = Utf8StringMarshaller.ConvertToManaged(matchedPathSpec);

                    return callbackObj.OnNotify(new(diffSoFar), in delta, pathSpec);
                }
                catch (Git2Exception e) // TODO: Figure out what to do to propagate exceptions
                {
                    NativeApi.git_error_set_str(e.ErrorClass, e.Message);
                    return (int)e.ErrorCode;
                }
                catch (Exception e)
                {
                    NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                    return -1;
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
            private static int Progress(Git2.Diff* diffSoFar, byte* oldPath, byte* newPath, nint payload)
            {
                var callbackObj = (SharpGit2.GitDiffOptions.ICallbacks)((GCHandle)payload).Target!;

                try
                {
                    var path0 = Utf8StringMarshaller.ConvertToManaged(oldPath);
                    var path1 = Utf8StringMarshaller.ConvertToManaged(newPath);

                    return callbackObj.OnProgress(new(diffSoFar), path0, path1);
                }
                catch (Git2Exception e) // TODO: Figure out what to do to propagate exceptions
                {
                    NativeApi.git_error_set_str(e.ErrorClass, e.Message);
                    return (int)e.ErrorCode;
                }
                catch (Exception e)
                {
                    NativeApi.git_error_set_str(GitErrorClass.Callback, e.Message);
                    return -1;
                }
            }
        }
    }
}
