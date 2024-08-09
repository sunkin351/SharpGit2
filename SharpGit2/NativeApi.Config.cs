using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2;

internal unsafe static partial class NativeApi
{
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
    internal static partial GitError git_config_add_file_ondisk(Git2.Config* config, string path, GitConfigLevel level, Git2.Repository* repository, int force);

    // TODO: Docs
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_config_backend_foreach_match(Git2.ConfigBackend* config, string? regex, delegate* unmanaged[Cdecl]<GitConfigEntry.Unmanaged*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Delete a config variable from the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">the configuration</param>
    /// <param name="name">the variable to delete</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_config_delete_entry(Git2.Config* config, string name);

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
    internal static partial GitError git_config_delete_multivar(Git2.Config* config, string name, string regex);

    /// <summary>
    /// Free a config entry
    /// </summary>
    /// <param name="entry">The entry to free.</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_config_entry_free(GitConfigEntry.Unmanaged* entry);

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
    internal static partial GitError git_config_find_global(Git2.Buffer* outPath);

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
    internal static partial GitError git_config_find_programdata(Git2.Buffer* outPath);

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
    internal static partial GitError git_config_find_system(Git2.Buffer* outPath);

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
    internal static partial GitError git_config_find_xdg(Git2.Buffer* outPath);

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
    internal static partial GitError git_config_foreach(Git2.Config* config, delegate* unmanaged[Cdecl]<GitConfigEntry.Unmanaged*, nint, GitError> callback, nint payload);

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
    internal static partial GitError git_config_foreach_match(Git2.Config* config, string regex, delegate* unmanaged[Cdecl]<GitConfigEntry.Unmanaged*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Free the configuration and its associated memory and files
    /// </summary>
    /// <param name="config">the configuration to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_config_free(Git2.Config* config);

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
    internal static partial GitError git_config_get_bool(int* value, Git2.Config* config, string name);

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
    internal static partial GitError git_config_get_entry(GitConfigEntry.Unmanaged** value, Git2.Config* config, string name);

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
    internal static partial GitError git_config_get_int32(int* value, Git2.Config* config, string name);

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
    internal static partial GitError git_config_get_int64(long* value, Git2.Config* config, string name);


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
    internal static partial GitError git_config_get_mapped(int* value, Git2.Config* config, string name, Git2.ConfigMap* map, nuint map_n);

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
    internal static partial GitError git_config_get_multivar_foreach(Git2.Config* config, string name, string? regex, delegate* unmanaged[Cdecl]<GitConfigEntry.Unmanaged*, nint, GitError> callback, nint payload);

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
    internal static partial GitError git_config_get_path(Git2.Buffer* buffer, Git2.Config* config, string name);

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
    internal static partial GitError git_config_get_string(byte** value, Git2.Config* config, string name);

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
    internal static partial GitError git_config_get_string_buf(Git2.Buffer* value, Git2.Config* config, string name);

    /// <summary>
    /// Free a config iterator
    /// </summary>
    /// <param name="iterator">the iterator to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_config_iterator_free(Git2.ConfigIterator* iterator);

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
    internal static partial GitError git_config_iterator_glob_new(Git2.ConfigIterator** iterator, Git2.Config* config, string regex);

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
    internal static partial GitError git_config_iterator_new(Git2.ConfigIterator** iterator, Git2.Config* config);

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
    internal static partial GitError git_config_lock(Git2.Transaction** transaction, Git2.Config* config);

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
    internal static partial GitError git_config_lookup_map_value(int* map_value, Git2.ConfigMap* maps, nuint map_n, byte* value);

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
    internal static partial GitError git_config_multivar_iterator_new(Git2.ConfigIterator** iterator, Git2.Config* config, string name, string? regex);

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
    internal static partial GitError git_config_new(Git2.Config** @out);

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
    internal static partial GitError git_config_next(GitConfigEntry.Unmanaged** @out, Git2.ConfigIterator* iterator);

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
    internal static partial GitError git_config_open_default(Git2.Config** @out);

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
    internal static partial GitError git_config_open_global(Git2.Config** @out, Git2.Config* config);

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
    internal static partial GitError git_config_open_level(Git2.Config** @out, Git2.Config* parent, GitConfigLevel level);

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
    internal static partial GitError git_config_open_ondisk(Git2.Config** @out, string path);

    /// <summary>
    /// Set the value of a boolean config variable in the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="value">the value to store</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_config_set_bool(Git2.Config* config, string name, int value);

    /// <summary>
    /// Set the value of an integer config variable in the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="value">Integer value for the variable</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_config_set_int32(Git2.Config* config, string name, int value);

    /// <summary>
    /// Set the value of a long integer config variable in the config file with the highest level (usually the local one).
    /// </summary>
    /// <param name="config">where to look for the variable</param>
    /// <param name="name">the variable's name</param>
    /// <param name="value">Long integer value for the variable</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_config_set_int64(Git2.Config* config, string name, long value);

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
    internal static partial GitError git_config_set_multivar(Git2.Config* config, string name, string regex, string value);

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
    internal static partial GitError git_config_set_string(Git2.Config* config, string name, string value);

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
    internal static partial GitError git_config_snapshot(Git2.Config** @out, Git2.Config* config);
}
