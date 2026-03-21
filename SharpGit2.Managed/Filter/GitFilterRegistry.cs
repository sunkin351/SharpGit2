using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SharpGit2.Managed.Attributes;

namespace SharpGit2.Managed.Filter;

public static class GitFilterRegistry
{
    private static readonly ReaderWriterLockSlim RegistryLock = new();
    private static readonly List<GitFilterDefinition> FilterRegistry = new();
    private static readonly Dictionary<string, GitFilterDefinition> NameLookup = new();
    
    private static volatile bool _shutdown = false;
    
    private const string CrlfFilterName = "crlf";
    private const string IdentFilterName = "ident";

    static GitFilterRegistry()
    {
        Insert(CrlfFilterName, new GitCrlfFilter(), 0);
        Insert(IdentFilterName, new GitIdentFilter(), 100);
    }

    internal static void GlobalShutdown()
    {
        if (Interlocked.Exchange(ref _shutdown, true))
            return;
        
        RegistryLock.EnterWriteLock();
        try
        {
            foreach (var definition in FilterRegistry)
            {
                definition.Filter.Dispose();
            }
            
            FilterRegistry.Clear();
            NameLookup.Clear();
        }
        finally
        {
            RegistryLock.ExitWriteLock();
        }
    }

    private static void Insert(string name, IGitFilter filter, int priority)
    {
        ImmutableArray<(string, GitAttributeValue)> parsedAttributes = [];

        if (filter.Attributes is { Length: > 0 } attributesString)
        {
            var builder = ImmutableArray.CreateBuilder<(string, GitAttributeValue)>();

            ReadOnlySpan<char> attributesSpan = attributesString;
            foreach (var range in attributesSpan.Split(' '))
            {
                var span = attributesSpan[range];

                if (span.IsEmpty)
                    continue;
                
                string attributeName;
                GitAttributeValue attributeValue = default;

                int eqIdx = span.IndexOf('=');

                if (eqIdx == 0)
                    continue;

                if (eqIdx < 0)
                {
                    switch (span[0])
                    {
                        case '-':
                            attributeName = span[1..].ToString();
                            attributeValue = GitAttributeValue.False;
                            break;
                        case '+':
                            attributeName = span[1..].ToString();
                            attributeValue = GitAttributeValue.True;
                            break;
                        case '!':
                            attributeName = span[1..].ToString();
                            attributeValue = GitAttributeValue.Unspecified;
                            break;
                        default:
                            attributeName = span.ToString();
                            break;
                    }
                }
                else
                {
                    attributeName = span[..eqIdx].ToString();
                    attributeValue = span[(eqIdx + 1)..].ToString();
                }
                
                builder.Add((attributeName, attributeValue));
            }
            
            parsedAttributes = builder.ToImmutable();
        }

        var filterDefinition = new GitFilterDefinition(name, filter, priority, parsedAttributes);

        var registry = FilterRegistry;

        int idx = registry.BinarySearch(filterDefinition);

        if (idx < 0)
        {
            registry.Insert(~idx, filterDefinition);
        }
        else
        {
            int count = registry.Count;
            while (++idx < count && registry[idx].Priority == priority)
            {
            }
            
            registry.Insert(idx, filterDefinition);
        }
        
        NameLookup.Add(name, filterDefinition);
    }

    public static void Register(string name, IGitFilter filter, int priority)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(filter);
        
