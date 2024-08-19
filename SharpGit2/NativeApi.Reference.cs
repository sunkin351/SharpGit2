using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2;

internal static unsafe partial class NativeApi
{
    /// <summary>
    /// Compare two references
    /// </summary>
    /// <param name="reference1">The first reference</param>
    /// <param name="reference2">The second reference</param>
    /// <returns>0 if the same, else a stable but meaningless ordering.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_reference_cmp(Git2.Reference* reference1, Git2.Reference* reference2);

    /// <summary>
    /// Create a new direct reference
    /// </summary>
    /// <param name="reference">Pointer to the newly created reference</param>
    /// <param name="repository">Repository where that reference will live</param>
    /// <param name="name">The name of the reference</param>
    /// <param name="id">The object id pointed to by the reference.</param>
    /// <param name="force">Overwrite existing references</param>
    /// <param name="logMessage">The one line long message to be appended to the reflog</param>
    /// <returns>0 on success, <see cref="GitError.Exists"/>, <see cref="GitError.InvalidSpec"/> or an error code</returns>
    /// <remarks>
    /// A direct reference (also called an object id reference) refers directly to a specific object id (a.k.a. OID or SHA) in the repository.
    /// The id permanently refers to the object (although the reference itself can be moved).
    /// For example, in libgit2 the direct ref "refs/tags/v0.17.0" refers to OID 5b9fac39d8a76b9139667c26a63e6b3f204b3977.
    /// <br/><br/>
    /// The direct reference will be created in the repository and written to the disk.
    /// The generated reference object must be freed by the user.
    /// <br/><br/>
    /// Valid reference names must follow one of two patterns:<br/>
    /// 1. Top-level names must contain only capital letters and underscores, and must begin and end with a letter. (e.g. "HEAD", "ORIG_HEAD").<br/>
    /// 2. Names prefixed with "refs/" can be almost anything.You must avoid the characters '~', '^', ':', '\', '?', '[', and '*', and the sequences ".." and "@{" which have special meaning to revparse.
    /// <br/><br/>
    /// This function will return an error if a reference already exists with the given name unless <paramref name="force"/> is true,
    /// in which case it will be overwritten.
    /// <br/><br/>
    /// The message for the reflog will be ignored if the reference does not belong in the standard set (HEAD, branches and remote-tracking branches)
    /// and it does not have a reflog.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_create(Git2.Reference** reference, Git2.Repository* repository, string name, GitObjectID* id, int force, string? logMessage);


    /// <summary>
    /// Conditionally creates a new direct reference
    /// </summary>
    /// <param name="reference">Pointer to the newly created reference</param>
    /// <param name="repository">Repository where that reference will live</param>
    /// <param name="name">The name of the reference</param>
    /// <param name="id">The object id pointed to by the reference.</param>
    /// <param name="force">Overwrite existing references</param>
    /// <param name="currentId">The expected value of the reference at the time of update</param>
    /// <param name="logMessage">The one line long message to be appended to the reflog</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.Modified"/> if the value of the reference has changed,
    /// <see cref="GitError.Exists"/>, <see cref="GitError.InvalidSpec"/> or an error code
    /// </returns>
    /// <remarks>
    /// A direct reference (also called an object id reference) refers directly to a specific object id (a.k.a. OID or SHA) in the repository.
    /// The id permanently refers to the object (although the reference itself can be moved).
    /// For example, in libgit2 the direct ref "refs/tags/v0.17.0" refers to OID 5b9fac39d8a76b9139667c26a63e6b3f204b3977.
    /// <br/><br/>
    /// The direct reference will be created in the repository and written to the disk.
    /// The generated reference object must be freed by the user.
    /// <br/><br/>
    /// Valid reference names must follow one of two patterns:<br/>
    /// 1. Top-level names must contain only capital letters and underscores, and must begin and end with a letter. (e.g. "HEAD", "ORIG_HEAD").<br/>
    /// 2. Names prefixed with "refs/" can be almost anything.You must avoid the characters '~', '^', ':', '\', '?', '[', and '*', and the sequences ".." and "@{" which have special meaning to revparse.
    /// <br/><br/>
    /// This function will return an error if a reference already exists with the given name unless <paramref name="force"/> is true,
    /// in which case it will be overwritten.
    /// <br/><br/>
    /// The message for the reflog will be ignored if the reference does not belong in the standard set (HEAD, branches and remote-tracking branches)
    /// and it does not have a reflog.
    /// <br/><br/>
    /// It will return <see cref="GitError.Modified"/> if the reference's value at the time of updating does not match the one passed through <paramref name="currentId"/> (i.e. if the ref has changed since the user read it).
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_create_matching(Git2.Reference** reference, Git2.Repository* repository, string name, GitObjectID* id, int force, GitObjectID* currentId, string? logMessage);

