using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using CommunityToolkit.HighPerformance.Buffers;
using Mono.Unix.Native;
using SharpGit2.Managed.Attributes;
using SharpGit2.Managed.Config;

namespace SharpGit2.Managed.Filter;

internal sealed class GitCrlfFilter : IGitFilter
{
    public string Attributes => "crlf eol text";
    
    public void Initialize()
    {
    }

    public void Dispose()
    {
    }

    public bool ShouldFilter(
        GitFilterSource source,
        IReadOnlyDictionary<string, GitAttributeValue>? attributes,
        ref object? context)
    {
        var autoCrlf = source.Repository.ConfigMapLookup(GitConfigMapItem.AutoCRLF);
        var safeCrlf = source.Repository.ConfigMapLookup(GitConfigMapItem.SafeCRLF);
        var coreEol = source.Repository.ConfigMapLookup(GitConfigMapItem.EOL);
        
        if ((source.Options.Flags & GitFilterFlags.AllowUnsafe) != 0
            && safeCrlf == GitConfigMapValues.SafeCrlfFail)
            safeCrlf = GitConfigMapValues.SafeCrlfWarn;
        
        CrlfType crlf_action = 0, attr_action = 0;

        if (attributes == null || attributes.Count == 0)
        {
            crlf_action = CrlfType.Undefined;
        }
        else
        {
            crlf_action = CheckCrlf(attributes["text"]);
            
            if (crlf_action == CrlfType.Undefined)
                crlf_action = CheckCrlf(attributes["crlf"]);

            if (crlf_action != CrlfType.Binary)
            {
                int eol = attributes["eol"] switch
                {
                    { IsString: true, String: "lf" } => GitConfigMapValues.Eol_LF,
                    { IsString: true, String: "crlf" } => GitConfigMapValues.Eol_CRLF,
                    _ => GitConfigMapValues.Eol_Unset
                };

                if (crlf_action == CrlfType.Auto)
                {
                    if (eol == GitConfigMapValues.Eol_LF)
                        crlf_action = CrlfType.AutoInput;
                    else if (eol == GitConfigMapValues.Eol_CRLF)
                        crlf_action = CrlfType.AutoCrlf;
                }
                else if (eol == GitConfigMapValues.Eol_LF)
                {
                    crlf_action = CrlfType.TextInput;
                }
                else if (eol == GitConfigMapValues.Eol_CRLF)
                {
                    crlf_action = CrlfType.TextCrlf;
                }
                
                attr_action = crlf_action;
            }

            if (crlf_action == CrlfType.Text)
            {
                crlf_action = TextEolIsCRLF(autoCrlf, coreEol) ? CrlfType.TextCrlf : CrlfType.TextInput;
            }
            else if (crlf_action == CrlfType.Undefined)
            {
                switch (autoCrlf)
                {
                    case GitConfigMapValues.AutoCrlfFalse:
                        crlf_action = CrlfType.Binary;
                        break;
                    case GitConfigMapValues.AutoCrlfTrue:
                        crlf_action = CrlfType.AutoCrlf;
                        break;
                    case GitConfigMapValues.AutoCrlfInput:
                        crlf_action = CrlfType.AutoInput;
                        break;
                    default:
                        throw new Git2Exception("Internal Exception! A bug-check has failed!");
                }
            }
        }

        if (crlf_action == CrlfType.Binary)
            return false;

        context = new FilterContext()
        {
            attr_action = attr_action,
            crlf_action = crlf_action,
            auto_crlf = autoCrlf,
            core_eol = coreEol,
            safe_crlf = safeCrlf
        };
        return true;
    }

    private static bool TextEolIsCRLF(int autoCRLF, int coreEol)
    {
        return autoCRLF == GitConfigMapValues.AutoCrlfTrue
               || (autoCRLF != GitConfigMapValues.AutoCrlfInput
                   && (coreEol == GitConfigMapValues.Eol_CRLF
                       || (coreEol == GitConfigMapValues.Eol_Unset && OperatingSystem.IsWindows())));
    }
    
    public Stream WriteStream(GitFilterSource source, Stream next, ref object? context)
    {
        Debug.Assert(next.CanWrite);
        
        var filterContext = (FilterContext?)context;

        if (filterContext == null)
        {
            if (!this.ShouldFilter(source, null, ref context)) // This filter has been determined it should be inactive, so return the next stream.
                return next;
            
            Debug.Assert(context != null);
            filterContext = (FilterContext)context!;
        }

        Func<ReadOnlySpan<byte>, IBufferWriter<byte>, bool> applyFunc = source.Mode == GitFilterMode.ToWorktree
            ? (input, output) =>
            {
                if (filterContext.OutputEOL() != GitConfigMapValues.Eol_CRLF)
                {
                    return false;
                }
            
                bool isBinary = Utilities.GatherTextStats(input, false, out var stats);
                
                if (stats.LF == 0 || stats.LF == stats.CRLF)
                {
                    return false;
                }

                if (filterContext.crlf_action is CrlfType.Auto or CrlfType.AutoInput or CrlfType.AutoCrlf)
                {
                    if (stats.CR > 0 || isBinary)
                    {
                        return false;
                    }
                }
                
                Utilities.FromLfToCrlf(input, output);
                return true;
            }
            : (input, output) =>
            {
                if (filterContext.crlf_action == CrlfType.Binary)
                {
                    // Binary attribute? Nothing to do. Write through.
                    return false;
                }
            
                bool isBinary = Utilities.GatherTextStats(input, false, out var stats);

                if (filterContext.crlf_action is CrlfType.Auto or CrlfType.AutoInput or CrlfType.AutoCrlf)
                {
                    // If the file in the index has any CR in it, do not convert.
                    // This is the new safer autocrlf handling.
                    if (isBinary || HasCRInIndex(source))
                    {
                        return false;
                    }
                }
            
                CheckSafeCrlf(filterContext, source, ref stats);

                if (stats.CRLF == 0)
                {
                    return false;
                }

                Debug.Assert(input.IndexOf("\r\n"u8) >= 0);

                bool success = Utilities.TryFromCrlfToLf(input, output.GetSpan(input.Length), out int written);
                Debug.Assert(success);
                
                output.Advance(written);
                
                return true;
            };
        
        return new GitFilterBufferedStream(source, next, applyFunc);
    }
    
