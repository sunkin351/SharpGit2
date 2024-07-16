using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

internal unsafe readonly partial struct ReferenceHandle
{
    [LibraryImport(Git2.LibraryName)]
    internal static partial void git_reference_free(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_delete(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_dup(ReferenceHandle* duplicant, nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_reference_is_branch(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_reference_is_note(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_reference_is_remote(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_reference_is_tag(nint reference);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int git_reference_is_valid_name(string refname);

    [LibraryImport(Git2.LibraryName)]
    internal static partial byte* git_reference_name(nint reference);

}