    /// <summary>
    /// Delete an existing reference.
    /// </summary>
    /// <param name="reference">The reference to remove</param>
    /// <returns>0, <see cref="GitError.Modified"/> or an error code</returns>
    /// <remarks>
    /// This method works for both direct and symbolic references.
    /// The reference will be immediately removed on disk but the memory will not be freed.
    /// Callers must call <see cref="git_reference_free(Git2.Reference*)"/>.
    /// This function will return an error if the reference has changed from the time it was looked up.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_delete(Git2.Reference* reference);

    /// <summary>
    /// Create a copy of an existing reference.
    /// </summary>
    /// <param name="duplicant">pointer where to store the copy</param>
    /// <param name="reference">object to copy</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>Call <see cref="git_reference_free(Git2.Reference*)"/> to free the data.</remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_dup(Git2.Reference** duplicant, Git2.Reference* reference);

    /// <summary>
    /// Lookup a reference by DWIMing its short name
    /// </summary>
    /// <param name="reference">pointer in which to store the reference</param>
    /// <param name="repository">the repository in which to look</param>
    /// <param name="shorthand">the short name for the reference</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>Apply the git precedence rules to the given shorthand to determine which reference the user is referring to.</remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_dwim(Git2.Reference** reference, Git2.Repository* repository, string shorthand);

    /// <summary>
    /// Ensure there is a reflog for a particular reference.
    /// </summary>
    /// <param name="repository">the repository</param>
    /// <param name="referenceName">the reference's name</param>
    /// <returns>
    /// 0 or an error code.
    /// </returns>
    /// <remarks>
    /// Make sure that successive updates to the reference will append to its log.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_ensure_log(Git2.Repository* repository, string referenceName);