    private static bool HasCRInIndex(GitFilterSource source)
    {
        var repo = source.Repository;
        var path = source.Path;

        if (path == null)
            return false;

        GitIndex index;
        try
        {
            index = repo.Index;
        }
        catch
        {
            return false;
        }

        if (!index.TryGetByPath(path, 0, out GitIndexEntry entry) && !index.TryGetByPath(path, 1, out entry))
        {
            return false;
        }

        if (((FilePermissions)entry.Mode & FilePermissions.S_IFMT) != FilePermissions.S_IFREG)
        {
            // Don't filter non-blobs
            return true;
        }

        var blob = repo.Objects.LookupBlob(in entry.Id);

        if (blob == null)
            return false;

        if (blob.TryGetSpan(out var span))
        {
            return span.Contains((byte)'\r');
        }

        using var stream = blob.GetContentStream();

        byte[] array = ArrayPool<byte>.Shared.Rent(128 * 1024);
        try
        {
            int read;
            while ((read = stream.Read(array, 0, array.Length)) > 0)
            {
                if (array.AsSpan(0, read).Contains((byte)'\r'))
                {
                    return true;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        return false;
    }

    private static void CheckSafeCrlf(FilterContext context, GitFilterSource source, ref Utilities.TextStats stats)
    {
        if (context.safe_crlf == GitConfigMapValues.SafeCrlfFalse)
            return;

        string filename = source.Path;

        switch (context.OutputEOL())
        {
            case GitConfigMapValues.Eol_LF:
                // CRLFs would not be restored by checkout, check if we'd remove CRLFs
                if (stats.CRLF != 0)
                {
                    if (context.safe_crlf != GitConfigMapValues.SafeCrlfWarn)
                    {
                        throw new Git2Exception(
                            string.IsNullOrEmpty(filename)
                                ? "CRLF would be replaced by LF"
                                : $"CRLF would be replaced by LF in '{filename}'"
                            );
                    }
                    else
                    {
                        // TODO: Issue a Warning when available
                    }
                }

                break;
            case GitConfigMapValues.Eol_CRLF:
                // CRLFs would be added by checkout, check if we have "naked" LFs
                if (stats.CRLF != stats.LF)
                {
                    if (context.safe_crlf != GitConfigMapValues.SafeCrlfWarn)
                    {
                        throw new Git2Exception(
                            string.IsNullOrEmpty(filename)
                                ? "LF would be replaced by CRLF"
                                : $"LF would be replaced by CRLF in '{filename}'"
                            );
                    }
                    else
                    {
                        // TODO: Issue a Warning when available
                    }
                }

                break;
        }
    }
    
    private static CrlfType CheckCrlf(GitAttributeValue value)
    {
        if (value.IsTrue)
        {
            return CrlfType.Text;
        }

        if (value.IsFalse)
        {
            return CrlfType.Binary;
        }

        if (value == "input")
            return CrlfType.TextInput;

        if (value == "auto")
            return CrlfType.Auto;

        // if (ReferenceEquals(value, Constants.attribute_internal_unset))
        //     ;

        return CrlfType.Undefined;
    }
    
    private enum CrlfType
    {
        Undefined,
        Binary,
        Text,
        TextInput,
        TextCrlf,
        Auto,
        AutoInput,
        AutoCrlf
    }

    private sealed class FilterContext
    {
        public CrlfType attr_action;
        public CrlfType crlf_action;

        public int auto_crlf;
        public int safe_crlf;
        public int core_eol;

        public int OutputEOL()
        {
            switch (crlf_action)
            {
                case CrlfType.Binary:
                    return GitConfigMapValues.Eol_Unset;
                case CrlfType.Undefined:
                case CrlfType.AutoCrlf:
                case CrlfType.TextCrlf:
                    return GitConfigMapValues.Eol_CRLF;
                case CrlfType.AutoInput:
                case CrlfType.TextInput:
                    return GitConfigMapValues.Eol_LF;
                case CrlfType.Auto:
                case CrlfType.Text:
                    return TextEolIsCRLF(auto_crlf, core_eol) ? GitConfigMapValues.Eol_CRLF : GitConfigMapValues.Eol_LF;
                default:
                    Debug.Fail("Unknown line ending configuration!");
                    return core_eol;
            }
        }
    }
}