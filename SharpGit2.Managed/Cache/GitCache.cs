using System.Runtime.CompilerServices;
using BitFaster.Caching.Lru;

namespace SharpGit2.Managed.Cache;

internal enum GitCacheStore
{
    Any = 0,
    Raw = 1,
    Parsed = 2
}

internal sealed class GitCache
{
    private static readonly int[] MaxObjectSize = [
        0,      /* GIT_OBJECT__EXT1 */
        4096,   /* GIT_OBJECT_COMMIT */
        4096,   /* GIT_OBJECT_TREE */
        0,      /* GIT_OBJECT_BLOB */
        4096,   /* GIT_OBJECT_TAG */
        0,      /* GIT_OBJECT__EXT2 */
        0,      /* GIT_OBJECT_OFS_DELTA */
        0       /* GIT_OBJECT_REF_DELTA */
    ];

    private static volatile ObjectTypeMask AllowedObjects = ObjectTypeMask.Commit | ObjectTypeMask.Tree | ObjectTypeMask.Tag;

    internal static void SetMaxObjectSize(GitObjectType type, int size)
    {
        if ((uint)type >= (uint)MaxObjectSize.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(size);

        MaxObjectSize[(int)type] = size;

#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
        if (size > 0)
        {
            Interlocked.Or(
                ref Unsafe.As<ObjectTypeMask, uint>(ref AllowedObjects),
                1u << (int)type);
        }
        else
        {
            Interlocked.And(
                ref Unsafe.As<ObjectTypeMask, uint>(ref AllowedObjects),
                ~(1u << (int)type));
        }
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
    }

    private readonly ClassicLru<GitObjectID, GitObject> _map = new(1, 128, EqualityComparer<GitObjectID>.Default);
    private readonly Lock _writeLock = new();

    internal GitCache()
    {
    }

    public void Clear()
    {
        _map.Clear();
    }

    private static bool ShouldStore(GitObjectType type, int objectSize)
    {
        return (AllowedObjects & (ObjectTypeMask)(1u << (int)type)) != 0 && MaxObjectSize[(int)type] >= objectSize;
    }

    private GitObject? CacheGet(in GitObjectID oid, GitCacheStore flags)
    {
        if (_map.TryGet(oid, out var obj) && (flags == GitCacheStore.Any || obj.Store == flags))
        {
            return obj;
        }

        return null;
    }

    private GitObject CacheStore(GitObject entry)
    {
        if (!ShouldStore(entry.ObjectType, entry.ObjectSize))
        {
            return entry;
        }

        // Locking over a concurrent collection because this operation can't be atomic with the collection's current API surface.
        lock (_writeLock)
        {
            if (_map.TryGet(entry.Oid, out var existingEntry))
            {
                if (entry.Store == existingEntry.Store)
                {
                    return existingEntry;
                }
                else if (existingEntry.Store == GitCacheStore.Raw && entry.Store == GitCacheStore.Parsed)
                {
                    _map.AddOrUpdate(entry.Oid, entry);
                }
            }
            else
            {
                _map.AddOrUpdate(entry.Oid, entry);
            }
        }

        return entry;
    }

    public GitObject StoreRaw(GitObject entry)
    {
        entry.Store = GitCacheStore.Raw;
        return CacheStore(entry);
    }

    public GitObject StoreParsed(GitObject entry)
    {
        entry.Store = GitCacheStore.Parsed;
        return CacheStore(entry);
    }

    public bool TryGetRaw(in GitObjectID oid, out GitObject? value)
    {
        return (value = CacheGet(in oid, GitCacheStore.Raw)) != null;
    }

    public bool TryGetParsed(in GitObjectID oid, out GitObject? value)
    {
        return (value = CacheGet(in oid, GitCacheStore.Parsed)) != null;
    }

    public bool TryGetAny(in GitObjectID oid, out GitObject? value)
    {
        return (value = CacheGet(in oid, GitCacheStore.Any)) != null;
    }

    [Flags]
    private enum ObjectTypeMask : uint
    {
        Commit = 1 << (int)GitObjectType.Commit,
        Tree = 1 << (int)GitObjectType.Tree,
        Blob = 1 << (int)GitObjectType.Blob,
        Tag = 1 << (int)GitObjectType.Tag,
        OffsetDelta = 1 << (int)GitObjectType.Offset_Delta,
        REFDelta = 1 << (int)GitObjectType.REF_Delta,
    }
}