    /// <summary>
    /// Perform a callback on each reference in the repository.
    /// </summary>
    /// <param name="repository">Repository where to find the refs</param>
    /// <param name="callback">Function which will be called for every listed ref</param>
    /// <param name="payload">Additional data to pass to the callback</param>
    /// <returns>0 on success, non-zero callback return value, or error code</returns>
    /// <remarks>
    /// The <paramref name="callback"/> function will be called for each reference in the repository,
    /// receiving the reference object and the <paramref name="payload"/> value passed to this method.
    /// Returning a non-zero value from the callback will terminate the iteration.
    /// Note that the callback function is responsible to call <see cref="git_reference_free(Git2.Reference*)"/> on each reference passed to it.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_foreach(Git2.Repository* repository, delegate* unmanaged[Cdecl]<Git2.Reference*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Perform a callback on each reference in the repository whose name matches the given pattern.
    /// </summary>
    /// <param name="repository">Repository where to find the refs</param>
    /// <param name="glob">Pattern to match (fnmatch-style) against reference name.</param>
    /// <param name="callback">Function which will be called for every listed ref</param>
    /// <param name="payload">Additional data to pass to the callback</param>
    /// <returns>0 on success, <see cref="GitError.User"/> on non-zero callback, or error code</returns>
    /// <remarks>
    /// This function acts like <see cref="git_reference_foreach"/> with an additional pattern match being applied to the reference name before issuing the callback function.
    /// See that function for more information.
    /// 
    /// The pattern is matched using fnmatch or "glob" style where a '*' matches any sequence of letters, a '?' matches any letter,
    /// and square brackets can be used to define character ranges (such as "[0-9]" for digits).
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_foreach_glob(Git2.Repository* repository, string glob, delegate* unmanaged[Cdecl]<byte*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Perform a callback on the fully-qualified name of each reference.
    /// </summary>
    /// <param name="repository">Repository where to find the refs</param>
    /// <param name="callback">Function which will be called for every listed ref name</param>
    /// <param name="payload">Additional data to pass to the callback</param>
    /// <returns>0 on success, non-zero callback return value, or error code</returns>
    /// <remarks>
    /// The <paramref name="callback"/> function will be called for each reference in the repository,
    /// receiving the name of the reference and the <paramref name="payload"/> value passed to this method.
    /// Returning a non-zero value from the callback will terminate the iteration.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_foreach_name(Git2.Repository* repository, delegate* unmanaged[Cdecl]<byte*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Free the given reference.
    /// </summary>
    /// <param name="reference">The reference</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_reference_free(Git2.Reference* reference);

    /// <summary>
    /// Check if a reflog exists for the specified reference.
    /// </summary>
    /// <param name="repository">the repository</param>
    /// <param name="referenceName">the reference's name</param>
    /// <returns>0 when no reflog can be found, 1 when it exists; otherwise an error code from <see cref="GitError"/>.</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_reference_has_log(Git2.Repository* repository, string referenceName);

    /// <summary>
    /// Check if a reference is a local branch.
    /// </summary>
    /// <param name="reference">A git reference</param>
    /// <returns>1 when the reference lives in the refs/heads namespace; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_reference_is_branch(Git2.Reference* reference);

    /// <summary>
    /// Check if a reference is a note.
    /// </summary>
    /// <param name="reference">A git reference</param>
    /// <returns>1 when the reference lives in the refs/notes namespace; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_reference_is_note(Git2.Reference* reference);

    /// <summary>
    /// Check if a reference is a note.
    /// </summary>
    /// <param name="reference">A git reference</param>
    /// <returns>1 when the reference lives in the refs/notes namespace; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_reference_is_remote(Git2.Reference* reference);

    /// <summary>
    /// Check if a reference is a tag.
    /// </summary>
    /// <param name="reference">A git reference</param>
    /// <returns>1 when the reference lives in the refs/tags namespace; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_reference_is_tag(Git2.Reference* reference);

    /// <summary>
    /// Ensure the reference name is well-formed.
    /// </summary>
    /// <param name="refname">name to be checked.</param>
    /// <returns>1 if the reference name is acceptable; 0 if it isn't</returns>
    /// <remarks>
    /// Valid reference names must follow one of two patterns:
    /// 1. Top-level names must contain only capital letters and underscores, and must begin and end with a letter. (e.g. "HEAD", "ORIG_HEAD").
    /// 2. Names prefixed with "refs/" can be almost anything. You must avoid the characters '~', '^', ':', '\', '?', '[', and '*', and the sequences ".." and "@{" which have special meaning to revparse.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_reference_is_valid_name(string refname);

    /// <summary>
    /// Free the iterator and its associated resources
    /// </summary>
    /// <param name="iterator">the iterator to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_reference_iterator_free(Git2.ReferenceIterator* iterator);

    /// <summary>
    /// Create an iterator for the repo's references that match the specified glob
    /// </summary>
    /// <param name="iterator">pointer in which to store the iterator</param>
    /// <param name="repository">the repository</param>
    /// <param name="glob">the glob to match against the reference names</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_iterator_glob_new(Git2.ReferenceIterator** iterator, Git2.Repository* repository, string glob);

    /// <summary>
    /// Create an iterator for the repo's references
    /// </summary>
    /// <param name="iterator">pointer in which to store the iterator</param>
    /// <param name="repository">the repository</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_iterator_new(Git2.ReferenceIterator** iterator, Git2.Repository* repository);

    /// <summary>
    /// Fill a list with all the references that can be found in a repository.
    /// </summary>
    /// <param name="list">Pointer to a git_strarray structure where the reference names will be stored</param>
    /// <param name="repository">Repository where to find the refs</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The string array will be filled with the names of all references;
    /// these values are owned by the user and should be free'd manually when no longer needed,
    /// using <see cref="git_strarray_dispose(Git2.StringArray*)"/>.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_list(Native.GitStringArray* list, Git2.Repository* repository);

    /// <summary>
    /// Lookup a reference by name in a repository.
    /// </summary>
    /// <param name="reference">pointer to the looked-up reference</param>
    /// <param name="repository">the repository to look up the reference</param>
    /// <param name="name">the long name for the reference (e.g. HEAD, refs/heads/master, refs/tags/v0.1.0, ...)</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.NotFound"/>, <see cref="GitError.InvalidSpec"/> or an error code.
    /// </returns>
    /// <remarks>
    /// The returned reference must be freed by the user.
    /// 
    /// The name will be checked for validity. See <see cref="GitReference.git_reference_is_valid_name(string)"/> for rules about valid names.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_lookup(Git2.Reference** reference, Git2.Repository* repository, string name);

    /// <summary>
    /// Get the full name of a reference.
    /// </summary>
    /// <param name="reference">The Reference</param>
    /// <returns>the full name for the ref</returns>
    /// <remarks>
    /// See <see cref="git_reference_name_is_valid(int*, string)"/> for rules about valid names.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial byte* git_reference_name(Git2.Reference* reference);

    /// <summary>
    /// Ensure the reference name is well-formed.
    /// </summary>
    /// <param name="valid">output pointer to set with validity of given reference name</param>
    /// <param name="referenceName">name to be checked.</param>
    /// <returns>0 on success or an error code</returns>
    /// <remarks>
    /// See <see cref="git_reference_is_valid_name(string)"/> for rules about valid names.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_name_is_valid(int* valid, string referenceName);

    /// <summary>
    /// Lookup a reference by name and resolve immediately to OID.
    /// </summary>
    /// <param name="id">Pointer to oid to be filled in</param>
    /// <param name="repository">The repository in which to look up the reference</param>
    /// <param name="name">The long name for the reference (e.g. HEAD, refs/heads/master, refs/tags/v0.1.0, ...)</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.NotFound"/>, <see cref="GitError.InvalidSpec"/> or an error code.
    /// </returns>
    /// <remarks>
    /// This function provides a quick way to resolve a reference name straight through to the object id that it refers to.
    /// This avoids having to allocate or free any <see cref="GitReference"/> objects for simple situations.
    /// 
    /// The name will be checked for validity. See <see cref="git_reference_is_valid_name(string)"/> for rules about valid names.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_name_to_id(GitObjectID* id, Git2.Repository* repository, string name);

    ///<inheritdoc cref="git_reference_name_to_id(GitObjectID*, nint, string)"/>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_name_to_id(GitObjectID* id, Git2.Repository* repository, byte* name);

    /// <summary>
    /// Get the next reference
    /// </summary>
    /// <param name="reference">pointer in which to store the reference</param>
    /// <param name="iterator">the iterator</param>
    /// <returns>0, <see cref="GitError.IterationOver"/> if there are no more; or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_next(Git2.Reference** reference, Git2.ReferenceIterator* iterator);

    /// <summary>
    /// Get the next reference's name
    /// </summary>
    /// <param name="name">pointer in which to store the string</param>
    /// <param name="iterator">the iterator</param>
    /// <returns>0, <see cref="GitError.IterationOver"/> if there are no more; or an error code</returns>
    /// <remarks>
    /// This function is provided for convenience in case only the names are interesting
    /// as it avoids the allocation of the <see cref="GitReference"/> object which <see cref="git_reference_next(Git2.Reference**, nint)"/> needs.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_next_name(byte** name, Git2.ReferenceIterator* iterator);

    /// <summary>
    /// Normalize reference name and check validity.
    /// </summary>
    /// <param name="buffer_out">User allocated buffer to store normalized name</param>
    /// <param name="buffer_size">Size of buffer_out</param>
    /// <param name="name">Reference name to be checked.</param>
    /// <param name="flags">Flags to constrain name validation rules - see the <see cref="GitReferenceFormat"/> enum.</param>
    /// <returns>0 on success, <see cref="GitError.BufferTooShort"/> if buffer is too small, <see cref="GitError.InvalidSpec"/> or an error code.</returns>
    /// <remarks>
    /// This will normalize the reference name by removing any leading slash '/' characters and collapsing runs of adjacent slashes between name components into a single slash.
    /// 
    /// Once normalized, if the reference name is valid, it will be returned in the user allocated buffer.
    /// 
    /// See <see cref="git_reference_is_valid_name(string)"/> for rules about valid names.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_normalize_name(byte* buffer_out, nuint buffer_size, byte* name, GitReferenceFormat flags);

    /// <summary>
    /// Get the repository where a reference resides.
    /// </summary>
    /// <param name="reference">The reference</param>
    /// <returns>a pointer to the repo</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial Git2.Repository* git_reference_owner(Git2.Reference* reference);

    /// <summary>
    /// Recursively peel reference until object of the specified type is found.
    /// </summary>
    /// <param name="gitObject">Pointer to the peeled <see cref="GitObject"/></param>
    /// <param name="reference">The reference to be processed</param>
    /// <param name="type">The type of the requested object (<see cref="GitObjectType.Commit"/>, <see cref="GitObjectType.Tag"/>, <see cref="GitObjectType.Tree"/>, <see cref="GitObjectType.Blob"/> or <see cref="GitObjectType.Any"/>).</param>
    /// <returns>0 on success, <see cref="GitError.Ambiguous"/>, <see cref="GitError.NotFound"/> or an error code</returns>
    /// <remarks>
    /// The retrieved peeled object is owned by the repository and should be closed with the <see cref="git_object_free"/> method.
    /// 
    /// If you pass <see cref="GitObjectType.Any"/> as the target type, then the object will be peeled until a non-tag object is met.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_peel(Git2.Object** gitObject, Git2.Reference* reference, GitObjectType type);

    /// <summary>
    /// Delete an existing reference by name
    /// </summary>
    /// <param name="repository">The parent repository</param>
    /// <param name="name">The reference to remove</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// This method removes the named reference from the repository without looking at its old value.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_remove(Git2.Repository* repository, string name);

    /// <summary>
    /// Rename an existing reference.
    /// </summary>
    /// <param name="newReference"></param>
    /// <param name="reference">The reference to rename</param>
    /// <param name="newName">The new name for the reference</param>
    /// <param name="force">Overwrite an existing reference</param>
    /// <param name="logMessage">The one line long message to be appended to the reflog</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.InvalidSpec"/>, <see cref="GitError.Exists"/> or an error code
    /// </returns>
    /// <remarks>
    /// This method works for both direct and symbolic references.
    /// 
    /// The new name will be checked for validity. See <see cref="git_reference_is_valid_name(string)"/> for rules about valid names.
    /// 
    /// If the <paramref name="force"/> flag is not enabled, and there's already a reference with the given name, the renaming will fail.
    /// 
    /// IMPORTANT: The user needs to write a proper reflog entry if the reflog is enabled for the repository. We only rename the reflog if it exists.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_rename(Git2.Reference** newReference, Git2.Reference* reference, string newName, int force, string? logMessage);

    /// <summary>
    /// Resolve a symbolic reference to a direct reference.
    /// </summary>
    /// <param name="peeledReference">Pointer to the peeled reference</param>
    /// <param name="reference">The reference</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// This method iteratively peels a symbolic reference until it resolves to a direct reference to an OID.
    /// 
    /// The peeled reference is returned in the <paramref name="resolvedRef"/> argument, and must be freed manually once it's no longer needed.
    /// 
    /// If a direct reference is passed as an argument, a copy of that reference is returned. This copy must be manually freed too.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_resolve(Git2.Reference** resolvedRef, Git2.Reference* reference);

    /// <summary>
    /// Conditionally create a new reference with the same name as the given reference but a different OID target.
    /// The reference must be a direct reference, otherwise this will fail.
    /// </summary>
    /// <param name="newReference">Pointer to the newly created reference</param>
    /// <param name="reference">The reference</param>
    /// <param name="id">The new target OID for the reference</param>
    /// <param name="LogMessage">The one line long message to be appended to the reflog</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.Modified"/> if the value of the reference has changed since it was read, or an error code
    /// </returns>
    /// <remarks>
    /// The new reference will be written to disk, overwriting the given reference.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_set_target(Git2.Reference** newReference, Git2.Reference* reference, GitObjectID* id, string? LogMessage);

    /// <summary>
    /// Get the reference's short name
    /// </summary>
    /// <param name="reference">The Reference</param>
    /// <returns>the human-readable version of the name</returns>
    /// <remarks>
    /// This will transform the reference name into a name "human-readable" version. If no shortname is appropriate, it will return the full name.
    /// The memory is owned by the reference and must not be freed.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial byte* git_reference_shorthand(Git2.Reference* reference);

    /// <summary>
    /// Create a new symbolic reference.
    /// </summary>
    /// <param name="reference">Pointer to the newly created reference</param>
    /// <param name="repository">Repository where that reference will live</param>
    /// <param name="name">The name of the reference</param>
    /// <param name="target">The target of the reference</param>
    /// <param name="force">Overwrite existing references</param>
    /// <param name="logMessage">The one line long message to be appended to the reflog</param>
    /// <returns>0 on success, <see cref="GitError.Exists"/>, <see cref="GitError.InvalidSpec"/> or an error code</returns>
    /// <remarks>
    /// A symbolic reference is a reference name that refers to another reference name.
    /// If the other name moves, the symbolic name will move, too. As a simple example,
    /// the "HEAD" reference might refer to "refs/heads/master" while on the "master" branch
    /// of a repository.
    /// 
    /// The symbolic reference will be created in the repository and written to the disk.
    /// The generated reference object must be freed by the user.
    /// 
    /// Valid reference names must follow one of two patterns:
    /// 1.  Top-level names must contain only capital letters and underscores,
    ///     and must begin and end with a letter. (e.g. "HEAD", "ORIG_HEAD").
    /// 2.  Names prefixed with "refs/" can be almost anything.You must avoid
    ///     the characters '~', '^', ':', '\', '?', '[', and ' * ', and the
    ///     sequences ".." and "@{" which have special meaning to revparse.
    /// 
    /// This function will return an error if a reference already exists with
    /// the given name unless force is true, in which case it will be overwritten.
    /// 
    /// The message for the reflog will be ignored if the reference does not
    /// belong in the standard set (HEAD, branches and remote-tracking branches)
    /// and it does not have a reflog.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_symbolic_create(Git2.Reference** reference, Git2.Repository* repository, string name, string target, int force, string? logMessage);

    /// <summary>
    /// Conditionally create a new symbolic reference.
    /// </summary>
    /// <param name="reference">Pointer to the newly created reference</param>
    /// <param name="repository">Repository where that reference will live</param>
    /// <param name="name">The name of the reference</param>
    /// <param name="target">The target of the reference</param>
    /// <param name="force">Overwrite existing references</param>
    /// <param name="currentValue">The expected value of the reference when updating</param>
    /// <param name="logMessage">The one line long message to be appended to the reflog</param>
    /// <returns>0 on success, <see cref="GitError.Exists"/>, <see cref="GitError.InvalidSpec"/>, <see cref="GitError.Modified"/> or an error code</returns>
    /// <remarks>
    /// Similar to <see cref="git_reference_symbolic_create(Git2.Reference**, Git2.Repository*, string, string, int, string?)"/>.
    /// 
    /// It will return <see cref="GitError.Modified"/> if the reference's value at
    /// the time of updating does not match the one passed through current_value
    /// (i.e. if the ref has changed since the user read it).
    /// 
    /// If <paramref name="currentValue"/> is null, this function will return <see cref="GitError.Modified"/> if the ref already exists.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_symbolic_create_matching(Git2.Reference** reference, Git2.Repository* repository, string name, string target, int force, string? currentValue, string? logMessage);

    /// <summary>
    /// Create a new reference with the same name as the given reference but a different symbolic target. The reference must be a symbolic reference, otherwise this will fail.
    /// </summary>
    /// <param name="newReference">Pointer to the newly created reference</param>
    /// <param name="currentReference">The reference</param>
    /// <param name="target">The new target for the reference</param>
    /// <param name="logMessage">The one line long message to be appended to the reflog</param>
    /// <returns>0 on success, <see cref="GitError.InvalidSpec"/> or an error code</returns>
    /// <remarks>
    /// The new reference will be written to disk, overwriting the given reference.
    /// 
    /// The target name will be checked for validity. See <see cref="git_reference_is_valid_name(string)"/> for rules about valid names.
    /// 
    /// The message for the reflog will be ignored if the reference does not belong in the standard set (HEAD, branches and remote-tracking branches) and it does not have a reflog.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_symbolic_set_target(Git2.Reference** newReference, Git2.Reference* currentReference, string target, string? logMessage);

    /// <inheritdoc cref="git_reference_symbolic_set_target(Git2.Reference**, nint, string, string?)"/>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_reference_symbolic_set_target(Git2.Reference** newReference, Git2.Reference* currentReference, byte* target, string? logMessage);

    /// <summary>
    /// Get full name to the reference pointed to by a symbolic reference.
    /// </summary>
    /// <param name="reference">The reference</param>
    /// <returns>a pointer to the name if available, NULL otherwise</returns>
    /// <remarks>Only available if the reference is symbolic.</remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial byte* git_reference_symbolic_target(Git2.Reference* reference);

    /// <summary>
    /// Get the OID pointed to by a direct reference
    /// </summary>
    /// <param name="reference">The reference</param>
    /// <returns>a pointer to the oid if available, NULL otherwise</returns>
    /// <remarks>
    /// Only available if the reference is direct (i.e. an object id reference, not a symbolic one).
    /// To find the OID of a symbolic ref, call <see cref="git_reference_resolve(Git2.Reference**, Git2.Reference*)"/>
    /// and then this function(or maybe use <see cref="git_reference_name_to_id(GitObjectID*, Git2.Repository*, string)"/>
    /// to directly resolve a reference name all the way through to an OID)
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitObjectID* git_reference_target(Git2.Reference* reference);

    /// <summary>
    /// Return the peeled OID target of this reference.
    /// </summary>
    /// <param name="reference">The reference</param>
    /// <returns>A pointer to the oid if available, NULL otherwise</returns>
    /// <remarks>
    /// This peeled OID only applies to direct references that point to a hard Tag object: it is the result of peeling such Tag.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitObjectID* git_reference_target_peel(Git2.Reference* reference);

    /// <summary>
    /// Get the type of a reference
    /// </summary>
    /// <param name="reference">The Reference</param>
    /// <returns>The type</returns>
    /// <remarks>
    /// Either <see cref="GitReferenceType.Direct"/> or <see cref="GitReferenceType.Symbolic"/>
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitReferenceType git_reference_target_type(Git2.Reference* reference);

}
