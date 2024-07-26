using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

internal unsafe static partial class NativeApi
{
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_libgit2_init();

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_libgit2_shutdown();

    // Needs to be determined how best to handle variadic parameters
    //[LibraryImport(Git2.LibraryName)]
    //internal static partial int git_libgit2_opts(int option, ...);

    /// <summary>
    /// Return the last git_error object that was generated for the current thread.
    /// </summary>
    /// <returns>A git_error object</returns>
    /// <remarks>
    /// This function will never return NULL.
    /// 
    /// Callers should not rely on this to determine whether an error has occurred.
    /// For error checking, callers should examine the return codes of libgit2 functions.
    /// 
    /// This call can only reliably report error messages when an error has occurred.
    /// (It may contain stale information if it is called after a different function that succeeds.)
    /// 
    /// The memory for this object is managed by libgit2. It should not be freed.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial Git2.Error* git_error_last();
    
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_commitarray_dispose(Git2.git_commitarray* commitarray);

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_strarray_dispose(Git2.git_strarray* strarray);

}
