using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

internal unsafe static partial class NativeApi
{
    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_libgit2_init();

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_libgit2_shutdown();

    // Needs to be determined how best to handle variadic parameters
    //[LibraryImport(Git2.LibraryName)]
    //internal static partial int git_libgit2_opts(int option, ...);
    
    [LibraryImport(Git2.LibraryName)]
    internal static partial void git_commitarray_dispose(Git2.git_commitarray* commitarray);

    [LibraryImport(Git2.LibraryName)]
    internal static partial void git_strarray_dispose(Git2.git_strarray* strarray);

}
