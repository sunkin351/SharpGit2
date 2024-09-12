using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using SharpGit2.Marshalling;

#pragma warning disable IDE1006, CA1401

namespace SharpGit2;

public static unsafe partial class NativeApi
{
    #region LibGit2
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
    public static partial int git_libgit2_init();

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
    public static partial int git_libgit2_shutdown();

    /// <summary>
    /// Query compile time options for libgit2.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitFeatures git_libgit2_features();

    /// <summary>
    /// Return the version of the libgit2 library being currently used.
    /// </summary>
    /// <param name="major">Store the major version number</param>
    /// <param name="minor">Store the minor version number</param>
    /// <param name="rev">Store the revision (patch) number</param>
    /// <returns>0 on success or an error code on failure</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_libgit2_version(int* major, int* minor, int* rev);
    #endregion

    #region Annotated Commit
    /// <summary>
    /// Frees an annotated commit.
    /// </summary>
    /// <param name="commit">annotated commit to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_annotated_commit_free(Git2.AnnotatedCommit* commit);

    /// <summary>
    /// Create an annotated commit from the given fetch head data.
    /// The resulting annotated commit must be freed with <see cref="git_annotated_commit_free"/>.
    /// </summary>
    /// <param name="commit_out">pointer to store the git_annotated_commit result in</param>
    /// <param name="repository">repository that contains the given commit</param>
    /// <param name="branchName">name of the (remote) branch</param>
    /// <param name="remoteUrl">url of the remote</param>
    /// <param name="id">the commit object id of the remote branch</param>
    /// <returns>
    /// 0 on success or an error code
    /// </returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_annotated_commit_from_fetchhead(
        Git2.AnnotatedCommit** commit_out,
        Git2.Repository* repository,
        string branchName,
        string remoteUrl,
        GitObjectID* id);

    /// <summary>
    /// Creates an annotated commit from the given reference.
    /// The resulting annotated commit must be freed with <see cref="git_annotated_commit_free"/>.
    /// </summary>
    /// <param name="commit_out">pointer to store the git_annotated_commit result in</param>
    /// <param name="repo">repository that contains the given reference</param>
    /// <param name="reference">reference to use to lookup the annotated commit</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_annotated_commit_from_ref(Git2.AnnotatedCommit** commit_out, Git2.Repository* repo, Git2.Reference* reference);

    /// <summary>
    /// Creates an annotated commit from a revision string.
    /// </summary>
    /// <param name="commit_out">pointer to store the git_annotated_commit result in</param>
    /// <param name="repo">repository that contains the given commit</param>
    /// <param name="revspec">the extended sha syntax string to use to lookup the commit</param>
    /// <returns>0 on success or an error code</returns>
    /// <remarks>
    /// See <c>man gitrevisions</c>, or http://git-scm.com/docs/git-rev-parse.html#_specifying_revisions for information on the syntax accepted.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_annotated_commit_from_revspec(Git2.AnnotatedCommit** commit_out, Git2.Repository* repo, string revspec);

    /// <summary>
    /// Gets the commit ID that the given annotated commit referes to.
    /// </summary>
    /// <param name="commit">the given annotated commit</param>
    /// <returns>The commit ID</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_annotated_commit_id(Git2.AnnotatedCommit* commit);

    /// <summary>
    /// Creates an annotated commit from the given id.
    /// The resulting annotated commit must be freed with <see cref="git_annotated_commit_free"/>.
    /// </summary>
    /// <param name="commit_out">pointer to store the git_annotated_commit result in</param>
    /// <param name="repo">repository that contains the given commit</param>
    /// <param name="id">the commit object id to lookup</param>
    /// <returns>0 on success or an error code</returns>
    /// <remarks>
    /// An annotated commit contains information about how it was looked up,
    /// which may be useful for functions like merge or rebase to provide context
    /// to the operation. For example, conflict files will include the name of the
    /// source or target branches being merged. It is therefore preferable to use
    /// the most specific function (e.g. <see cref="git_annotated_commit_from_ref"/>)
    /// instead of this one when that data is known.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_annotated_commit_lookup(Git2.AnnotatedCommit** commit_out, Git2.Repository* repo, GitObjectID* id);

    /// <summary>
    /// Get the refname that the given annotated commit referes to.
    /// </summary>
    /// <param name="commit">the given annotated commit</param>
    /// <returns>The ref name</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_annotated_commit_ref(Git2.AnnotatedCommit* commit);
    #endregion

    #region Apply
    /// <summary>
    /// Apply a diff to the given repository, making changes directly in the working directory, the index, or both.
    /// </summary>
    /// <param name="repository">the repository to apply to</param>
    /// <param name="diff">the diff to apply</param>
    /// <param name="location">the location to apply (workdir, index or both)</param>
    /// <param name="options">the options for the apply (or null for defaults)</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_apply(Git2.Repository* repository, Git2.Diff* diff, GitApplyLocationType location, Native.GitApplyOptions* options);

    /// <summary>
    /// Apply a diff to a tree, and return the resulting image as an index.
    /// </summary>
    /// <param name="postimage_out">the postimage of the application</param>
    /// <param name="repository">the repository to apply</param>
    /// <param name="preimage">the tree to apply the diff to</param>
    /// <param name="diff">the diff to apply</param>
    /// <param name="options">the options for the apply (or null for defaults)</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_apply_to_tree(
        Git2.Index** postimage_out,
        Git2.Repository* repository,
        Git2.Tree* preimage,
        Git2.Diff* diff,
        Native.GitApplyOptions* options);
    #endregion

    #region Attribute
    /// <summary>
    /// Add a macro definition.
    /// </summary>
    /// <param name="repository">The repository to add the macro in.</param>
    /// <param name="name">The name of the macro.</param>
    /// <param name="values">The value for the macro.</param>
    /// <returns>0 on success or an error code</returns>
    /// <remarks>
    /// Macros will automatically be loaded from the top level <c>.gitattributes</c>
    /// file of the repository (plus the built-in "binary" macro). This function
    /// allows you to add others.<br/><br/> For example, to add the default macro, you would call:
    /// <code>
    /// git_attr_add_macro(repo, "binary", "-diff -crlf");
    /// </code>
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_attr_add_macro(Git2.Repository* repository, string name, string values);

    /// <summary>
    /// Flush the gitattributes cache.
    /// </summary>
    /// <param name="repository">The repository containing the gitattributes cache</param>
    /// <returns>0 on success or an error code</returns>
    /// <remarks>
    /// Call this if you have reason to believe that the attributes files on disk no longer
    /// match the cached contents of memory. This will cause the attributes files to be
    /// reloaded the next time that an attribute access function is called.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_attr_cache_flush(Git2.Repository* repository);

    /// <summary>
    /// Loop over all the git attributes for a path.
    /// </summary>
    /// <param name="repository">The repository containing the path.</param>
    /// <param name="flags">A combination of <see cref="GitAttributeCheckFlags"/> flags.</param>
    /// <param name="path">
    /// Path inside the repo to check attributes. This does not have to exist,
    /// but if it does not, then it will be treated as a plain file (i.e. not a directory).
    /// </param>
    /// <param name="callback">
    /// Function to invoke on each attribute name and value.
    /// <br/><br/>
    /// Parameter Names: name, value, payload<br/>
    /// Returns: 0 to continue looping, non-zero to stop. This value will be returned from git_attr_foreach.
    /// </param>
    /// <param name="payload">
    /// Passed on as extra parameter to callback function.
    /// </param>
    /// <returns>
    /// 0 on success, non-zero callback return value, or error code
    /// </returns>
    /// <remarks>
    /// The callback will be invoked only once per attribute name, even if there are multiple rules for a given file. The highest priority rule will be used.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_attr_foreach(
        Git2.Repository* repository,
        GitAttributeCheckFlags flags,
        string path,
        // 
        // returns: 0 to continue looping, non-zero to stop. This value will be returned from git_attr_foreach.
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback, 
        nint payload);

    /// <summary>
    /// Loop over all the git attributes for a path.
    /// </summary>
    /// <param name="repository">The repository containing the path.</param>
    /// <param name="options"></param>
    /// <param name="path">
    /// Path inside the repo to check attributes. This does not have to exist,
    /// but if it does not, then it will be treated as a plain file (i.e. not a directory).
    /// </param>
    /// <param name="callback">
    /// Function to invoke on each attribute name and value.
    /// <br/><br/>
    /// Parameter Names: name, value, payload<br/>
    /// Returns: 0 to continue looping, non-zero to stop. This value will be returned from git_attr_foreach.
    /// </param>
    /// <param name="payload">
    /// Passed on as extra parameter to callback function.
    /// </param>
    /// <returns>
    /// 0 on success, non-zero callback return value, or error code
    /// </returns>
    /// <remarks>
    /// The callback will be invoked only once per attribute name, even if there are multiple rules for a given file. The highest priority rule will be used.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_attr_foreach_ext(
        Git2.Repository* repository,
        Native.GitAttributeOptions* options,
        string path,
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback,
        nint payload);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial GitError git_attr_get(
        byte** value_out,
        Git2.Repository* repository,
        GitAttributeCheckFlags flags,
        string path,
        string name);

    /// <summary>
    /// Look up the value of one git attribute for path.
    /// </summary>
    /// <param name="value_out">
    /// Output of the value of the attribute. Use the <see cref="GitAttributeCheckFlags"/>
    /// flags to test for TRUE, FALSE, UNSPECIFIED, etc. or just use the string value for
    /// attributes set to a value. You should NOT modify or free this value.
    /// </param>
    /// <param name="repository">The repository containing the path.</param>
    /// <param name="flags"></param>
    /// <param name="path">
    /// The path to check for attributes. Relative paths are
    /// interpreted relative to the repo root. The file does
    /// not have to exist, but if it does not, then it will
    /// be treated as a plain file (not a directory).
    /// </param>
    /// <param name="name">The name of the attribute to look up.</param>
    /// <returns>
    /// 0 on success or an error code.
    /// </returns>
    public static GitError git_attr_get(
        out string? value_out,
        Git2.Repository* repository,
        GitAttributeCheckFlags flags,
        string path,
        string name)
    {
        byte* _value = null;
        var error = git_attr_get(&_value, repository, flags, path, name);

        value_out = error == GitError.OK ? Utf8StringMarshaller.ConvertToManaged(_value) : null;
        return error;
    }

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial GitError git_attr_get_ext(
        byte** value_out,
        Git2.Repository* repository,
        Native.GitAttributeOptions* options,
        string path,
        string name);

    /// <summary>
    /// Look up the value of one git attribute for path with extended options.
    /// </summary>
    /// <param name="value_out">
    /// Output of the value of the attribute. Use the <see cref="GitAttributeCheckFlags"/>
    /// flags to test for TRUE, FALSE, UNSPECIFIED, etc. or just use the string value for
    /// attributes set to a value. You should NOT modify or free this value.
    /// </param>
    /// <param name="repository">The repository containing the path.</param>
    /// <param name="flags"></param>
    /// <param name="path">
    /// The path to check for attributes. Relative paths are
    /// interpreted relative to the repo root. The file does
    /// not have to exist, but if it does not, then it will
    /// be treated as a plain file (not a directory).
    /// </param>
    /// <param name="name">The name of the attribute to look up.</param>
    /// <returns>
    /// 0 on success or an error code.
    /// </returns>
    public static GitError git_attr_get_ext(
        out string? value_out,
        Git2.Repository* repository,
        Native.GitAttributeOptions* options,
        string path,
        string name)
    {
        byte* _value = null;
        var error = git_attr_get_ext(&_value, repository, options, path, name);

        value_out = error == GitError.OK ? Utf8StringMarshaller.ConvertToManaged(_value) : null;
        return error;
    }

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial GitError git_attr_get_many(
        byte** values_out,
        Git2.Repository* repository,
        GitAttributeCheckFlags flags,
        string path,
        nuint attributeCount,
        byte** name);

    /// <summary>
    /// Look up a list of git attributes for path.
    /// </summary>
    /// <param name="values_out"></param>
    /// <param name="repository"></param>
    /// <param name="flags"></param>
    /// <param name="path"></param>
    /// <param name="names"></param>
    /// <returns></returns>
    public static GitError git_attr_get_many(
        out string[]? values_out,
        Git2.Repository* repository,
        GitAttributeCheckFlags flags,
        string path,
        IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        values_out = null;

        IList<string> _names = names as IList<string> ?? names.ToList();

        int nameCount = _names.Count;

        if (nameCount == 0)
        {
            return GitError.OK;
        }

        int marshalledCount = 0;
        GitError error;

        if (nameCount <= 16)
        {
            byte** pNames = stackalloc byte*[nameCount * 2];
            byte** pValues = pNames + nameCount;

            try
            {
                for (int i = 0; i < nameCount; ++i, ++marshalledCount)
                {
                    pNames[i] = Utf8StringMarshaller.ConvertToUnmanaged(_names[i]);
                }

                error = git_attr_get_many(pValues, repository, flags, path, (nuint)nameCount, pNames);

                if (error == GitError.OK)
                {
                    var array = new string[nameCount];

                    for (int i = 0; i < nameCount; ++i)
                    {
                        array[i] = Utf8StringMarshaller.ConvertToManaged(pValues[i])!;
                    }

                    values_out = array;
                }

                return error;
            }
            finally
            {
                for (int i = 0; i < marshalledCount; ++i)
                {
                    Utf8StringMarshaller.Free(pNames[i]);
                }
            }
        }
        else
        {
            byte** pNames = (byte**)NativeMemory.AllocZeroed((nuint)nameCount * (nuint)sizeof(void*) * 2);
            byte** pValues = pNames + nameCount;
            try
            {
                for (int i = 0; i < nameCount; ++i, ++marshalledCount)
                {
                    pNames[i] = Utf8StringMarshaller.ConvertToUnmanaged(_names[i]);
                }

                error = git_attr_get_many(pValues, repository, flags, path, (nuint)nameCount, pNames);

                if (error == GitError.OK)
                {
                    var array = new string[nameCount];

                    for (int i = 0; i < nameCount; ++i)
                    {
                        array[i] = Utf8StringMarshaller.ConvertToManaged(pValues[i])!;
                    }

                    values_out = array;
                }

                return error;
            }
            finally
            {
                for (int i = 0; i < marshalledCount; ++i)
                {
                    Utf8StringMarshaller.Free(pNames[i]);
                }

                NativeMemory.Free(pNames);
            }
        }
    }


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial GitError git_attr_get_many_ext(
        byte** values_out,
        Git2.Repository* repository,
        Native.GitAttributeOptions* options,
        string path,
        nuint attributeCount,
        byte** name);

    /// <summary>
    /// Look up a list of git attributes for path with extended options.
    /// </summary>
    /// <param name="values_out">The values for each name, in order</param>
    /// <param name="repository">The repository containing the path.</param>
    /// <param name="options">The options to use when querying these attributes</param>
    /// <param name="path">
    /// The path inside the repo to check attributes. This does not have to exist,
    /// but if it does not, then it will be treated as a plain file (i.e. not a directory).
    /// </param>
    /// <param name="names">
    /// Attribute names
    /// </param>
    /// <returns>0 on success or an error code.</returns>
    public static GitError git_attr_get_many_ext(
        out string[]? values_out,
        Git2.Repository* repository,
        Native.GitAttributeOptions* options,
        string path,
        IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        values_out = null;

        var _names = names as IReadOnlyList<string> ?? names.ToList();

        int nameCount = _names.Count;

        if (nameCount == 0)
        {
            values_out = [];
            return GitError.OK;
        }

        int marshalledCount = 0;
        GitError error;

        if (nameCount <= 16)
        {
            byte** pNames = stackalloc byte*[nameCount * 2];
            byte** pValues = pNames + nameCount;

            try
            {
                for (int i = 0; i < nameCount; ++i, ++marshalledCount)
                {
                    pNames[i] = Utf8StringMarshaller.ConvertToUnmanaged(_names[i]);
                }

                error = git_attr_get_many_ext(pValues, repository, options, path, (nuint)nameCount, pNames);

                if (error == GitError.OK)
                {
                    var array = new string[nameCount];

                    for (int i = 0; i < nameCount; ++i)
                    {
                        array[i] = Utf8StringMarshaller.ConvertToManaged(pValues[i])!;
                    }

                    values_out = array;
                }

                return error;
            }
            finally
            {
                for (int i = 0; i < marshalledCount; ++i)
                {
                    Utf8StringMarshaller.Free(pNames[i]);
                }
            }
        }
        else
        {
            byte** pNames = (byte**)NativeMemory.AllocZeroed((nuint)nameCount * (nuint)sizeof(void*) * 2);
            byte** pValues = pNames + nameCount;
            try
            {
                for (int i = 0; i < nameCount; ++i, ++marshalledCount)
                {
                    pNames[i] = Utf8StringMarshaller.ConvertToUnmanaged(_names[i]);
                }

                error = git_attr_get_many_ext(pValues, repository, options, path, (nuint)nameCount, pNames);

                if (error == GitError.OK)
                {
                    var array = new string[nameCount];

                    for (int i = 0; i < nameCount; ++i)
                    {
                        array[i] = Utf8StringMarshaller.ConvertToManaged(pValues[i])!;
                    }

                    values_out = array;
                }

                return error;
            }
            finally
            {
                for (int i = 0; i < marshalledCount; ++i)
                {
                    Utf8StringMarshaller.Free(pNames[i]);
                }

                NativeMemory.Free(pNames);
            }
        }
    }

    /// <summary>
    /// Return the value type for a given attribute.
    /// </summary>
    /// <param name="attribute">The attribute</param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitAttributeValueType git_attr_value(string attribute);
    #endregion

    #region Blame
    /// <summary>
    /// Get blame data for a file that has been modified in memory.
    /// The reference parameter is a pre-calculated blame for the
    /// in-odb history of the file. This means that once a file blame
    /// is completed (which can be expensive), updating the buffer
    /// blame is very fast.
    /// </summary>
    /// <param name="blame_out">pointer that will receive the resulting blame data</param>
    /// <param name="reference">cached blame from the history of the file (usually the output from git_blame_file)</param>
    /// <param name="buffer">the (possibly) modified contents of the file</param>
    /// <param name="bufferLength">number of valid bytes in the buffer</param>
    /// <returns>0 on success, or an error code. (use git_error_last for information about the error)</returns>
    /// <remarks>
    /// Lines that differ between the buffer and the committed version
    /// are marked as having a zero OID for their final_commit_id.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blame_buffer(
        Git2.Blame** blame_out,
        Git2.Blame* reference,
        byte* buffer,
        nuint bufferLength);

    /// <summary>
    /// Get the blame for a single file.
    /// </summary>
    /// <param name="blame_out">pointer that will receive the blame object</param>
    /// <param name="repository">repository whose history is to be walked</param>
    /// <param name="path">path to file to consider</param>
    /// <param name="options">
    /// options for the blame operation. If NULL, this is treated as though GIT_BLAME_OPTIONS_INIT were passed.
    /// </param>
    /// <returns>
    /// 0 on success, or an error code. (use git_error_last for information about the error.)
    /// </returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blame_file(
        Git2.Blame** blame_out,
        Git2.Repository* repository,
        string path,
        Native.GitBlameOptions* options);

    /// <summary>
    /// Free memory allocated by git_blame_file or git_blame_buffer.
    /// </summary>
    /// <param name="blame">the blame structure to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_blame_free(Git2.Blame* blame);

    /// <summary>
    /// Gets the blame hunk at the given index.
    /// </summary>
    /// <param name="blame">the blame structure to query</param>
    /// <param name="index">index of the hunk to retrieve</param>
    /// <returns>the hunk at the given index, or NULL on error</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitBlameHunk* git_blame_get_hunk_byindex(
        Git2.Blame* blame,
        uint index);

    /// <summary>
    /// Gets the hunk that relates to the given line number in the newest commit.
    /// </summary>
    /// <param name="blame">the blame structure to query</param>
    /// <param name="lineNumber">the (1-based) line number to find a hunk for</param>
    /// <returns>the hunk that contains the given line, or NULL on error</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitBlameHunk* git_blame_get_hunk_byline(
        Git2.Blame* blame,
        nuint lineNumber);

    /// <summary>
    /// Gets the number of hunks that exist in the blame structure.
    /// </summary>
    /// <param name="blame">The blame structure to query.</param>
    /// <returns>The number of hunks.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial uint git_blame_get_hunk_count(Git2.Blame* blame);
    #endregion

    #region Blob
    /// <summary>
    /// Write an in-memory buffer to the ODB as a blob
    /// </summary>
    /// <param name="id">return the id of the written blob</param>
    /// <param name="repository">repository where the blob will be written</param>
    /// <param name="buffer">data to be written into the blob</param>
    /// <param name="bufferLength">length of the data</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_create_from_buffer(
        GitObjectID* id,
        Git2.Repository* repository,
        byte* buffer,
        nuint bufferLength);

    /// <summary>
    /// Read a file from the filesystem and write its content to the Object Database as a loose blob
    /// </summary>
    /// <param name="id">return the id of the written blob</param>
    /// <param name="repository">repository where the blob will be written. this repository can be bare or not</param>
    /// <param name="path">file from which the blob will be created</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_create_from_disk(
        GitObjectID* id,
        Git2.Repository* repository,
        string path);

    /// <summary>
    /// Create a stream to write a new blob into the object db
    /// </summary>
    /// <param name="nativeStream">the stream into which to write</param>
    /// <param name="repository">
    /// Repository where the blob will be written. This repository
    /// can be bare or not.
    /// </param>
    /// <param name="hintpath">
    /// If not NULL, will be used to select data filters to apply
    /// onto the content of the blob to be created.
    /// </param>
    /// <returns>0 on success or an error code</returns>
    /// <remarks>
    /// This function may need to buffer the data on disk and will in general
    /// not be the right choice if you know the size of the data to write.
    /// If you have data in memory, use <see cref="git_blob_create_from_buffer"/>.
    /// If you do not, but know the size of the contents (and don't want/need
    /// to perform filtering), use <see cref="git_odb_open_wstream"/>.
    /// <br/><br/>
    /// Don't close this stream yourself but pass it to <see cref="git_blob_create_from_stream_commit"/>
    /// to commit the write to the object db and get the object id.
    /// <br/><br/>
    /// If the <paramref name="hintpath"/> parameter is filled, it will be used to determine what
    /// git filters should be applied to the object before it is written to
    /// the object database.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_create_from_stream(
        Native.GitWriteStream** stream_out,
        Git2.Repository* repository,
        string hintpath);

    /// <summary>
    /// Close the stream and write the blob to the object db
    /// </summary>
    /// <param name="id">the id of the new blob</param>
    /// <param name="stream">the stream to close</param>
    /// <returns>0 on success or an error code</returns>
    /// <remarks>
    /// The stream will be closed and freed.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_create_from_stream_commit(
        GitObjectID* id,
        Native.GitWriteStream* stream);

    /// <summary>
    /// Read a file from the working folder of a repository and write it to the Object Database as a loose blob
    /// </summary>
    /// <param name="id">return the id of the written blob</param>
    /// <param name="repository">repository where the blob will be written. this repository cannot be bare</param>
    /// <param name="relativePath">file from which the blob will be created, relative to the repository's working dir</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_create_from_workdir(
        GitObjectID* id,
        Git2.Repository* repository,
        string relativePath);

    /// <summary>
    /// Determine if the given content is most certainly binary or not; this is the same mechanism used by
    /// <see cref="git_blob_is_binary"/> but only looking at raw data.
    /// </summary>
    /// <param name="data">The blob data which content should be analyzed</param>
    /// <param name="length">The length of the data</param>
    /// <returns>1 if the content of the blob is detected as binary; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_blob_data_is_binary(byte* data, nuint length);

    /// <summary>
    /// Create an in-memory copy of a blob. The copy must be explicitly free'd or it will leak.
    /// </summary>
    /// <param name="blob_out">Pointer to store the copy of the object</param>
    /// <param name="source">Original object to copy</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_dup(Git2.Blob** blob_out, Git2.Blob* source);

    /// <summary>
    /// Get a buffer with the filtered content of a blob.
    /// </summary>
    /// <param name="outBuffer">The buffer to be filled in</param>
    /// <param name="blob">Pointer to the blob</param>
    /// <param name="asPath">Path used for file attribute lookups, etc.</param>
    /// <param name="options">Options to use for filtering the blob</param>
    /// <returns>0 on success or an error code</returns>
    /// <remarks>
    /// This applies filters as if the blob was being checked out to the
    /// working directory under the specified filename. This may apply CRLF
    /// filtering or other types of changes depending on the file attributes
    /// set for the blob and the content detected in it.
    /// <br/><br/>
    /// The output is written into <paramref name="outBuffer"/> which the
    /// caller must free when done (via <see cref="git_buf_dispose(Native.GitBuffer*)"/>).
    /// <br/><br/>
    /// If no filters need to be applied, then the out buffer will just be
    /// populated with a pointer to the raw content of the blob. In that case,
    /// be careful to not free the blob until done with the buffer or copy
    /// it into memory you own.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_filter(
        Native.GitBuffer* outBuffer,
        Git2.Blob* blob,
        string asPath,
        Native.GitBlobFilterOptions* options);

    /// <summary>
    /// Close an open blob.
    /// </summary>
    /// <param name="blob">The blob to close</param>
    /// <remarks>
    /// This is a wrapper around <see cref="git_object_free"/>.<br/>
    /// IMPORTANT: It is necessary to call this method when you stop using a blob. Failure to do so will cause a memory leak.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_blob_free(Git2.Blob* blob);

    /// <summary>
    /// Get the id of a blob.
    /// </summary>
    /// <param name="blob">a previously loaded blob.</param>
    /// <returns>SHA1 hash for this blob.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_blob_id(Git2.Blob* blob);

    /// <summary>
    /// Determine if the blob content is most certainly binary or not.
    /// </summary>
    /// <param name="blob">The blob which content should be analyzed</param>
    /// <returns>1 if the content of the blob is detected as binary; 0 otherwise.</returns>
    /// <remarks>
    /// The heuristic used to guess if a file is binary is taken from core git:
    /// Searching for NUL bytes and looking for a reasonable ratio of printable
    /// to non-printable characters among the first 8000 bytes.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_blob_is_binary(Git2.Blob* blob);

    /// <summary>
    /// Lookup a blob object from a repository.
    /// </summary>
    /// <param name="blob_out">pointer to store a pointer to the looked up blob</param>
    /// <param name="repository">the repo to use when locating the blob.</param>
    /// <param name="id">identity of the blob to locate.</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_lookup(Git2.Blob** blob_out, Git2.Repository* repository, GitObjectID* id);

    /// <summary>
    /// Lookup a blob object from a repository, given a prefix of its identifier (short id).
    /// </summary>
    /// <param name="blob_out">pointer to store a pointer to the looked up blob</param>
    /// <param name="repository">the repo to use when locating the blob.</param>
    /// <param name="id">identity of the blob to locate.</param>
    /// <param name="length">the length of the short identifier</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_blob_lookup_prefix(Git2.Blob** blob_out, Git2.Repository* repository, GitObjectID* id, nuint length);

    /// <summary>
    /// Get the repository that contains the blob.
    /// </summary>
    /// <param name="blob">A previously loaded blob.</param>
    /// <returns>Repository that contains this blob.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial Git2.Repository* git_blob_owner(Git2.Blob* blob);

    /// <summary>
    /// Get a read-only buffer with the raw content of a blob.
    /// </summary>
    /// <param name="blob">pointer to the blob</param>
    /// <returns>the pointer, or NULL on error</returns>
    /// <remarks>
    /// A pointer to the raw content of a blob is returned; this pointer is
    /// owned publicly by the object and shall not be free'd. The pointer
    /// may be invalidated at a later time.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial void* git_blob_rawcontent(Git2.Blob* blob);

    /// <summary>
    /// Get the size in bytes of the contents of a blob
    /// </summary>
    /// <param name="blob">pointer to the blob</param>
    /// <returns>size in bytes</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial ulong git_blob_rawsize(Git2.Blob* blob);
    #endregion

    #region Branch
    /// <summary>
    /// Create a new branch pointing at a target commit
    /// </summary>
    /// <param name="reference_out">Pointer where to store the underlying reference.</param>
    /// <param name="repository">the repository to create the branch in.</param>
    /// <param name="branch_name">
    /// Name for the branch; this name is validated for consistency.
    /// It should also not conflict with an already existing branch name.
    /// </param>
    /// <param name="target">
    /// Commit to which this branch should point. This object must belong to the given <paramref name="repository"/>.
    /// </param>
    /// <param name="force">Overwrite existing branch.</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.InvalidSpec"/> or an error code. A proper reference is written
    /// in the refs/heads namespace pointing to the provided target commit.
    /// </returns>
    /// <remarks>
    /// A new direct reference will be created pointing to this target commit.
    /// If <paramref name="force"/> is true and a reference already exists with the given name,
    /// it'll be replaced.
    /// <br/><br/>
    /// The returned reference must be freed by the user.
    /// <br/><br/>
    /// The branch name will be checked for validity. See <see cref="git_tag_create"/> for rules about valid names.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_create(
        Git2.Reference** reference_out,
        Git2.Repository* repository,
        string branch_name,
        Git2.Commit* target,
        int force);

    /// <summary>
    /// Create a new branch pointing at a target commit
    /// </summary>
    /// <param name="reference_out">Pointer where to store the underlying reference.</param>
    /// <param name="repository">the repository to create the branch in.</param>
    /// <param name="branch_name">
    /// Name for the branch; this name is validated for consistency.
    /// It should also not conflict with an already existing branch name.
    /// </param>
    /// <param name="commit">Commit to which this branch should point. This object must belong to the given <paramref name="repository"/>.</param>
    /// <param name="force">Overwrite existing branch.</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.InvalidSpec"/> or an error code. A proper reference is written
    /// in the refs/heads namespace pointing to the provided target commit.
    /// </returns>
    /// <remarks>
    /// This behaves like <see cref="git_branch_create"/> but takes an annotated commit,
    /// which lets you specify which extended sha syntax string was specified
    /// by a user, allowing for more exact reflog messages.
    /// See the documentation for <see cref="git_branch_create"/>.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_create_from_annotated(
        Git2.Reference** reference_out,
        Git2.Repository* repository,
        string branch_name,
        Git2.AnnotatedCommit* commit,
        int force);

    /// <summary>
    /// Delete an existing branch reference.
    /// </summary>
    /// <param name="branch">A valid reference representing a branch</param>
    /// <returns>0 on success or an error code.</returns>
    /// <remarks>
    /// Note that if the deletion succeeds, the reference object will not be valid anymore,
    /// and should be freed immediately by the user using <see cref="git_reference_free"/>.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_delete(Git2.Reference* branch);

    /// <summary>
    /// Determine if any HEAD points to the current branch
    /// </summary>
    /// <param name="branch">A reference to a local branch.</param>
    /// <returns>
    /// 1 if branch is checked out, 0 if it isn't,
    /// an error code otherwise.
    /// </returns>
    /// <remarks>
    /// This will iterate over all known linked repositories (usually in the form of worktrees)
    /// and report whether any HEAD is pointing at the current branch.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_branch_is_checked_out(Git2.Reference* branch);

    /// <summary>
    /// Determine if HEAD points to the given branch
    /// </summary>
    /// <param name="branch">A reference to a local branch.</param>
    /// <returns>
    /// 1 if HEAD points at the branch, 0 if it isn't,
    /// or a negative value as an error code.
    /// </returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_branch_is_head(Git2.Reference* branch);

    /// <summary>
    /// Free a branch iterator
    /// </summary>
    /// <param name="iterator">the iterator to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_branch_iterator_free(Git2.BranchIterator* iterator);

    /// <summary>
    /// Create an iterator which loops over the requested branches.
    /// </summary>
    /// <param name="iterator_out">the iterator</param>
    /// <param name="repository">Repository where to find the branches.</param>
    /// <param name="flags">Filtering flags for the branch listing.</param>
    /// <returns>0 on success or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_iterator_new(Git2.BranchIterator** iterator_out, Git2.Repository* repository, GitBranchType flags);

    /// <summary>
    /// Lookup a branch by its name in a repository.
    /// </summary>
    /// <param name="branch_out">pointer to the looked-up branch reference</param>
    /// <param name="repository">the repository to look up the branch</param>
    /// <param name="branch_name">Name of the branch to be looked-up; this name is validated for consistency.</param>
    /// <param name="branchType">
    /// Type of the considered branch. This should be either
    /// <see cref="GitBranchType.Local"/> or <see cref="GitBranchType.Remote"/>.
    /// </param>
    /// <returns>
    /// 0 on success; <see cref="GitError.NotFound"/> when no matching branch exists,
    /// <see cref="GitError.InvalidSpec"/>, otherwise an error code.
    /// </returns>
    /// <remarks>
    /// The generated reference must be freed by the user.
    /// The branch name will be checked for validity.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_lookup(
        Git2.Reference** branch_out,
        Git2.Repository* repository,
        string branch_name,
        GitBranchType branchType);

    /// <summary>
    /// Move/rename an existing local branch reference.
    /// </summary>
    /// <param name="branch_out">New reference object for the updated name.</param>
    /// <param name="branch">Current underlying reference of the branch.</param>
    /// <param name="new_branch_name">Target name of the branch once the move is performed; this name is validated for consistency.</param>
    /// <param name="force">Overwrite existing branch.</param>
    /// <returns>0 on success, <see cref="GitError.InvalidSpec"/> or an error code.</returns>
    /// <remarks>
    /// The new branch name will be checked for validity.
    /// See <see cref="git_tag_create"/> for rules about valid names.
    /// <br/><br/>
    /// Note that if the move succeeds, the old reference object will
    /// not be valid anymore, and should be freed immediately by the
    /// user using <see cref="git_reference_free"/>.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_move(
        Git2.Reference** branch_out,
        Git2.Reference* branch,
        string new_branch_name,
        int force);

    /// <summary>
    /// Get the branch name
    /// </summary>
    /// <param name="name_out">
    /// Pointer to the abbreviated reference name. Owned by ref,
    /// do not free.
    /// </param>
    /// <param name="branch_reference">
    /// A reference object, ideally pointing to a branch
    /// </param>
    /// <returns>
    /// 0 on success; <see cref="GitError.Invalid"/> if the reference
    /// isn't either a local or remote branch, otherwise an error code.
    /// </returns>
    /// <remarks>
    /// Given a reference object, this will check that it really is a
    /// branch (ie. it lives under "refs/heads/" or "refs/remotes/"),
    /// and return the branch part of it.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_name(byte** name_out, Git2.Reference* branch_reference);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valid"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_name_is_valid(int* valid, string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reference_out"></param>
    /// <param name="type_out"></param>
    /// <param name="iterator"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_next(Git2.Reference** reference_out, GitBranchType* type_out, Git2.BranchIterator* iterator);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_remote_name(Native.GitBuffer* name_out, Git2.Repository* repository, string refname);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_set_upstream(Git2.Reference* branch, string branch_name);


    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_upstream(Git2.Reference** branch_out, Git2.Reference* branch);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_upstream_merge(Native.GitBuffer* buffer, Git2.Repository* repository, string refname);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_upstream_name(Native.GitBuffer* buffer, Git2.Repository* repository, string refname);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_branch_upstream_remote(Native.GitBuffer* buffer, Git2.Repository* repository, string refname);
    #endregion

    #region Buffer
    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static bool git_buf_contains_nul(Native.GitBuffer* buffer)
    {
        return buffer->ContainsNul();
    }


    /// <summary>
    /// Free the memory referred to by the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to deallocate</param>
    /// <remarks>
    /// Note that this does not free the git_buf itself, just the memory pointed to by <see cref="Native.GitBuffer.Pointer"/>.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_buf_dispose(Native.GitBuffer* buffer);

    /// <summary>
    /// Free the memory referred to by the git_buf. This is an alias of <see cref="git_buf_dispose"/> and is preserved for backward compatibility.
    /// </summary>
    /// <param name="buffer">The buffer to deallocate</param>
    /// <remarks>
    /// This function is deprecated, but there is no plan to remove this function at this time.
    /// </remarks>
    [Obsolete("Use git_buf_dispose()")]
    public static void git_buf_free(Native.GitBuffer* buffer) => git_buf_dispose(buffer);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="target_size"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_buf_grow(Native.GitBuffer* buffer, nuint target_size);

    /// <summary>
    /// Check quickly if buffer looks like it contains binary data
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="target_size"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_buf_is_binary(Native.GitBuffer* buffer);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="data"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_buf_set(Native.GitBuffer* buffer, void* data, nuint length);
    #endregion

    #region Checkout
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_checkout_head(Git2.Repository* repository, Native.GitCheckoutOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="index"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_checkout_index(Git2.Repository* repository, Git2.Index* index, Native.GitCheckoutOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="treeish"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_checkout_tree(Git2.Repository* repository, Git2.Object* treeish, Native.GitCheckoutOptions* options);
    #endregion

    #region Cherrypick
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="commit"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_cherrypick(Git2.Repository* repository, Git2.Commit* commit, Native.GitCherrypickOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index_out"></param>
    /// <param name="repository"></param>
    /// <param name="cherrypick_commit"></param>
    /// <param name="our_commit"></param>
    /// <param name="mainline"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_cherrypick_commit(
        Git2.Index** index_out,
        Git2.Repository* repository,
        Git2.Commit* cherrypick_commit,
        Git2.Commit* our_commit,
        uint mainline,
        Native.GitMergeOptions* options);
    #endregion

    #region Clone
    /// <summary>
    /// Clone a remote repository.
    /// </summary>
    /// <param name="repo_out">pointer that will receive the resulting repository object</param>
    /// <param name="url">the remote repository to clone</param>
    /// <param name="local_path">local directory to clone to</param>
    /// <param name="options">configuration options for the clone. If NULL, the function works as though GIT_OPTIONS_INIT were passed</param>
    /// <returns>
    /// 0 on success, any non-zero return value from a callback function,
    /// or a negative value to indicate an error (use `git_error_last` for a detailed error message)
    /// </returns>
    /// <remarks>
    /// By default this creates its repository and initial remote to match git's defaults.
    /// You can use the options in the callback to customize how these are created.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_clone(Git2.Repository** repo_out, string url, string local_path, Native.GitCloneOptions* options);
    #endregion

    #region Commit
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial GitError git_commit_amend(
        GitObjectID* id,
        Git2.Commit* commit_to_amend,
        string update_ref,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        string message_encoding,
        byte* message,
        Git2.Tree* tree);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="commit_to_amend"></param>
    /// <param name="update_ref"></param>
    /// <param name="author"></param>
    /// <param name="committer"></param>
    /// <param name="message_encoding"></param>
    /// <param name="message"></param>
    /// <param name="tree"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static GitError git_commit_amend(
        GitObjectID* id,
        Git2.Commit* commit_to_amend,
        string update_ref,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        Encoding message_encoding,
        string message,
        Git2.Tree* tree)
    {
        if (!message_encoding.IsSingleByte)
            throw new NotSupportedException("Multibyte encodings are not supported! (i.e. encodings who's code unit is more than 1 byte wide)");

        var array = ArrayPool<byte>.Shared.Rent(message_encoding.GetMaxByteCount(message.Length + 1));
        try
        {
            var written = message_encoding.GetBytes(message, array);
            array[written] = 0;

            fixed (byte* pMessage = array)
            {
                return git_commit_amend(id, commit_to_amend, update_ref, author, committer, message_encoding.EncodingName, pMessage, tree);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitSignature* git_commit_author(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="signature_out"></param>
    /// <param name="commit"></param>
    /// <param name="mailmap"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_author_with_mailmap(Native.GitSignature** signature_out, Git2.Commit* commit, Git2.MailMap* mailmap);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_commit_body(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitSignature* git_commit_committer(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="signature_out"></param>
    /// <param name="commit"></param>
    /// <param name="mailmap"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_committer_with_mailmap(Native.GitSignature** signature_out, Git2.Commit* commit, Git2.MailMap* mailmap);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="repository"></param>
    /// <param name="update_ref"></param>
    /// <param name="author"></param>
    /// <param name="committer"></param>
    /// <param name="message_encoding"></param>
    /// <param name="message"></param>
    /// <param name="tree"></param>
    /// <param name="parent_count"></param>
    /// <param name="parents"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_create(
        GitObjectID* id,
        Git2.Repository* repository,
        string update_ref,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        byte* message_encoding,
        byte* message,
        Git2.Tree* tree,
        nuint parent_count,
        Git2.Commit** parents);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="repository"></param>
    /// <param name="update_ref"></param>
    /// <param name="author"></param>
    /// <param name="committer"></param>
    /// <param name="message_encoding"></param>
    /// <param name="message"></param>
    /// <param name="tree"></param>
    /// <param name="parent_count"></param>
    /// <param name="parents"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_create_buffer(
        Native.GitBuffer* buffer,
        Git2.Repository* repository,
        string update_ref,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        byte* message_encoding,
        byte* message,
        Git2.Tree* tree,
        nuint parent_count,
        Git2.Commit** parents);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="repository"></param>
    /// <param name="commit_content"></param>
    /// <param name="signature"></param>
    /// <param name="signature_field"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_create_with_signature(
        GitObjectID* id,
        Git2.Repository* repository,
        string commit_content,
        string signature,
        string signature_field);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit_out"></param>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_dup(Git2.Commit** commit_out, Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="signature"></param>
    /// <param name="signed_data"></param>
    /// <param name="repository"></param>
    /// <param name="commit_id"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_extract_signature(
        Native.GitBuffer* signature,
        Native.GitBuffer* signed_data,
        Git2.Repository* repository,
        GitObjectID* commit_id,
        string field);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_commit_free(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="header_out"></param>
    /// <param name="commit"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_header_field(
        Native.GitBuffer* header_out,
        Git2.Commit* commit,
        string field);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial GitObjectID* git_commit_id(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit_out"></param>
    /// <param name="repository"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_lookup(
        Git2.Commit** commit_out,
        Git2.Repository* repository,
        GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit_out"></param>
    /// <param name="repository"></param>
    /// <param name="id"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_lookup_prefix(
        Git2.Commit** commit_out,
        Git2.Repository* repository,
        GitObjectID* id,
        nuint length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_commit_message(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_commit_message_encoding(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_commit_message_raw(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ancestor"></param>
    /// <param name="commit"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_nth_gen_ancestor(Git2.Commit** ancestor, Git2.Commit* commit, uint n);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial Git2.Repository* git_commit_owner(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="commit"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_parent(Git2.Commit** parent, Git2.Commit* commit, uint n);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="commit"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_commit_parent_id(Git2.Commit* commit, uint n);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial uint git_commit_parentcount(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_commit_raw_header(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_commit_summary(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ulong git_commit_time(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_commit_time_offset(Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tree_out"></param>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_commit_tree(Git2.Tree** tree_out, Git2.Commit* commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_commit_tree_id(Git2.Commit* commit);
    #endregion

    #region Config
    /// <summary>
    /// Add an on-disk config file instance to an existing config
    /// </summary>
    /// <param name="config">the configuration to add the file to</param>
    /// <param name="path">path to the configuration file to add</param>
    /// <param name="level">the priority level of the backend</param>
    /// <param name="repository">optional repository to allow parsing of conditional includes</param>
    /// <param name="force">replace config file at the given priority level</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.Exists"/> when adding more
    /// than one file for a given priority level (and <paramref name="force"/> set to <see langword="false"/>),
    /// <see cref="GitError.NotFound"/> when the file doesn't exist or error code
    /// </returns>
    /// <remarks>
    /// The on-disk file pointed at by path will be opened and parsed;
    /// it's expected to be a native Git config file following the default
    /// Git config syntax (see man git-config).
    /// <br/><br/>
    /// If the file does not exist, the file will still be added and it will
    /// be created the first time we write to it.
    /// <br/><br/>
    /// Note that the configuration object will free the file automatically.
    /// <br/><br/>
    /// Further queries on this config object will access each of the config
    /// file instances in order (instances with a higher priority level will
    /// be accessed first).
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_add_file_ondisk(Git2.Config* config, string path, GitConfigLevel level, Git2.Repository* repository, int force);

    // TODO: Docs
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_backend_foreach_match(Git2.ConfigBackend* config, string? regex, delegate* unmanaged[Cdecl]<Native.GitConfigEntry*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Delete a config variable from the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">the configuration</param>
    /// <param name="name">the variable to delete</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_delete_entry(Git2.Config* config, string name);

    /// <summary>
    /// Deletes one or several entries from a multivar in the local config file.
    /// </summary>
    /// <param name="config">where to look for the variables</param>
    /// <param name="name">the variable's name</param>
    /// <param name="regex">a regular expression to indicate which values to delete</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The regular expression is applied case-sensitively on the value.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_delete_multivar(Git2.Config* config, string name, string regex);

    /// <summary>
    /// Free a config entry
    /// </summary>
    /// <param name="entry">The entry to free.</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_config_entry_free(Native.GitConfigEntry* entry);

    /// <summary>
    /// Locate the path to the global configuration file
    /// </summary>
    /// <param name="outPath">Pointer to a user-allocated git_buf in which to store the path</param>
    /// <returns>
    /// 0 if a global configuration file has been found. Its path will be stored in <paramref name="outPath"/>
    /// </returns>
    /// <remarks>
    /// The user or global configuration file is usually located in "$HOME/.gitconfig".<br/>
    /// This method will try to guess the full path to that file, if the file exists.
    /// The returned path may be used on any git_config call to load the global configuration file.<br/>
    /// This method will not guess the path to the xdg compatible config file (".config/git/config").
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_find_global(Native.GitBuffer* outPath);

    /// <summary>
    /// Locate the path to the configuration file in ProgramData
    /// </summary>
    /// <param name="outPath">Pointer to a user-allocated git_buf in which to store the path</param>
    /// <returns>
    /// 0 if a ProgramData configuration file has been found. Its path will be stored in <paramref name="outPath"/>
    /// </returns>
    /// <remarks>
    /// Look for the file in "%PROGRAMDATA%" used by portable git.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_find_programdata(Native.GitBuffer* outPath);

    /// <summary>
    /// Locate the path to the system configuration file
    /// </summary>
    /// <param name="outPath">Pointer to a user-allocated git_buf in which to store the path</param>
    /// <returns>0 if a system configuration file has been found. Its path will be stored in <paramref name="outPath"/>.</returns>
    /// <remarks>
    /// If <c>/etc/gitconfig</c> doesn't exist, it will look for <c>%PROGRAMFILES%\Git\etc\gitconfig</c>
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_find_system(Native.GitBuffer* outPath);

    /// <summary>
    /// Locate the path to the global xdg compatible configuration file
    /// </summary>
    /// <param name="outPath">Pointer to a user-allocated git_buf in which to store the path</param>
    /// <returns>0 if a xdg compatible configuration file has been found. Its path will be stored in <paramref name="outPath"/>.</returns>
    /// <remarks>
    /// The xdg compatible configuration file is usually located in <c>$HOME/.config/git/config</c>
    /// <br/><br/>
    /// This method will try to guess the full path to that file, if the file exists.
    /// The returned path may be used on any <c>git_config</c> call to load the xdg compatible configuration file.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_find_xdg(Native.GitBuffer* outPath);

    /// <summary>
    /// Perform an operation on each config variable.
    /// </summary>
    /// <param name="config">where to get the variables from</param>
    /// <param name="callback">the function to call on each variable</param>
    /// <param name="payload">the data to pass to the callback</param>
    /// <returns>0 on success, non-zero callback return value, or error code</returns>
    /// <remarks>
    /// The callback receives the normalized name and value of each variable in the config backend,
    /// and the data pointer passed to this function. If the callback returns a non-zero value,
    /// the function stops iterating and returns that value to the caller.
    /// <br/><br/>
    /// The pointers passed to the callback are only valid as long as the iteration is ongoing.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_foreach(Git2.Config* config, delegate* unmanaged[Cdecl]<Native.GitConfigEntry*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Perform an operation on each config variable matching a regular expression.
    /// </summary>
    /// <param name="config">where to get the variables from</param>
    /// <param name="regex">regular expression to match against config names</param>
    /// <param name="callback">the function to call on each variable</param>
    /// <param name="payload">the data to pass to the callback</param>
    /// <returns>0 or the return value of the callback which didn't return 0</returns>
    /// <remarks>
    /// This behaves like <see cref="git_config_foreach"/> with an additional filter
    /// of a regular expression that filters which config keys are passed to the callback.
    /// <br/><br/>
    /// The regular expression is applied case-sensitively on the normalized form of the
    /// variable name: the section and variable parts are lower-cased. The subsection is
    /// left unchanged.
    /// <br/><br/>
    /// The regular expression is applied case-sensitively on the normalized form of the
    /// variable name: the case-insensitive parts are lower-case.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_foreach_match(Git2.Config* config, string regex, delegate* unmanaged[Cdecl]<Native.GitConfigEntry*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Free the configuration and its associated memory and files
    /// </summary>
    /// <param name="config">the configuration to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_config_free(Git2.Config* config);

    /// <summary>
    /// Get the value of a boolean config variable.
    /// </summary>
    /// <param name="value">pointer to the variable where the value should be stored</param>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// This function uses the usual C convention of 0 being false and anything else true.
    /// <br/><br/>
    /// All config files will be looked into, in the order of their defined level.
    /// A higher level means a higher priority. The first occurrence of the variable will be returned here.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_bool(int* value, Git2.Config* config, string name);

    /// <summary>
    /// Get the git_config_entry of a config variable.
    /// </summary>
    /// <param name="value">pointer to the variable git_config_entry</param>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Free the git_config_entry after use with <see cref="git_config_entry_free"/>
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_entry(Native.GitConfigEntry** value, Git2.Config* config, string name);

    /// <summary>
    /// Get the value of an integer config variable.
    /// </summary>
    /// <param name="value">pointer to the variable where the value should be stored</param>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// All config files will be looked into, in the order of their defined level.
    /// A higher level means a higher priority. The first occurrence of the variable will be returned here.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_int32(int* value, Git2.Config* config, string name);

    /// <summary>
    /// Get the value of a long integer config variable.
    /// </summary>
    /// <param name="value">pointer to the variable where the value should be stored</param>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// All config files will be looked into, in the order of their defined level.
    /// A higher level means a higher priority. The first occurrence of the variable will be returned here.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_int64(long* value, Git2.Config* config, string name);


    /// <summary>
    /// Query the value of a config variable and return it mapped to an integer constant.
    /// </summary>
    /// <param name="value">place to store the result of the mapping</param>
    /// <param name="config">config file to get the variables from</param>
    /// <param name="name">name of the config variable to lookup</param>
    /// <param name="map">array of <see cref="Git2.ConfigMap"/> objects specifying the possible mappings</param>
    /// <param name="map_n">number of mapping objects in `maps`</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// This is a helper method to easily map different possible values to a variable to integer constants that easily identify them.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_mapped(int* value, Git2.Config* config, string name, Native.GitConfigMap* map, nuint map_n);

    /// <summary>
    /// Get each value of a multivar in a foreach callback
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="regex">regular expression to filter which variables we're interested in. Use <see langword="null"/> to indicate all</param>
    /// <param name="callback">the function to be called on each value of the variable</param>
    /// <param name="payload">opaque pointer to pass to the callback</param>
    /// <returns>0 or an error code.</returns>
    /// <remarks>
    /// The callback will be called on each variable found.
    /// <br/><br/>
    /// The regular expression is applied case-sensitively on the normalized form of the variable name:
    /// the section and variable parts are lower-cased. The subsection is left unchanged.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_multivar_foreach(Git2.Config* config, string name, string? regex, delegate* unmanaged[Cdecl]<Native.GitConfigEntry*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Get the value of a path config variable.
    /// </summary>
    /// <param name="buffer">the buffer in which to store the result</param>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// A leading '~' will be expanded to the global search path
    /// (which defaults to the user's home directory but can be
    /// overridden via <c>git_libgit2_opts()</c>.)
    /// <br/><br/>
    /// All config files will be looked into, in the order of
    /// their defined level. A higher level means a higher
    /// priority. The first occurrence of the variable will be
    /// returned here.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_path(Native.GitBuffer* buffer, Git2.Config* config, string name);

    /// <summary>
    /// Get the value of a string config variable.
    /// </summary>
    /// <param name="value">pointer to the string</param>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// This function can only be used on snapshot config objects.
    /// The string is owned by the config and should not be freed by the user.
    /// The pointer will be valid until the config is freed.
    /// <br/><br/>
    /// All config files will be looked into, in the order of their defined level.
    /// A higher level means a higher priority.
    /// The first occurrence of the variable will be returned here.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_string(byte** value, Git2.Config* config, string name);

    /// <summary>
    /// Get the value of a string config variable.
    /// </summary>
    /// <param name="value">pointer to the string</param>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The value of the config will be copied into the buffer.
    /// <br/><br/>
    /// All config files will be looked into, in the order of their defined level.
    /// A higher level means a higher priority.
    /// The first occurrence of the variable will be returned here.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_get_string_buf(Native.GitBuffer* value, Git2.Config* config, string name);

    /// <summary>
    /// Free a config iterator
    /// </summary>
    /// <param name="iterator">the iterator to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_config_iterator_free(Git2.ConfigIterator* iterator);

    /// <summary>
    /// Iterate over all the config variables whose name matches a pattern
    /// </summary>
    /// <param name="iterator">pointer to store the iterator</param>
    /// <param name="config">where to ge the variables from</param>
    /// <param name="regex">regular expression to match the names</param>
    /// <returns>0 or an error code.</returns>
    /// <remarks>
    /// Use <see cref="git_config_next"/> to advance the iteration and <see cref="git_config_iterator_free(Git2.ConfigIterator*)"> when done.
    /// <br/><br/>
    /// The regular expression is applied case-sensitively on the normalized
    /// form of the variable name: the section and variable parts are lower-cased.
    /// The subsection is left unchanged.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_iterator_glob_new(Git2.ConfigIterator** iterator, Git2.Config* config, string regex);

    /// <summary>
    /// Iterate over all the config variables
    /// </summary>
    /// <param name="iterator">pointer to store the iterator</param>
    /// <param name="config">where to get the variables from</param>
    /// <returns>0 or an error code.</returns>
    /// <remarks>
    /// Use <see cref="git_config_next"/> to advance the iteration and <see cref="git_config_iterator_free(Git2.ConfigIterator*)"> when done.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_iterator_new(Git2.ConfigIterator** iterator, Git2.Config* config);

    /// <summary>
    /// Lock the backend with the highest priority
    /// </summary>
    /// <param name="transaction">the resulting transaction, use this to commit or undo the changes</param>
    /// <param name="config">the configuration in which to lock</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Locking disallows anybody else from writing to that backend. Any updates made after locking will not be visible to a reader until the file is unlocked.
    /// 
    /// You can apply the changes by calling <see cref="git_transaction_commit"/> before freeing the transaction. Either of these actions will unlock the config.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_lock(Git2.Transaction** transaction, Git2.Config* config);

    /// <summary>
    /// Maps a string value to an integer constant
    /// </summary>
    /// <param name="map_value">place to store the result of the parsing</param>
    /// <param name="maps">array of <see cref="Git2.ConfigMap"/> objects specifying the possible mappings</param>
    /// <param name="map_n">number of mapping objects in <paramref name="maps"/></param>
    /// <param name="value">value to parse</param>
    /// <returns>0 or an error code.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_lookup_map_value(int* map_value, Native.GitConfigMap* maps, nuint map_n, byte* value);

    /// <summary>
    /// Get each value of a multivar
    /// </summary>
    /// <param name="iterator">pointer to store the iterator</param>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">	the variable's name</param>
    /// <param name="regex">regular expression to filter which variables we're interested in. Use NULL to indicate all</param>
    /// <returns>0 or an error code.</returns>
    /// <remarks>
    /// The regular expression is applied case-sensitively on the normalized form of the variable name:
    /// the section and variable parts are lower-cased. The subsection is left unchanged.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_multivar_iterator_new(Git2.ConfigIterator** iterator, Git2.Config* config, string name, string? regex);

    /// <summary>
    /// Allocate a new configuration object
    /// </summary>
    /// <param name="out">pointer to the new configuration</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// This object is empty, so you have to add a file to it before you can do anything with it.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_new(Git2.Config** @out);

    /// <summary>
    /// Return the current entry and advance the iterator
    /// </summary>
    /// <param name="out">pointer to store the entry</param>
    /// <param name="iterator">the iterator</param>
    /// <returns>0 or an error code. GIT_ITEROVER if the iteration has completed</returns>
    /// <remarks>
    /// The pointers returned by this function are valid until the next call to git_config_next or until the iterator is freed.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_next(Native.GitConfigEntry** @out, Git2.ConfigIterator* iterator);

    /// <summary>
    /// Open the global, XDG and system configuration files
    /// </summary>
    /// <param name="out">Pointer to store the config instance</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Utility wrapper that finds the global, XDG and system configuration
    /// files and opens them into a single prioritized config object that
    /// can be used when accessing default config data outside a repository.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_open_default(Git2.Config** @out);

    /// <summary>
    /// Open the global/XDG configuration file according to git's rules
    /// </summary>
    /// <param name="out">Pointer to store the config object</param>
    /// <param name="config">the config object in which to look</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Git allows you to store your global configuration at <c>$HOME/.gitconfig</c> or <c>$XDG_CONFIG_HOME/git/config</c>.
    /// For backwards compatibility, the XDG file shouldn't be used unless the use has created it explicitly.
    /// With this function you'll open the correct one to write to.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_open_global(Git2.Config** @out, Git2.Config* config);

    /// <summary>
    /// Build a single-level focused config object from a multi-level one.
    /// </summary>
    /// <param name="out">The configuration instance to create</param>
    /// <param name="config">Multi-level config to search for the given level</param>
    /// <param name="level">Configuration level to search for</param>
    /// <returns>0 on success, <see cref="GitError.NotFound"/> if the passed level cannot be found in the multi-level parent config, or an error code</returns>
    /// <remarks>
    /// The returned config object can be used to perform get/set/delete
    /// operations on a single specific level.
    /// <br/><br/>
    /// Getting several times the same level from the same parent multi-level
    /// config will return different config instances, but containing the same
    /// config_file instance.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_open_level(Git2.Config** @out, Git2.Config* parent, GitConfigLevel level);

    /// <summary>
    /// Create a new config instance containing a single on-disk file
    /// </summary>
    /// <param name="out">The configuration instance to create</param>
    /// <param name="path">Path to the on-disk file to open</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// This method is a simple utility wrapper for the following sequence of calls:<br/>
    /// - <see cref="git_config_new(Git2.Config**)"/><br/>
    /// - <see cref="git_config_add_file_ondisk(Git2.Config*, string, GitConfigLevel, Git2.Repository*, int)"/>
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_open_ondisk(Git2.Config** @out, string path);

    /// <summary>
    /// Set the value of a boolean config variable in the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="value">the value to store</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_set_bool(Git2.Config* config, string name, int value);

    /// <summary>
    /// Set the value of an integer config variable in the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="value">Integer value for the variable</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_set_int32(Git2.Config* config, string name, int value);

    /// <summary>
    /// Set the value of a long integer config variable in the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="value">Long integer value for the variable</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_set_int64(Git2.Config* config, string name, long value);

    /// <summary>
    /// Set a multivar in the local config file.
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="regex">a regular expression to indicate which values to replace</param>
    /// <param name="value">the new value.</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The regular expression is applied case-sensitively on the value.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_set_multivar(Git2.Config* config, string name, string regex, string value);

    /// <summary>
    /// Set the value of a string config variable in the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="value">the string to store.</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// A copy of the string is made and the user is free to use it afterwards.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_set_string(Git2.Config* config, string name, string value);

    /// <summary>
    /// Create a snapshot of the configuration
    /// </summary>
    /// <param name="out">pointer in which to store the snapshot config object</param>
    /// <param name="config">configuration to snapshot</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Create a snapshot of the current state of a configuration,
    /// which allows you to look into a consistent view of the configuration
    /// for looking up complex values (e.g. a remote, submodule).
    /// <br/><br/>
    /// The string returned when querying such a config object is valid until it is freed.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_config_snapshot(Git2.Config** @out, Git2.Config* config);
    #endregion

    #region Credential

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential_out"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_credential_default_new(Git2.Credential** credential_out);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_credential_free(Git2.Credential* credential);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_credential_get_username(Git2.Credential* credential);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_credential_has_username(Git2.Credential* credential);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential_out"></param>
    /// <param name="username"></param>
    /// <param name="public_key"></param>
    /// <param name="public_key_length"></param>
    /// <param name="sign_callback"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_credential_ssh_custom_new(
        Git2.Credential** credential_out,
        string username,
        byte* public_key,
        nuint public_key_length,
        delegate* unmanaged[Cdecl]<void*, byte**, nuint*, byte*, nuint, void**, int> sign_callback,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential_out"></param>
    /// <param name="username"></param>
    /// <param name="prompt_callback"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_credential_ssh_custom_new(
        Git2.Credential** credential_out,
        string username,
        delegate* unmanaged[Cdecl]<byte*, int, byte*, int, int, void*, void*, void**, void> prompt_callback,
        void* payload);

    /// <summary>
    /// Create a new ssh key credential object used for querying an ssh-agent.
    /// The supplied credential parameter will be internally duplicated.
    /// </summary>
    /// <param name="credential_out"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_credential_ssh_key_from_agent(
        Git2.Credential** credential_out,
        string username);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential_out"></param>
    /// <param name="username"></param>
    /// <param name="public_key"></param>
    /// <param name="private_key"></param>
    /// <param name="pass_phrase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_credential_ssh_key_memory_new(
        Git2.Credential** credential_out,
        string username,
        string? public_key,
        string private_key,
        string? pass_phrase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential_out"></param>
    /// <param name="username"></param>
    /// <param name="public_key"></param>
    /// <param name="private_key"></param>
    /// <param name="pass_phrase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_credential_ssh_key_new(
        Git2.Credential** credential_out,
        string username,
        string? public_key,
        string private_key,
        string? pass_phrase);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_credential_username_new(Git2.Credential** credential_out, string username);

    /// <summary>
    /// Stock callback usable as a git_credential_acquire_cb.
    /// This calls <see cref="git_credential_userpass_plaintext_new(Git2.Credential**, string)"/> unless the
    /// protocol has not specified <see cref="GitCredentialType.UserPassPlainText"/>
    /// as an allowed type.
    /// </summary>
    /// <remarks>
    /// The payload provided when specifying this callback is interpreted as <see cref="Native.GitCredentialUserPassPayload"/> pointer.
    /// <br/><br/>
    /// This is provided as a function pointer to allow passing it as a parameter to certain libgit2 methods.
    /// </remarks>
    public static readonly delegate* unmanaged[Cdecl]<Git2.Credential**, byte*, byte*, GitCredentialType, nint, int> git_credential_userpass;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential_out"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_credential_userpass_plaintext_new(Git2.Credential** credential_out, string username, string password);

    #endregion

    #region Describe
    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="committish"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_describe_commit(
        Git2.DescribeResult** result,
        Git2.Object* committish,
        Native.GitDescribeOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="format_out"></param>
    /// <param name="result"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_describe_format(
        Native.GitBuffer* format_out,
        Git2.DescribeResult* result,
        Native.GitDescribeFormatOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_describe_result_free(Git2.DescribeResult* result);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result_out"></param>
    /// <param name="repository"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_describe_workdir(
        Git2.DescribeResult** result_out,
        Git2.Repository* repository,
        Native.GitDescribeOptions* options);
    #endregion

    #region Diff
    /// <summary>
    /// Directly run a diff between a blob and a buffer.
    /// </summary>
    /// <param name="oldBlob">Blob for old side of diff, or NULL for empty blob</param>
    /// <param name="old_as_path">Treat old blob as if it had this filename; can be NULL</param>
    /// <param name="buffer">Raw data for new side of diff, or NULL for empty</param>
    /// <param name="buffer_length">Length of raw data for new side of diff</param>
    /// <param name="buffer_as_path">Treat buffer as if it had this filename; can be NULL</param>
    /// <param name="options">Options for diff, or NULL for default options</param>
    /// <param name="file_cb">Callback for "file"; made once if there is a diff; can be NULL</param>
    /// <param name="binary_cb">Callback for binary files; can be NULL</param>
    /// <param name="hunk_cb">Callback for each hunk in diff; can be NULL</param>
    /// <param name="line_cb">Callback for each line in diff; can be NULL</param>
    /// <param name="payload">Payload passed to each callback function</param>
    /// <returns>0 on success, non-zero callback return value, or error code</returns>
    /// <remarks>
    /// 
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_blob_to_buffer(
        Git2.Blob* oldBlob,
        string? old_as_path,
        byte* buffer,
        nuint buffer_length,
        string? buffer_as_path,
        Native.GitDiffOptions* options,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, float, nint, int> file_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffBinary*, nint, int> binary_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, nint, int> hunk_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, Native.GitDiffLine*, nint, int> line_cb,
        nint payload);

    /// <summary>
    /// Directly run a diff on two blobs.
    /// </summary>
    /// <param name="old_blob"></param>
    /// <param name="old_as_path"></param>
    /// <param name="new_blob"></param>
    /// <param name="new_as_path"></param>
    /// <param name="options"></param>
    /// <param name="file_cb"></param>
    /// <param name="binary_cb"></param>
    /// <param name="hunk_cb"></param>
    /// <param name="line_cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_blob_to_buffer(
        Git2.Blob* old_blob,
        string? old_as_path,
        Git2.Blob* new_blob,
        string? new_as_path,
        Native.GitDiffOptions* options,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, float, nint, int> file_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffBinary*, nint, int> binary_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, nint, int> hunk_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, Native.GitDiffLine*, nint, int> line_cb,
        nint payload);

    /// <summary>
    /// Directly run a diff between two buffers.
    /// </summary>
    /// <param name="old_buffer">Raw data for old side of diff, or NULL for empty</param>
    /// <param name="old_length">Length of the raw data for old side of the diff</param>
    /// <param name="old_as_path">Treat old buffer as if it had this filename; can be NULL</param>
    /// <param name="new_buffer">Raw data for new side of diff, or NULL for empty</param>
    /// <param name="new_length">Length of raw data for new side of diff</param>
    /// <param name="new_as_path">Treat buffer as if it had this filename; can be NULL</param>
    /// <param name="options">Options for diff, or NULL for default options</param>
    /// <param name="file_cb">Callback for "file"; made once if there is a diff; can be NULL</param>
    /// <param name="binary_cb">Callback for binary files; can be NULL</param>
    /// <param name="hunk_cb">Callback for each hunk in diff; can be NULL</param>
    /// <param name="line_cb">Callback for each line in diff; can be NULL</param>
    /// <param name="payload">Payload passed to each callback function</param>
    /// <returns>0 on success, non-zero callback return value, or error code</returns>
    /// <remarks>
    /// 
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_buffers(
        void* old_buffer,
        nuint old_length,
        string? old_as_path,
        void* new_buffer,
        nuint new_length,
        string? new_as_path,
        Native.GitDiffOptions* options,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, float, nint, int> file_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffBinary*, nint, int> binary_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, nint, int> hunk_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, Native.GitDiffLine*, nint, int> line_cb,
        nint payload);

    public static GitError git_diff_buffers(
        ReadOnlySpan<byte> old_buffer,
        string? old_as_path,
        ReadOnlySpan<byte> new_buffer,
        string? new_as_path,
        Native.GitDiffOptions* options,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, float, nint, int> file_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffBinary*, nint, int> binary_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, nint, int> hunk_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, Native.GitDiffLine*, nint, int> line_cb,
        nint payload)
    {
        fixed (byte* pOld = old_buffer, pNew = new_buffer)
        {
            return git_diff_buffers(
                pOld, (nuint)old_buffer.Length, old_as_path,
                pNew, (nuint)new_buffer.Length, new_as_path,
                options, file_cb, binary_cb, hunk_cb, line_cb, payload);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_out"></param>
    /// <param name="repository"></param>
    /// <param name="commit"></param>
    /// <param name="patch_no"></param>
    /// <param name="total_patches"></param>
    /// <param name="flags"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_commit_as_email(
        Native.GitBuffer* _out,
        Git2.Repository* repository,
        Git2.Commit* commit,
        nuint patch_no,
        nuint total_patches,
        uint flags,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_find_similar(
        Git2.Diff* diff,
        Native.GitDiffFindOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff"></param>
    /// <param name="file_cb"></param>
    /// <param name="binary_cb"></param>
    /// <param name="hunk_cb"></param>
    /// <param name="line_cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_foreach(
        Git2.Diff* diff,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, float, nint, int> file_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffBinary*, nint, int> binary_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, nint, int> hunk_cb,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, Native.GitDiffLine*, nint, int> line_cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_out"></param>
    /// <param name="diff"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_format_email(
        Native.GitBuffer* _out,
        Git2.Diff* diff,
        Native.GitDiffFormatEmailOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_diff_free(Git2.Diff* diff);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff_out"></param>
    /// <param name="content"></param>
    /// <param name="content_length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_from_buffer(
        Git2.Diff** diff_out,
        byte* content,
        nuint content_length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitDiffDelta* git_diff_get_delta(Git2.Diff* diff, nuint idx);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stats_out"></param>
    /// <param name="diff"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_get_stats(
        Git2.DiffStats* stats_out,
        Git2.Diff* diff);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff_out"></param>
    /// <param name="repository"></param>
    /// <param name="old_index"></param>
    /// <param name="new_index"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_index_to_index(
        Git2.Diff** diff_out,
        Git2.Repository* repository,
        Git2.Index* old_index,
        Git2.Index* new_index,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff_out"></param>
    /// <param name="repository"></param>
    /// <param name="index"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_index_to_workdir(
        Git2.Diff** diff_out,
        Git2.Repository* repository,
        Git2.Index* index,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_diff_is_sorted_icase(Git2.Diff* diff);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="onto"></param>
    /// <param name="from"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_merge(Git2.Diff* onto, Git2.Diff* from);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_diff_num_deltas(Git2.Diff* diff);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_diff_num_deltas_of_type(Git2.Diff* diff, GitDeltaType type);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="diff"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_patchid(
        GitObjectID* id_out,
        Git2.Diff* diff,
        Native.GitDiffPatchIdOptions* options);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff"></param>
    /// <param name="format"></param>
    /// <param name="line_cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_print(
        Git2.Diff* diff,
        GitDiffFormatType format,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, Native.GitDiffLine*, nint, int> line_cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stats"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_diff_stats_deletions(Git2.DiffStats* stats);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stats"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_diff_stats_files_changed(Git2.DiffStats* stats);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stats"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_diff_stats_free(Git2.DiffStats* stats);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stats"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_diff_stats_insertions(Git2.DiffStats* stats);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="output"></param>
    /// <param name="stats"></param>
    /// <param name="format"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_stats_to_buf(
        Native.GitBuffer* output,
        Git2.DiffStats* stats,
        GitDiffStatsFormat format,
        nuint width);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte git_diff_status_char(GitDeltaType status);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="diff"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_to_buffer(
        Native.GitBuffer* buffer,
        Git2.Diff* diff,
        GitDiffFormatType format);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff_out"></param>
    /// <param name="repository"></param>
    /// <param name="old_tree"></param>
    /// <param name="index"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_tree_to_index(
        Git2.Diff** diff_out,
        Git2.Repository* repository,
        Git2.Tree* old_tree,
        Git2.Index* index,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff_out"></param>
    /// <param name="repository"></param>
    /// <param name="old_tree"></param>
    /// <param name="new_tree"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_tree_to_tree(
        Git2.Diff** diff_out,
        Git2.Repository* repository,
        Git2.Tree* old_tree,
        Git2.Tree* new_tree,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff_out"></param>
    /// <param name="repository"></param>
    /// <param name="old_tree"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_tree_to_workdir(
        Git2.Diff** diff_out,
        Git2.Repository* repository,
        Git2.Tree* old_tree,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="diff_out"></param>
    /// <param name="repository"></param>
    /// <param name="old_tree"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_diff_tree_to_workdir_with_index(
        Git2.Diff** diff_out,
        Git2.Repository* repository,
        Git2.Tree* old_tree,
        Native.GitDiffOptions* options);

    #endregion

    #region Error
    /// <summary>
    /// 
    /// </summary>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_error_clear();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool git_error_exists();

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
    public static partial Native.GitErrorDetails* git_error_last();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="errorClass"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_error_set_str(GitErrorClass errorClass, string message);
    #endregion

    #region Filter
    /// <summary>
    /// 
    /// </summary>
    /// <param name="result_out"></param>
    /// <param name="filters"></param>
    /// <param name="blob"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_filter_list_apply_to_blob(Native.GitBuffer* result_out, Git2.FilterList* filters, Git2.Blob* blob);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result_out"></param>
    /// <param name="filters"></param>
    /// <param name="buffer"></param>
    /// <param name="buffer_length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_filter_list_apply_to_blob(Native.GitBuffer* result_out, Git2.FilterList* filters, byte* buffer, nuint buffer_length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result_out"></param>
    /// <param name="filters"></param>
    /// <param name="repository"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_filter_list_apply_to_file(Native.GitBuffer* result_out, Git2.FilterList* filters, Git2.Repository* repository, string path);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_filter_list_contains(Git2.FilterList* filters, string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filters"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_filter_list_free(Git2.FilterList* filters);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="repository"></param>
    /// <param name="blob"></param>
    /// <param name="path"></param>
    /// <param name="mode"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_filter_list_load(
        Git2.FilterList** filters,
        Git2.Repository* repository,
        Git2.Blob* blob,
        string path,
        GitFilterMode mode,
        GitFilterFlags flags);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="repository"></param>
    /// <param name="blob"></param>
    /// <param name="path"></param>
    /// <param name="mode"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_filter_list_load_ext(
        Git2.FilterList** filters,
        Git2.Repository* repository,
        Git2.Blob* blob,
        string path,
        GitFilterMode mode,
        Native.GitFilterOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="blob"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_filter_list_stream_blob(
        Git2.FilterList* filters,
        Git2.Blob* blob,
        Native.GitWriteStream* target);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="buffer"></param>
    /// <param name="length"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_filter_list_stream_buffer(
        Git2.FilterList* filters,
        byte* buffer,
        nuint length,
        Native.GitWriteStream* target);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="repository"></param>
    /// <param name="path"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_filter_list_stream_file(
        Git2.FilterList* filters,
        Git2.Repository* repository,
        string path,
        Native.GitWriteStream* target);

    #endregion

    #region Graph
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ahead"></param>
    /// <param name="behind"></param>
    /// <param name="repository"></param>
    /// <param name="local"></param>
    /// <param name="upstream"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_graph_ahead_behind(
        nuint* ahead,
        nuint* behind,
        Git2.Repository* repository,
        GitObjectID* local,
        GitObjectID* upstream);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="commit"></param>
    /// <param name="ancestor"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_graph_descendant_of(
        Git2.Repository* repository,
        GitObjectID* commit,
        GitObjectID* ancestor);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="commit"></param>
    /// <param name="descendant_array"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_graph_reachable_from_any(
        Git2.Repository* repository,
        GitObjectID* commit,
        GitObjectID* descendant_array,
        nuint length);
    #endregion

    #region Ignore
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="rules"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_ignore_add_rule(Git2.Repository* repository, string rules);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="rules"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_ignore_clear_internal_rules(Git2.Repository* repository, string rules);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ignored"></param>
    /// <param name="repository"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_ignore_path_is_ignored(int* ignored, Git2.Repository* repository, string path);
    #endregion

    #region Index
    /// <summary>
    /// Add or update an index entry from an in-memory struct
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="sourceEntry">new entry object</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If a previous index entry exists that has the same path and stage as the given 'source_entry', it will be replaced. Otherwise, the 'source_entry' will be added.
    /// <br/><br/>
    /// A full copy (including the 'path' string) of the given 'source_entry' will be inserted on the index.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_add(Git2.Index* index, Native.GitIndexEntry* sourceEntry);

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_add(Git2.Index* index, GitIndexEntry sourceEntry);

    /// <summary>
    /// Add or update index entries matching files in the working directory.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="pathSpec">array of path patterns</param>
    /// <param name="options">combination of git_index_add_option_t flags</param>
    /// <param name="callback">
    /// notification callback for each added/updated path
    /// (also gets index of matching pathspec entry);
    /// can be NULL; return 0 to add, greater than 0 to skip, and less than 0 to abort scan.
    /// </param>
    /// <param name="payload">
    /// payload passed through to callback function
    /// </param>
    /// <returns>
    /// 0 on success, negative callback return value, or error code
    /// </returns>
    /// <remarks>
    /// This method will fail in bare index instances.
    /// <br/><br/>
    /// The <paramref name="pathSpec"/> is a list of file names or shell glob patterns
    /// that will be matched against files in the repository's working directory.
    /// Each file that matches will be added to the index (either updating an existing
    /// entry or adding a new entry). You can disable glob expansion and force exact
    /// matching with the <see cref="GitIndexAddOptions.DisablePathspecMatch"/> flag.
    /// <br/><br/>
    /// Files that are ignored will be skipped (unlike <see cref="git_index_add_bypath"/>).
    /// If a file is already tracked in the index, then it will be updated even if it is
    /// ignored. Pass the <see cref="GitIndexAddOptions.Force"/> flag to skip the checking
    /// of ignore rules.
    /// <br/><br/>
    /// To emulate <c>git add -A</c> and generate an error if the pathspec contains the exact
    /// path of an ignored file (when not using FORCE), add the <see cref="GitIndexAddOptions.CheckPathspec"/>
    /// flag. This checks that each entry in the pathspec that is an exact match to a filename
    /// on disk is either not ignored or already in the index. If this check fails, the function
    /// will return <see cref="GitError.InvalidSpec"/>.
    /// <br/><br/>
    /// To emulate <c>git add -A</c> with the "dry-run" option, just use a callback function that
    /// always returns a positive value. See below for details.
    /// <br/><br/>
    /// If any files are currently the result of a merge conflict, those files will no longer
    /// be marked as conflicting. The data about the conflicts will be moved to the
    /// "resolve undo" (REUC) section.
    /// <br/><br/>
    /// If you provide a callback function, it will be invoked on each matching item in the
    /// working directory immediately before it is added to / updated in the index. Returning
    /// zero will add the item to the index, greater than zero will skip the item, and less than
    /// zero will abort the scan and return that value to the caller.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_add_all(
        Git2.Index* index,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] pathSpec,
        GitIndexAddOptions options,
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback,
        nint payload);

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_add_all(
        Git2.Index* index,
        Native.GitStringArray* pathSpec,
        GitIndexAddOptions options,
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback,
        nint payload);

    /// <summary>
    /// Add or update an index entry from a file on disk
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">filename to add</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The <paramref name="path"/> must be relative to the repository's working folder
    /// and must be readable.
    /// <br/><br/>
    /// This method will fail in bare index instances.
    /// <br/><br/>
    /// This forces the file to be added to the index, not looking at gitignore rules.
    /// Those rules can be evaluated through the git_status APIs (in status.h) before
    /// calling this.
    /// <br/><br/>
    /// If this file currently is the result of a merge conflict, this file will no
    /// longer be marked as conflicting. The data about the conflict will be moved to
    /// the "resolve undo" (REUC) section.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_add_bypath(Git2.Index* index, string path);

    /// <summary>
    /// Add or update an index entry from a buffer in memory
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="entry">entry to add</param>
    /// <param name="buffer">data to be written into the blob</param>
    /// <param name="length">length of the data</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// This method will create a blob in the repository that owns
    /// the index and then add the index entry to the index. The <see cref="GitIndexEntry.Path"/>
    /// of the entry represents the position of the blob relative to
    /// the repository's root folder.
    /// <br/><br/>
    /// If a previous index entry exists that has the same path as
    /// the given <paramref name="entry"/>, it will be replaced.
    /// Otherwise, the <paramref name="entry"/> will be added.
    /// <br/><br/>
    /// This forces the file to be added to the index, not looking
    /// at gitignore rules. Those rules can be evaluated through the
    /// <c>git_status_*</c> APIs before calling this.
    /// <br/><br/>
    /// If this file currently is the result of a merge conflict,
    /// this file will no longer be marked as conflicting. The data
    /// about the conflict will be moved to the "resolve undo"
    /// (REUC) section.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_add_from_buffer(Git2.Index* index, GitIndexEntry entry, byte* buffer, nuint length);

    ///<inheritdoc cref="git_index_add_from_buffer"/>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_add_from_buffer(Git2.Index* index, Native.GitIndexEntry* entry, byte* buffer, nuint length);

    /// <summary>
    /// Read index capabilities flags.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <returns>
    /// A combination of <see cref="GitIndexCapabilities"/> values.
    /// </returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitIndexCapabilities git_index_caps(Git2.Index* index);

    /// <summary>
    /// Get the checksum of the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>a pointer to the checksum of the index</returns>
    /// <remarks>
    /// This checksum is the SHA-1 hash over the index file
    /// (except the last 20 bytes which are the checksum itself).
    /// In cases where the index does not exist on-disk,
    /// it will be zeroed out.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_index_checksum(Git2.Index* index);

    /// <summary>
    /// Clear the contents (all the entries) of an index object.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>0 on success, error code less than 0 on failure</returns>
    /// <remarks>
    /// This clears the index object in memory; changes must be explicitly
    /// written to disk for them to take effect persistently.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_clear(Git2.Index* index);

    /// <summary>
    /// Add or update index entries to represent a conflict.
    /// Any staged entries that exist at the given paths will be removed.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="ancestor_entry">the entry data for the ancestor of the conflict</param>
    /// <param name="our_entry">the entry data for our side of the merge conflict</param>
    /// <param name="their_entry">the entry data for their side of the merge conflict</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The entries are the entries from the tree included in the merge.
    /// Any entry may be null to indicate that that file was not present
    /// in the trees during the merge. For example, ancestor_entry may be
    /// <see langword="null"/> to indicate that a file was added in both
    /// branches and must be resolved.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_conflict_add(
        Git2.Index* index,
        GitIndexEntry ancestor_entry,
        GitIndexEntry our_entry,
        GitIndexEntry their_entry);

    ///<inheritdoc cref="git_index_conflict_add"/>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_conflict_add(
        Git2.Index* index,
        Native.GitIndexEntry* ancestor_entry,
        Native.GitIndexEntry* our_entry,
        Native.GitIndexEntry* their_entry);

    /// <summary>
    /// Remove all conflicts in the index (entries with a stage greater than 0).
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_conflict_cleanup(Git2.Index* index);

    /// <summary>
    /// Get the index entries that represent a conflict of a single file.
    /// </summary>
    /// <param name="ancestor_out">Pointer to store the ancestor entry</param>
    /// <param name="our_out">Pointer to store the our entry</param>
    /// <param name="their_out">Pointer to store the their entry</param>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to search</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The entries are not modifiable and should not be freed.
    /// Because the <see cref="Native.GitIndexEntry"/> struct
    /// is a publicly defined struct, you should be able to make
    /// your own permanent copy of the data if necessary.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_conflict_get(
        Native.GitIndexEntry** ancestor_out,
        Native.GitIndexEntry** our_out,
        Native.GitIndexEntry** their_out,
        Git2.Index* index,
        string path);

    /// <summary>
    /// Frees a <see cref="Git2.IndexConflictIterator"/> object
    /// </summary>
    /// <param name="iterator">pointer to the iterator</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_index_conflict_iterator_free(Git2.IndexConflictIterator* iterator);

    /// <summary>
    /// Create an iterator for the conflicts in the index.
    /// </summary>
    /// <param name="iterator_out">The newly created conflict iterator</param>
    /// <param name="index">The index to scan</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The index must not be modified while iterating; the results are undefined.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_conflict_iterator_new(Git2.IndexConflictIterator** iterator_out, Git2.Index* index);

    /// <summary>
    /// Returns the current conflict (ancestor, ours and theirs entry)
    /// and advance the iterator publicly to the next value.
    /// </summary>
    /// <param name="ancestor_out">Pointer to store the ancestor side of the conflict</param>
    /// <param name="our_out">Pointer to store our side of the conflict</param>
    /// <param name="their_out">Pointer to store their side of the conflict</param>
    /// <param name="iterator">The conflict iterator.</param>
    /// <returns>0 on success, <see cref="GitError.IterationOver"/> when iteration is complete, or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_conflict_next(
        Native.GitIndexEntry** ancestor_out,
        Native.GitIndexEntry** our_out,
        Native.GitIndexEntry** their_out,
        Git2.IndexConflictIterator* iterator);

    /// <summary>
    /// Removes the index entries that represent a conflict of a single file.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to remove conflicts for</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_conflict_remove(Git2.Index* index, string path);

    /// <summary>
    /// Get the count of entries currently in the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>integer count of current entries</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_index_entrycount(Git2.Index* index);

    /// <summary>
    /// Find the first position of any entries which point to given path in the Git index.
    /// </summary>
    /// <param name="position">the address to which the position of the index entry is written (optional)</param>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to search</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_find(nuint* position, Git2.Index* index, string path);

    /// <summary>
    /// Find the first position of any entries matching a prefix. To find the first position of a path inside a given folder, suffix the prefix with a '/'.
    /// </summary>
    /// <param name="position">the address to which the position of the index entry is written (optional)</param>
    /// <param name="index">an existing index object</param>
    /// <param name="path">the prefix to search for</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_find_prefix(nuint* position, Git2.Index* index, string path);

    /// <summary>
    /// Free an existing index object.
    /// </summary>
    /// <param name="index">an existing index object</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_index_free(Git2.Index* index);

    /// <summary>
    /// Get a pointer to one of the entries in the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="n">the position of the entry</param>
    /// <returns>a pointer to the entry; NULL if out of bounds</returns>
    /// <remarks>
    /// The entry is not modifiable and should not be freed.
    /// Because the <see cref="GitIndexEntry.Unmanaged"/> struct
    /// is a publicly defined struct, you should be able to make
    /// your own permanent copy of the data if necessary.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitIndexEntry* git_index_get_byindex(Git2.Index* index, nuint n);

    /// <summary>
    /// Get a pointer to one of the entries in the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to search</param>
    /// <param name="stage">stage to search</param>
    /// <returns>a pointer to the entry; <see langword="null"/> if it was not found</returns>
    /// <remarks>
    /// The entry is not modifiable and should not be freed.
    /// Because the <see cref="GitIndexEntry.Unmanaged"/> struct
    /// is a publicly defined struct, you should be able to make
    /// your own permanent copy of the data if necessary.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitIndexEntry* git_index_get_bypath(Git2.Index* index, string path, GitIndexStageFlags stage);

    /// <summary>
    /// Determine if the index contains entries representing file conflicts.
    /// </summary>
    /// <param name="index">An existing index object.</param>
    /// <returns><see langword="true"/> if at least one conflict is found, <see langword="false"/> otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_index_has_conflicts(Git2.Index* index);

    /// <summary>
    /// Free the index iterator
    /// </summary>
    /// <param name="iterator">The iterator to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_index_iterator_free(Git2.IndexIterator* iterator);

    /// <summary>
    /// Create an iterator that will return every entry contained
    /// in the index at the time of creation. Entries are returned
    /// in order, sorted by path. This iterator is backed by a snapshot
    /// that allows callers to modify the index while iterating without
    /// affecting the iterator.
    /// </summary>
    /// <param name="iterator_out">The newly created iterator</param>
    /// <param name="index">The index to iterate</param>
    /// <returns>0 or an error code.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_iterator_new(Git2.IndexIterator** iterator_out, Git2.Index* index);

    /// <summary>
    /// Return the next index entry in-order from the iterator.
    /// </summary>
    /// <param name="entry_out">Pointer to store the index entry in</param>
    /// <param name="iterator">The iterator</param>
    /// <returns>0 on success, <see cref="GitError.IterationOver"/> on iterator completion, or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_iterator_next(Native.GitIndexEntry** entry_out, Git2.IndexIterator* iterator);

    /// <summary>
    /// Get the repository this index relates to
    /// </summary>
    /// <param name="index">The index</param>
    /// <returns>A pointer to the repository</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.Repository* git_index_owner(Git2.Index* index);

    /// <summary>
    /// Get the full path to the index file on disk.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>path to index file or NULL for in-memory index</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_index_path(Git2.Index* index);

    /// <summary>
    /// Update the contents of an existing index object in memory by reading from the hard disk.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="force">if true, always reload, vs. only read if file has changed</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If <paramref name="force"/> is true, this performs a "hard" read that discards
    /// in-memory changes and always reloads the on-disk index data. If there is no
    /// on-disk version, the index will be cleared.
    /// <br/><br/>
    /// If <paramref name="force"/> is false, this does a "soft" read that reloads the
    /// index data from disk only if it has changed since the last time it was loaded.
    /// Purely in-memory index data will be untouched. Be aware: if there are changes
    /// on disk, unwritten in-memory changes are discarded.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_read(Git2.Index* index, int force);

    /// <summary>
    /// Read a tree into the index file with stats
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="force">tree to read</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The current index contents will be replaced by the specified tree.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_read_tree(Git2.Index* index, Git2.Tree* tree);

    /// <summary>
    /// Remove an entry from the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to search</param>
    /// <param name="stage">stage to search</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_remove(Git2.Index* index, string path, GitIndexStageFlags stage);

    /// <summary>
    /// Remove all matching index entries.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <param name="pathSpec">array of path patterns</param>
    /// <param name="callback">
    /// notification callback for each removed path (also gets index of matching pathspec entry); can be NULL; return 0 to add, greater than 0 to skip, less than 0 to abort scan.
    /// </param>
    /// <param name="payload">payload passed through to callback function</param>
    /// <returns>0 on success, negative callback return value, or error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_remove_all(
        Git2.Index* index,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] pathSpec,
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback,
        nint payload);

    /// <summary>
    /// Remove an index entry corresponding to a file on disk
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">filename to remove</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The <paramref name="path"/> must be relative to the repository's
    /// working folder. It may exist.
    /// <br/><br/>
    /// If this file currently is the result of a merge conflict, this
    /// file will no longer be marked as conflicting. The data about the
    /// conflict will be moved to the "resolve undo" (REUC) section.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_remove_bypath(Git2.Index* index, string path);

    /// <summary>
    /// Remove all entries from the index under a given directory
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="directory">container directory path</param>
    /// <param name="stage">stage to search</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_remove_directory(Git2.Index* index, string directory, GitIndexStageFlags stage);

    /// <summary>
    /// Set index capabilities flags.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <param name="caps">A combination of GIT_INDEX_CAPABILITY values</param>
    /// <returns>0 on success, -1 on failure</returns>
    /// <remarks>
    /// If you pass <see cref="GitIndexCapabilities.FromOwner"/> for <paramref name="caps"/>,
    /// then capabilities will be read from the config of the owner object, looking at
    /// <c>core.ignorecase</c>, <c>core.filemode</c>, and <c>core.symlinks</c>
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_set_caps(Git2.Index* index, GitIndexCapabilities caps);

    /// <summary>
    /// Set index on-disk version.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <param name="version">The new version number</param>
    /// <returns>0 on success, -1 on failure</returns>
    /// <remarks>
    /// Valid values are 2, 3, or 4. If 2 is given, git_index_write may write an index with version 3 instead, if necessary to accurately represent the index.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_set_version(Git2.Index* index, uint version);

    /// <summary>
    /// Update all index entries to match the working directory
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <param name="pathSpec">array of path patterns</param>
    /// <param name="callback">
    /// notification callback for each updated path (also gets index of matching pathspec entry);
    /// can be NULL; return 0 to add, greater than 0 to skip, less than 0 to abort scan.
    /// </param>
    /// <param name="payload">payload passed through to callback function</param>
    /// <returns>0 on success, negative callback return value, or error code</returns>
    /// <remarks>
    /// This method will fail in bare index instances.
    /// <br/><br/>
    /// This scans the existing index entries and synchronizes them with the working directory,
    /// deleting them if the corresponding working directory file no longer exists otherwise
    /// updating the information (including adding the latest version of file to the ODB if needed).
    /// <br/><br/>
    /// If you provide a callback function, it will be invoked on each matching item in the index
    /// immediately before it is updated (either refreshed or removed depending on working directory
    /// state). Return 0 to proceed with updating the item, greater than 0 to skip the item,
    /// and less than 0 to abort the scan.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_update_all(
        Git2.Index* index,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] pathSpec,
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback,
        nint payload);

    /// <summary>
    /// Get index on-disk version.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <returns>the index version</returns>
    /// <remarks>
    /// Valid return values are 2, 3, or 4. If 3 is returned,
    /// an index with version 2 may be written instead,
    /// if the extension data in version 3 is not necessary.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial uint git_index_version(Git2.Index* index);

    /// <summary>
    /// Write an existing index object from memory back to disk using an atomic file lock.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_write(Git2.Index* index);

    /// <summary>
    /// Write the index as a tree
    /// </summary>
    /// <param name="id_out">Pointer where to store the OID of the written tree</param>
    /// <param name="index">Index to write</param>
    /// <returns>0 on success, <see cref="GitError.Unmerged"/> when the index is not clean, or an error code</returns>
    /// <remarks>
    /// This method will scan the index and write a representation of its current state back to disk;
    /// it recursively creates tree objects for each of the subtrees stored in the index, but only returns
    /// the OID of the root tree. This is the OID that can be used e.g. to create a commit.
    /// <br/><br/>
    /// The index instance cannot be bare, and needs to be associated to an existing repository.
    /// <br/><br/>
    /// The index must not contain any file in conflict.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_write_tree(GitObjectID* id_out, Git2.Index* index);

    /// <summary>
    /// Write the index as a tree to the given repository
    /// </summary>
    /// <param name="id_out">Pointer where to store the OID of the written tree</param>
    /// <param name="index">Index to write</param>
    /// <param name="repo">Repository where to write the tree</param>
    /// <returns>0 on success, <see cref="GitError.Unmerged"/> when the index is not clean, or an error code</returns>
    /// <remarks>
    /// This method will do the same as <see cref="git_index_write_tree(GitObjectID*, Git2.Index*)"/>,
    /// but letting the user choose the repository where the tree will be written.
    /// <br/><br/>
    /// The index must not contain any file in conflict.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_index_write_tree_to(GitObjectID* id_out, Git2.Index* index, Git2.Repository* repo);
    #endregion

    #region Indexer
    /// <summary>
    /// Add data to the indexer
    /// </summary>
    /// <param name="indexer">the indexer</param>
    /// <param name="data">the data to add</param>
    /// <param name="size">the size of the data in bytes</param>
    /// <param name="stats">stat storage</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_indexer_append(Git2.Indexer* indexer, void* data, nuint size, GitIndexerProgress* stats);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexer"></param>
    /// <param name="stats"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_indexer_commit(Git2.Indexer* indexer, GitIndexerProgress* stats);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexer"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_indexer_free(Git2.Indexer* indexer);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexer"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_indexer_hash(Git2.Indexer* indexer);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexer"></param>
    /// <param name="stats"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_indexer_name(Git2.Indexer* indexer);

