using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2;

internal unsafe static partial class NativeApi
{
    /// <summary>
    /// Init the global state
    /// </summary>
    /// <returns>
    /// The number of initializations of the library, or an error code.
    /// </returns>
    /// <remarks>
    /// This function must be called before any other libgit2 function
    /// in order to set up global state and threading.
    /// <br/><br/>
    /// This function may be called multiple times - it will return the
    /// number of times the initialization has been called (including
    /// this one) that have not subsequently been shutdown.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_libgit2_init();

    /// <summary>
    /// Shutdown the global state
    /// </summary>
    /// <returns>
    /// The number of remaining initializations of the library, or an error code.
    /// </returns>
    /// <remarks>
    /// Clean up the global state and threading context after calling it as many
    /// times as <see cref="git_libgit2_init"/> was called - it will return the
    /// number of remainining initializations that have not been shutdown (after this one).
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_libgit2_shutdown();

    /// <summary>
    /// Query compile time options for libgit2.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitFeatures git_libgit2_features();

    /// <summary>
    /// Return the version of the libgit2 library being currently used.
    /// </summary>
    /// <param name="major">Store the major version number</param>
    /// <param name="minor">Store the minor version number</param>
    /// <param name="rev">Store the revision (patch) number</param>
    /// <returns>0 on success or an error code on failure</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_libgit2_version(int* major, int* minor, int* rev);

    // Needs to be determined how best to handle variadic parameters
    //[LibraryImport(Git2.LibraryName)]
    //internal static partial int git_libgit2_opts(int option, ...);

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_error_clear();

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool git_error_exists();

    /// <summary>
    /// Return the last git_error object that was generated for the current thread.
    /// </summary>
    /// <returns>A git_error object</returns>
    /// <remarks>
    /// This function will never return NULL.
    /// <br/><br/>
    /// Callers should not rely on this to determine whether an error has occurred.
    /// For error checking, callers should examine the return codes of libgit2 functions.
    /// <br/><br/>
    /// This call can only reliably report error messages when an error has occurred.
    /// (It may contain stale information if it is called after a different function that succeeds.)
    /// <br/><br/>
    /// The memory for this object is managed by libgit2. It should not be freed.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial Native.GitErrorDetails* git_error_last();


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_error_set_str(GitErrorClass errorClass, string message);

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_commitarray_dispose(Git2.CommitArray* commitarray);

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_strarray_dispose(Native.GitStringArray* strarray);

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_buf_dispose(Git2.Buffer* buffer);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_clone(Git2.Repository** repo_out, string url, string local_path, Native.GitCloneOptions* options);

}
