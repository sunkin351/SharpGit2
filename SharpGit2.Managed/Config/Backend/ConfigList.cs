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

internal struct ConfigMapEntryHead<T> where T: GitConfigEntry
{
    public T Entry;
    public bool Multivar;
}