using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SharpGit2.Managed.Config.Backend;


internal sealed class ConfigListEntry : GitConfigEntry
{
    public required IGitConfigBackend Backend;

    public ConfigListEntry(string name, string? value, string backendType, string originPath, uint includeDepth, GitConfigLevel level) : base(name, value, backendType, originPath, includeDepth, level)
    {
    }

    public ConfigListEntry Duplicate()
    {
        return new ConfigListEntry(this.Name, this.Value, this.BackendType, this.OriginPath, this.IncludeDepth, this.Level)
        {
            Backend = this.Backend
        };
    }
}

internal struct ConfigMapEntryHead
{
    public ConfigListEntry Entry;
    public bool Multivar;
}

internal sealed class ConfigEntryList
{
    public ConfigEntryList? Next;
    public ConfigEntryList? Last;
    public required ConfigListEntry Entry;
}

internal sealed class ConfigList
{
    //internal HashSet<string> Strings = [];
    internal Dictionary<string, ConfigMapEntryHead> Map = [];
    internal ConfigEntryList? Entries;

    public ConfigList()
    {
    }

    public ConfigList Duplicate()
    {
        var newList = new ConfigList();

        foreach (var entry in this.Enumerate())
        {
            newList.Append(entry.Duplicate());
        }

        return newList;
    }

    public void Append(ConfigListEntry entry)
    {
        ref var map_head = ref CollectionsMarshal.GetValueRefOrAddDefault(Map, entry.Name, out bool existed);

        if (existed)
        {
            map_head.Multivar = true;
        }

        map_head.Entry = entry;

        var list_head = new ConfigEntryList()
        {
            Entry = entry,
        };

        if (this.Entries is { } tmp)
        {
            tmp.Last!.Next = list_head;
        }
        else
        {
            this.Entries = list_head;
        }

        this.Entries.Last = list_head;
    }

    public bool TryGet(string key, [NotNullWhen(true)] out ConfigListEntry? value)
    {
        if (!this.Map.TryGetValue(key, out var tmp))
        {
            value = null;
            return false;
        }

        value = tmp.Entry;
        return true;
    }

    public bool TryGetUnique(string key, [NotNullWhen(true)] out ConfigListEntry? value, bool throwIfNotUnique)
    {
        if (!this.Map.TryGetValue(key, out var tmp))
        {
            goto fail;
        }

        if (tmp.Multivar)
        {
            if (throwIfNotUnique)
                throw new Git2Exception("Entry is not unique due to being a multivar");

            goto fail;
        }

        if (tmp.Entry.IncludeDepth > 0)
        {
            if (throwIfNotUnique)
                throw new Git2Exception("Entry is not unique due to being included");

            goto fail;
        }

        value = tmp.Entry;
        return true;

    fail:
        value = null;
        return false;
    }

    public IEnumerable<ConfigListEntry> Enumerate()
    {
        for (var head = this.Entries; head != null; head = head.Next)
        {
            yield return head.Entry;
        }
    }
}

internal sealed class ConfigList2 : IEnumerable<ConfigListEntry>
{
    //internal HashSet<string> Strings = [];
    internal readonly Dictionary<string, ConfigMapEntryHead> Map = [];
    internal readonly LinkedList<ConfigListEntry> Entries = new();

    public ConfigList2()
    {
    }

    public ConfigList Duplicate()
    {
        var newList = new ConfigList();

        foreach (var entry in this)
        {
            newList.Append(entry.Duplicate());
        }

        return newList;
    }

    public void Append(ConfigListEntry entry)
    {
        ref var map_head = ref CollectionsMarshal.GetValueRefOrAddDefault(Map, entry.Name, out bool existed);

        if (existed)
        {
            map_head.Multivar = true;
        }

        map_head.Entry = entry;

        this.Entries.AddLast(entry);
    }

    public bool TryGet(string key, [NotNullWhen(true)] out ConfigListEntry? value)
    {
        if (!this.Map.TryGetValue(key, out var tmp))
        {
            value = null;
            return false;
        }

        value = tmp.Entry;
        return true;
    }

    public bool TryGetUnique(string key, [NotNullWhen(true)] out ConfigListEntry? value, bool throwIfNotUnique)
    {
        if (!this.Map.TryGetValue(key, out var tmp))
        {
            goto fail;
        }

        if (tmp.Multivar)
        {
            if (throwIfNotUnique)
                throw new Git2Exception("Entry is not unique due to being a multivar");

            goto fail;
        }

        if (tmp.Entry.IncludeDepth > 0)
        {
            if (throwIfNotUnique)
                throw new Git2Exception("Entry is not unique due to being included");

            goto fail;
        }

        value = tmp.Entry;
        return true;

    fail:
        value = null;
        return false;
    }

    public IEnumerator<ConfigListEntry> GetEnumerator()
    {
        return this.Entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

internal struct ConfigList3
{
    internal Dictionary<string, (ConfigListEntry Entry, bool Multivar)> Map;
    internal LinkedList<ConfigListEntry> Entries;

    public ConfigList3()
    {
        Map = new();
        Entries = new();
    }
}