        RegistryLock.EnterWriteLock();
        try
        {
            if (_shutdown)
                throw new InvalidOperationException("Cannot register filter after shutdown!");
            
            if (NameLookup.ContainsKey(name))
                throw new InvalidOperationException($"Attempted to reregister existing filter '{name}'");
            
            Insert(name, filter, priority);
        }
        finally
        {
            RegistryLock.ExitWriteLock();
        }
    }

    public static void Unregister(string name)
    {
        if (_shutdown)
            return;
        
        // Do not allow the unregistration of default filters
        if (name is CrlfFilterName or IdentFilterName)
            throw new InvalidOperationException($"Cannot unregister filter '{name}'");
        
        RegistryLock.EnterWriteLock();
        try
        {
            if (!NameLookup.Remove(name, out var definition))
            {
                throw new KeyNotFoundException($"Cannot find filter '{name}' to unregister");
            }

            bool success = FilterRegistry.Remove(definition);
            Debug.Assert(success);

            definition.Filter.Dispose();
        }
        finally
        {
            RegistryLock.ExitWriteLock();
        }
    }
    
    internal static Dictionary<string, GitAttributeValue>? CheckAttributes(
        GitRepository repo,
        GitFilterSession session,
        GitFilterDefinition definition,
        GitFilterSource source)
    {
        if (definition.Attributes.IsDefaultOrEmpty)
        {
            return [];
        }

        GitAttributeOptions attributeOptions = default;

        if ((source.Options.Flags & GitFilterFlags.NoSystemAttributes) != 0)
            attributeOptions.Flags |= GitAttributeCheckFlags.NoSystem;
        
        if ((source.Options.Flags & GitFilterFlags.AttributesFromHead) != 0)
            attributeOptions.Flags |= GitAttributeCheckFlags.IncludeHead;
        
        if ((source.Options.Flags & GitFilterFlags.AttributesFromCommit) != 0)
        {
            attributeOptions.Flags |= GitAttributeCheckFlags.IncludeCommit;
            attributeOptions.AttributeCommitId = source.Options.AttributeCommitId;
        }

        string[] attributeNames = definition.Attributes.SelectToArray(x => x.name);
        var attributeValues = new GitAttributeValue[attributeNames.Length];

        int foundCount = repo.GetAttributesWithSession(
            session.AttributeSession,
            in attributeOptions,
            source.Path,
            attributeNames,
            attributeValues);

        if (foundCount != attributeNames.Length)
        {
            return null;
        }

        for (int i = 0; i < attributeValues.Length; ++i)
        {
            var wanted = definition.Attributes[i].value;
            var found = attributeValues[i];

            if (wanted.Type != found.Type)
                return null;
            
            if (wanted.Type == GitAttributeValue.ValueType.String
                 && wanted.String != found.String
                 && wanted.String is not "*")
                return null;
        }

        return attributeNames.Zip(attributeValues, KeyValuePair.Create).ToDictionary();
    }

    internal static GitFilterList? LoadFilterList(
        GitRepository repo,
        GitBlob? blob,
        string path,
        GitFilterMode mode,
        GitFilterSession filterSession)
    {
        RegistryLock.EnterReadLock();
        try
        {
            var source = new GitFilterSource()
            {
                Repository = repo,
                Path = path,
                Mode = mode,
                Options = filterSession.Options
            };

            if (blob != null)
            {
                source.Oid = blob.Oid;
            }

            GitFilterList? filterList = null;

            foreach (var filterDefinition in FilterRegistry)
            {
                object? context = null;
                Dictionary<string, GitAttributeValue>? attributes = null;
                
                if (!filterDefinition.Attributes.IsDefaultOrEmpty)
                {
                    attributes = CheckAttributes(repo, filterSession, filterDefinition, source);

                    if (attributes == null)
                        continue;
                }
                
                filterDefinition.EnsureInitialized();

                if (filterDefinition.Filter.ShouldFilter(source, attributes, ref context))
                {
                    filterList ??= new GitFilterList(source);

                    filterList.Add(filterDefinition.FilterName, filterDefinition.Filter, context);
                }
            }

            return filterList;
        }
        finally
        {
            RegistryLock.ExitReadLock();
        }
    }

    public static bool TryLookup(string name, [NotNullWhen(true)] out IGitFilter? filter)
    {
        if (NameLookup.TryGetValue(name, out var definition))
        {
            filter = definition.Filter;
            return true;
        }

        filter = null;
        return false;
    }
}