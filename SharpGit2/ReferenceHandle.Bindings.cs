using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

unsafe readonly partial struct ReferenceHandle
{

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_reference_cmp(nint reference1, nint reference2);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_create(ReferenceHandle* reference, nint repository, string name, GitObjectID* id, int force, string logMessage);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_create_matching(ReferenceHandle* reference, nint repository, string name, GitObjectID* id, int force, GitObjectID* currentId, string logMessage);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_delete(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_dup(ReferenceHandle* duplicant, nint reference);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_dwim(ReferenceHandle* reference, nint repository, string shorthand);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_ensure_log(nint repository, string referenceName);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_foreach(nint repository, delegate* unmanaged<nint, nint, GitError> callback, nint payload);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_foreach_glob(nint repository, string glob, delegate* unmanaged<byte*, nint, GitError> callback, nint payload);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_foreach_name(nint repository, delegate* unmanaged<byte*, nint, GitError> callback, nint payload);

    [LibraryImport(Git2.LibraryName)]
    internal static partial void git_reference_free(nint reference);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int git_reference_has_log(nint repository, string referenceName);

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
    internal static partial void git_reference_iterator_free(nint iterator);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_iterator_glob_new(nint* iterator, nint repository, string glob);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_iterator_new(nint* iterator, nint repository);
    
    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_list(Git2.git_strarray* list, nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial byte* git_reference_name(nint reference);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_name_is_valid(int* valid, string referenceName);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_name_to_id(GitObjectID* id, nint repository, string name);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_next(ReferenceHandle* reference, nint iterator);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_next_name(byte** name, nint iterator);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_normalize_name(byte* buffer_out, nuint buffer_size, byte* name, GitReferenceFormat flags);

    [LibraryImport(Git2.LibraryName)]
    internal static partial nint git_reference_owner(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_peel(GitObjectHandle* gitObject, nint reference, GitObjectType type);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_rename(ReferenceHandle* newReference, nint reference, string newName, int force, string logMessage);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_resolve(ReferenceHandle* peeledReference, nint reference);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_set_target(ReferenceHandle* newReference, nint reference, GitObjectID* id, string LogMessage);

    [LibraryImport(Git2.LibraryName)]
    internal static partial byte* git_reference_shorthand(nint reference);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_symbolic_create(ReferenceHandle* reference, nint repository, string name, string target, int force, string logMessage);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_symbolic_create_matching(ReferenceHandle* reference, nint repository, string name, string target, int force, string currentValue, string logMessage);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_symbolic_set_target(ReferenceHandle* reference, nint refHandle, string target, string logMessage);

    [LibraryImport(Git2.LibraryName)]
    internal static partial byte* git_reference_symbolic_target(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitObjectID* git_reference_target(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitObjectID* git_reference_target_peel(nint reference);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitReferenceType git_reference_target_type(nint reference);

}