#if !GIT_EXPERIMENTAL_SHA256
    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexer_out"></param>
    /// <param name="path"></param>
    /// <param name="mode"></param>
    /// <param name="odb"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_indexer_new(Git2.Indexer** indexer_out, string path, uint mode, Git2.ObjectDatabase* odb, Native.GitIndexerOptions* options);
#else
    /// <summary>
    /// Create a new indexer instance
    /// </summary>
    /// <param name="indexer_out">where to store the indexer instance</param>
    /// <param name="path">to the directory where the packfile should be stored</param>
    /// <param name="oid_type">the oid type to use for objects</param>
    /// <param name="options"></param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_indexer_new(Git2.Indexer** indexer_out, string path, GitObjectIDType oid_type, Native.GitIndexerOptions* options);
#endif
    #endregion

    #region Mailmap
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mailmap"></param>
    /// <param name="real_name"></param>
    /// <param name="real_email"></param>
    /// <param name="replace_name"></param>
    /// <param name="replace_email"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_mailmap_add_entry(Git2.MailMap* mailmap, string real_name, string real_email, string replace_name, string replace_email);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mailmap"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_mailmap_free(Git2.MailMap* mailmap);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mailmap_out"></param>
    /// <param name="buffer"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_mailmap_from_buffer(Git2.MailMap** mailmap_out, byte* buffer, nuint length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mailmap_out"></param>
    /// <param name="buffer"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_mailmap_from_repository(Git2.MailMap** mailmap_out, Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mailmap_out"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_mailmap_new(Git2.MailMap** mailmap_out);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="real_name"></param>
    /// <param name="real_email"></param>
    /// <param name="mailmap"></param>
    /// <param name="name"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_mailmap_resolve(byte** real_name, byte** real_email, Git2.MailMap* mailmap, byte* name, byte* email);

    ///<inheritdoc cref="git_mailmap_resolve(byte**, byte**, Git2.MailMap*, byte*, byte*)"/>
    [SkipLocalsInit]
    public static GitError git_mailmap_resolve(out string? real_name, out string? real_email, Git2.MailMap* mailmap, string name, string email)
    {
        GitError error;
        byte* _real_name = null, _real_email = null;
        scoped Utf8StringMarshaller.ManagedToUnmanagedIn _name = default, _email = default;
        try
        {
            _name.FromManaged(name, stackalloc byte[64]);
            _email.FromManaged(email, stackalloc byte[64]);

            error = git_mailmap_resolve(&_real_name, &_real_email, mailmap, _name.ToUnmanaged(), _email.ToUnmanaged());
        }
        finally
        {
            _name.Free();
            _email.Free();
        }

        if (error == GitError.OK)
        {
            real_name = Utf8StringMarshaller.ConvertToManaged(_real_name);
            real_email = Utf8StringMarshaller.ConvertToManaged(_real_email);
        }
        else
        {
            real_name = null;
            real_email = null;
        }

        return error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="signature_out"></param>
    /// <param name="mailmap"></param>
    /// <param name="signature"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_mailmap_resolve_signature(Native.GitSignature* signature_out, Git2.MailMap* mailmap, Native.GitSignature* signature);
    #endregion

    #region Merge
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="their_heads"></param>
    /// <param name="their_heads_count"></param>
    /// <param name="merge_options"></param>
    /// <param name="checkout_options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge(
        Git2.Repository* repository,
        Git2.AnnotatedCommit** their_heads,
        nuint their_heads_count,
        Native.GitMergeOptions* merge_options,
        Native.GitCheckoutOptions* checkout_options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="analysis_out"></param>
    /// <param name="preference_out"></param>
    /// <param name="repository"></param>
    /// <param name="their_heads"></param>
    /// <param name="their_heads_count"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_analysis(
        GitMergeAnalysisResult* analysis_out,
        GitMergePreference* preference_out,
        Git2.Repository* repository,
        Git2.AnnotatedCommit** their_heads,
        nuint their_heads_count);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="analysis_out"></param>
    /// <param name="preference_out"></param>
    /// <param name="repository"></param>
    /// <param name="our_ref"></param>
    /// <param name="their_heads"></param>
    /// <param name="their_heads_count"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_analysis_for_ref(
        GitMergeAnalysisResult* analysis_out,
        GitMergePreference* preference_out,
        Git2.Repository* repository,
        Git2.Reference* our_ref,
        Git2.AnnotatedCommit** their_heads,
        nuint their_heads_count);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="one"></param>
    /// <param name="two"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_base(
        GitObjectID* id_out,
        Git2.Repository* repository,
        GitObjectID* one,
        GitObjectID* two);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="length"></param>
    /// <param name="input_array"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_base_many(
        GitObjectID* id_out,
        Git2.Repository* repository,
        nuint length,
        GitObjectID* input_array);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="length"></param>
    /// <param name="input_array"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_base_octopus(
        GitObjectID* id_out,
        Git2.Repository* repository,
        nuint length,
        GitObjectID* input_array);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids_out"></param>
    /// <param name="repository"></param>
    /// <param name="one"></param>
    /// <param name="two"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_bases(
        Native.GitObjectIDArray* ids_out,
        Git2.Repository* repository,
        GitObjectID* one,
        GitObjectID* two);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="length"></param>
    /// <param name="input_array"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_bases_many(
        Native.GitObjectIDArray* id_out,
        Git2.Repository* repository,
        nuint length,
        GitObjectID* input_array);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index_out"></param>
    /// <param name="repository"></param>
    /// <param name="our_commit"></param>
    /// <param name="their_commit"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_commits(
        Git2.Index** index_out,
        Git2.Repository* repository,
        Git2.Commit* our_commit,
        Git2.Commit* their_commit,
        Native.GitMergeOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="ancestor"></param>
    /// <param name="ours"></param>
    /// <param name="theirs"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_file(
        Native.GitMergeFileResult* result,
        Native.GitMergeFileInput* ancestor,
        Native.GitMergeFileInput* ours,
        Native.GitMergeFileInput* theirs,
        Native.GitMergeFileOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="ancestor"></param>
    /// <param name="ours"></param>
    /// <param name="theirs"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_file_from_index(
        Native.GitMergeFileResult* result,
        Native.GitIndexEntry* ancestor,
        Native.GitIndexEntry* ours,
        Native.GitIndexEntry* theirs,
        Native.GitMergeFileOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_merge_file_result_free(Native.GitMergeFileResult* result);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index_out"></param>
    /// <param name="repository"></param>
    /// <param name="ancestor_tree"></param>
    /// <param name="our_tree"></param>
    /// <param name="their_tree"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_merge_trees(
        Git2.Index** index_out,
        Git2.Repository* repository,
        Git2.Tree* ancestor_tree,
        Git2.Tree* our_tree,
        Git2.Tree* their_tree,
        Native.GitMergeOptions* options);
    #endregion

    #region Message
    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="message"></param>
    /// <param name="strip_comments"></param>
    /// <param name="comment_char"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_message_prettify(Native.GitBuffer* result, string message, int strip_comments, byte comment_char);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_message_trailer_array_free(Native.GitMessageTrailerArray* array);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_message_trailers(Native.GitMessageTrailerArray* result, string message);

    #endregion

    #region Note
    /// <summary>
    /// 
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitSignature* git_note_author(Git2.Note* note);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="notes_commit_out"></param>
    /// <param name="notes_blob_out"></param>
    /// <param name="repository"></param>
    /// <param name="parent"></param>
    /// <param name="author"></param>
    /// <param name="committer"></param>
    /// <param name="oid"></param>
    /// <param name="note"></param>
    /// <param name="allow_note_overwrite"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_commit_create(
        GitObjectID* notes_commit_out,
        GitObjectID* notes_blob_out,
        Git2.Repository* repository,
        Git2.Commit* parent,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        GitObjectID* oid,
        string note,
        int allow_note_overwrite);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iterator_out"></param>
    /// <param name="notes_commit"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_commit_iterator_new(
        Git2.NoteIterator** iterator_out,
        Git2.Commit* notes_commit);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="note_out"></param>
    /// <param name="repository"></param>
    /// <param name="notes_commit"></param>
    /// <param name="oid"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_commit_read(
        Git2.Note** note_out,
        Git2.Repository* repository,
        Git2.Commit* notes_commit,
        GitObjectID* oid);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="notes_commit_out"></param>
    /// <param name="repository"></param>
    /// <param name="notes_commit"></param>
    /// <param name="author"></param>
    /// <param name="committer"></param>
    /// <param name="oid"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_commit_remove(
        GitObjectID* notes_commit_out,
        Git2.Repository* repository,
        Git2.Commit* notes_commit,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        GitObjectID* oid);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitSignature* git_note_committer(Git2.Note* note);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="notes_commit_out"></param>
    /// <param name="repository"></param>
    /// <param name="notes_ref"></param>
    /// <param name="author"></param>
    /// <param name="committer"></param>
    /// <param name="oid"></param>
    /// <param name="note"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_create(
        GitObjectID* notes_commit_out,
        Git2.Repository* repository,
        string notes_ref,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        GitObjectID* oid,
        string note,
        int force);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_default_ref(Native.GitBuffer* result, Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="notes_ref"></param>
    /// <param name="note_cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_foreach(
        Git2.Repository* repository,
        string? notes_ref,
        delegate* unmanaged[Cdecl]<GitObjectID*, GitObjectID*, nint, int> note_cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="note"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_note_free(Git2.Note* note);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_note_id(Git2.Note* note);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iterator"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_note_iterator_free(Git2.NoteIterator* iterator);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iterator"></param>
    /// <param name="repository"></param>
    /// <param name="notes_ref"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_iterator_new(Git2.NoteIterator** iterator, Git2.Repository* repository, string notes_ref);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_note_message(Git2.Note* note);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="node_id"></param>
    /// <param name="annotated_id"></param>
    /// <param name="iterator"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_next(GitObjectID* node_id, GitObjectID* annotated_id, Git2.NoteIterator* iterator);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="note_out"></param>
    /// <param name="repository"></param>
    /// <param name="notes_ref"></param>
    /// <param name="oid"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_read(
        Git2.Note** note_out,
        Git2.Repository* repository,
        string? notes_ref,
        GitObjectID* oid);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="notes_ref"></param>
    /// <param name="author"></param>
    /// <param name="committer"></param>
    /// <param name="oid"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_note_remove(
        Git2.Repository* repository,
        string? notes_ref,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        GitObjectID* oid);

    #endregion

    #region Object
    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj_out"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_object_dup(Git2.Object** obj_out, Git2.Object* obj);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instance"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_object_free(Git2.Object* instance);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_object_id(Git2.Object* instance);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj_out"></param>
    /// <param name="repository"></param>
    /// <param name="id"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_object_lookup(Git2.Object** obj_out, Git2.Repository* repository, GitObjectID* id, GitObjectType type);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj_out"></param>
    /// <param name="treeish"></param>
    /// <param name="id"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_object_lookup_bypath(Git2.Object** obj_out, Git2.Object* treeish, GitObjectID* id, GitObjectType type);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj_out"></param>
    /// <param name="repository"></param>
    /// <param name="id"></param>
    /// <param name="length"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_object_lookup_prefix(Git2.Object** obj_out, Git2.Repository* repository, GitObjectID* id, nuint length, GitObjectType type);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial Git2.Repository* git_object_owner(Git2.Object* obj);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj_out"></param>
    /// <param name="obj"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_object_peel(Git2.Object** obj_out, Git2.Object* obj, GitObjectType type);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valid"></param>
    /// <param name="buffer"></param>
    /// <param name="length"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_object_rawcontent_is_valid(int* valid, byte* buffer, nuint length, GitObjectType type);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_object_short_id(Native.GitBuffer* id_out, Git2.Object* obj);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectType git_object_type(Git2.Object* obj);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_object_typeisloose(GitObjectType type);

    #endregion

    #region Object Database
    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <param name="backend"></param>
    /// <param name="priority"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_add_alternate(Git2.ObjectDatabase* odb, Git2.ObjectDatabaseBackend* backend, int priority);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <param name="backend"></param>
    /// <param name="priority"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_add_backend(Git2.ObjectDatabase* odb, Git2.ObjectDatabaseBackend* backend, int priority);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_add_disk_alternate(Git2.ObjectDatabase* odb, string path);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_exists(Git2.ObjectDatabase* odb, GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <param name="id"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_exists_ext(Git2.ObjectDatabase* odb, GitObjectID* id, GitObjectDatabaseLookupFlags flags);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="odb"></param>
    /// <param name="short_id"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_exists_prefix(GitObjectID* id_out, Git2.ObjectDatabase* odb, GitObjectID* short_id, nuint length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <param name="ids"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_expand_ids(Git2.ObjectDatabase* odb, GitObjectDatabaseExpandID* ids, nuint count);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <param name="cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_foreach(
        Git2.ObjectDatabase* odb,
        delegate* unmanaged[Cdecl]<GitObjectID*, nint, int> cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_odb_free(Git2.ObjectDatabase* odb);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="backend_out"></param>
    /// <param name="odb"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_get_backend(Git2.ObjectDatabaseBackend** backend_out, Git2.ObjectDatabase* odb, nuint position);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_odb_num_backends(Git2.ObjectDatabase* odb);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void* git_odb_object_data(Git2.ObjectDatabaseObject* @object);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object_out"></param>
    /// <param name="object"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_object_dup(Git2.ObjectDatabaseObject** object_out, Git2.ObjectDatabaseObject* @object);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_odb_object_free(Git2.ObjectDatabaseObject* @object);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_odb_object_id(Git2.ObjectDatabaseObject* @object);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_odb_object_size(Git2.ObjectDatabaseObject* @object);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectType git_odb_object_type(Git2.ObjectDatabaseObject* @object);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream_out"></param>
    /// <param name="length"></param>
    /// <param name="type"></param>
    /// <param name="odb"></param>
    /// <param name="oid"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_object_rstream(
        Native.GitObjectDatabaseStream** stream_out,
        nuint* length,
        GitObjectType* type,
        Git2.ObjectDatabase* odb,
        GitObjectID* oid);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream_out"></param>
    /// <param name="odb"></param>
    /// <param name="length"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_object_wstream(
        Native.GitObjectDatabaseStream** stream_out,
        Git2.ObjectDatabase* odb,
        ulong length,
        GitObjectType type);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object_out"></param>
    /// <param name="odb"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_read(Git2.ObjectDatabaseObject** object_out, Git2.ObjectDatabase* odb, GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="length_out"></param>
    /// <param name="type_out"></param>
    /// <param name="odb"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_read_header(nuint* length_out, GitObjectType* type_out, Git2.ObjectDatabase* odb, GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object_out"></param>
    /// <param name="odb"></param>
    /// <param name="short_id"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_read_prefix(Git2.ObjectDatabaseObject** object_out, Git2.ObjectDatabase* odb, GitObjectID* short_id, nuint length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_refresh(Git2.ObjectDatabase* odb);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <param name="commit_graph"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_set_commit_graph(Git2.ObjectDatabase* odb, Git2.CommitGraph* commit_graph);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="stream"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_stream_finalize_write(GitObjectID* id_out, Native.GitObjectDatabaseStream* stream);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_odb_stream_free(Native.GitObjectDatabaseStream* stream);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="buffer"></param>
    /// <param name="buffer_length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_stream_read(Native.GitObjectDatabaseStream* stream, byte* buffer, nuint buffer_length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="buffer"></param>
    /// <param name="buffer_length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_stream_write(Native.GitObjectDatabaseStream* stream, byte* buffer, nuint buffer_length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="odb"></param>
    /// <param name="buffer"></param>
    /// <param name="buffer_length"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_write(
        GitObjectID* id_out,
        Git2.ObjectDatabase* odb,
        byte* buffer,
        nuint buffer_length,
        GitObjectType type);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odb"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_write_multi_pack_index(Git2.ObjectDatabase* odb);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writepack_out"></param>
    /// <param name="odb"></param>
    /// <param name="progress_cb"></param>
    /// <param name="progress_payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_odb_write_pack(
        Native.GitObjectDatabaseWritePack** writepack_out,
        Git2.ObjectDatabase* odb,
        delegate* unmanaged[Cdecl]<GitIndexerProgress*, nint, int> progress_cb,
        nint progress_payload);
    #endregion

    #region Pack Builder
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_foreach(
        Git2.PackBuilder* builder,
        delegate* unmanaged[Cdecl]<void*, nuint, nint, int> cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_packbuilder_free(Git2.PackBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_packbuilder_hash(Git2.PackBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_insert(
        Git2.PackBuilder* builder,
        GitObjectID* id,
        string? name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_insert_commit(
        Git2.PackBuilder* builder,
        GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_insert_recur(
        Git2.PackBuilder* builder,
        GitObjectID* id,
        string? name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_insert_tree(
        Git2.PackBuilder* builder,
        GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="walk"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_insert_walk(
        Git2.PackBuilder* builder,
        Git2.RevWalk* walk);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_packbuilder_name(Git2.PackBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder_out"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_new(Git2.PackBuilder** builder_out, Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_packbuilder_object_count(Git2.PackBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="progress_cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_set_callbacks(
        Git2.PackBuilder* builder,
        delegate* unmanaged[Cdecl]<int, uint, uint, nint, int> progress_cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="thread_count"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_set_threads(Git2.PackBuilder* builder, uint thread_count);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="path"></param>
    /// <param name="mode"></param>
    /// <param name="progress_cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_write(
        Git2.PackBuilder* builder,
        string path,
        uint mode,
        delegate* unmanaged[Cdecl]<GitIndexerProgress*, nint, int> progress_cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_packbuilder_write_buf(
        Native.GitBuffer* buffer,
        Git2.PackBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_packbuilder_written(Git2.PackBuilder* builder);
    #endregion

    #region Patch
    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_patch_free(Git2.Patch* patch);

    /// <summary>
    /// Directly generate a patch from the difference between a blob and a buffer.
    /// </summary>
    /// <param name="patch_out">The generated patch; NULL on error</param>
    /// <param name="old_blob">Blob for old side of diff, or NULL for empty blob</param>
    /// <param name="old_as_path">Treat old blob as if it had this filename; can be NULL</param>
    /// <param name="buffer">Raw data for new side of diff, or NULL for empty</param>
    /// <param name="buffer_length">Length of raw data for new side of diff</param>
    /// <param name="buffer_as_path">Treat buffer as if it had this filename; can be NULL</param>
    /// <param name="options">Options for diff, or NULL for default options</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// This is just like <see cref="git_diff_blob_to_buffer"/> except it generates
    /// a patch object for the difference instead of directly making callbacks.
    /// You can use the standard Git Patch accessor functions to read the patch data,
    /// and you must call <see cref="git_patch_free(Git2.Patch*)"/> on the patch when
    /// you are done.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_from_blob_and_buffer(
        Git2.Patch** patch_out,
        Git2.Blob* old_blob,
        string? old_as_path,
        void* buffer,
        nuint buffer_length,
        string? buffer_as_path,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch_out"></param>
    /// <param name="old_blob"></param>
    /// <param name="old_as_path"></param>
    /// <param name="new_blob"></param>
    /// <param name="new_as_path"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_from_blobs(
        Git2.Patch** patch_out,
        Git2.Blob* old_blob,
        string? old_as_path,
        Git2.Blob* new_blob,
        string? new_as_path,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch_out"></param>
    /// <param name="old_buffer"></param>
    /// <param name="old_length"></param>
    /// <param name="old_as_path"></param>
    /// <param name="new_buffer"></param>
    /// <param name="new_length"></param>
    /// <param name="new_as_path"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_from_buffers(
        Git2.Patch** patch_out,
        void* old_buffer,
        nuint old_length,
        string? old_as_path,
        void* new_buffer,
        nuint new_length,
        string? new_as_path,
        Native.GitDiffOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch_out"></param>
    /// <param name="diff"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_from_diff(
        Git2.Patch** patch_out,
        Git2.Diff* diff,
        nuint idx);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitDiffDelta* git_patch_get_delta(Git2.Patch* patch);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hunk_out"></param>
    /// <param name="lines_in_hunk"></param>
    /// <param name="patch"></param>
    /// <param name="hunk_idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_get_hunk(
        Native.GitDiffHunk** hunk_out,
        nuint* lines_in_hunk,
        Git2.Patch* patch,
        nuint hunk_idx);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line_out"></param>
    /// <param name="patch"></param>
    /// <param name="hunk_idx"></param>
    /// <param name="line_of_hunk"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_get_line_in_hunk(
        Native.GitDiffLine** line_out,
        Git2.Patch* patch,
        nuint hunk_idx,
        nuint line_of_hunk);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="total_context"></param>
    /// <param name="total_additions"></param>
    /// <param name="total_deletions"></param>
    /// <param name="patch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_line_stats(
        nuint* total_context,
        nuint* total_additions,
        nuint* total_deletions,
        Git2.Patch* patch);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_patch_num_hunks(Git2.Patch* patch);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch"></param>
    /// <param name="hunk_idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_patch_num_lines_in_hunk(Git2.Patch* patch, nuint hunk_idx);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.Repository* git_patch_owner(Git2.Patch* patch);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch"></param>
    /// <param name="print_cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_print(
        Git2.Patch* patch,
        delegate* unmanaged[Cdecl]<Native.GitDiffDelta*, Native.GitDiffHunk*, Native.GitDiffLine*, nint, int> print_cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="patch"></param>
    /// <param name="include_context"></param>
    /// <param name="include_hunk_headers"></param>
    /// <param name="include_file_headers"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_patch_size(Git2.Patch* patch, int include_context, int include_hunk_headers, int include_file_headers);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer_out"></param>
    /// <param name="patch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_patch_to_buf(Native.GitBuffer* buffer_out, Git2.Patch* patch);
    #endregion

    #region PathSpec
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathspec"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_pathspec_free(Git2.PathSpec* pathspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list_out"></param>
    /// <param name="diff"></param>
    /// <param name="flags"></param>
    /// <param name="pathspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_pathspec_match_diff(
        Git2.PathSpecMatchList** list_out,
        Git2.Diff* diff,
        GitPathSpecFlags flags,
        Git2.PathSpec* pathspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list_out"></param>
    /// <param name="index"></param>
    /// <param name="flags"></param>
    /// <param name="pathspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_pathspec_match_index(
        Git2.PathSpecMatchList** list_out,
        Git2.Index* index,
        GitPathSpecFlags flags,
        Git2.PathSpec* pathspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="match_list"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitDiffDelta* git_pathspec_match_list_diff_entry(Git2.PathSpecMatchList* match_list, nuint pos);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="match_list"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_pathspec_match_list_entry(Git2.PathSpecMatchList* match_list, nuint pos);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="match_list"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_pathspec_match_list_entrycount(Git2.PathSpecMatchList* match_list);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="match_list"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_pathspec_match_list_failed_entry(Git2.PathSpecMatchList* match_list, nuint pos);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="match_list"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_pathspec_match_list_failed_entrycount(Git2.PathSpecMatchList* match_list);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="match_list"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_pathspec_match_list_free(Git2.PathSpecMatchList* match_list);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list_out"></param>
    /// <param name="tree"></param>
    /// <param name="flags"></param>
    /// <param name="pathspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_pathspec_match_tree(
        Git2.PathSpecMatchList** list_out,
        Git2.Tree* tree,
        GitPathSpecFlags flags,
        Git2.PathSpec* pathspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list_out"></param>
    /// <param name="repository"></param>
    /// <param name="flags"></param>
    /// <param name="pathspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_pathspec_match_workdir(
        Git2.PathSpecMatchList** list_out,
        Git2.Repository* repository,
        GitPathSpecFlags flags,
        Git2.PathSpec* pathspec);

    /// <summary>
    /// Try to match a path against a pathspec
    /// </summary>
    /// <param name="pathspec">The compiled pathspec</param>
    /// <param name="flags">Combination of <see cref="GitPathSpecFlags"/> options to control match</param>
    /// <param name="path">The pathname to attempt to match</param>
    /// <returns>1 is path matches spec, 0 if it does not</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_pathspec_matchs_path(
        Git2.PathSpec* pathspec,
        GitPathSpecFlags flags,
        string path);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathspec_out"></param>
    /// <param name="pathspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_pathspec_new(
        Git2.PathSpec** pathspec_out,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] pathspec);


    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_pathspec_new(
        Git2.PathSpec** pathspec_out,
        Native.GitStringArray* pathspec);

    #endregion

    #region Rebase
    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_rebase_abort(Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="rebase"></param>
    /// <param name="author"></param>
    /// <param name="committer"></param>
    /// <param name="message_encoding"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_rebase_commit(
        GitObjectID* id,
        Git2.Rebase* rebase,
        Native.GitSignature* author,
        Native.GitSignature* committer,
        byte* message_encoding,
        byte* message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <param name="signature"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_rebase_finish(
        Git2.Rebase* rebase,
        Native.GitSignature* signature);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_rebase_free(Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase_out"></param>
    /// <param name="repository"></param>
    /// <param name="branch"></param>
    /// <param name="upstream"></param>
    /// <param name="onto"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_rebase_init(
        Git2.Rebase** rebase_out,
        Git2.Repository* repository,
        Git2.AnnotatedCommit* branch,
        Git2.AnnotatedCommit* upstream,
        Git2.AnnotatedCommit* onto,
        Native.GitRebaseOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index_out"></param>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_rebase_inmemory_index(
        Git2.Index** index_out,
        Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_rebase_next(
        Native.GitRebaseOperation** operation,
        Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_rebase_onto_id(Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_rebase_onto_name(Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase_out"></param>
    /// <param name="repository"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_rebase_open(
        Git2.Rebase** rebase_out,
        Git2.Repository* repository,
        Native.GitRebaseOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitRebaseOperation* git_rebase_operation_byindex(Git2.Rebase* rebase, nuint idx);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_rebase_operation_current(Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_rebase_operation_entrycount(Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_rebase_orig_head_id(Git2.Rebase* rebase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rebase"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_rebase_orig_head_name(Git2.Rebase* rebase);
    #endregion

    #region RefDB
    /// <summary>
    /// 
    /// </summary>
    /// <param name="refdb"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_refdb_compress(Git2.RefDB* refdb);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refdb"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_refdb_free(Git2.RefDB* refdb);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refdb"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_refdb_new(Git2.RefDB** refdb, Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refdb"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_refdb_open(Git2.RefDB** refdb, Git2.Repository* repository);
    #endregion

    #region Reference
    /// <summary>
    /// Compare two references
    /// </summary>
    /// <param name="reference1">The first reference</param>
    /// <param name="reference2">The second reference</param>
    /// <returns>0 if the same, else a stable but meaningless ordering.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_reference_cmp(Git2.Reference* reference1, Git2.Reference* reference2);

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
    public static partial GitError git_reference_create(Git2.Reference** reference, Git2.Repository* repository, string name, GitObjectID* id, int force, string? logMessage);


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
    public static partial GitError git_reference_create_matching(Git2.Reference** reference, Git2.Repository* repository, string name, GitObjectID* id, int force, GitObjectID* currentId, string? logMessage);

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
    public static partial GitError git_reference_delete(Git2.Reference* reference);

    /// <summary>
    /// Create a copy of an existing reference.
    /// </summary>
    /// <param name="duplicant">pointer where to store the copy</param>
    /// <param name="reference">object to copy</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>Call <see cref="git_reference_free(Git2.Reference*)"/> to free the data.</remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reference_dup(Git2.Reference** duplicant, Git2.Reference* reference);

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
    public static partial GitError git_reference_dwim(Git2.Reference** reference, Git2.Repository* repository, string shorthand);

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
    public static partial GitError git_reference_ensure_log(Git2.Repository* repository, string referenceName);

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
    public static partial GitError git_reference_foreach(Git2.Repository* repository, delegate* unmanaged[Cdecl]<Git2.Reference*, nint, int> callback, nint payload);

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
    public static partial GitError git_reference_foreach_glob(Git2.Repository* repository, string glob, delegate* unmanaged[Cdecl]<byte*, nint, int> callback, nint payload);

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
    public static partial GitError git_reference_foreach_name(Git2.Repository* repository, delegate* unmanaged[Cdecl]<byte*, nint, int> callback, nint payload);

    /// <summary>
    /// Free the given reference.
    /// </summary>
    /// <param name="reference">The reference</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_reference_free(Git2.Reference* reference);

    /// <summary>
    /// Check if a reflog exists for the specified reference.
    /// </summary>
    /// <param name="repository">the repository</param>
    /// <param name="referenceName">the reference's name</param>
    /// <returns>0 when no reflog can be found, 1 when it exists; otherwise an error code from <see cref="GitError"/>.</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_reference_has_log(Git2.Repository* repository, string referenceName);

    /// <summary>
    /// Check if a reference is a local branch.
    /// </summary>
    /// <param name="reference">A git reference</param>
    /// <returns>1 when the reference lives in the refs/heads namespace; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_reference_is_branch(Git2.Reference* reference);

    /// <summary>
    /// Check if a reference is a note.
    /// </summary>
    /// <param name="reference">A git reference</param>
    /// <returns>1 when the reference lives in the refs/notes namespace; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_reference_is_note(Git2.Reference* reference);

    /// <summary>
    /// Check if a reference is a note.
    /// </summary>
    /// <param name="reference">A git reference</param>
    /// <returns>1 when the reference lives in the refs/notes namespace; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_reference_is_remote(Git2.Reference* reference);

    /// <summary>
    /// Check if a reference is a tag.
    /// </summary>
    /// <param name="reference">A git reference</param>
    /// <returns>1 when the reference lives in the refs/tags namespace; 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_reference_is_tag(Git2.Reference* reference);

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
    public static partial int git_reference_is_valid_name(string refname);

    /// <summary>
    /// Free the iterator and its associated resources
    /// </summary>
    /// <param name="iterator">the iterator to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_reference_iterator_free(Git2.ReferenceIterator* iterator);

    /// <summary>
    /// Create an iterator for the repo's references that match the specified glob
    /// </summary>
    /// <param name="iterator">pointer in which to store the iterator</param>
    /// <param name="repository">the repository</param>
    /// <param name="glob">the glob to match against the reference names</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reference_iterator_glob_new(Git2.ReferenceIterator** iterator, Git2.Repository* repository, string glob);

    /// <summary>
    /// Create an iterator for the repo's references
    /// </summary>
    /// <param name="iterator">pointer in which to store the iterator</param>
    /// <param name="repository">the repository</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reference_iterator_new(Git2.ReferenceIterator** iterator, Git2.Repository* repository);

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
    public static partial GitError git_reference_list(Native.GitStringArray* list, Git2.Repository* repository);

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
    public static partial GitError git_reference_lookup(Git2.Reference** reference, Git2.Repository* repository, string name);

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
    public static partial byte* git_reference_name(Git2.Reference* reference);

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
    public static partial GitError git_reference_name_is_valid(int* valid, string referenceName);

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
    public static partial GitError git_reference_name_to_id(GitObjectID* id, Git2.Repository* repository, string name);

    ///<inheritdoc cref="git_reference_name_to_id(GitObjectID*, nint, string)"/>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reference_name_to_id(GitObjectID* id, Git2.Repository* repository, byte* name);

    /// <summary>
    /// Get the next reference
    /// </summary>
    /// <param name="reference">pointer in which to store the reference</param>
    /// <param name="iterator">the iterator</param>
    /// <returns>0, <see cref="GitError.IterationOver"/> if there are no more; or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reference_next(Git2.Reference** reference, Git2.ReferenceIterator* iterator);

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
    public static partial GitError git_reference_next_name(byte** name, Git2.ReferenceIterator* iterator);

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
    public static partial GitError git_reference_normalize_name(byte* buffer_out, nuint buffer_size, byte* name, GitReferenceFormat flags);

    /// <summary>
    /// Get the repository where a reference resides.
    /// </summary>
    /// <param name="reference">The reference</param>
    /// <returns>a pointer to the repo</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.Repository* git_reference_owner(Git2.Reference* reference);

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
    public static partial GitError git_reference_peel(Git2.Object** gitObject, Git2.Reference* reference, GitObjectType type);

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
    public static partial GitError git_reference_remove(Git2.Repository* repository, string name);

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
    public static partial GitError git_reference_rename(Git2.Reference** newReference, Git2.Reference* reference, string newName, int force, string? logMessage);

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
    public static partial GitError git_reference_resolve(Git2.Reference** resolvedRef, Git2.Reference* reference);

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
    public static partial GitError git_reference_set_target(Git2.Reference** newReference, Git2.Reference* reference, GitObjectID* id, string? LogMessage);

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
    public static partial byte* git_reference_shorthand(Git2.Reference* reference);

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
    public static partial GitError git_reference_symbolic_create(Git2.Reference** reference, Git2.Repository* repository, string name, string target, int force, string? logMessage);

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
    public static partial GitError git_reference_symbolic_create_matching(Git2.Reference** reference, Git2.Repository* repository, string name, string target, int force, string? currentValue, string? logMessage);

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
    public static partial GitError git_reference_symbolic_set_target(Git2.Reference** newReference, Git2.Reference* currentReference, string target, string? logMessage);

    /// <inheritdoc cref="git_reference_symbolic_set_target(Git2.Reference**, nint, string, string?)"/>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reference_symbolic_set_target(Git2.Reference** newReference, Git2.Reference* currentReference, byte* target, string? logMessage);

    /// <summary>
    /// Get full name to the reference pointed to by a symbolic reference.
    /// </summary>
    /// <param name="reference">The reference</param>
    /// <returns>a pointer to the name if available, NULL otherwise</returns>
    /// <remarks>Only available if the reference is symbolic.</remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_reference_symbolic_target(Git2.Reference* reference);

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
    public static partial GitObjectID* git_reference_target(Git2.Reference* reference);

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
    public static partial GitObjectID* git_reference_target_peel(Git2.Reference* reference);

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
    public static partial GitReferenceType git_reference_type(Git2.Reference* reference);
    #endregion

    #region RefLog
    /// <summary>
    /// 
    /// </summary>
    /// <param name="refdb"></param>
    /// <param name="id"></param>
    /// <param name="committer"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reflog_append(
        Git2.RefLog* refdb,
        GitObjectID* id,
        Native.GitSignature* committer,
        string message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reflog_delete(
        Git2.Repository* repository,
        string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refdb"></param>
    /// <param name="idx"></param>
    /// <param name="rewrite_previous_entry"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reflog_drop(
        Git2.RefLog* refdb,
        nuint idx,
        int rewrite_previous_entry);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reflog"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.RefLogEntry* git_reflog_entry_byindex(Git2.RefLog* reflog, nuint idx);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitSignature* git_reflog_entry_committer(Git2.RefLogEntry* entry);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_reflog_entry_id_new(Git2.RefLogEntry* entry);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_reflog_entry_id_old(Git2.RefLogEntry* entry);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_reflog_entry_message(Git2.RefLogEntry* entry);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reflog"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_reflog_entrycount(Git2.RefLog* reflog);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reflog"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_reflog_free(Git2.RefLog* reflog);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reflog"></param>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reflog_read(Git2.RefLog** reflog, Git2.Repository* repository, string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="old_name"></param>
    /// <param name="new_name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reflog_rename(Git2.Repository* repository, string old_name, string new_name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reflog"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_reflog_write(Git2.RefLog* reflog);
    #endregion

    #region RefSpec
    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitDirection git_refspec_direction(Git2.RefSpec* refspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_refspec_dst(Git2.RefSpec* refspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec"></param>
    /// <param name="refname"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_refspec_dst_matches(Git2.RefSpec* refspec, string refname);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_refspec_force(Git2.RefSpec* refspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_refspec_free(Git2.RefSpec* refspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec_out"></param>
    /// <param name="input"></param>
    /// <param name="is_fetch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_refspec_parse(Git2.RefSpec** refspec_out, string input, int is_fetch);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="refspec"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_refspec_rtransform(Native.GitBuffer* buffer, Git2.RefSpec* refspec, string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_refspec_src(Git2.RefSpec* refspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec"></param>
    /// <param name="refname"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_refspec_src_matches(Git2.RefSpec* refspec, string refname);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_refspec_string(Git2.RefSpec* refspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="refspec"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_refspec_transform(Native.GitBuffer* buffer, Git2.RefSpec* refspec, string name);
    #endregion

    #region Remote
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="remote"></param>
    /// <param name="refspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_add_fetch(
        Git2.Repository* repository,
        string remote,
        string refspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="remote"></param>
    /// <param name="refspec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_add_push(
        Git2.Repository* repository,
        string remote,
        string refspec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitRemoteAutoTagOption git_remote_autotag(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="direction"></param>
    /// <param name="callbacks"></param>
    /// <param name="proxy_options"></param>
    /// <param name="custom_headers"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_connect(
        Git2.Remote* remote,
        GitDirection direction,
        Native.GitRemoteCallbacks* callbacks,
        Native.GitProxyOptions* proxy_options,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] custom_headers);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="direction"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_connect_ext(
        Git2.Remote* remote,
        GitDirection direction,
        Native.GitRemoteConnectOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_remote_connected(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_out"></param>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_create(
        Git2.Remote** remote_out,
        Git2.Repository* repository,
        string name,
        string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_out"></param>
    /// <param name="repository"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_create_anonymous(
        Git2.Remote** remote_out,
        Git2.Repository* repository,
        string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_out"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_create_detached(
        Git2.Remote** remote_out,
        string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_out"></param>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="url"></param>
    /// <param name="fetch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_create_with_fetchspec(
        Git2.Remote** remote_out,
        Git2.Repository* repository,
        string name,
        string url,
        string fetch);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_out"></param>
    /// <param name="url"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_create_with_opts(
        Git2.Remote** remote_out,
        string url,
        Native.GitRemoteCreateOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="branch_out"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_default_branch(Native.GitBuffer* branch_out, Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_delete(Git2.Repository* repository, string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_disconnect(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="refspecs"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_download(
        Git2.Remote* remote,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] refspecs,
        Native.GitFetchOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_out"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_dup(
        Git2.Remote** remote_out,
        Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="refspecs"></param>
    /// <param name="options"></param>
    /// <param name="reflog_message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_fetch(
        Git2.Remote* remote,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] refspecs,
        Native.GitFetchOptions* options,
        string? reflog_message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_remote_free(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="array"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_get_fetch_refspecs(
        Native.GitStringArray* array,
        Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="array"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_get_push_refspecs(
        Native.GitStringArray* array,
        Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.RefSpec* git_remote_get_refspec(Git2.Remote* remote, nuint idx);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool git_remote_is_valid_name(string remote_name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list_out"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_list(Native.GitStringArray* list_out, Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_out"></param>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_lookup(Git2.Remote** remote_out, Git2.Repository* repository, string name);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote_heads_out"></param>
    /// <param name="remote_heads_count"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_ls(
        Native.GitRemoteHead*** remote_heads_out,
        nuint* remote_heads_count,
        Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_remote_name(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="is_valid"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_name_is_valid(int* is_valid, string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.Repository* git_remote_owner(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="callbacks"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_prune(Git2.Remote* remote, Native.GitRemoteCallbacks* callbacks);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_remote_prune_refs(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="refspecs"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_remote_push(
        Git2.Remote* remote,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] refspecs,
        Native.GitPushOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_remote_pushurl(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_remote_refspec_count(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="problems"></param>
    /// <param name="repository"></param>
    /// <param name="current_name"></param>
    /// <param name="new_name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_rename(
        Native.GitStringArray* problems,
        Git2.Repository* repository,
        string current_name,
        string new_name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="remote_name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_set_autotag(Git2.Repository* repository, string remote_name, GitRemoteAutoTagOption value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_set_instance_pushurl(Git2.Remote* remote, string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_set_instance_url(Git2.Remote* remote, string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="remote_name"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_set_pushurl(Git2.Repository* repository, string remote_name, string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="remote_name"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_set_url(Git2.Repository* repository, string remote_name, string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitIndexerProgress* git_remote_stats(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_stop(Git2.Remote* remote);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="callbacks"></param>
    /// <param name="update_fetchhead"></param>
    /// <param name="download_tags"></param>
    /// <param name="reflog_message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_update_tips(
        Git2.Remote* remote,
        Native.GitRemoteCallbacks* callbacks,
        int update_fetchhead,
        GitRemoteAutoTagOption download_tags,
        string? reflog_message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="refspecs"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_remote_upload(
        Git2.Remote* remote,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] refspecs,
        Native.GitPushOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_remote_url(Git2.Remote* remote);
    #endregion

    #region Repository
    /// <summary>
    /// Gets the parents of the next commit, given the current repository state.
    /// Generally, this is the HEAD commit, except when performing a merge,
    /// in which case it is two or more commits.
    /// </summary>
    /// <param name="commits">a `git_commitarray` that will contain the commit parents</param>
    /// <param name="repository">the repository</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_commit_parents(Git2.CommitArray* commits, Git2.Repository* repository);

    /// <summary>
    /// Get the path of the shared common directory for this repository.
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <returns>the path to the common dir</returns>
    /// <remarks>
    /// If the repository is bare, it is the root directory for the repository.
    /// If the repository is a worktree, it is the parent repo's gitdir.
    /// Otherwise, it is the gitdir.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_repository_commondir(Git2.Repository* repository);

    /// <summary>
    /// Get the configuration file for this repository.
    /// </summary>
    /// <param name="config">Pointer to store the loaded configuration</param>
    /// <param name="repository">A repository object</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If a configuration file has not been set, the default config set for the repository will be returned,
    /// including global and system configurations (if they are available).
    /// 
    /// The configuration file must be freed once it's no longer being used by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_config(Git2.Config** config, Git2.Repository* repository);

    /// <summary>
    /// Get a snapshot of the repository's configuration
    /// </summary>
    /// <param name="config">Pointer to store the loaded configuration</param>
    /// <param name="repository">the repository</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Convenience function to take a snapshot from the repository's configuration.
    /// The contents of this snapshot will not change, even if the underlying config files are modified.
    /// The configuration file must be freed once it's no longer being used by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_config_snapshot(Git2.Config** config, Git2.Repository* repository);

    /// <summary>
    /// Detach the HEAD.
    /// </summary>
    /// <param name="repository">Repository pointer</param>
    /// <returns>0 on success, <see cref="GitError.UnbornBranch"/> when HEAD points to a non existing branch or an error code</returns>
    /// <remarks>
    /// If the HEAD is already detached and points to a Commit, 0 is returned.
    /// <br/><br/>
    /// If the HEAD is already detached and points to a Tag, the HEAD is updated into making it point to the peeled Commit, and 0 is returned.
    ///<br/><br/>
    /// If the HEAD is already detached and points to a non committish, the HEAD is unaltered, and -1 is returned.
    ///<br/><br/>
    /// Otherwise, the HEAD will be detached and point to the peeled Commit.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_detach_head(Git2.Repository* repository);

    /// <summary>
    /// Look for a git repository and copy its path in the given buffer.
    /// The lookup start from base_path and walk across parent directories
    /// if nothing has been found. The lookup ends when the first repository
    /// is found, or when reaching a directory referenced in <paramref name="ceilingDirs"/>
    /// or when the filesystem changes (in case <paramref name="acrossFS"/> is true).
    /// </summary>
    /// <param name="out">A pointer to a user-allocated <see cref="Git2.Buffer"/> which will contain the found path.</param>
    /// <param name="startPath">The base path where the lookup starts.</param>
    /// <param name="acrossFS">
    /// If true, then the lookup will not stop when a filesystem device change is detected while exploring parent directories.
    /// </param>
    /// <param name="ceilingDirs">
    /// A <see cref="Git2.PathListSeparator"/> separated list of absolute symbolic link free paths.
    /// The lookup will stop when any of this paths is reached. Note that the lookup
    /// always performs on <paramref name="startPath"/> no matter <paramref name="startPath"/>
    /// appears in <paramref name="ceilingDirs"/>. <paramref name="ceilingDirs"/> might be
    /// <see langword="null"/> (which is equivalent to an empty string).
    /// </param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_discover(Native.GitBuffer* @out, string startPath, int acrossFS, string? ceilingDirs);

    /// <summary>
    /// Invoke <paramref name="callback"/> for each entry in the given FETCH_HEAD file.
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <param name="callback">Callback function</param>
    /// <param name="payload">Pointer to callback data (optional)</param>
    /// <returns>
    /// 0 on success, non-zero callback return value,
    /// <see cref="GitError.NotFound"/> if there is no FETCH_HEAD file,
    /// or other error code.
    /// </returns>
    /// <remarks>
    /// Return a non-zero value from the callback to stop the loop.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_fetchhead_foreach(Git2.Repository* repository, delegate* unmanaged[Cdecl]<byte*, byte*, GitObjectID*, uint, nint, int> callback, nint payload);

    /// <summary>
    /// Free a previously allocated repository
    /// </summary>
    /// <param name="repository">repository handle to close. If NULL nothing occurs.</param>
    /// <remarks>
    /// Note that after a repository is free'd, all the objects it has spawned will still exist
    /// until they are manually closed by the user with <see cref="git_object_free"/>, but accessing
    /// any of the attributes of an object without a backing repository will result in undefined behavior
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_repository_free(Git2.Repository* repository);

    /// <summary>
    /// Get the currently active namespace for this repository
    /// </summary>
    /// <param name="repository">The repo</param>
    /// <returns>the active namespace, or NULL if there isn't one</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_repository_get_namespace(Git2.Repository* repository);

    /// <summary>
    /// Calculate hash of file using repository filtering rules.
    /// </summary>
    /// <param name="out">Output value of calculated SHA</param>
    /// <param name="repository">Repository pointer</param>
    /// <param name="path">
    /// Path to file on disk whose contents should be hashed.
    /// This may be an absolute path or a relative path,
    /// in which case it will be treated as a path within
    /// the working directory.
    /// </param>
    /// <param name="type">
    /// The object type to hash as (e.g. <see cref="GitObjectType.Blob"/>)
    /// </param>
    /// <param name="asPath">
    /// The path to use to look up filtering rules.
    /// If this is an empty string then no filters will be applied
    /// when calculating the hash. If this is `NULL` and the `path`
    /// parameter is a file within the repository's working directory,
    /// then the `path` will be used.
    /// </param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// If you simply want to calculate the hash of a file on disk with no filters,
    /// you can just use the <see cref="git_odb_hashfile"/> API. However, if you want
    /// to hash a file in the repository and you want to apply filtering rules
    /// (e.g. crlf filters) before generating the SHA, then use this function.
    /// <br/><br/>
    /// Note: If the repository has core.safecrlf set to fail and the filtering
    /// triggers that failure, then this function will return an error and not
    /// calculate the hash of the file.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_hashfile(GitObjectID* @out, Git2.Repository* repository, string path, GitObjectType type, string asPath);

    /// <summary>
    /// Retrieve and resolve the reference pointed at by HEAD.
    /// </summary>
    /// <param name="head">pointer to the reference which will be retrieved</param>
    /// <param name="repository">a repository object</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.UnbornBranch"/> when HEAD points to a non existing branch,
    /// <see cref="GitError.NotFound"/> when HEAD is missing, or an error code otherwise
    /// </returns>
    /// <remarks>
    /// The returned <see cref="GitReference"/> will be owned by caller and
    /// <see cref="GitReference.Dispose"/> must be called when done with it
    /// to release the allocated memory and prevent a leak.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_head(Git2.Reference** head, Git2.Repository* repository);

    /// <summary>
    /// Check if a repository's HEAD is detached
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>
    /// 1 if HEAD is detached, 0 if it's not; error code if there was an error.
    /// </returns>
    /// <remarks>
    /// A repository's HEAD is detached when it points directly to a commit instead of a branch.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_repository_head_detached(Git2.Repository* repository);

    /// <summary>
    /// Check if a worktree's HEAD is detached
    /// </summary>
    /// <param name="repository">a repository object</param>
    /// <param name="worktreeName">name of the worktree to retrieve HEAD for</param>
    /// <returns>1 if HEAD is detached, 0 if its not; error code if there was an error</returns>
    /// <remarks>
    /// A worktree's HEAD is detached when it points directly to a commit instead of a branch.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_repository_head_detached_for_worktree(Git2.Repository* repository, string worktreeName);

    /// <summary>
    /// Check if the current branch is unborn
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>
    /// 1 if the current branch is unborn, 0 if it's not,
    /// or an error code if there was an error
    /// </returns>
    /// <remarks>
    /// An unborn branch is one named from HEAD but which doesn't exist
    /// in the refs namespace, because it doesn't have any commit to point to.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_repository_head_unborn(Git2.Repository* repository);

    /// <summary>
    /// Retrieve the configured identity to use for reflogs
    /// </summary>
    /// <param name="name">where to store the pointer to the name</param>
    /// <param name="email">where to store the pointer to the email</param>
    /// <param name="repository">the repository</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The memory is owned by the repository and must not be freed by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_ident(byte** name, byte** email, Git2.Repository* repository);

    /// <summary>
    /// Get the Index file for this repository.
    /// </summary>
    /// <param name="index">Pointer to store the loaded index</param>
    /// <param name="repository">A repository object</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If a custom index has not been set, the default index for the repository will be returned (the one located in .git/index).
    /// <br/><br/>
    /// The index must be freed with <see cref="GitIndex.Dispose"/> once it's no longer being used by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_index(Git2.Index** index, Git2.Repository* repository);

    /// <summary>
    /// Creates a new Git repository in the given folder.
    /// </summary>
    /// <param name="repository">
    /// Pointer to the repo which will be created or reinitialized
    /// </param>
    /// <param name="path">
    /// the path to the repository
    /// </param>
    /// <param name="isBare">
    /// If true, a Git repository without a working directory is created at the pointed path.
    /// If false, provided path will be considered as the working directory into which the .git directory will be created.
    /// </param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_init(Git2.Repository** repository, string path, uint isBare);

    /// <summary>
    /// Create a new Git repository in the given folder with extended controls.
    /// </summary>
    /// <param name="repository">Pointer to the repo which will be created or reinitialized.</param>
    /// <param name="path">The path to the repository.</param>
    /// <param name="options">Pointer to git_repository_init_options struct.</param>
    /// <returns>0 or an error code on failure.</returns>
    /// <remarks>
    /// This will initialize a new git repository (creating the repo_path if requested by flags)
    /// and working directory as needed. It will auto-detect the case sensitivity of the file system
    /// and if the file system supports file mode bits correctly.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_init_ext(Git2.Repository** repository, string path, Native.GitRepositoryInitOptions* options);

    /// <summary>
    /// Check if a repository is bare
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>1 if the repository is bare, 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_repository_is_bare(Git2.Repository* repository);

    /// <summary>
    /// Check if a repository is empty
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>1 if the repository is empty, 0 if it isn't, error code if the repository is corrupted</returns>
    /// <remarks>
    /// An empty repository has just been initialized and contains no references
    /// apart from HEAD, which must be pointing to the unborn master branch,
    /// or the branch specified for the repository in the <see cref="RepositoryInitOptions.InitialHead"/> configuration variable.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_repository_is_empty(Git2.Repository* repository);

    /// <summary>
    /// Determine if the repository was a shallow clone
    /// </summary>
    /// <param name="repository">The repository</param>
    /// <returns>1 if shallow, zero if not</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_repository_is_shallow(Git2.Repository* repository);

    /// <summary>
    /// Check if a repository is a linked work tree
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>1 if the repository is a linked work tree, 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_repository_is_worktree(Git2.Repository* repository);

    /// <summary>
    /// Get the location of a specific repository file or directory
    /// </summary>
    /// <param name="out">Buffer to store the path at</param>
    /// <param name="repository">Repository to get path for</param>
    /// <param name="itemType">The repository item for which to retrieve the path</param>
    /// <returns>0, <see cref="GitError.NotFound"/> if the path cannot exist or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_item_path(Native.GitBuffer* @out, Git2.Repository* repository, GitRepositoryItemType itemType);

    /// <summary>
    /// If a merge is in progress, invoke 'callback' for each commit ID in the MERGE_HEAD file.
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <param name="callback">Callback function</param>
    /// <param name="payload">Pointer to callback data (optional)</param>
    /// <returns>0 on success, non-zero callback return value, <see cref="GitError.NotFound"/> if there is no MERGE_HEAD file, or other error code.</returns>
    /// <remarks>
    /// Return a non-zero value from the callback to stop the loop.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_mergehead_foreach(Git2.Repository* repository, delegate* unmanaged[Cdecl]<GitObjectID*, nint, int> callback, nint payload);

    /// <summary>
    /// Retrieve git's prepared message
    /// </summary>
    /// <param name="out"><see cref="Git2.Buffer"/> to write data into</param>
    /// <param name="repository">Repository to read prepared message from</param>
    /// <returns>0, <see cref="GitError.NotFound"/> if no message exists, or an error code</returns>
    /// <remarks>
    /// Operations such as git revert/cherry-pick/merge with the -n option stop just short of
    /// creating a commit with the changes and save their prepared message in .git/MERGE_MSG
    /// so the next git-commit execution can present it to the user for them to amend if they wish.
    /// <br/><br/>
    /// Use this function to get the contents of this file.
    /// Don't forget to remove the file after you create the commit.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_message(Native.GitBuffer* @out, Git2.Repository* repository);

    /// <summary>
    /// Remove git's prepared message.
    /// </summary>
    /// <param name="repository">Repository to remove prepared message from.</param>
    /// <returns>0 or an error code.</returns>
    /// <remarks>
    /// Remove the message that <seealso cref="git_repository_message(Git2.Buffer*, Git2.Repository*)"/> retrieves.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_message_remove(Git2.Repository* repository);

    /// <summary>
    /// Get the Object Database for this repository.
    /// </summary>
    /// <param name="db_out">Pointer to store the loaded ODB</param>
    /// <param name="repository">A repository object</param>
    /// <returns>0, or an error code</returns>
    /// <remarks>
    /// If a custom ODB has not been set, the default database for the repository will be returned (the one located in .git/objects).
    /// <br/><br/>
    /// The ODB must be freed once it's no longer being used by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_odb(Git2.ObjectDatabase** db_out, Git2.Repository* repository);

#if GIT_EXPERIMENTAL_SHA256
    /// <summary>
    /// Gets the object type used by this repository.
    /// </summary>
    /// <param name="repository">the repository</param>
    /// <returns>the object id type</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectType git_repository_oid_type(Git2.Repository* repository);
#endif

    /// <summary>
    /// Open a git repository.
    /// </summary>
    /// <param name="repository">pointer to the repo which will be opened</param>
    /// <param name="path">the path to the repository</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The <paramref name="path"/> argument must point to either a git repository folder, or an existing work dir.
    /// <br/><br/>
    /// The method will automatically detect if <paramref name="path"/> is a normal or bare repository or fail if 'path' is neither.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_open(Git2.Repository** repository, string path);

    /// <summary>
    /// Open a bare repository on the serverside.
    /// </summary>
    /// <param name="repository">Pointer to the repo which will be opened.</param>
    /// <param name="path">Direct path to the bare repository</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// This is a fast open for bare repositories that will come in handy if you're e.g. hosting git repositories and need to access them efficiently
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_open_bare(Git2.Repository** repository, string path);

    /// <summary>
    /// Find and open a repository with extended controls.
    /// </summary>
    /// <param name="repository">
    /// Pointer to the repo which will be opened.
    /// This can actually be <see langword="null"/> if you only want
    /// to use the error code to see if a repo at
    /// this path could be opened.
    /// </param>
    /// <param name="path">
    /// Path to open as git repository. If the flags permit "searching",
    /// then this can be a path to a subdirectory inside the working directory
    /// of the repository. May be <see langword="null"/> if flags contains <see cref="GitRepositoryOpenFlags.FromEnvironment"/>.
    /// </param>
    /// <param name="flags"></param>
    /// <param name="ceiling_dirs">
    /// A <see cref="Git2.PathListSeparator"/> delimited list of path
    /// prefixes at which the search for a containing repository should terminate.
    /// </param>
    /// <returns>
    /// 0 on success, <see cref="GitError.NotFound"/> if no repository could be found,
    /// or -1 if there was a repository but open failed for some reason (such as repo corruption or system errors).
    /// </returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_open_ext(Git2.Repository** repository, string? path, GitRepositoryOpenFlags flags, string? ceiling_dirs);

    /// <summary>
    /// Open working tree as a repository
    /// </summary>
    /// <param name="repository">Output pointer containing opened repository</param>
    /// <param name="worktree">Working tree to open</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Open the working directory of the working tree as a normal repository that can then be worked on.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_open_from_worktree(Git2.Repository** repository, Git2.Worktree* worktree);

    /// <summary>
    /// Get the path of this repository
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <returns>the path to the repository</returns>
    /// <remarks>
    /// This is the path of the ".git" folder for normal repositories,
    /// or of the repository itself for bare repositories.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_repository_path(Git2.Repository* repository);

    /// <summary>
    /// Get the Reference Database Backend for this repository.
    /// </summary>
    /// <param name="refDB">Pointer to store the loaded refdb</param>
    /// <param name="repository">A repository object</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_refdb(Git2.ReferenceDatabase** refDB, Git2.Repository* repository);

    /// <summary>
    /// Make the repository HEAD point to the specified reference.
    /// </summary>
    /// <param name="repository">Repository pointer</param>
    /// <param name="refname">Canonical name of the reference the HEAD should point at</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// If the provided reference points to a Tree or a Blob, the HEAD is unaltered and -1 is returned.
    /// <br/><br/>
    /// If the provided reference points to a branch, the HEAD will point to that branch,
    /// staying attached, or become attached if it isn't yet. If the branch doesn't exist yet,
    /// no error will be return. The HEAD will then be attached to an unborn branch.
    /// <br/><br/>
    /// Otherwise, the HEAD will be detached and will directly point to the Commit.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_set_head(Git2.Repository* repository, byte* refname);

    ///<inheritdoc cref="git_repository_set_head(nint, byte*)"/>
    public static GitError git_repository_set_head(Git2.Repository* repository, string refname)
    {
        scoped Utf8StringMarshaller.ManagedToUnmanagedIn refNameIn = new();
        try
        {
            refNameIn.FromManaged(refname, stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize]);

            return git_repository_set_head(repository, refNameIn.ToUnmanaged());
        }
        finally
        {
            refNameIn.Free();
        }
    }

    /// <summary>
    /// Make the repository HEAD directly point to the Commit.
    /// </summary>
    /// <param name="repository">Repository pointer</param>
    /// <param name="committish">Object id of the Commit the HEAD should point to</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// If the provided committish cannot be found in the repository, the HEAD is unaltered and <see cref="GitError.NotFound"/> is returned.
    /// <br/><br/>
    /// If the provided committish cannot be peeled into a commit, the HEAD is unaltered and -1 (<see cref="GitError.GenericError"/>) is returned.
    /// <br/><br/>
    /// Otherwise, the HEAD will eventually be detached and will directly point to the peeled Commit.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_set_head_detached(Git2.Repository* repository, GitObjectID* committish);

    /// <summary>
    /// Make the repository HEAD directly point to the Commit.
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="committish"></param>
    /// <returns></returns>
    /// <remarks>
    /// This behaves like <see cref="git_repository_set_head_detached(Git2.Repository*, GitObjectID*)"/>
    /// but takes an annotated commit, which lets you specify which extended sha syntax string
    /// was specified by a user, allowing for more exact reflog messages.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_set_head_detached_from_annotated(Git2.Repository* repository, nint committish); // TODO: Needs to be exposed

    /// <summary>
    /// Set the identity to be used for writing reflogs
    /// </summary>
    /// <param name="repository">the repository to configure</param>
    /// <param name="name">the name to use for the reflog entries</param>
    /// <param name="email">the email to use for the reflog entries</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If both are set, this name and email will be used to write to the reflog.
    /// Pass NULL to unset. When unset, the identity will be taken from the repository's configuration.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_set_ident(Git2.Repository* repository, string? name, string? email);

    /// <summary>
    /// Sets the active namespace for this Git Repository
    /// </summary>
    /// <param name="repository">The repo</param>
    /// <param name="namespace">
    /// The namespace. This should not include the refs folder, e.g. to namespace all references under `refs/namespaces/foo/`, use `foo` as the namespace.
    /// </param>
    /// <returns>0 on success, -1 on error</returns>
    /// <remarks>
    /// This namespace affects all reference operations for the repo.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_set_namespace(Git2.Repository* repository, string @namespace);

    /// <summary>
    /// Set the path to the working directory for this repository
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <param name="workdir">The path to a working directory</param>
    /// <param name="updateGitlink">
    /// Create/update gitlink in workdir and set config "core.worktree" (if workdir is not the parent of the .git directory)
    /// </param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The working directory doesn't need to be the same one that
    /// contains the ".git" folder for this repository.
    /// <br/><br/>
    /// If this repository is bare, setting its working directory
    /// will turn it into a normal repository, capable of performing
    /// all the common workdir operations (checkout, status, index manipulation, etc).
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_set_workdir(Git2.Repository* repository, string workdir, int updateGitlink);

    /// <summary>
    /// Determines the status of a git repository - ie, whether an operation (merge, cherry-pick, etc) is in progress.
    /// </summary>
    /// <param name="repository">Repository pointer</param>
    /// <returns>The state of the repository</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitRepositoryState git_repository_state(Git2.Repository* repository);

    /// <summary>
    /// Remove all the metadata associated with an ongoing command like merge, revert, cherry-pick, etc. For example: MERGE_HEAD, MERGE_MSG, etc.
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <returns>0 on success, or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_repository_state_cleanup(Git2.Repository* repository);

    /// <summary>
    /// Get the path of the working directory for this repository
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <returns>The path to the working directory if it exists, or <see langword="null"/> if it does not.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_repository_workdir(Git2.Repository* repository);
    #endregion

    #region Reset
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="target"></param>
    /// <param name="reset_type"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_reset(
        Git2.Repository* repository,
        Git2.Object* target,
        GitResetType reset_type,
        Native.GitCheckoutOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="target"></param>
    /// <param name="pathspecs"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_reset_default(
        Git2.Repository* repository,
        Git2.Object* target,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] pathspecs);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="commit"></param>
    /// <param name="reset_type"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_reset_from_annotated(
        Git2.Repository* repository,
        Git2.AnnotatedCommit* commit,
        GitResetType reset_type,
        Native.GitCheckoutOptions* options);
    #endregion

    #region Revert
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="target"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_revert(
        Git2.Repository* repository,
        Git2.Commit* target,
        Native.GitRevertOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index_out"></param>
    /// <param name="repository"></param>
    /// <param name="revert_commit"></param>
    /// <param name="our_commit"></param>
    /// <param name="mainline"></param>
    /// <param name="merge_options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_revert_commit(
        Git2.Index** index_out,
        Git2.Repository* repository,
        Git2.Commit* revert_commit,
        Git2.Commit* our_commit,
        uint mainline,
        Native.GitMergeOptions* merge_options);
    #endregion

    #region RevParse
    /// <summary>
    /// 
    /// </summary>
    /// <param name="revspec"></param>
    /// <param name="repository"></param>
    /// <param name="spec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revparse(
        Native.GitRevSpec* revspec,
        Git2.Repository* repository,
        string spec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object_out"></param>
    /// <param name="reference_out"></param>
    /// <param name="repository"></param>
    /// <param name="spec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revparse_ext(
        Git2.Object** object_out,
        Git2.Reference** reference_out,
        Git2.Repository* repository,
        string spec);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="object_out"></param>
    /// <param name="repository"></param>
    /// <param name="spec"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revparse_single(
        Git2.Object** object_out,
        Git2.Repository* repository,
        string spec);
    #endregion

    #region RevWalk
    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="hide_cb"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_add_hide_cb(
        Git2.RevWalk* walk,
        delegate* unmanaged[Cdecl]<GitObjectID*, nint, int> hide_cb,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_free(Git2.RevWalk* walk);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="commit_id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_hide(Git2.RevWalk* walk, GitObjectID* commit_id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="glob"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_hide_glob(Git2.RevWalk* walk, string glob);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_hide_head(Git2.RevWalk* walk);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="refname"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_hide_ref(Git2.RevWalk* walk, string refname);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk_out"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_new(Git2.RevWalk** walk_out, Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="walk"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_next(GitObjectID* id_out, Git2.RevWalk* walk);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_push(Git2.RevWalk* walk, GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="glob"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_push_glob(Git2.RevWalk* walk, string glob);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_push_head(Git2.RevWalk* walk);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_push_range(Git2.RevWalk* walk, string range);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="refname"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_push_ref(Git2.RevWalk* walk, string refname);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.Repository* git_revwalk_repository(Git2.RevWalk* walk, GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_reset(Git2.RevWalk* walk);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_simplify_first_parent(Git2.RevWalk* walk);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="walk"></param>
    /// <param name="sort_mode"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_revwalk_sorting(
        Git2.RevWalk* walk,
        GitSortType sort_mode);
    #endregion

    #region Stash
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="idx"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_stash_apply(
        Git2.Repository* repository,
        nuint idx,
        Native.GitStashApplyOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_stash_drop(
        Git2.Repository* repository,
        nuint idx);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="callback"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_stash_foreach(
        Git2.Repository* repository,
        delegate* unmanaged[Cdecl]<nuint, byte*, GitObjectID*, nint, int> callback,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="idx"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_stash_pop(
        Git2.Repository* repository,
        nuint idx,
        Native.GitStashApplyOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="stasher"></param>
    /// <param name="message"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_stash_save(
        GitObjectID* id_out,
        Git2.Repository* repository,
        Native.GitSignature* stasher,
        string? message,
        GitStashFlags flags);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_stash_save_with_opts(
        GitObjectID* id_out,
        Git2.Repository* repository,
        Native.GitStashSaveOptions* options);
    #endregion

    #region Status
    /// <summary>
    /// 
    /// </summary>
    /// <param name="statuslist"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitStatusEntry* git_status_byindex(Git2.StatusList* statuslist, nuint idx);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_status_file(GitStatusFlags* status_flags, Git2.Repository* repository, string path);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_status_foreach(
        Git2.Repository* repository,
        delegate* unmanaged[Cdecl]<byte*, GitStatusFlags, nint, int> callback,
        nint payload);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_status_foreach_ext(
        Git2.Repository* repository,
        Native.GitStatusOptions* options,
        delegate* unmanaged[Cdecl]<byte*, GitStatusFlags, nint, int> callback,
        nint payload);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_status_list_entrycount(Git2.StatusList* statuslist);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_status_list_free(Git2.StatusList* statuslist);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_status_list_new(
        Git2.StatusList** statuslist_out,
        Git2.Repository* repository,
        Native.GitStatusOptions* options);


    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_status_should_ignore(int* ignored, Git2.Repository* repository, string path);
    #endregion

    #region Submodule
    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_add_finalize(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule_out"></param>
    /// <param name="repository"></param>
    /// <param name="url"></param>
    /// <param name="path"></param>
    /// <param name="use_gitlink"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_add_setup(
        Git2.Submodule** submodule_out,
        Git2.Repository* repository,
        string url,
        string path,
        int use_gitlink);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <param name="write_index"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_add_to_index(
        Git2.Submodule* submodule,
        int write_index);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_submodule_branch(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository_out"></param>
    /// <param name="submodule"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_clone(
        Git2.Repository** repository_out,
        Git2.Submodule* submodule,
        Native.GitSubmoduleUpdateOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule_out"></param>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_dup(
        Git2.Submodule** submodule_out,
        Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitSubmoduleRecurseType git_submodule_fetch_recurse_submodules(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="callback"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_foreach(
        Git2.Repository* repository,
        delegate* unmanaged[Cdecl]<Git2.Submodule*, byte*, nint, int> callback,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_submodule_free(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_submodule_head_id(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitSubmoduleIgnoreType git_submodule_ignore(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_submodule_index_id(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <param name="overwrite"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_init(
        Git2.Submodule* submodule,
        int overwrite);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="location_status"></param>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_location(
        uint* location_status,
        Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule_out"></param>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_lookup(
        Git2.Submodule** submodule_out,
        Git2.Repository* repository,
        string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_submodule_name(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository_out"></param>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_open(
        Git2.Repository** repository_out,
        Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.Repository* git_submodule_owner(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_submodule_path(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_reload(
        Git2.Submodule* submodule,
        int force);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository_out"></param>
    /// <param name="submodule"></param>
    /// <param name="use_gitlink"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_repo_init(
        Git2.Repository** repository_out,
        Git2.Submodule* submodule,
        int use_gitlink);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="repository"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_resolve_url(
        Native.GitBuffer* buffer,
        Git2.Repository* repository,
        string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="branch"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_set_branch(
        Git2.Repository* repository,
        string name,
        string branch);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="fetch_recurse_submodules"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_set_fetch_recurse_submodules(
        Git2.Repository* repository,
        string name,
        GitSubmoduleRecurseType fetch_recurse_submodules);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="ignore"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_set_ignore(
        Git2.Repository* repository,
        string name,
        GitSubmoduleIgnoreType ignore);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="update"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_set_update(
        Git2.Repository* repository,
        string name,
        GitSubmoduleUpdateType update);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_set_url(
        Git2.Repository* repository,
        string name,
        string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="status"></param>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="ignore"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_status(
        uint* status,
        Git2.Repository* repository,
        string name,
        GitSubmoduleIgnoreType ignore);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_sync(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_submodule_update(
        Git2.Submodule* submodule,
        Native.GitSubmoduleUpdateOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitSubmoduleUpdateType git_submodule_update_strategy(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_submodule_url(Git2.Submodule* submodule);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submodule"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_submodule_wd_id(Git2.Submodule* submodule);
    #endregion

    #region Tag
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="tag_name"></param>
    /// <param name="target"></param>
    /// <param name="tagger"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_annotation_create(
        GitObjectID* id_out,
        Git2.Repository* repository,
        string tag_name,
        Git2.Object* target,
        Native.GitSignature* tagger,
        string message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="tag_name"></param>
    /// <param name="target"></param>
    /// <param name="tagger"></param>
    /// <param name="message"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_create(
        GitObjectID* id_out,
        Git2.Repository* repository,
        string tag_name,
        Git2.Object* target,
        Native.GitSignature* tagger,
        string message,
        int force);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="raw_tag_data"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_create_from_buffer(
        GitObjectID* id_out,
        Git2.Repository* repository,
        string raw_tag_data,
        int force);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="buffer"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_create_from_buffer(
        GitObjectID* id_out,
        Git2.Repository* repository,
        byte* buffer,
        int force);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="repository"></param>
    /// <param name="tag_name"></param>
    /// <param name="target"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_create_lightweight(
        GitObjectID* id_out,
        Git2.Repository* repository,
        string tag_name,
        Git2.Object* target,
        int force);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="tag_name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_delete(
        Git2.Repository* repository,
        string tag_name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag_out"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_dup(
        Git2.Tag** tag_out,
        Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="callback"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_foreach(
        Git2.Repository* repository,
        delegate* unmanaged[Cdecl]<byte*, GitObjectID*, nint, int> callback,
        nint payload);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_tag_free(Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_tag_id(Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag_names_out"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_list(Native.GitStringArray* tag_names_out, Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag_names_out"></param>
    /// <param name="pattern"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_list_match(
        Native.GitStringArray* tag_names_out,
        string pattern,
        Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag_out"></param>
    /// <param name="repository"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_lookup(
        Git2.Tag** tag_out,
        Git2.Repository* repository,
        GitObjectID* id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag_out"></param>
    /// <param name="repository"></param>
    /// <param name="id"></param>
    /// <param name="id_length"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_lookup_prefix(
        Git2.Tag** tag_out,
        Git2.Repository* repository,
        GitObjectID* id,
        nuint id_length);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_tag_message(Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_tag_name(Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valid"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_name_is_valid(int* valid, string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.Repository* git_tag_owner(Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="target_out"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_peel(Git2.Object** target_out, Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Native.GitSignature* git_tag_tagger(Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="target_out"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tag_target(Git2.Object** target_out, Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectID* git_tag_target_id(Git2.Tag* tag);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitObjectType git_tag_target_type(Git2.Tag* tag);
    #endregion

    #region Trace
    /// <summary>
    /// Sets the system tracing configuration to the specified level with the specified callback.
    /// When system events occur at a level equal to, or lower than, the given level they will be
    /// reported to the given callback.
    /// </summary>
    /// <param name="level">Level to set tracing to</param>
    /// <param name="">Function to call with trace data</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_trace_set(GitTraceLevel level, delegate* unmanaged[Cdecl]<GitTraceLevel, byte*, void> callback);
    #endregion

    #region Transaction
    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_transaction_commit(Git2.Transaction* transaction);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_transaction_free(Git2.Transaction* transaction);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="refname"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_transaction_lock_ref(
        Git2.Transaction* transaction,
        string refname);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction_out"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_transaction_new(Git2.Transaction** transaction_out, Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="refname"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_transaction_remove(
        Git2.Transaction* transaction,
        string refname);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="refname"></param>
    /// <param name="reflog"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_transaction_set_reflog(
        Git2.Transaction* transaction,
        string refname,
        Git2.RefLog* reflog);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="refname"></param>
    /// <param name="target"></param>
    /// <param name="signature"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_transaction_set_symbolic_target(
        Git2.Transaction* transaction,
        string refname,
        string target,
        Native.GitSignature* signature,
        string message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="refname"></param>
    /// <param name="target"></param>
    /// <param name="signature"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_transaction_set_target(
        Git2.Transaction* transaction,
        string refname,
        GitObjectID* target,
        Native.GitSignature* signature,
        string message);
    #endregion

    #region Tree
    /// <summary>
    /// Create a tree based on another one with the specified modifications
    /// </summary>
    /// <param name="id_out">id of the new tree</param>
    /// <param name="repo">
    /// the repository in which to create the tree, must be the same as for <paramref name="baseline"/>
    /// </param>
    /// <param name="baseline">the tree to base these changes on</param>
    /// <param name="updateCount">the number of elements in the update list</param>
    /// <param name="updates">the list of updates to perform</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Given the <paramref name="baseline"/>, perform the changes described
    /// in the list of updates and create a new tree.
    /// 
    /// This function is optimized for common file/directory addition,
    /// removal and replacement in trees. It is much more efficient than
    /// reading the tree into a git_index and modifying that, but in
    /// exchange it is not as flexible.
    /// 
    /// Deleting and adding the same entry is undefined behaviour,
    /// changing a tree to a blob or viceversa is not supported.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tree_create_updated(
        GitObjectID* id_out,
        Git2.Repository* repo,
        Git2.Tree* baseline,
        nuint updateCount,
        ReadOnlySpan<GitTreeUpdate> updates);

    /// <summary>
    /// Create an in-memory copy of a tree. The copy must be explicitly free'd
    /// or it will result in a memory leak.
    /// </summary>
    /// <param name="tree_out">Pointer to store the copy of the tree</param>
    /// <param name="tree">Original tree to copy</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tree_dup(Git2.Tree** tree_out, Git2.Tree* tree);

    /// <summary>
    /// Lookup a tree entry by SHA value
    /// </summary>
    /// <param name="tree">a previously loaded tree.</param>
    /// <param name="id">the sha being looked for</param>
    /// <returns>the tree entry; NULL if not found</returns>
    /// <remarks>
    /// This returns a git_tree_entry that is owned by the git_tree.
    /// You don't have to free it, but you must not use it after the git_tree is released.
    /// 
    /// Warning: this must examine every entry in the tree, so it is not fast.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.TreeEntry* git_tree_entry_byid(Git2.Tree* tree, GitObjectID* id);

    /// <summary>
    /// Lookup a tree entry by its position in the tree
    /// </summary>
    /// <param name="tree">a previously loaded tree</param>
    /// <param name="index">the position in the entry list</param>
    /// <returns>the tree entry; NULL if not found</returns>
    /// <remarks>
    /// This returns a git_tree_entry that is owned by the git_tree.
    /// You don't have to free it, but you must not use it after the git_tree is released.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.TreeEntry* git_tree_entry_byindex(Git2.Tree* tree, nuint index);

    /// <summary>
    /// Lookup a tree entry by its filename
    /// </summary>
    /// <param name="tree">a previously loaded tree.</param>
    /// <param name="filename">the filename of the desired entry</param>
    /// <returns>he tree entry; NULL if not found</returns>
    /// <remarks>
    /// This returns a git_tree_entry that is owned by the git_tree.
    /// You don't have to free it, but you must not use it after the git_tree is released.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.TreeEntry* git_tree_entry_byname(Git2.Tree* tree, string filename);

    /// <summary>
    /// Retrieve a tree entry contained in a tree or in any of its subtrees, given its relative path.
    /// </summary>
    /// <param name="entry_out">Pointer where to store the tree entry</param>
    /// <param name="tree">Previously loaded tree which is the root of the relative path</param>
    /// <param name="path">Path to the contained entry</param>
    /// <returns>0 on success; <see cref="GitError.NotFound"/> if the path does not exist</returns>
    /// <remarks>
    /// Unlike the other lookup functions, the returned tree entry
    /// is owned by the user and must be freed explicitly with
    /// <see cref="git_tree_entry_free"/>
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tree_entry_bypath(Git2.TreeEntry** entry_out, Git2.Tree* tree, string path);

    /// <summary>
    /// Compare two tree entries
    /// </summary>
    /// <param name="entry1">first tree entry</param>
    /// <param name="entry2">second tree entry</param>
    /// <returns>Standard <see cref="IComparable{T}"/> behavior</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int git_tree_entry_cmp(Git2.TreeEntry* entry1, Git2.TreeEntry* entry2);

    /// <summary>
    /// Duplicate a tree entry
    /// </summary>
    /// <param name="entry_out">pointer where to store the copy</param>
    /// <param name="entry">tree entry to duplicate</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Create a copy of a tree entry. The returned copy is owned by the user, and must be freed explicitly with
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tree_entry_dup(Git2.TreeEntry** entry_out, Git2.TreeEntry* entry);

    /// <summary>
    /// Get the UNIX file attributes of a tree entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>filemode as an integer</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitFileMode git_tree_entry_filemode(Git2.TreeEntry* entry);

    /// <summary>
    /// Get the UNIX file attributes of a tree entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>filemode as an integer</returns>
    /// <remarks>
    /// This function does not perform any normalization and is only useful
    /// if you need to be able to recreate the original tree object.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitFileMode git_tree_entry_filemode_raw(Git2.TreeEntry* entry);

    /// <summary>
    /// Free a user-owned tree entry
    /// </summary>
    /// <param name="entry">The entry to free</param>
    /// <remarks>
    /// IMPORTANT: This function is only needed for tree entries owned by the user,
    /// such as the ones returned by <see cref="git_tree_entry_dup(Git2.TreeEntry**, Git2.TreeEntry*)"/>
    /// or <see cref="git_tree_entry_bypath(Git2.TreeEntry**, Git2.Tree*, string)"/>
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_tree_entry_free(Git2.TreeEntry* entry);

    /// <summary>
    /// Get the id of the object pointed by the entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>the oid of the object</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial GitObjectID* git_tree_entry_id(Git2.TreeEntry* entry);

    /// <summary>
    /// Get the filename of a tree entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>the name of the file</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial byte* git_tree_entry_name(Git2.TreeEntry* entry);

    /// <summary>
    /// Convert a tree entry to the git_object it points to
    /// </summary>
    /// <param name="object_out">pointer to the converted object</param>
    /// <param name="repo">repository where to lookup the pointed object</param>
    /// <param name="entry">a tree entry</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// You must call <see cref="git_object_free"/> on the object when you are done with it.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tree_entry_to_object(Git2.Object** object_out, Git2.Repository* repo, Git2.TreeEntry* entry);

    /// <summary>
    /// Get the type of the object pointed by the entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>the type of the pointed object</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial GitObjectType git_tree_entry_type(Git2.TreeEntry* entry);

    /// <summary>
    /// Get the number of entries listed in a tree
    /// </summary>
    /// <param name="tree">a previously loaded tree</param>
    /// <returns>the number of entries in the tree</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial nuint git_tree_entrycount(Git2.Tree* tree);

    /// <summary>
    /// Close an open tree
    /// </summary>
    /// <param name="tree">The tree to close</param>
    /// <remarks>
    /// You can no longer use the git_tree pointer after this call.
    /// 
    /// IMPORTANT: You MUST call this method when you stop using a tree to release memory. Failure to do so will cause a memory leak.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_tree_free(Git2.Tree* tree);

    /// <summary>
    /// Get the id of a tree
    /// </summary>
    /// <param name="tree">a previously loaded tree</param>
    /// <returns>object identity for the tree</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial GitObjectID* git_tree_id(Git2.Tree* tree);

    /// <summary>
    /// Lookup a tree object from the repository
    /// </summary>
    /// <param name="tree_out">pointer to the looked up tree</param>
    /// <param name="repository">the repo to use when locating the tree</param>
    /// <param name="id">identity of the tree to locate</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tree_lookup(Git2.Tree** tree_out, Git2.Repository* repository, GitObjectID* id);

    /// <summary>
    /// Lookup a tree object from the repository, given a prefix of its identifier (short id).
    /// </summary>
    /// <param name="tree_out">pointer to the looked up tree</param>
    /// <param name="repository">the repo to use when locating the tree</param>
    /// <param name="id">identity of the tree to locate</param>
    /// <param name="len">the length of the short identifier</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tree_lookup_prefix(Git2.Tree** tree_out, Git2.Repository* repository, GitObjectID* id, nuint len);

    /// <summary>
    /// Get the repository that contains the tree
    /// </summary>
    /// <param name="tree">A previously loaded tree</param>
    /// <returns>Repository that contains this tree</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    public static partial Git2.Repository* git_tree_owner(Git2.Tree* tree);

    /// <summary>
    /// Traverse the entries in a tree and its subtrees in post or pre order.
    /// </summary>
    /// <param name="tree">The tree to walk</param>
    /// <param name="mode">Traversal mode (pre or post-order)</param>
    /// <param name="callback">Function to call on each tree entry</param>
    /// <param name="payload">Opaque pointer to be passed on each callback</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The entries will be traversed in the specified order, children subtrees will be automatically loaded as required,
    /// and the <paramref name="callback"/> will be called once per entry with the current (relative) root for the entry and the entry data itself.
    /// 
    /// If the callback returns a positive value, the passed entry will be skipped on the traversal (in pre mode). A negative value stops the walk.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_tree_walk(Git2.Tree* tree, GitTreeWalkMode mode, delegate* unmanaged[Cdecl]<byte*, Git2.TreeEntry*, nint, int> callback, nint payload);
    #endregion

    #region Tree Builder
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_treebuilder_clear(Git2.TreeBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint git_treebuilder_entrycount(Git2.TreeBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="filter"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_treebuilder_filter(
        Git2.TreeBuilder* builder,
        delegate* unmanaged[Cdecl]<Git2.TreeEntry*, nint, int> filter,
        nint payload);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_treebuilder_free(Git2.TreeBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="filename"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Git2.TreeEntry* git_treebuilder_get(Git2.TreeBuilder* builder, string filename);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry_out"></param>
    /// <param name="builder"></param>
    /// <param name="filename"></param>
    /// <param name="id"></param>
    /// <param name="filemode"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_treebuilder_insert(
        Git2.TreeEntry** entry_out,
        Git2.TreeBuilder* builder,
        string filename,
        GitObjectID* id,
        GitFileMode filemode);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder_out"></param>
    /// <param name="repository"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_treebuilder_new(
        Git2.TreeBuilder** builder_out,
        Git2.Repository* repository,
        Git2.Tree* source);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="filename"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_treebuilder_remove(
        Git2.TreeBuilder* builder,
        string filename);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_treebuilder_write(
        GitObjectID* id_out,
        Git2.TreeBuilder* builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id_out"></param>
    /// <param name="builder"></param>
    /// <param name="tree"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_treebuilder_write_with_buffer(
        GitObjectID* id_out,
        Git2.TreeBuilder* builder,
        Native.GitBuffer* tree);
    #endregion

    #region Worktree
    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree_out"></param>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <param name="path"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_add(
        Git2.Worktree** worktree_out,
        Git2.Repository* repository,
        string name,
        string path,
        Native.GitWorktreeAddOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree"></param>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_worktree_free(Git2.Worktree* worktree);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reason"></param>
    /// <param name="worktree"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_is_locked(Native.GitBuffer* reason, Git2.Worktree* worktree);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_is_prunable(
        Git2.Worktree* worktree,
        Native.GitWorktreePruneOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="names_out"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_list(
        Native.GitStringArray* names_out,
        Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_lock(
        Git2.Worktree* worktree,
        string reason);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree_out"></param>
    /// <param name="repository"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_lookup(
        Git2.Worktree** worktree_out,
        Git2.Repository* repository,
        string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_worktree_name(Git2.Worktree* worktree);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree_out"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_open_from_repository(
        Git2.Worktree** worktree_out,
        Git2.Repository* repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* git_worktree_path(Git2.Worktree* worktree);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_prune(
        Git2.Worktree* worktree,
        Native.GitWorktreePruneOptions* options);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_unlock(Git2.Worktree* worktree);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worktree"></param>
    /// <returns></returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial GitError git_worktree_validate(Git2.Worktree* worktree);
    #endregion

    #region Disposal
    /// <summary>
    /// 
    /// </summary>
    /// <param name="commitarray"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_commitarray_dispose(Git2.CommitArray* commitarray);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="strarray"></param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void git_strarray_dispose(Native.GitStringArray* strarray);

    #endregion
}
