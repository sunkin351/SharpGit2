using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SharpGit2.Managed;

internal struct ValueList<T>
{
    private T[] _array;
    private int _count;

    public ValueList()
    {
        _array = [];
        _count = 0;
    }

    public ValueList(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _array = capacity == 0 ? [] : new T[capacity];
        _count = 0;
    }

    public readonly int Count => _count;

    public int Capacity
    {
        readonly get => _array?.Length ?? 0;
        [MemberNotNull(nameof(_array))]
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _count);

            var array = _array;

            if (array is null)
            {
                _array = value > 0 ? new T[value] : [];
            }
            else if (array.Length != value)
            {
                if (value > 0)
                {
                    var tmp = new T[value];
                    Array.Copy(array, tmp, _count);

                    _array = tmp;
                }
                else
                {
                    _array = [];
                }
            }
        }
    }

    public readonly ref T this[int i]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)i, (uint)_count);

            Debug.Assert(_array is not null && (uint)_count <= (uint)_array.Length);

            return ref _array[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(T value)
    {
        var array = _array;
        var count = _count;

        if (array is not null && (uint)count < (uint)array.Length)
        {
            _count = count + 1;
            array[count] = value;
            return count;
        }
        else
        {
            return AddWithResize(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int AddWithResize(T value)
    {
        Debug.Assert(_count == _array.Length);

        int count = _count;
        Grow(count + 1);

        _count = count + 1;
        _array[count] = value;
        return count;
    }

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            int count = _count;
            _count = 0;

            if (count > 0)
            {
                Array.Clear(_array, 0, count);
            }
        }
        else
        {
            _count = 0;
        }
    }

    [MemberNotNull(nameof(_array))]
    public int EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        if (_array is null || _array.Length < capacity)
        {
            Grow(capacity);
        }

        return _array.Length;
    }

    public Span<T> GetSpan()
    {
        return _array.AsSpan(0, _count);
    }

    [MemberNotNull(nameof(_array))]
    private void Grow(int newCap)
    {
        this.Capacity = GetNewCapacity(newCap);
    }

    /*
     * Taken directly from List<T>'s implementation
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetNewCapacity(int capacity)
    {
        Debug.Assert(_array is null || _array.Length < capacity);

        int newCapacity = _array is null || _array.Length == 0 ? 4 : 2 * _array.Length;

        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newCapacity > Array.MaxLength)
            newCapacity = Array.MaxLength;

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newCapacity < capacity)
            newCapacity = capacity;

        return newCapacity;
    }
}
