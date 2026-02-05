using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;

using CommunityToolkit.HighPerformance.Helpers;

namespace SharpGit2.Managed;

[Flags]
internal enum XDiffFlags
{
    None = 0,

    NeedMinimal = 1 << 0,
    IgnoreWhitespace = 1 << 1,
    IgnoreWhitespaceChange = 1 << 2,
    IgnoreWhitespaceAtEOL = 1 << 3,
    IgnoreCRAtEOL = 1 << 4,

    IgnoreBlankLines = 1 << 7,

    PatienceDiff = 1 << 14,
    HistogramDiff = 1 << 15,

    IndentHeuristic = 1 << 23,
}

internal static class XDiff
{
    public static void RunDiff(
        ReadOnlySpan<char> file1, ReadOnlySpan<char> file2, XDiffFlags flags,
        EmitConfiguration<char> config, IEmitCallbacks<char> callbacks,
        Regex? ignoreRegex = null, IEnumerable<string>? anchors = null)
    {
        var context = new XDiffContext<char>(flags);

        context.Initialize(file1, file2);

        switch (flags & XDL.AlgorithmMask)
        {
            case XDiffFlags.PatienceDiff:
                RunPatienceDiff(ref context, GetIsAnchorFunc(anchors));
                break;
            case XDiffFlags.HistogramDiff:
                RunHistogramDiff(ref context);
                break;
            default:
                RunMeyersDiff(ref context);
                break;
        }

        CompactChanges(ref context.Xdf1, context.File1, ref context.Xdf2, context.File2, flags);
        CompactChanges(ref context.Xdf2, context.File2, ref context.Xdf1, context.File1, flags);

        var changes = BuildScript(context.Xdf1.RChange, context.Xdf2.RChange, context.Xdf1.Records.Count, context.Xdf2.Records.Count);

        if (changes is not null)
        {
            if ((flags & XDiffFlags.IgnoreBlankLines) != 0)
            {
                context.MarkIgnorableLines(changes);
            }

            if (ignoreRegex is not null)
            {
                MarkIgnorableRegex(changes, ref context, ignoreRegex);
            }

            context.Emit(changes, config, callbacks);
        }

        // optimized anchor identification
        static Func<ReadOnlySpan<char>, bool>? GetIsAnchorFunc(IEnumerable<string>? anchors)
        {
            if (anchors is null)
                return null;

            string singleValue;

            if (anchors is HashSet<string> set)
            {
                switch (set.Count)
                {
                    case 0:
                        return null;
                    case 1:
                        singleValue = set.First();

                        var comparer = set.Comparer;

                        if (comparer == EqualityComparer<string>.Default)
                        {
                            goto single_anchor;
                        }
                        else
                        {
                            var alternateComparer = (IAlternateEqualityComparer<ReadOnlySpan<char>, string>)comparer;
                            return (span) =>
                            {
                                return alternateComparer.Equals(span, singleValue);
                            };
                        }
                }
            }
            else
            {
                using var enumerator = anchors.GetEnumerator();
                
                if (!enumerator.MoveNext())
                    return null;

                singleValue = enumerator.Current;

                if (!enumerator.MoveNext())
                    goto single_anchor;

                set = [singleValue, enumerator.Current];

                while (enumerator.MoveNext())
                {
                    set.Add(enumerator.Current);
                }
            }

            return set.GetAlternateLookup<ReadOnlySpan<char>>().Contains;

            // Ensures we don't have multiple instances of the same lambda body
        single_anchor:
            return (span) =>
            {
                return span.SequenceEqual(singleValue); // StringComparison.Ordinal
            };
        }

        static void MarkIgnorableRegex(TextChange script, ref XDiffContext<char> context, Regex ignoreRegex)
        {
            ReadOnlySpan<XRecord> records1 = context.Xdf1.Records.GetSpan();
            ReadOnlySpan<XRecord> records2 = context.Xdf2.Records.GetSpan();

            ReadOnlySpan<char> fileData1 = context.File1;
            ReadOnlySpan<char> fileData2 = context.File2;

            for (TextChange? xch = script; xch is not null; xch = xch.Next)
            {
                if (xch.Ignore)
                    continue;

                bool ignore = true;
                var recs = records1.Slice(xch.Index1, xch.Change1);

                for (int i = 0; i < recs.Length && ignore; ++i)
                {
                    ref readonly var record = ref recs[i];

                    ignore = ignoreRegex.IsMatch(fileData1.Slice(record.LineOffset, record.LineLength));
                }

                if (ignore)
                {
                    recs = records2.Slice(xch.Index2, xch.Change2);

                    for (int i = 0; i < recs.Length && ignore; ++i)
                    {
                        ref readonly var record = ref recs[i];

                        ignore = ignoreRegex.IsMatch(fileData2.Slice(record.LineOffset, record.LineLength));
                    }
                }

                xch.Ignore = ignore;
            }
        }
    }

    public static void RunDiff(
        ReadOnlySpan<byte> utf8File1, ReadOnlySpan<byte> utf8File2, XDiffFlags flags,
        EmitConfiguration<byte> config, IEmitCallbacks<byte> callbacks, IEnumerable<ImmutableArray<byte>>? utf8Anchors = null)
    {
        var context = new XDiffContext<byte>(flags);

        context.Initialize(utf8File1, utf8File2);

        switch (flags & XDL.AlgorithmMask)
        {
            case XDiffFlags.PatienceDiff:
                RunPatienceDiff(ref context, GetIsAnchorFunc(utf8Anchors));
                break;
            case XDiffFlags.HistogramDiff:
                RunHistogramDiff(ref context);
                break;
            default:
                RunMeyersDiff(ref context);
                break;
        }

        CompactChanges(ref context.Xdf1, context.File1, ref context.Xdf2, context.File2, flags);
        CompactChanges(ref context.Xdf2, context.File2, ref context.Xdf1, context.File1, flags);

        var changes = BuildScript(context.Xdf1.RChange, context.Xdf2.RChange, context.Xdf1.Records.Count, context.Xdf2.Records.Count);

        if (changes is not null)
        {
            if ((flags & XDiffFlags.IgnoreBlankLines) != 0)
            {
                context.MarkIgnorableLines(changes);
            }

            context.Emit(changes, config, callbacks);
        }

        // optimized anchor identification
        static Func<ReadOnlySpan<byte>, bool>? GetIsAnchorFunc(IEnumerable<ImmutableArray<byte>>? anchors)
        {
            if (anchors is null)
                return null;

            ImmutableArray<byte> singleValue;

            if (anchors is HashSet<ImmutableArray<byte>> set)
            {
                switch (set.Count)
                {
                    case 0:
                        return null;
                    case 1:
                        singleValue = set.First();

                        // there is no default comparer that does what we want here, assume custom comparer
                        var alternateComparer = (IAlternateEqualityComparer<ReadOnlySpan<byte>, ImmutableArray<byte>>)set.Comparer;

                        return (span) =>
                        {
                            return alternateComparer.Equals(span, singleValue);
                        };
                }
            }
            else
            {
                using var enumerator = anchors.GetEnumerator();

                if (!enumerator.MoveNext())
                    return null;

                singleValue = enumerator.Current;

                if (!enumerator.MoveNext())
                    goto single_anchor;

                set = new HashSet<ImmutableArray<byte>>(new Utf8Comparer())
                {
                    singleValue,
                    enumerator.Current
                };

                while (enumerator.MoveNext())
                {
                    set.Add(enumerator.Current);
                }
            }

            return set.GetAlternateLookup<ReadOnlySpan<byte>>().Contains;

        single_anchor:
            return (span) =>
            {
                return span.SequenceEqual(singleValue.AsSpan()); // StringComparison.Ordinal
            };
        }
    }

    #region Patience Algorithm
    private static void RunPatienceDiff<TChar>(ref XDiffContext<TChar> context, Func<ReadOnlySpan<TChar>, bool>? isAnchor)
        where TChar: struct, INumberBase<TChar>
    {
        PatienceHashmap map = default;

        map.RunPatience(ref context, isAnchor, 1, context.Xdf1.Records.Count, 1, context.Xdf2.Records.Count);
    }

    private struct PatienceEntry
    {
        public int Next, Previous;
        public uint Hash;
        public int Line1, Line2;
        public bool Anchor;
    }

    private struct PatienceHashmap
    {
        public PatienceEntry[]? _entries;
        private int _count;
        private int _first;
        private int _last;
        private bool _hasMatches;

        public void RunPatience<TChar>(ref XDiffContext<TChar> context, Func<ReadOnlySpan<TChar>, bool>? isAnchor, int line1, int count1, int line2, int count2)
            where TChar : struct, INumberBase<TChar>
        {
            if (count1 == 0)
            {
                for (; count2 > 0; count2 -= 1, line2 += 1)
                {
                    context.Xdf2.RChange[line2 - 1] = 1;
                }

                return;
            }
            else if (count2 == 0)
            {
                for (; count1 > 0; count1 -= 1, line1 += 1)
                {
                    context.Xdf1.RChange[line1 - 1] = 1;
                }

                return;
            }

            this.FillHashmap(ref context, isAnchor, line1, count1, line2, count2);

            if (!_hasMatches)
            {
                for (; count1 > 0; count1 -= 1, line1 += 1)
                {
                    context.Xdf1.RChange[line1 - 1] = 1;
                }

                for (; count2 > 0; count2 -= 1, line2 += 1)
                {
                    context.Xdf2.RChange[line2 - 1] = 1;
                }

                return;
            }

            int first = this.FindLongestCommonSequence();

            if (first >= 0)
            {
                this.WalkCommonSequence(ref context, isAnchor, first, line1, count1, line2, count2);
            }
            else
            {
                FallbackToClassicDiff(ref context, line1, count1, line2, count2);
            }
        }

        private void FillHashmap<TChar>(ref XDiffContext<TChar> context, Func<ReadOnlySpan<TChar>, bool>? isAnchor, int line1, int count1, int line2, int count2)
            where TChar : struct, INumberBase<TChar>
        {
            _first = -1;
            _last = -1;
            _count = 0;
            _hasMatches = false;
            var entries = new PatienceEntry[checked(count1 * 2)];
            _entries = entries;

            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i].Next = -1;
                entries[i].Previous = -1;
            }

            while (count1-- > 0)
            {
                InsertRecord<TChar, FirstPass>(ref context, line1++, isAnchor);
            }

            while (count2-- > 0)
            {
                InsertRecord<TChar, SecondPass>(ref context, line2++, isAnchor);
            }
        }

        private void InsertRecord<TChar, TPass>(ref XDiffContext<TChar> context, int line, Func<ReadOnlySpan<TChar>, bool>? isAnchor)
            where TChar: struct, INumberBase<TChar>
            where TPass: struct, IInsertPass
        {
            XRecord record = TPass.SelectRecord(ref context, line - 1);
            var entries = _entries;

            int index = (int)((record.Hash << 1) % (uint)entries!.Length);

            while (entries[index].Line1 != 0)
            {
                if (entries[index].Hash != record.Hash)
                {
                    if (++index >= entries.Length)
                    {
                        index = 0;
                    }

                    continue;
                }

                if (TPass.IsSecondPass)
                    _hasMatches = true;

                TPass.OnMatch(ref entries[index], line);

                return;
            }

            if (TPass.IsSecondPass)
                return;

            ref var entry = ref entries[index];

            entry.Line1 = line;
            entry.Hash = record.Hash;
            entry.Anchor = isAnchor?.Invoke(context.File1.Slice(record.LineOffset, record.LineLength)) ?? false;

            if (_first < 0)
                _first = index;

            if (_last >= 0)
            {
                entries[_last].Next = index;
                entry.Previous = _last;
            }

            _last = index;
            _count += 1;
        }

        private interface IInsertPass
        {
            static abstract bool IsSecondPass { get; }

            static abstract void OnMatch(ref PatienceEntry entry, int line);

            static abstract XRecord SelectRecord<TChar>(ref XDiffContext<TChar> context, int index) where TChar : struct, INumberBase<TChar>;
        }

        private struct FirstPass : IInsertPass
        {
            public static bool IsSecondPass => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void OnMatch(ref PatienceEntry entry, int line)
            {
                entry.Line2 = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static XRecord SelectRecord<TChar>(ref XDiffContext<TChar> context, int index) where TChar : struct, INumberBase<TChar>
            {
                return context.Xdf1.Records[index];
            }
        }

        private struct SecondPass : IInsertPass
        {
            public static bool IsSecondPass => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void OnMatch(ref PatienceEntry entry, int line)
            {
                entry.Line2 = entry.Line2 != 0 ? -1 : line;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static XRecord SelectRecord<TChar>(ref XDiffContext<TChar> context, int index) where TChar : struct, INumberBase<TChar>
            {
                return context.Xdf2.Records[index];
            }
        }

        private int FindLongestCommonSequence()
        {
            var entries = _entries;
            var sequence = new int[_count];

            int anchor_i = -1, longest = 0, entry;

            for (entry = _first; (uint)entry < (uint)entries!.Length; entry = entries[entry].Next)
            {
                if (entries[entry].Line2 is 0 or -1)
                    continue;

                int i = this.BinarySearch(sequence, longest, entry);

                entries[entry].Previous = i < 0 ? -1 : sequence[i];
                i += 1;

                if (i <= anchor_i)
                    continue;

                sequence[i] = entry;
                if (entries[entry].Anchor)
                {
                    anchor_i = i;
                    longest = anchor_i + 1;
                }
                else if (i == longest)
                {
                    longest += 1;
                }
            }

            if (longest == 0)
                return -1;

            entry = sequence[longest - 1];
            entries[entry].Next = -1;

            while (entries[entry].Previous >= 0)
            {
                int previous = entries[entry].Previous;
                entries[previous].Next = entry;

                entry = previous;
            }

            return entry;
        }

        private int BinarySearch(int[] sequence, int longest, int entry)
        {
            int left = -1, right = longest;

            ref var entryRef = ref _entries![entry];

            while (left + 1 < right)
            {
                int middle = left + (right - left) / 2;

                if (_entries[sequence[middle]].Line2 > entryRef.Line2)
                {
                    right = middle;
                }
                else
                {
                    left = middle;
                }
            }

            return left;
        }

        private void WalkCommonSequence<TChar>(ref XDiffContext<TChar> context, Func<ReadOnlySpan<TChar>, bool>? isAnchor, int first, int line1, int count1, int line2, int count2)
            where TChar: struct, INumberBase<TChar>
        {
            int end1 = line1 + count1, end2 = line2 + count2;
            int next1, next2;

            PatienceEntry[] entries = _entries!;

            while (true)
            {
                if (first >= 0)
                {
                    next1 = entries[first].Line1;
                    next2 = entries[first].Line2;

                    while (next1 > line1 && next2 > line2
                        && Match(ref context, next1 - 1, next2 - 1))
                    {
                        next1 -= 1;
                        next2 -= 1;
                    }
                }
                else
                {
                    next1 = end1;
                    next2 = end2;
                }

                while (line1 < next1 && line2 < next2
                    && Match(ref context, line1, line2))
                {
                    line1 += 1;
                    line2 += 1;
                }

                if (next1 > line1 || next2 > line2)
                {
                    this.RunPatience(ref context, isAnchor, line1, next1 - line1, line2, next2 - line2);
                }

                if (first < 0)
                    return;

                int next;
                while ((next = entries[first].Next) >= 0
                    && entries[next].Line1 == entries[first].Line1 + 1
                    && entries[next].Line2 == entries[first].Line2 + 1)
                {
                    first = next;
                }

                line1 = entries[first].Line1 + 1;
                line2 = entries[first].Line2 + 1;
                first = entries[first].Next;
            }

            static bool Match(ref XDiffContext<TChar> context, int line1, int line2)
            {
                return context.Xdf1.Records[line1 - 1].Hash == context.Xdf2.Records[line2 - 1].Hash;
            }
        }
    }

    #endregion

    #region Histogram Algorithm
    private static void RunHistogramDiff<TChar>(ref XDiffContext<TChar> context) where TChar : struct, INumberBase<TChar>
    {
        HistogramContext hist = default;

        hist.RunHistogram(ref context, 1, context.Xdf1.Records.Count, 1, context.Xdf2.Records.Count);
    }

    private struct HistogramContext
    {
        private ValueList<HistogramRecord> _recordObjects;
        private int[]? _records, _lineMap;
        private int[]? _nextPtrs;
        private int _tableBits;
        private int _maxChainLength, /*_keyShift,*/ _ptrShift;
        private int _count;
        private bool _hasCommon;

        public void RunHistogram<TChar>(ref XDiffContext<TChar> context, int line1, int count1, int line2, int count2)
            where TChar: struct, INumberBase<TChar>
        {
        redo: // manual tail-call
            if (count1 <= 0)
            {
                if (count2 <= 0)
                    return;

                for (; count2 > 0; count2 -= 1, line2 += 1)
                {
                    context.Xdf2.RChange[line2 - 1] = 1;
                }

                return;
            }
            else if (count2 <= 0)
            {
                for (; count1 > 0; count1 -= 1, line1 += 1)
                {
                    context.Xdf1.RChange[line1 - 1] = 1;
                }

                return;
            }

            Region lcs = default;

            if (FindLCS(ref context, line1, count1, line2, count2, ref lcs))
            {
                FallbackToClassicDiff(ref context, line1, count1, line2, count2);
                return;
            }

            if (lcs.begin1 == 0 && lcs.begin2 == 0)
            {
                for (; count2 > 0; count2 -= 1, line2 += 1)
                {
                    context.Xdf2.RChange[line2 - 1] = 1;
                }

                for (; count1 > 0; count1 -= 1, line1 += 1)
                {
                    context.Xdf1.RChange[line1 - 1] = 1;
                }

                return;
            }

            this.RunHistogram(ref context, line1, lcs.begin1 - line1, line2, lcs.begin2 - line2);

            count1 = (line1 + count1 - 1) - lcs.end1;
            line1 = lcs.end1 + 1;
            count2 = (line2 + count2 - 1) - lcs.end2;
            line2 = lcs.end2 + 1;
            goto redo;
        }


        private bool FindLCS<TChar>(ref XDiffContext<TChar> context, int line1, int count1, int line2, int count2, ref Region lcs)
            where TChar: struct, INumberBase<TChar>
        {
            // find_lcs()

            // initialize state
            _tableBits = XDL.HashBits(count1);

            int[]? tmp = _records;
            if (tmp is null || tmp.Length < 1 << _tableBits)
                tmp = _records = new int[1 << _tableBits];

            Array.Fill(tmp, -1);

            tmp = _lineMap;
            if (tmp is null || tmp.Length < count1)
                tmp = _lineMap = new int[count1];

            Array.Fill(tmp, -1);

            tmp = _nextPtrs;
            if (tmp is null || tmp.Length < count1)
                _nextPtrs = new int[count1];
            else
                Array.Clear(tmp);

            _recordObjects.EnsureCapacity(count1);

            _ptrShift = line1;
            _maxChainLength = 64;

            _hasCommon = false;

            try
            {
                // run algorithm
                if (!this.ScanA(ref context, line1, count1))
                    return true;

                _count = _maxChainLength + 1;

                for (int b_ptr = line2; b_ptr <= (line2 + count2 - 1);)
                {
                    b_ptr = this.TryLCS(ref context, b_ptr, line1, count1, line2, count2, ref lcs);
                }
            }
            finally
            {
                _recordObjects.Clear();
            }

            return _hasCommon && _maxChainLength < _count;
        }

        private int TryLCS<TChar>(ref XDiffContext<TChar> context, int b_ptr, int line1, int count1, int line2, int count2, ref Region lcs)
            where TChar : struct, INumberBase<TChar>
        {
            // try_lcs()

            int b_next = checked(b_ptr + 1);

            int recIndex = _records![this.TableHash(ref context.Xdf2, b_ptr)];

            for (; recIndex >= 0; recIndex = _recordObjects[recIndex].Next)
            {
                ref var rec = ref _recordObjects[recIndex];

                if (rec.Count > _count)
                {
                    if (!_hasCommon)
                    {
                        _hasCommon = CompareRecords(ref context.Xdf1, ref context.Xdf2, rec.Line, b_ptr);
                    }

                    continue;
                }

                int aStart = rec.Line;
                if (!CompareRecords(ref context.Xdf1, ref context.Xdf2, aStart, b_ptr))
                {
                    continue;
                }

                _hasCommon = true;
                while (true)
                {
                    int nextPointer = this.NextPointer(aStart);
                    int bStart = b_ptr;
                    int aEnd = aStart;
                    int bEnd = bStart;
                    int recCount = rec.Count;

                    while (line1 < aStart && line2 < bStart && CompareRecords(ref context.Xdf1, ref context.Xdf2, aStart - 1, bStart - 1))
                    {
                        aStart -= 1;
                        bStart -= 1;

                        if (recCount > 1)
                            recCount = Math.Min(recCount, this.Count(aStart));
                    }

                    while (aEnd < (line1 + count1 - 1) && bEnd < (line2 + count2 - 1)
                        && CompareRecords(ref context.Xdf1, ref context.Xdf2, aEnd + 1, bEnd + 1))
                    {
                        aEnd += 1;
                        bEnd += 1;

                        if (recCount > 1)
                            recCount = Math.Min(recCount, this.Count(aEnd));
                    }

                    if (b_next <= bEnd)
                        b_next = bEnd + 1;

                    if (lcs.end1 - lcs.begin1 < aEnd - aStart || recCount < _count)
                    {
                        lcs.begin1 = aStart;
                        lcs.begin2 = bStart;
                        lcs.end1 = aEnd;
                        lcs.end2 = bEnd;
                        _count = recCount;
                    }

                    if (nextPointer == 0)
                        break;

                    bool shouldBreak = false;
                    while (nextPointer <= aStart)
                    {
                        nextPointer = this.NextPointer(nextPointer);

                        if (nextPointer == 0)
                        {
                            shouldBreak = true;
                            break;
                        }
                    }

                    if (shouldBreak)
                        break;

                    aStart = nextPointer;
                }
            }

            return b_next;
        }

        private bool ScanA<TChar>(ref XDiffContext<TChar> context, int line1, int count1)
            where TChar : struct, INumberBase<TChar>
        {
            // scanA()

            int ptr = line1 + count1 - 1;
            while (line1 <= ptr)
            {
                uint tableIndex = TableHash(ref context.Xdf1, ptr);

                ref var rec_chain = ref _records![tableIndex];

                int recIndex = rec_chain;

                int chainLen = 0;
                while (recIndex >= 0)
                {
                    ref var rec = ref _recordObjects[recIndex];

                    if (CompareRecords(ref context.Xdf1, ref context.Xdf1, rec.Line, ptr))
                    {
                        /*
                         * ptr is identical to another element. Insert
                         * it onto the front of the existing element
                         * chain.
                         */
                        this.NextPointer(ptr) = rec.Line;
                        rec.Line = ptr;

                        /* cap rec.Count at int.MaxValue */
                        if (rec.Count < int.MaxValue)
                            rec.Count += 1;

                        this.LineMap(ptr) = recIndex;
                        goto continue_scan;
                    }

                    recIndex = rec.Next;
                    chainLen += 1;
                }

                if (chainLen == _maxChainLength)
                    return false;

                /*
                 * This is the first time we have ever seen this particular
                 * element in the sequence. Construct a new chain for it.
                 */
                recIndex = _recordObjects.Add(new()
                {
                    Line = ptr,
                    Count = 1,
                    Next = rec_chain
                });

                rec_chain = recIndex;
                this.LineMap(ptr) = recIndex;

            continue_scan:
                ptr -= 1;
            }

            return true;
        }

        private uint TableHash(ref XDiffFile file, int line)
        {
            return XDL.HashNumber(file.Records[line - 1].Hash, _tableBits);
        }

        private ref int NextPointer(int pointer) => ref _nextPtrs![pointer - _ptrShift];

        private ref int LineMap(int pointer) => ref _lineMap![pointer - _ptrShift];

        private int Count(int ptr) => _recordObjects[this.LineMap(ptr)].Count;

        private static bool CompareRecords(ref XDiffFile first, ref XDiffFile second, int line1, int line2)
        {
            return first.Records[line1 - 1].Hash == second.Records[line2 - 1].Hash;
        }
    }

    private struct HistogramRecord
    {
        public int Line, Count;
        public int Next;
    }

    private struct Region
    {
        public int begin1, end1;
        public int begin2, end2;
    }
    #endregion

    #region Meyers Algorithm
    private static void RunMeyersDiff<TChar>(ref XDiffContext<TChar> context) where TChar : struct, INumberBase<TChar>
    {
        var (off1, lim1, off2, lim2) = TrimFiles(ref context.Xdf1, ref context.Xdf2);

        CleanupRecords(ref context.Xdf1, ref context.Xdf2, ref context.Classifier, off1, lim1, off2, lim2);

        MeyersContext mcontext = new(ref context.Xdf1, ref context.Xdf2, (context.Flags & XDiffFlags.NeedMinimal) != 0);

        mcontext.RecordsCompare(0, context.Xdf1.NReff, 0, context.Xdf2.NReff);

        mcontext.Dispose();
    }

    private ref struct MeyersContext
    {
        public int _mxcost;
        public int _snake_count;
        public int _heur_min;

        public Span<int> kvdf, kvdb;
        public bool need_min;

        public uint[] _dd1_Ha, _dd2_Ha;
        public byte[] _dd1_RChange, _dd2_RChange;
        public int[] _dd1_RIndex, _dd2_RIndex;

        private int[] _kvd_pooled_array;

        public MeyersContext(ref XDiffFile xdf1, ref XDiffFile xdf2, bool need_min)
        {
            int ndiags = xdf1.NReff + xdf2.NReff + 3;

            int[] kvd = ArrayPool<int>.Shared.Rent(ndiags * 2 + 2);

            Span<int> kvdf = kvd.AsSpan(0, ndiags + 1), kvdb = kvd.AsSpan(ndiags + 1, ndiags + 1);

            _mxcost = Math.Min(XDL.BogoSqrt(ndiags), XDL.MAX_COST_MIN);
            _snake_count = XDL.SNAKE_CNT;
            _heur_min = XDL.HEUR_MIN_COST;

            this.kvdf = kvdf;
            this.kvdb = kvdb;
            this.need_min = need_min;

            _dd1_RChange = xdf1.RChange;
            _dd1_Ha = xdf1.Ha!;
            _dd1_RIndex = xdf1.RIndex!;

            _dd2_RChange = xdf2.RChange;
            _dd2_Ha = xdf2.Ha!;
            _dd2_RIndex = xdf2.RIndex!;

            _kvd_pooled_array = kvd;
        }

        public void RecordsCompare(int off1, int lim1, int off2, int lim2)
        {
            uint[] ha1 = _dd1_Ha, ha2 = _dd2_Ha;

        recurse:
            // Shrink the box by walking through each diagonal snake (SW and NE).
            for (; off1 < lim1 && off2 < lim2 && ha1[off1] == ha2[off2]; off1++, off2++) ;
            for (; off1 < lim1 && off2 < lim2 && ha1[lim1 - 1] == ha2[lim2 - 1]; lim1--, lim2--) ;

            if (off1 == lim1)
            {
                byte[] rchg2 = _dd2_RChange;
                int[] rindex2 = _dd2_RIndex;

                for (; off2 < lim2; off2++)
                    rchg2[rindex2[off2]] = 1;
            }
            else if (off2 == lim2)
            {
                byte[] rchg1 = _dd1_RChange;
                int[] rindex1 = _dd1_RIndex;

                for (; off1 < lim1; off1++)
                    rchg1[rindex1[off1]] = 1;
            }
            else
            {
                var spl = this.Split(off1, lim1, off2, lim2);

                this.RecordsCompare(off1, spl.i1, off2, spl.i2);

                off1 = spl.i1;
                off2 = spl.i2;
                goto recurse;
            }
        }

        private SplitInfo Split(int off1, int lim1, int off2, int lim2)
        {
            SplitInfo spl = default;

            int dmin = off1 - lim2, dmax = lim1 - off2;
            int fmid = off1 - off2, bmid = lim1 - lim2;
            bool odd = ((fmid - bmid) & 1) != 0;
            int fmin = fmid, fmax = fmid;
            int bmin = bmid, bmax = bmid;
            int ec, d, i1, i2, prev1, best, dd, v, k;

            uint[] ha1 = _dd1_Ha, ha2 = _dd2_Ha;

            /*
             * Set initial diagonal values for both forward and backward path.
             */
            kvdf[fmid] = off1;
            kvdb[bmid] = lim1;

            for (ec = 1; ; ++ec)
            {
                bool got_snake = false;

                /*
                 * We need to extend the diagonal "domain" by one. If the next
                 * values exits the box boundaries we need to change it in the
                 * opposite direction because (max - min) must be a power of
                 * two.
                 *
                 * Also we initialize the external K value to -1 so that we can
                 * avoid extra conditions in the check inside the core loop.
                 */

                if (fmin > dmin)
                {
                    kvdf[--fmin - 1] = -1;
                }
                else
                {
                    ++fmin;
                }

                if (fmax < dmax)
                {
                    kvdf[++fmax + 1] = -1;
                }
                else
                {
                    --fmax;
                }

                for (d = fmax; d >= fmin; d -= 2)
                {
                    if (kvdf[d - 1] >= kvdf[d + 1])
                    {
                        i1 = kvdf[d - 1] + 1;
                    }
                    else
                    {
                        i1 = kvdf[d + 1];
                    }

                    prev1 = i1;
                    i2 = i1 - d;

                    for (; i1 < lim1 && i2 < lim2 && ha1[i1] == ha2[i2]; i1++, i2++)
                    {
                    }

                    if (i1 - prev1 > _snake_count)
                    {
                        got_snake = true;
                    }

                    kvdf[d] = i1;
                    if (odd && bmin <= d && d <= bmax && kvdb[d] <= i1)
                    {
                        spl.i1 = i1;
                        spl.i2 = i2;
                        spl.min_lo = spl.min_hi = 1;
                        return spl;
                    }
                }

                /*
                 * We need to extend the diagonal "domain" by one. If the next
                 * values exits the box boundaries we need to change it in the
                 * opposite direction because (max - min) must be a power of
                 * two.
                 *
                 * Also we initialize the external K value to -1 so that we can
                 * avoid extra conditions in the check inside the core loop.
                 */

                if (bmin > dmin)
                {
                    kvdb[--bmin - 1] = XDL.LINE_MAX;
                }
                else
                {
                    ++bmin;
                }

                if (bmax < dmax)
                {
                    kvdb[++bmax + 1] = XDL.LINE_MAX;
                }
                else
                {
                    --bmax;
                }

                for (d = bmax; d >= bmin; d -= 2)
                {
                    if (kvdb[d - 1] < kvdb[d + 1])
                    {
                        i1 = kvdb[d - 1];
                    }
                    else
                    {
                        i1 = kvdb[d + 1] - 1;
                    }

                    prev1 = i1;
                    i2 = i1 - d;

                    for (; i1 > off1 && i2 > off2 && ha1[i1 - 1] == ha2[i2 - 1]; i1--, i2--)
                    {
                    }

                    if (prev1 - i1 > _snake_count)
                    {
                        got_snake = true;
                    }

                    kvdb[d] = i1;

                    if (!odd && fmin <= d && d <= fmax && i1 <= kvdf[d])
                    {
                        spl.i1 = i1;
                        spl.i2 = i2;
                        spl.min_lo = spl.min_hi = 1;
                        return spl;
                    }
                }

                if (need_min)
                    continue;

                /*
                 * If the edit cost is above the heuristic trigger and if
                 * we got a good snake, we sample current diagonals to see
                 * if some of them have reached an "interesting" path. Our
                 * measure is a function of the distance from the diagonal
                 * corner (i1 + i2) penalized with the distance from the
                 * mid diagonal itself. If this value is above the current
                 * edit cost times a magic factor (XDL_K_HEUR) we consider
                 * it interesting.
                 */
                if (got_snake && ec > _heur_min)
                {
                    for (best = 0, d = fmax; d >= fmin; d -= 2)
                    {
                        dd = d > fmid ? d - fmid : fmid - d;
                        i1 = kvdf[d];
                        i2 = i1 - d;
                        v = i1 - off1 + (i2 - off2) - dd;

                        if (v > XDL.K_HEUR * ec && v > best
                            && off1 + _snake_count <= i1 && i1 < lim1
                            && off2 + _snake_count <= i2 && i2 < lim2)
                        {
                            for (k = 1; ha1[i1 - k] == ha2[i2 - k]; k++)
                            {
                                if (k == _snake_count)
                                {
                                    best = v;
                                    spl.i1 = i1;
                                    spl.i2 = i2;
                                    break;
                                }
                            }
                        }
                    }

                    if (best > 0)
                    {
                        spl.min_lo = 1;
                        spl.min_hi = 0;
                        return spl;
                    }

                    for (best = 0, d = bmax; d >= bmin; d -= 2)
                    {
                        dd = d > bmid ? d - bmid : bmid - d;
                        i1 = kvdb[d];
                        i2 = i1 - d;
                        v = lim1 - i1 + (lim2 - i2) - dd;

                        if (v > XDL.K_HEUR * ec && v > best &&
                            off1 < i1 && i1 <= lim1 - _snake_count &&
                            off2 < i2 && i2 <= lim2 - _snake_count)
                        {
                            for (k = 0; ha1[i1 + k] == ha2[i2 + k]; k++)
                            {
                                if (k == _snake_count - 1)
                                {
                                    best = v;
                                    spl.i1 = i1;
                                    spl.i2 = i2;
                                    break;
                                }
                            }
                        }
                    }

                    if (best > 0)
                    {
                        spl.min_lo = 0;
                        spl.min_hi = 1;
                        return spl;
                    }
                }

                /*
                 * Enough is enough. We spent too much time here and now we
                 * collect the furthest reaching path using the (i1 + i2)
                 * measure.
                 */

                if (ec >= _mxcost)
                {
                    int fbest, fbest1, bbest, bbest1;

                    fbest = fbest1 = -1;
                    for (d = fmax; d >= fmin; d -= 2)
                    {
                        i1 = Math.Min(kvdf[d], lim1);
                        i2 = i1 - d;
                        if (lim2 < i2)
                        {
                            i1 = lim2 + d;
                            i2 = lim2;
                        }

                        if (fbest < i1 + i2)
                        {
                            fbest = i1 + i2;
                            fbest1 = i1;
                        }
                    }

                    bbest = bbest1 = XDL.LINE_MAX;
                    for (d = bmax; d >= bmin; d -= 2)
                    {
                        i1 = Math.Max(off1, kvdb[d]);
                        i2 = i1 - d;

                        if (i2 < off2)
                        {
                            i1 = off2 + d;
                            i2 = off2;
                        }

                        if (i1 + i2 < bbest)
                        {
                            bbest = i1 + i2;
                            bbest1 = i1;
                        }
                    }

                    if (lim1 + lim2 - bbest < fbest - (off1 + off2))
                    {
                        spl.i1 = fbest1;
                        spl.i2 = fbest - fbest1;
                        spl.min_lo = 1;
                        spl.min_hi = 0;
                    }
                    else
                    {
                        spl.i1 = bbest1;
                        spl.i2 = bbest - bbest1;
                        spl.min_lo = 0;
                        spl.min_hi = 1;
                    }

                    return spl;
                }
            }
        }

        public void Dispose()
        {
            var kvd = _kvd_pooled_array;

            _kvd_pooled_array = null!;
            kvdf = default;
            kvdb = default;

            ArrayPool<int>.Shared.Return(kvd);
        }

        private struct SplitInfo
        {
            public int i1, i2;
            public int min_lo, min_hi;
        }
    }

    private static (int line1, int count1, int line2, int count2) TrimFiles(ref XDiffFile xdf1, ref XDiffFile xdf2)
    {
        var recs1 = xdf1.Records.GetSpan();
        var recs2 = xdf2.Records.GetSpan();
        int lim = Math.Min(recs1.Length, recs2.Length);

        int i = 0;
        for (; i < lim; ++i)
        {
            if (recs1[i].Hash != recs2[i].Hash)
            {
                break;
            }
        }

        xdf1.DStart = xdf2.DStart = i;

        int r1 = recs1.Length - 1, r2 = recs2.Length - 1;

        for (; r1 >= i && r2 >= i; --r1, --r2)
        {
            if (recs1[r1].Hash != recs2[r2].Hash)
            {
                break;
            }
        }

        xdf1.DLength = r1 - i + 1;
        xdf2.DLength = r2 - i + 1;

        return (xdf1.DStart, xdf1.DLength, xdf2.DStart, xdf2.DLength);
    }

    private static void CleanupRecords(ref XDiffFile xdf1, ref XDiffFile xdf2, ref LineClassifier classifier, int off1, int len1, int off2, int len2)
    {
        var xdf1_recs = xdf1.Records.GetSpan().Slice(off1, len1);
        var xdf2_recs = xdf2.Records.GetSpan().Slice(off2, len2);

        var dis = ArrayPool<byte>.Shared.Rent(xdf1_recs.Length + xdf2_recs.Length + 2);
        var dis1 = dis.AsSpan(0, xdf1_recs.Length + 1);
        var dis2 = dis.AsSpan(xdf1_recs.Length + 1, xdf2_recs.Length + 1);

        int mlim = Math.Min(XDL.BogoSqrt(xdf1_recs.Length), XDL.MAX_EQLIMIT);

        var classifiedRecs = classifier.RcRecords;
        int i;
        for (i = 0; i < xdf1_recs.Length; ++i)
        {
            int hash = (int)xdf1_recs[i].Hash;

            var nm = (uint)hash < (uint)classifiedRecs.Length ? classifiedRecs[hash].Length2 : 0;

            dis1[i] = (byte)((nm == 0) ? 0 : (nm >= mlim) ? 2 : 1);
        }

        mlim = Math.Min(XDL.BogoSqrt(xdf2_recs.Length), XDL.MAX_EQLIMIT);

        for (i = 0; i < xdf2_recs.Length; i += 1)
        {
            int hash = (int)xdf2_recs[i].Hash;

            var nm = (uint)hash < (uint)classifiedRecs.Length ? classifiedRecs[hash].Length1 : 0;

            dis2[i] = (byte)((nm == 0) ? 0 : (nm >= mlim) ? 2 : 1);
        }

        int nreff;
        for (nreff = 0, i = 0; i < xdf1_recs.Length; ++i)
        {
            if (dis1[i] == 1 || (dis1[i] == 2 && !CleanMatch(dis1, i)))
            {
                xdf1.RIndex![nreff] = i + off1;
                xdf1.Ha![nreff] = xdf1_recs[i].Hash;
                nreff += 1;
            }
            else
            {
                xdf1.RChange[i + off1] = 1;
            }
        }

        xdf1.NReff = nreff;

        for (nreff = 0, i = 0; i < xdf2_recs.Length; ++i)
        {
            if (dis2[i] == 1 || (dis2[i] == 2 && !CleanMatch(dis2, i)))
            {
                xdf2.RIndex![nreff] = i + off2;
                xdf2.Ha![nreff] = xdf2_recs[i].Hash;
                nreff += 1;
            }
            else
            {
                xdf2.RChange[i + off2] = 1;
            }
        }

        xdf2.NReff = nreff;

        ArrayPool<byte>.Shared.Return(dis);

        static bool CleanMatch(ReadOnlySpan<byte> dis, int i)
        {
            const int XDL_SIMSCAN_WINDOW = XDL.SIMSCAN_WINDOW;

            int s = 0, e = dis.Length;
            if (i > XDL_SIMSCAN_WINDOW + 1)
            {
                s = i - (XDL_SIMSCAN_WINDOW + 1);
            }

            if (e - i > XDL_SIMSCAN_WINDOW)
            {
                e = i + XDL_SIMSCAN_WINDOW;
            }

            dis = dis[s..e];
            i -= s;

            int rdis0 = 0, rpdis0 = 0, rdis1 = 0, rpdis1 = 0;

            for (int r = 1; (i - r) >= 0; ++r)
            {
                switch (dis[i - r])
                {
                    case 0:
                        rdis0 += 1;
                        continue;
                    case 2:
                        rpdis0 += 1;
                        continue;
                }

                break;
            }

            if (rdis0 == 0)
                return false;

            for (int r = 1; (i + r) < dis.Length; ++r)
            {
                switch (dis[i + r])
                {
                    case 0:
                        rdis1 += 1;
                        continue;
                    case 2:
                        rpdis1 += 1;
                        continue;
                }

                break;
            }

            if (rdis1 == 0)
                return false;

            rdis1 += rdis0;
            rpdis1 += rpdis0;

            return rpdis1 * XDL.KPDIS_RUN < rpdis1 + rdis1;
        }
    }

    private static void FallbackToClassicDiff<TChar>(ref XDiffContext<TChar> context, int line1, int count1, int line2, int count2)
        where TChar: struct, INumberBase<TChar>
    {
        int len = context.Xdf1.RChange.Length;
        context.Xdf1.Ha ??= new uint[len];
        context.Xdf1.RIndex ??= new int[len];

        len = context.Xdf2.RChange.Length;
        context.Xdf2.Ha ??= new uint[len];
        context.Xdf2.RIndex ??= new int[len];

        line1 -= 1;
        line2 -= 1;

        CleanupRecords(ref context.Xdf1, ref context.Xdf2, ref context.Classifier, line1, count1, line2, count2);

        MeyersContext mcontext = new MeyersContext(ref context.Xdf1, ref context.Xdf2, false);

        mcontext.RecordsCompare(0, context.Xdf1.NReff, 0, context.Xdf2.NReff);

        mcontext.Dispose();
    }
    #endregion

    private static TextChange? BuildScript(byte[] rChange1, byte[] rChange2, int xdf1_record_length, int xdf2_record_length)
    {
        TextChange? cscr = null;

        for (int i1 = xdf1_record_length - 1, i2 = xdf2_record_length - 1; i1 >= 0 || i2 >= 0; --i1, --i2)
        {
            bool t1 = (uint)i1 < (uint)rChange1.Length && rChange1[i1] != 0;
            bool t2 = (uint)i2 < (uint)rChange2.Length && rChange2[i2] != 0;

            if (t1 || t2)
            {
                int length1, length2;

                if (t1)
                {
                    length1 = 1;
                    for (; (uint)i1 - 1 < (uint)rChange1.Length && rChange1[i1 - 1] != 0; --i1, ++length1)
                    {
                    }
                }
                else
                {
                    length1 = 0;
                }

                if (t2)
                {
                    length2 = 1;
                    for (; (uint)i2 - 1 < (uint)rChange2.Length && rChange2[i2 - 1] != 0; --i2, ++length2)
                    {
                    }
                }
                else
                {
                    length2 = 0;
                }

                cscr = new TextChange()
                {
                    Next = cscr,
                    Index1 = i1,
                    Index2 = i2,
                    Change1 = length1,
                    Change2 = length2
                };
            }
        }

        return cscr;
    }

    internal class TextChange
    {
        public TextChange? Next;
        public int Index1, Index2;
        public int Change1, Change2;
        public bool Ignore;
    }

    private ref struct XDiffContext<TChar> where TChar : struct, INumberBase<TChar>
    {
        public readonly XDiffFlags Flags;
        private readonly int _sampleCount;
        public ReadOnlySpan<TChar> File1, File2;

        public XDiffFile Xdf1, Xdf2;
        public LineClassifier Classifier;

        public XDiffContext(XDiffFlags flags)
        {
            Flags = flags;
            _sampleCount = (flags & XDL.AlgorithmMask) == XDiffFlags.HistogramDiff ? 20 : 256;
        }

        public void Initialize(ReadOnlySpan<TChar> file1, ReadOnlySpan<TChar> file2)
        {
            File1 = file1;
            File2 = file2;

            int f1guess = GuessLines(file1, _sampleCount);
            int f2guess = GuessLines(file2, _sampleCount);

            this.Classifier.Initialize(f1guess + f2guess + 3);

            this.Xdf1.Initialize(file1, f1guess, ref this, 1);
            this.Xdf2.Initialize(file2, f2guess, ref this, 2);

            static int GuessLines(ReadOnlySpan<TChar> fileData, int sample)
            {
                if (fileData.IsEmpty)
                    return 1;

                int lineCount = 0;
                int charsConsumed = 0;

                TChar newLine = TChar.CreateChecked('\n');

                while (lineCount++ < sample)
                {
                    int index = fileData.Slice(charsConsumed).IndexOf(newLine);

                    if (index < 0)
                        return lineCount;

                    charsConsumed += index + 1;
                }

                return (int)(fileData.Length / ((double)charsConsumed / lineCount));
            }
        }

        public void MarkIgnorableLines(TextChange script)
        {
            // xdl_mark_ignorable_lines()

            ReadOnlySpan<XRecord> records1 = this.Xdf1.Records.GetSpan();
            ReadOnlySpan<XRecord> records2 = this.Xdf2.Records.GetSpan();

            ReadOnlySpan<TChar> fileData1 = this.File1;
            ReadOnlySpan<TChar> fileData2 = this.File2;

            var flags = this.Flags;

            for (TextChange? xch = script; xch is not null; xch = xch.Next)
            {
                bool ignore = true;
                var recs = records1.Slice(xch.Index1, xch.Change1);

                for (int i = 0; i < recs.Length && ignore; ++i)
                {
                    ref readonly var record = ref recs[i];

                    ignore = IsBlankLine(fileData1.Slice(record.LineOffset, record.LineLength), flags);
                }

                if (ignore)
                {
                    recs = records2.Slice(xch.Index2, xch.Change2);

                    for (int i = 0; i < recs.Length && ignore; ++i)
                    {
                        ref readonly var record = ref recs[i];

                        ignore = IsBlankLine(fileData2.Slice(record.LineOffset, record.LineLength), flags);
                    }
                }

                xch.Ignore = ignore;
            }

            static bool IsBlankLine(ReadOnlySpan<TChar> line, XDiffFlags flags)
            {
                if ((flags & XDL.WhitespaceOptionsMask) == 0)
                    return line.Length == 0 || (line.Length == 1 && line[0] == TChar.CreateChecked('\n')); // a blank line at the end of the file may not have a new line

                return !XDL.ContainsAnyNonSpace(line);
            }
        }

        [InlineArray(80)]
        private struct FuncLineBuffer
        {
            public TChar _element;
        }

        public void Emit(TextChange script, EmitConfiguration<TChar> emitConfig, IEmitCallbacks<TChar> callbacks)
        {
            var hunkFunc = emitConfig.HunkFunc;
            if (hunkFunc is not null)
            {
                // xdl_call_hunk_func()
                TextChange? xche;

                for (TextChange? xch = script; xch is not null; xch = xche.Next)
                {
                    xche = GetHunk(ref xch, emitConfig);

                    if (xch is null)
                        break;

                    hunkFunc(xch.Index1, xche.Index1 + xche.Change1 - xch.Index1,
                             xch.Index2, xche.Index2 + xche.Change2 - xch.Index2,
                             callbacks);
                }
            }
            else
            {
                // xdl_emit_diff()
                Unsafe.SkipInit(out FuncLineBuffer memory);
                Span<TChar> func_line = memory;

                ReadOnlySpan<TChar> file1 = this.File1, file2 = this.File2;
                int recordCount1 = this.Xdf1.Records.Count, recordCount2 = this.Xdf2.Records.Count;

                int written = 0;
                int funcLinePrev = -1;

                TextChange? xch, xche;
                for (xch = script; xch is not null; xch = xche.Next)
                {
                    var xchp = xch;

                    xche = GetHunk(ref xch, emitConfig);

                    if (xch is null)
                        break;

                    pre_context_calculation:
                    int s1 = Math.Max(xch.Index1 - emitConfig.ContextLength, 0);
                    int s2 = Math.Max(xch.Index2 - emitConfig.ContextLength, 0);

                    if ((emitConfig.Flags & EmitConfigFlags.EmitFuncContext) != 0)
                    {
                        int fs1, i1 = xch.Index1;

                        if ((uint)i1 >= (uint)recordCount1)
                        {
                            int i2 = xch.Index2;

                            // We don't need additional context if a while function was added
                            while ((uint)i2 < (uint)recordCount2)
                            {
                                var record = this.Xdf2.Records[i2];

                                if (TryMatchFuncRecord(file2.Slice(record.LineOffset, record.LineLength), default, out _, emitConfig))
                                {
                                    goto post_context_calculation;
                                }

                                i2 += 1;
                            }

                            i1 = recordCount1 - 1;
                        }

                        fs1 = GetFuncLine(file1, default, out _, i1, -1, emitConfig);

                        while (fs1 > 0)
                        {
                            var record = this.Xdf1.Records[fs1 - 1];

                            var line = file1.Slice(record.LineOffset, record.LineLength);

                            if (!XDL.ContainsAnyNonSpace(line) || !TryMatchFuncRecord(line, default, out _, emitConfig))
                            {
                                break;
                            }

                            fs1 -= 1;
                        }

                        if (fs1 < 0)
                            fs1 = 0;

                        if (fs1 < s1)
                        {
                            s2 = Math.Max(s2 - (s1 - fs1), 0);
                            s1 = fs1;

                            Debug.Assert(xchp is not null);

                            // Did we extend context upwards into an ignored change?
                            while (xchp != xch && xchp.Index1 + xchp.Change1 <= s1 && xchp.Index2 + xchp.Change2 <= s2)
                            {
                                xchp = xchp.Next!;

                                Debug.Assert(xchp is not null);
                            }

                            if (xchp != xch)
                            {
                                xch = xchp;
                                goto pre_context_calculation;
                            }
                        }
                    }

                post_context_calculation:
                    Debug.Assert(xche is not null);

                    int lctx = emitConfig.ContextLength;
                    lctx = Math.Min(lctx, recordCount1 - (xche.Index1 + xche.Change1));
                    lctx = Math.Min(lctx, recordCount2 - (xche.Index2 + xche.Change2));

                    int e1 = xche.Index1 + xche.Change1 + lctx;
                    int e2 = xche.Index2 + xche.Change2 + lctx;

                    if ((emitConfig.Flags & EmitConfigFlags.EmitFuncContext) != 0)
                    {
                        int fe1 = GetFuncLine(file1, default, out _, xche.Index1 + xche.Change1, recordCount1, emitConfig);

                        while (fe1 > 0)
                        {
                            var record = this.Xdf1.Records[fe1 - 1];

                            var line = file1.Slice(record.LineOffset, record.LineLength);

                            if (XDL.ContainsAnyNonSpace(line))
                                break;

                            fe1 -= 1;
                        }

                        if (fe1 < 0)
                            fe1 = recordCount1;

                        if (fe1 > e1)
                        {
                            e2 = Math.Min(e2 + (fe1 - e1), recordCount2);
                            e1 = fe1;
                        }

                        /*
                        * Overlap with next change?  Then include it
                        * in the current hunk and start over to find
                        * its new end.
                        */
                        if (xche.Next is not null)
                        {
                            int l = Math.Min(xche.Next.Index1, recordCount1 - 1);

                            if (l - emitConfig.ContextLength <= e1 || GetFuncLine(file1, default, out _, l, e1, emitConfig) < 0)
                            {
                                xche = xche.Next;
                                goto post_context_calculation;
                            }
                        }
                    }

                    /*
                    * Emit current hunk header.
                    */
                    if ((emitConfig.Flags & EmitConfigFlags.EmitFuncNames) != 0)
                    {
                        GetFuncLine(file1, func_line, out written, s1 - 1, funcLinePrev, emitConfig);
                        funcLinePrev = s1 - 1;
                    }

                    if ((emitConfig.Flags & EmitConfigFlags.EmitNoHunkHeader) == 0)
                    {
                        int c1 = e1 - s1, c2 = e2 - s2;

                        callbacks.EmitHunk(
                            c1 != 0 ? s1 + 1 : s1,
                            c1,
                            c2 != 0 ? s2 + 1 : s2,
                            c2,
                            func_line.Slice(0, written));
                    }

                    // Emit pre-context
                    for (; s2 < xch.Index2; ++s2)
                    {
                        var record = this.Xdf2.Records[s2];
                        var line = file2.Slice(record.LineOffset, record.LineLength);

                        callbacks.EmitLine(EmitLineType.Context, line);
                    }

                    for (s1 = xch.Index1, s2 = xch.Index2; ; xch = xch.Next)
                    {
                        Debug.Assert(xch is not null);

                        // Merge previous with current change atom
                        for (; s1 < xch.Index1 && s2 < xch.Index2; s1++, s2++)
                        {
                            var record = this.Xdf2.Records[s2];
                            var line = file2.Slice(record.LineOffset, record.LineLength);

                            callbacks.EmitLine(EmitLineType.Context, line);
                        }

                        // Removes lines from the first file
                        for (s1 = xch.Index1; s1 < xch.Index1 + xch.Change1; s1++)
                        {
                            var record = this.Xdf1.Records[s1];
                            var line = file1.Slice(record.LineOffset, record.LineLength);

                            callbacks.EmitLine(EmitLineType.Removed, line);
                        }

                        // Adds lines from the second file
                        for (s2 = xch.Index2; s2 < xch.Index2 + xch.Change2; s2++)
                        {
                            var record = this.Xdf2.Records[s2];
                            var line = file2.Slice(record.LineOffset, record.LineLength);

                            callbacks.EmitLine(EmitLineType.Added, line);
                        }

                        if (xch == xche)
                            break;

                        s1 = xch.Index1 + xch.Change1;
                        s2 = xch.Index2 + xch.Change2;
                    }

                    // Emit post-context
                    for (s2 = xche.Index2 + xche.Change2; s2 < e2; s2++)
                    {
                        var record = this.Xdf2.Records[s2];
                        var line = file2.Slice(record.LineOffset, record.LineLength);

                        callbacks.EmitLine(EmitLineType.Context, line);
                    }
                }
            }
        }

        private int GetFuncLine(ReadOnlySpan<TChar> file, scoped Span<TChar> func_line, out int written, int start, int limit, EmitConfiguration<TChar> emitConfig)
        {
            int step = start > limit ? -1 : 1;

            var records = this.Xdf1.Records.GetSpan();

            for (int line = start; line != limit && (uint)line < (uint)records.Length; line += step)
            {
                var record = records[line];

                if (TryMatchFuncRecord(file.Slice(record.LineOffset, record.LineLength), func_line, out written, emitConfig))
                {
                    return line;
                }
            }

            written = 0;
            return -1;
        }

        private static bool TryMatchFuncRecord(ReadOnlySpan<TChar> line, Span<TChar> buffer, out int written, EmitConfiguration<TChar> emitConfig)
        {
            return emitConfig.FindFunc?.Invoke(line, buffer, out written) ?? TryDefaultFindFunc(line, buffer, out written);
        }

        private static bool TryDefaultFindFunc(ReadOnlySpan<TChar> rec, Span<TChar> buf, out int written)
        {
            if (rec.Length > 0)
            {
                char firstChar = (char)ushort.CreateTruncating(rec[0]);

                if (char.IsAsciiLetter(firstChar) || firstChar is '_' or '$')
                {
                    if (rec.Length > buf.Length)
                        rec = rec.Slice(0, buf.Length);

                    int end = rec.Length;
                    for (; end > 0; end -= 1)
                    {
                        if (!XDL.IsSpace(rec[end - 1]))
                            break;
                    }

                    rec = rec.Slice(0, end);

                    rec.CopyTo(buf);
                    written = rec.Length;
                    return true;
                }
            }

            written = 0;
            return false;
        }

        [return: NotNullIfNotNull(nameof(xscr))]
        private static TextChange? GetHunk(ref TextChange? xscr, EmitConfiguration<TChar> emitConfig)
        {
            int max_common = 2 * emitConfig.ContextLength + emitConfig.InterHunkContextLength;
            int max_ignorable = emitConfig.ContextLength;
            int ignored = 0;

            TextChange? xch, xchp, lxch;

            for (xchp = xscr; xchp != null && xchp.Ignore; xchp = xchp.Next)
            {
                xch = xchp.Next;

                if (xch == null || xch.Index1 - (xchp.Index1 + xchp.Change1) >= max_ignorable)
                    xscr = xch;
            }

            if (xscr == null)
                return null;


            lxch = xscr;

            for (xchp = lxch, xch = xchp.Next; xch != null; xchp = xch, xch = xch.Next)
            {
                int distance = xch.Index1 - (xchp.Index1 + xchp.Change1);

                if (distance > max_common)
                    break;

                if (distance < max_ignorable && (!xch.Ignore || ReferenceEquals(lxch, xchp)))
                {
                    lxch = xch;
                    ignored = 0;
                }
                else if (distance < max_ignorable && xch.Ignore)
                {
                    ignored += xch.Change2;
                }
                else if (lxch != xchp && xch.Index1 + ignored - (lxch.Index1 + lxch.Change1) > max_common)
                {
                    break;
                }
                else if (!xch.Ignore)
                {
                    lxch = xch;
                    ignored = 0;
                }
                else
                {
                    ignored += xch.Change2;
                }
            }

            return lxch;
        }
    }

    public struct XRecord
    {
        public int LineOffset { get; internal set; }

        public int LineLength { get; internal set; }

        public uint Hash { get; internal set; }
    }

    private struct XDiffFile
    {
        public ValueList<XRecord> Records;
        public int DStart, DLength;
        public byte[] RChange;
        public int[]? RIndex;
        public int NReff;
        public uint[]? Ha;

        public void Initialize<TChar>(ReadOnlySpan<TChar> fileData, int guessedRecordCount, ref XDiffContext<TChar> context, int pass)
            where TChar : struct, INumberBase<TChar>
        {
            // xdl_prepare_ctx()

            this.Records.Clear();
            this.Records.EnsureCapacity(guessedRecordCount);

            //var hbits = XDL.HashBits(guessedRecordCount);
            //var hsize = 1 << hbits;

            //this.HashBits = hbits;
            //this.RecordHash = new int[hsize];
            //Array.Fill(this.RecordHash, -1);

            ref var classifier = ref context.Classifier;
            var flags = context.Flags;

            if (!fileData.IsEmpty)
            {
                var span = fileData;

                XDL.HashLines(span, ref this.Records, flags);

                var recordSpan = this.Records.GetSpan();
                for (int i = 0; i < recordSpan.Length; ++i)
                {
                    classifier.ClassifyLine(ref context, pass, i, ref recordSpan[i]);
                }
            }

            var rchg = new byte[this.Records.Count + 1];
            int[]? rindex = null;
            uint[]? ha = null;

            if ((flags & XDL.AlgorithmMask) is not XDiffFlags.PatienceDiff and not XDiffFlags.HistogramDiff)
            {
                rindex = new int[rchg.Length];
                ha = new uint[rchg.Length];
            }

            this.RChange = rchg;
            this.RIndex = rindex;
            this.Ha = ha;
            this.NReff = 0;
            this.DStart = 0;
            this.DLength = this.Records.Count;
        }
    }

    internal const string InvalidCharacterTypeMessage = "Only `byte` and `char` are supported character types!";

    /// <summary>
    /// Penalty if there are no non-blank lines before the split
    /// </summary>
    private const int START_OF_FILE_PENALTY = 1;

    /// <summary>
    /// Penalty if there are no non-blank lines after the split
    /// </summary>
    private const int END_OF_FILE_PENALTY = 21;

    /// <summary>
    /// Multiplier for the number of blank lines around the split
    /// </summary>
    private const int TOTAL_BLANK_WEIGHT = -30;

    /// <summary>
    /// Multiplier for the number of blank lines after the split
    /// </summary>
    private const int POST_BLANK_WEIGHT = 6;

    /// <summary>
    /// Penalty applied if the line is indented more than its predecessor
    /// </summary>
    private const int RELATIVE_INDENT_PENALTY = -4;
    /// <summary>
    /// Penalty applied if the line is indented more than its predecessor
    /// </summary>
    private const int RELATIVE_INDENT_WITH_BLANK_PENALTY = 10;

    /// <summary>
    /// Penalty applied if the line is indented less than both its predecessor and
    /// its successor
    /// </summary>
    private const int RELATIVE_OUTDENT_PENALTY = 24;
    /// <summary>
    /// Penalty applied if the line is indented less than both its predecessor and
    /// its successor
    /// </summary>
    private const int RELATIVE_OUTDENT_WITH_BLANK_PENALTY = 17;

    /// <summary>
    /// Penalty applied if the line is indented less than its predecessor but not
    /// less than its successor
    /// </summary>
    private const int RELATIVE_DEDENT_PENALTY = 23;
    /// <summary>
    /// Penalty applied if the line is indented less than its predecessor but not
    /// less than its successor
    /// </summary>
    private const int RELATIVE_DEDENT_WITH_BLANK_PENALTY = 17;

    /// <summary>
    /// How far do we slide a hunk at most?
    /// </summary>
    private const int INDENT_HEURISTIC_MAX_SLIDING = 100;

    private static void CompactChanges<TChar>(
        ref XDiffFile file1, ReadOnlySpan<TChar> file1_data,
        ref XDiffFile file2, ReadOnlySpan<TChar> file2_data,
        XDiffFlags flags) where TChar : struct, INumberBase<TChar>
    {
        // xdl_change_compact()

        var g = new XdlGroup(ref file1);
        var go = new XdlGroup(ref file2);


        int earliest_end, end_matching_other, groupsize;

        while (true)
        {
            /*
             * If the group is empty in the to-be-compacted file, skip it:
             */
            if (g.end == g.start)
                goto next;

            /*
             * Now shift the change up and then down as far as possible in
             * each direction. If it bumps into any other changes, merge
             * them.
             */
            do
            {
                groupsize = g.end - g.start;

                /*
                 * Keep track of the last "end" index that causes this
                 * group to align with a group of changed lines in the
                 * other file. -1 indicates that we haven't found such
                 * a match yet:
                 */
                end_matching_other = -1;

                /* Shift the group backward as much as possible: */
                while (g.GroupSlideUp())
                {
                    if (!go.MovePrevious())
                        Debug.Fail("group sync broken sliding up");
                }

                /*
                 * This is this highest that this group can be shifted.
                 * Record its end index:
                 */
                earliest_end = g.end;

                if (go.end > go.start)
                    end_matching_other = g.end;

                /* Now shift the group forward as far as possible: */
                while (true)
                {
                    if (!g.GroupSlideDown())
                    {
                        break;
                    }

                    if (!go.MoveNext())
                    {
                        Debug.Fail("group sync broken sliding down");
                    }

                    if (go.end > go.start)
                        end_matching_other = g.end;
                }
            }
            while (groupsize != g.end - g.start);

            /*
            * If the group can be shifted, then we can possibly use this
            * freedom to produce a more intuitive diff.
            *
            * The group is currently shifted as far down as possible, so
            * the heuristics below only have to handle upwards shifts.
            */

            if (g.end == earliest_end)
            {
                /* no shifting was possible */
            }
            else if (end_matching_other != -1)
            {
                /*
                 * Move the possibly merged group of changes back to
                 * line up with the last group of changes from the
                 * other file that it can align with.
                 */
                while (go.end == go.start)
                {
                    if (!g.GroupSlideUp())
                        Debug.Fail("match disappeared");
                    if (!go.MovePrevious())
                        Debug.Fail("group sync broken sliding to match");
                }
            }
            else if ((flags & XDiffFlags.IndentHeuristic) != 0)
            {
                int shift, best_shift = -1;

                SplitScore best_score = default;

                shift = Math.Max(Math.Max(earliest_end, g.end - groupsize - 1), g.end - INDENT_HEURISTIC_MAX_SLIDING);

                for (; shift <= g.end; ++shift)
                {
                    SplitScore score = default;

                    var m = SplitMeasurement.MeasureSplit(in file1, shift, file1_data);
                    score.AddSplit(in m);
                    m = SplitMeasurement.MeasureSplit(in file1, shift - groupsize, file1_data);
                    score.AddSplit(in m);

                    if (best_shift == -1
                        || score.CompareTo(best_score) <= 0)
                    {
                        best_score = score;
                        best_shift = shift;
                    }
                }

                while (g.end > best_shift)
                {
                    if (!g.GroupSlideUp())
                        Debug.Fail("best shift unreached");
                    if (!go.MovePrevious())
                        Debug.Fail("group sync broken sliding to blank line");
                }
            }

        next:
            if (!g.MoveNext())
                break;
            if (!go.MoveNext())
                Debug.Fail("group sync broken moving to next group");
        }

        Debug.Assert(!go.MoveNext(), "group sync broken at end of file");
    }

    private struct SplitScore : IComparable<SplitScore>
    {
        /// <summary>
        /// We only consider whether the sum of the effective indents for splits are
        /// less than (-1), equal to (0), or greater than (+1) each other. The resulting
        /// value is multiplied by the following weight and combined with the penalty to
        /// determine the better of two scores.
        /// </summary>
        private const int INDENT_WEIGHT = 60;

        /// <summary>
        /// The effective indent of this split (smaller is preferred).
        /// </summary>
        public int effective_indent;
        /// <summary>
        /// Penalty for this split (smaller is preferred).
        /// </summary>
        public int penalty;

        public readonly int CompareTo(SplitScore other)
        {
            int cmp = this.effective_indent.CompareTo(other.effective_indent) * INDENT_WEIGHT + (this.penalty - other.penalty);

            // Normalizes the return value to 1, 0, or -1
            // This is not strictly necessary
            return int.Sign(cmp);
        }

        public void AddSplit(in SplitMeasurement m)
        {
            /*
             * A place to accumulate penalty factors (positive makes this index more
             * favored):
             */
            int post_blank, total_blank, indent;
            bool any_blanks;

            if (m.pre_indent == -1 && m.pre_blank == 0)
                this.penalty += START_OF_FILE_PENALTY;

            if (m.end_of_file)
                this.penalty += END_OF_FILE_PENALTY;

            /*
             * Set post_blank to the number of blank lines following the split,
             * including the line immediately after the split:
             */
            post_blank = m.indent == -1 ? 1 + m.post_blank : 0;
            total_blank = m.pre_blank + post_blank;

            this.penalty += TOTAL_BLANK_WEIGHT * total_blank + POST_BLANK_WEIGHT * post_blank;

            if (m.indent != -1)
                indent = m.indent;
            else
                indent = m.post_indent;

            any_blanks = total_blank != 0;

            this.effective_indent += indent;

            if (indent == -1)
            {
                /* No additional adjustments needed. */
            }
            else if (m.pre_indent == -1)
            {
                /* No additional adjustments needed. */
            }
            else if (indent > m.pre_indent)
            {
                /* The line is indented more than its predecessor. */
                this.penalty += any_blanks ? RELATIVE_INDENT_WITH_BLANK_PENALTY : RELATIVE_INDENT_PENALTY;
            }
            else if (indent == m.pre_indent)
            {
                /*
                 * The line has the same indentation level as its predecessor.
                 * No additional adjustments needed.
                 */
            }
            else
            {
                /*
                 * The line is indented less than its predecessor. It could be
                 * the block terminator of the previous block, but it could
                 * also be the start of a new block (e.g., an "else" block, or
                 * maybe the previous block didn't have a block terminator).
                 * Try to distinguish those cases based on what comes next:
                 */

                if (m.post_indent != -1 && m.post_indent > indent)
                {
                    this.penalty += any_blanks ? RELATIVE_OUTDENT_WITH_BLANK_PENALTY : RELATIVE_OUTDENT_PENALTY;
                }
                else
                {
                    this.penalty += any_blanks ? RELATIVE_DEDENT_WITH_BLANK_PENALTY : RELATIVE_DEDENT_PENALTY;
                }
            }
        }
    }

    private struct SplitMeasurement
    {
        /// <summary>
        /// Is the split at the end of the file (aside from any blank lines)?
        /// </summary>
        public bool end_of_file;
        /// <summary>
        /// How much is the line immediately following the split indented (or -1
        /// if the line is blank):
        /// </summary>
        public int indent;
        /// <summary>
        /// How many consecutive lines above the split are blank?
        /// </summary>
        public int pre_blank;
        /// <summary>
        /// How much is the nearest non-blank line above the split indented (or
        /// -1 if there is no such line)?
        /// </summary>
        public int pre_indent;
        /// <summary>
        /// How many lines after the line following the split are blank?
        /// </summary>
        public int post_blank;
        /// <summary>
        /// How much is the nearest non-blank line after the line following the
        /// split indented (or -1 if there is no such line)?
        /// </summary>
        public int post_indent;

        /// <summary>
        /// If more than this number of consecutive blank rows are found, just return
        /// this value.This avoids requiring O(N^2) work for pathological cases, and
        /// also ensures that the output of score_split fits in an int.
        /// </summary>
        private const int MAX_BLANKS = 20;

        public static SplitMeasurement MeasureSplit<TChar>(in XDiffFile xdf, int split, ReadOnlySpan<TChar> file) where TChar : struct, INumberBase<TChar>
        {
            SplitMeasurement m = default;
            if (split >= xdf.Records.Count)
            {
                m.end_of_file = true;
                m.indent = -1;
            }
            else
            {
                m.end_of_file = false;
                m.indent = GetIndent(file, ref xdf.Records[split]);
            }

            m.pre_blank = 0;
            m.pre_indent = -1;

            for (int i = split - 1; i >= 0; --i)
            {
                m.pre_indent = GetIndent(file, ref xdf.Records[i]);

                if (m.pre_indent != -1)
                    break;

                m.pre_blank += 1;

                if (m.pre_blank >= MAX_BLANKS)
                {
                    m.pre_indent = 0;
                    break;
                }
            }

            m.post_blank = 0;
            m.post_indent = -1;

            for (int i = split + 1; i < xdf.Records.Count; ++i)
            {
                m.post_indent = GetIndent(file, ref xdf.Records[i]);

                if (m.post_indent != -1)
                    break;

                m.post_blank += 1;

                if (m.post_blank >= MAX_BLANKS)
                {
                    m.post_indent = 0;
                    break;
                }
            }

            return m;

            static int GetIndent(ReadOnlySpan<TChar> fileData, ref XRecord record)
            {
                const int MAX_INDENT = 200;

                ReadOnlySpan<TChar> data = fileData.Slice(record.LineOffset, record.LineLength);
                TChar space = TChar.CreateChecked(' '), tab = TChar.CreateChecked('\t');

                for (int i = 0, ret = 0; i < data.Length; ++i)
                {
                    TChar c = data[i];

                    if (!XDL.IsSpace(c))
                    {
                        return ret;
                    }
                    else if (c == space)
                    {
                        ret += 1;
                    }
                    else if (c == tab)
                    {
                        ret += 8 - ret % 8;
                    }

                    if (ret >= MAX_INDENT)
                        return MAX_INDENT;
                }

                /* The line contains only whitespace. */
                return -1;
            }
        }
    }

    public enum EmitLineType
    {
        Context,
        Added,
        Removed,
        HunkDescription
    }

    [InlineArray(128)]
    internal struct OutHunkBuffer<TChar> where TChar : struct, INumberBase<TChar>
    {
        public TChar _first;
    }

    public interface IEmitCallbacks<TChar> where TChar : struct, INumberBase<TChar>
    {
        [SkipLocalsInit]
        void EmitHunk(int old_begin, int old_count, int new_begin, int new_count, ReadOnlySpan<TChar> func)
        {
            Unsafe.SkipInit(out OutHunkBuffer<TChar> buffer);
            Span<TChar> buf = buffer;

            buf[0] = TChar.CreateChecked('@');
            buf[1] = TChar.CreateChecked('@');
            buf[2] = TChar.CreateChecked(' ');
            buf[3] = TChar.CreateChecked('-');

            int nb = 4;

            Format(old_begin, buf.Slice(nb), out int written);

            nb += written;

            if (old_count != 1)
            {
                buf[nb++] = TChar.CreateChecked(',');

                Format(old_count, buf.Slice(nb), out written);
                nb += written;
            }

            buf[nb++] = TChar.CreateChecked(' ');
            buf[nb++] = TChar.CreateChecked('+');

            Format(new_begin, buf.Slice(nb), out written);
            nb += written;

            if (new_count != 1)
            {
                buf[nb++] = TChar.CreateChecked(',');

                Format(new_count, buf.Slice(nb), out written);
                nb += written;
            }

            buf[nb++] = TChar.CreateChecked(' ');
            buf[nb++] = TChar.CreateChecked('@');
            buf[nb++] = TChar.CreateChecked('@');

            if (!func.IsEmpty)
            {
                buf[nb++] = TChar.CreateChecked(' ');

                int funclen = Math.Min(func.Length, buf.Length - nb - 1);

                func.Slice(0, funclen).CopyTo(buf.Slice(nb));
                nb += funclen;
            }

            buf[nb++] = TChar.CreateChecked('\n');

            this.EmitLine(EmitLineType.HunkDescription, buf.Slice(0, nb));

            static void Format(int value, Span<TChar> buffer, out int written)
            {
                if (typeof(TChar) == typeof(byte))
                {
                    bool success = value.TryFormat(Unsafe.BitCast<Span<TChar>, Span<byte>>(buffer), out written);
                    Debug.Assert(success);
                }
                else if (typeof(TChar) == typeof(char))
                {
                    bool success = value.TryFormat(Unsafe.BitCast<Span<TChar>, Span<char>>(buffer), out written);
                    Debug.Assert(success);
                }
                else
                {
                    throw new NotSupportedException(InvalidCharacterTypeMessage);
                }
            }
        }

        void EmitLine(EmitLineType lineType, ReadOnlySpan<TChar> line);
    }

    public enum EmitConfigFlags
    {
        None = 0,
        EmitFuncNames = 1 << 0,
        EmitNoHunkHeader = 1 << 1,
        EmitFuncContext = 1 << 2,
    }

    public sealed class EmitConfiguration<TChar> where TChar : struct, INumberBase<TChar>
    {
        public int ContextLength;
        public int InterHunkContextLength;
        public EmitConfigFlags Flags;
        public FindFuncDelegate? FindFunc;
        public EmitHunkConsumeFuncDelegate? HunkFunc;

        public delegate bool FindFuncDelegate(ReadOnlySpan<TChar> line, Span<TChar> buffer, out int written);
        public delegate void EmitHunkConsumeFuncDelegate(int start_a, int count_a, int start_b, int count_b, IEmitCallbacks<TChar> callbacks);
    }

    internal static class XDL
    {
        public const int KPDIS_RUN = 4;
        public const int MAX_EQLIMIT = 1024;
        public const int SIMSCAN_WINDOW = 100;
        public const int GUESS_NLINES1 = 256;
        public const int GUESS_NLINES2 = 20;

        public const int MAX_COST_MIN = 256;
        public const int HEUR_MIN_COST = 256;
        public const int LINE_MAX = int.MaxValue;
        public const int SNAKE_CNT = 20;
        public const int K_HEUR = 4;

        public const XDiffFlags WhitespaceOptionsMask = XDiffFlags.IgnoreWhitespace | XDiffFlags.IgnoreWhitespaceChange | XDiffFlags.IgnoreWhitespaceAtEOL | XDiffFlags.IgnoreCRAtEOL;

        public const XDiffFlags AlgorithmMask = XDiffFlags.PatienceDiff | XDiffFlags.HistogramDiff;

        public static int HashBits(int size)
        {
            // xdl_hashbits()

            return Math.Max(32 - BitOperations.LeadingZeroCount((uint)size), 1);
        }

        public static uint HashNumber(uint value, int bits)
        {
            // XDL_HASHLONG() macro

            var v0 = unchecked(value + (value >> bits));
            var mask = unchecked((1u << bits) - 1);

            return v0 & mask;
        }

        public static int BogoSqrt(int value)
        {
            // xdl_bogosqrt()

            // optimized to be O(1) with Leading Zero Count
            if (value > 0)
            {
                return 2 << ((31 - BitOperations.LeadingZeroCount((uint)value)) / 2);
            }
            else
            {
                return 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSpace<T>(T value) where T : INumberBase<T>
        {
            // XDL_ISSPACE() macro

            return uint.CreateChecked(value) is ' ' or '\r' or '\n' or '\v' or '\t' or '\f';
        }

        public static bool ContainsAnyNonSpace<TChar>(ReadOnlySpan<TChar> span) where TChar : struct, INumberBase<TChar>
        {
            if (Unsafe.SizeOf<TChar>() == 1)
            {
                return ContainsAnyNonSpace(MemoryMarshal.Cast<TChar, byte>(span));
            }
            else if (Unsafe.SizeOf<TChar>() == 2)
            {
                return ContainsAnyNonSpace(MemoryMarshal.Cast<TChar, ushort>(span));
            }
            else
            {
                throw new NotSupportedException(InvalidCharacterTypeMessage);
            }
        }

        private static bool ContainsAnyNonSpace(ReadOnlySpan<byte> span)
        {
            if (!Vector128.IsHardwareAccelerated || span.Length < Vector128<byte>.Count)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    if (!IsSpace(span[i]))
                        return true;
                }

                return false;
            }
            else
            {
                ref byte dataRef = ref MemoryMarshal.GetReference(span);

                if (Vector512.IsHardwareAccelerated && span.Length >= Vector512<byte>.Count)
                {
                    int lenMinusOne = span.Length - Vector512<byte>.Count;

                    var const4 = Vector512.Create((byte)4);
                    var const9 = Vector512.Create((byte)9);
                    var constSpace = Vector512.Create((byte)' ');

                    Vector512<byte> value, cmp;

                    for (int i = 0; i < lenMinusOne; i += Vector512<byte>.Count)
                    {
                        value = Vector512.LoadUnsafe(ref dataRef, (nuint)i);

                        cmp = Vector512.GreaterThan(value - const9, const4);
                        cmp = Vector512.AndNot(cmp, Vector512.Equals(value, constSpace));

                        if (!Vector512.EqualsAll(cmp, Vector512<byte>.Zero))
                            return true;
                    }

                    value = Vector512.LoadUnsafe(ref dataRef, (nuint)lenMinusOne);

                    cmp = Vector512.GreaterThan(value - const9, const4);
                    cmp = Vector512.AndNot(cmp, Vector512.Equals(value, constSpace));

                    return !Vector512.EqualsAll(cmp, Vector512<byte>.Zero);
                }
                else if (Vector256.IsHardwareAccelerated && span.Length >= Vector256<byte>.Count)
                {
                    int lenMinusOne = span.Length - Vector256<byte>.Count;

                    var const4 = Vector256.Create((byte)4);
                    var const9 = Vector256.Create((byte)9);
                    var constSpace = Vector256.Create((byte)' ');

                    Vector256<byte> value, cmp;

                    for (int i = 0; i < lenMinusOne; i += Vector256<byte>.Count)
                    {
                        value = Vector256.LoadUnsafe(ref dataRef, (nuint)i);

                        cmp = Vector256.GreaterThan(value - const9, const4);
                        cmp = Vector256.AndNot(cmp, Vector256.Equals(value, constSpace));

                        if (!Vector256.EqualsAll(cmp, Vector256<byte>.Zero))
                            return true;
                    }

                    value = Vector256.LoadUnsafe(ref dataRef, (nuint)lenMinusOne);

                    cmp = Vector256.GreaterThan(value - const9, const4);
                    cmp = Vector256.AndNot(cmp, Vector256.Equals(value, constSpace));

                    return !Vector256.EqualsAll(cmp, Vector256<byte>.Zero);
                }
                else
                {
                    int lenMinusOne = span.Length - Vector128<byte>.Count;

                    var const4 = Vector128.Create((byte)4);
                    var const9 = Vector128.Create((byte)9);
                    var constSpace = Vector128.Create((byte)' ');

                    Vector128<byte> value, cmp;

                    for (int i = 0; i < lenMinusOne; i += Vector128<byte>.Count)
                    {
                        value = Vector128.LoadUnsafe(ref dataRef, (nuint)i);

                        cmp = Vector128.GreaterThan(value - const9, const4);
                        cmp = Vector128.AndNot(cmp, Vector128.Equals(value, constSpace));

                        if (!Vector128.EqualsAll(cmp, Vector128<byte>.Zero))
                            return true;
                    }

                    value = Vector128.LoadUnsafe(ref dataRef, (nuint)lenMinusOne);

                    cmp = Vector128.GreaterThan(value - const9, const4);
                    cmp = Vector128.AndNot(cmp, Vector128.Equals(value, constSpace));

                    return !Vector128.EqualsAll(cmp, Vector128<byte>.Zero);
                }
            }
        }

        private static bool ContainsAnyNonSpace(ReadOnlySpan<ushort> span)
        {
            if (!Vector128.IsHardwareAccelerated || span.Length < Vector128<ushort>.Count * 2)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    if (!IsSpace(span[i]))
                        return true;
                }

                return false;
            }
            else
            {
                ref ushort dataRef = ref MemoryMarshal.GetReference(span);

                if (Vector512.IsHardwareAccelerated && span.Length >= Vector512<ushort>.Count * 2)
                {
                    ref ushort minusOneIteration = ref Unsafe.Add(ref dataRef, span.Length - Vector512<ushort>.Count * 2);

                    var const4 = Vector512.Create((byte)4);
                    var const9 = Vector512.Create((byte)9);
                    var constSpace = Vector512.Create((byte)' ');

                    Vector512<byte> value, cmp;

                    while (Unsafe.IsAddressLessThan(ref dataRef, ref minusOneIteration))
                    {
                        value = LoadAndNarrow512(ref dataRef);

                        cmp = Vector512.GreaterThan(value - const9, const4);
                        cmp = Vector512.AndNot(cmp, Vector512.Equals(value, constSpace));

                        if (!Vector512.EqualsAll(cmp, Vector512<byte>.Zero))
                            return true;

                        dataRef = ref Unsafe.Add(ref dataRef, Vector512<ushort>.Count * 2);
                    }

                    value = LoadAndNarrow512(ref minusOneIteration);

                    cmp = Vector512.GreaterThan(value - const9, const4);
                    cmp = Vector512.AndNot(cmp, Vector512.Equals(value, constSpace));

                    return !Vector512.EqualsAll(cmp, Vector512<byte>.Zero);
                }
                else if(Vector256.IsHardwareAccelerated && span.Length >= Vector256<ushort>.Count * 2)
                {
                    ref ushort minusOneIteration = ref Unsafe.Add(ref dataRef, span.Length - Vector256<ushort>.Count * 2);

                    var const4 = Vector256.Create((byte)4);
                    var const9 = Vector256.Create((byte)9);
                    var constSpace = Vector256.Create((byte)' ');

                    Vector256<byte> value, cmp;

                    while (Unsafe.IsAddressLessThan(ref dataRef, ref minusOneIteration))
                    {
                        value = LoadAndNarrow256(ref dataRef);

                        cmp = Vector256.GreaterThan(value - const9, const4);
                        cmp = Vector256.AndNot(cmp, Vector256.Equals(value, constSpace));

                        if (!Vector256.EqualsAll(cmp, Vector256<byte>.Zero))
                            return true;

                        dataRef = ref Unsafe.Add(ref dataRef, Vector256<ushort>.Count * 2);
                    }

                    value = LoadAndNarrow256(ref minusOneIteration);

                    cmp = Vector256.GreaterThan(value - const9, const4);
                    cmp = Vector256.AndNot(cmp, Vector256.Equals(value, constSpace));

                    return !Vector256.EqualsAll(cmp, Vector256<byte>.Zero);
                }
                else
                {
                    ref ushort minusOneIteration = ref Unsafe.Add(ref dataRef, span.Length - Vector128<ushort>.Count * 2);

                    var const4 = Vector128.Create((byte)4);
                    var const9 = Vector128.Create((byte)9);
                    var constSpace = Vector128.Create((byte)' ');

                    Vector128<byte> value, cmp;

                    while (Unsafe.IsAddressLessThan(ref dataRef, ref minusOneIteration))
                    {
                        value = LoadAndNarrow128(ref dataRef);

                        cmp = Vector128.GreaterThan(value - const9, const4);
                        cmp = Vector128.AndNot(cmp, Vector128.Equals(value, constSpace));

                        if (!Vector128.EqualsAll(cmp, Vector128<byte>.Zero))
                            return true;

                        dataRef = ref Unsafe.Add(ref dataRef, Vector128<ushort>.Count * 2);
                    }

                    value = LoadAndNarrow128(ref minusOneIteration);

                    cmp = Vector128.GreaterThan(value - const9, const4);
                    cmp = Vector128.AndNot(cmp, Vector128.Equals(value, constSpace));

                    return !Vector128.EqualsAll(cmp, Vector128<byte>.Zero);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static Vector512<byte> LoadAndNarrow512(ref ushort dataRef)
                {
                    if (Avx512BW.IsSupported)
                    {
                        var valueLo = Vector512.LoadUnsafe(ref dataRef, 0).AsInt16();
                        var valueHi = Vector512.LoadUnsafe(ref dataRef, (nuint)Vector512<ushort>.Count).AsInt16();

                        return Avx512BW.PackUnsignedSaturate(valueLo, valueHi);
                    }
                    else
                    {
                        var valueLo = Vector512.LoadUnsafe(ref dataRef, 0);
                        var valueHi = Vector512.LoadUnsafe(ref dataRef, (nuint)Vector512<ushort>.Count);

                        valueLo = Vector512.Min(valueLo, Vector512.Create((ushort)255));
                        valueHi = Vector512.Min(valueHi, Vector512.Create((ushort)255));

                        return Vector512.Narrow(valueLo, valueHi);
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static Vector256<byte> LoadAndNarrow256(ref ushort dataRef)
                {
                    if (Avx2.IsSupported)
                    {
                        var valueLo = Vector256.LoadUnsafe(ref dataRef, 0).AsInt16();
                        var valueHi = Vector256.LoadUnsafe(ref dataRef, (nuint)Vector256<ushort>.Count).AsInt16();

                        return Avx2.PackUnsignedSaturate(valueLo, valueHi);
                    }
                    else
                    {
                        var valueLo = Vector256.LoadUnsafe(ref dataRef, 0);
                        var valueHi = Vector256.LoadUnsafe(ref dataRef, (nuint)Vector256<ushort>.Count);

                        valueLo = Vector256.Min(valueLo, Vector256.Create((ushort)255));
                        valueHi = Vector256.Min(valueHi, Vector256.Create((ushort)255));

                        return Vector256.Narrow(valueLo, valueHi);
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static Vector128<byte> LoadAndNarrow128(ref ushort dataRef)
                {
                    if (Sse2.IsSupported)
                    {
                        var valueLo = Vector128.LoadUnsafe(ref dataRef, 0).AsInt16();
                        var valueHi = Vector128.LoadUnsafe(ref dataRef, (nuint)Vector128<ushort>.Count).AsInt16();

                        return Sse2.PackUnsignedSaturate(valueLo, valueHi);
                    }
                    else
                    {
                        var valueLo = Vector128.LoadUnsafe(ref dataRef, 0);
                        var valueHi = Vector128.LoadUnsafe(ref dataRef, (nuint)Vector128<ushort>.Count);

                        valueLo = Vector128.Min(valueLo, Vector128.Create((ushort)255));
                        valueHi = Vector128.Min(valueHi, Vector128.Create((ushort)255));

                        return Vector128.Narrow(valueLo, valueHi);
                    }
                }
            }
        }

        public static void HashLines<TChar>(ReadOnlySpan<TChar> fileData, ref ValueList<XRecord> lines, XDiffFlags flags)
            where TChar : struct, INumberBase<TChar>
        {
            if ((flags & WhitespaceOptionsMask) == 0)
            {
                HashLines<TChar, HashImpl_NoOptions<TChar>>(fileData, ref lines);
            }
            else if ((flags & WhitespaceOptionsMask) == XDiffFlags.IgnoreCRAtEOL)
            {
                HashLines<TChar, HashImpl_IgnoreCRAtEOL<TChar>>(fileData, ref lines);
            }
            else if ((flags & XDiffFlags.IgnoreWhitespace) != 0)
            {
                HashLines<TChar, HashImpl_IgnoreWhitespace<TChar>>(fileData, ref lines);
            }
            else if ((flags & XDiffFlags.IgnoreWhitespaceChange) != 0)
            {
                HashLines<TChar, HashImpl_IgnoreWhitespaceChange<TChar>>(fileData, ref lines);
            }
            else
            {
                Debug.Assert((flags & XDiffFlags.IgnoreWhitespaceAtEOL) != 0);

                HashLines<TChar, HashImpl_IgnoreWhitespaceAtEOL<TChar>>(fileData, ref lines);
            }
        }

        private interface IHashImpl<TChar>
            where TChar : struct, INumberBase<TChar>
        {
            static virtual ReadOnlySpan<TChar> TrimSpan(ReadOnlySpan<TChar> span)
            {
                // default to no trimming
                return span;
            }

            static abstract uint HashLine(ReadOnlySpan<TChar> span);
        }

        private static void HashLines<TChar, THashImpl>(ReadOnlySpan<TChar> fileData, ref ValueList<XRecord> lineRecords)
            where TChar : struct, INumberBase<TChar>
            where THashImpl : IHashImpl<TChar>
        {
            TChar newLine = TChar.CreateChecked('\n');

            for (int lineBegin = 0; (uint)lineBegin < (uint)fileData.Length;)
            {
                int idx = fileData.Slice(lineBegin).IndexOf(newLine);
                int length;
                ReadOnlySpan<TChar> line;

                if (idx >= 0)
                {
                    line = fileData.Slice(lineBegin, idx);
                    length = idx + 1;
                }
                else
                {
                    line = fileData.Slice(lineBegin);
                    length = line.Length;
                }

                uint hash = THashImpl.HashLine(THashImpl.TrimSpan(line));

                lineRecords.Add(new XRecord()
                {
                    LineOffset = lineBegin,
                    LineLength = length,
                    Hash = hash
                });

                lineBegin += length;
            }
        }

        private struct HashImpl_NoOptions<TChar> : IHashImpl<TChar>
            where TChar : struct, INumberBase<TChar>
        {
            public static uint HashLine(ReadOnlySpan<TChar> span)
            {
                ref TChar dataRef = ref MemoryMarshal.GetReference(span);
                nuint length = (nuint)span.Length;

                uint ha = 5381;

                while (length >= 8)
                {
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(Unsafe.Add(ref dataRef, 0)));
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(Unsafe.Add(ref dataRef, 1)));
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(Unsafe.Add(ref dataRef, 2)));
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(Unsafe.Add(ref dataRef, 3)));
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(Unsafe.Add(ref dataRef, 4)));
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(Unsafe.Add(ref dataRef, 5)));
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(Unsafe.Add(ref dataRef, 6)));
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(Unsafe.Add(ref dataRef, 7)));

                    dataRef = ref Unsafe.Add(ref dataRef, 8);
                    length -= 8;
                }

                while (length > 0)
                {
                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(dataRef));

                    dataRef = ref Unsafe.Add(ref dataRef, 1);
                    length -= 1;
                }

                return ha;
            }
        }

        private struct HashImpl_IgnoreCRAtEOL<TChar> : IHashImpl<TChar>
            where TChar : struct, INumberBase<TChar>
        {
            public static ReadOnlySpan<TChar> TrimSpan(ReadOnlySpan<TChar> span)
            {
                return span.EndsWith(TChar.CreateChecked('\r')) ? span.Slice(0, span.Length - 1) : span;
            }

            public static uint HashLine(ReadOnlySpan<TChar> span)
            {
                return HashImpl_NoOptions<TChar>.HashLine(span);
            }
        }

        private struct HashImpl_IgnoreWhitespace<TChar> : IHashImpl<TChar>
            where TChar : struct, INumberBase<TChar>
        {
            public static uint HashLine(ReadOnlySpan<TChar> span)
            {
                uint ha = 5381;

                for (int index = 0; index < span.Length; ++index)
                {
                    TChar value = span[index];

                    if (!IsSpace(value))
                    {
                        ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(value));
                    }
                }

                return ha;
            }
        }

        private struct HashImpl_IgnoreWhitespaceChange<TChar> : IHashImpl<TChar>
            where TChar : struct, INumberBase<TChar>
        {
            public static ReadOnlySpan<TChar> TrimSpan(ReadOnlySpan<TChar> span)
            {
                return HashImpl_IgnoreWhitespaceAtEOL<TChar>.TrimSpan(span);
            }

            public static uint HashLine(ReadOnlySpan<TChar> span)
            {
                uint ha = 5381;

                for (int index = 0; index < span.Length; ++index)
                {
                    TChar value = span[index];

                    if (IsSpace(value))
                    {
                        ha = unchecked(ha + (ha << 5) ^ ' ');

                        while ((uint)index + 1 < (uint)span.Length && IsSpace(span[index + 1]))
                            index += 1;

                        continue;
                    }

                    ha = unchecked(ha + (ha << 5) ^ uint.CreateTruncating(value));
                }

                return ha;
            }
        }

        private struct HashImpl_IgnoreWhitespaceAtEOL<TChar> : IHashImpl<TChar>
            where TChar : struct, INumberBase<TChar>
        {
            public static ReadOnlySpan<TChar> TrimSpan(ReadOnlySpan<TChar> span)
            {
                int hashedLength = span.Length;
                for (; hashedLength > 0; --hashedLength)
                {
                    if (!IsSpace(span[hashedLength - 1]))
                        break;
                }

                return span.Slice(0, hashedLength);
            }

            public static uint HashLine(ReadOnlySpan<TChar> span)
            {
                return HashImpl_NoOptions<TChar>.HashLine(span);
            }
        }
    }

    private ref struct XdlGroup
    {
        private readonly ReadOnlySpan<XRecord> _records;
        private readonly byte[] _rChange;

        public int start;
        public int end;

        public XdlGroup(ref XDiffFile xdf)
        {
            _records = xdf.Records.GetSpan();
            _rChange = xdf.RChange;
            start = -1;
            end = -1;
        }

        public bool MoveNext()
        {
            start = ++end;

            if (end >= _records.Length)
                return false;

            while (_rChange[end] != 0)
            {
                end += 1;
            }

            return true;
        }

        public bool MovePrevious()
        {
            if (start <= 0)
                return false;

            end = --start;
            while (_rChange[start - 1] != 0)
                start -= 1;

            return true;
        }

        public bool GroupSlideDown()
        {
            if (end < _records.Length && _records[start].Hash == _records[end].Hash)
            {
                _rChange[start++] = 0;
                _rChange[end++] = 1;

                while (_rChange[end] != 0)
                    end += 1;

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GroupSlideUp()
        {
            if (start > 0 && _records[start - 1].Hash == _records[end - 1].Hash)
            {
                _rChange[--start] = 1;
                _rChange[--end] = 0;

                while (_rChange[start - 1] != 0)
                    start -= 1;

                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private struct LineClassifier
    {
        public int[] _hashLookup;
        private ValueList<LineClass> _rcrecs;
        public int _hbits;

        public LineClassifier(int size)
        {
            Initialize(size);
        }

        [MemberNotNull(nameof(_hashLookup))]
        public void Initialize(int size)
        {
            // xdl_init_classifier()
            _hbits = XDL.HashBits(size);

            int lookupSize = 1 << _hbits;

            int[]? lookup = _hashLookup;

            if (lookup is null || lookup.Length < lookupSize)
            {
                lookup = new int[lookupSize];
                _hashLookup = lookup;
            }

            Array.Fill(lookup, -1);

            _rcrecs.Clear();
            _rcrecs.EnsureCapacity(size);
        }

        public ReadOnlySpan<LineClass> RcRecords => _rcrecs.GetSpan();

        public void ClassifyLine<TChar>(ref XDiffContext<TChar> ctx, int pass, /*int hbits, int[] rhash,*/ int recIndex, ref XRecord rec)
            where TChar : struct, INumberBase<TChar>
        {
            // xdl_classify_record()

            int hi = (int)XDL.HashNumber(rec.Hash, _hbits);

            int lineOffset = rec.LineOffset, lineLength = rec.LineLength;
            uint lineHash = rec.Hash;

            var xdf1Span = ctx.File1;
            var xdf2Span = ctx.File2;

            int[] rcHash = _hashLookup;
            Span<LineClass> lineClasses = _rcrecs.GetSpan();

            int rcrec = rcHash[hi];

            if (rcrec >= 0)
            {
                var rhsSpan = (pass == 1 ? xdf1Span : xdf2Span).Slice(lineOffset, lineLength);

                do
                {
                    ref var lineClass = ref lineClasses[rcrec];

                    if (lineClass.Hash == lineHash)
                    {
                        var lhsSpan = (lineClass.Pass == 1 ? xdf1Span : xdf2Span).Slice(lineClass.LineOffset, lineClass.LineLength);

                        if (DoLinesMatch(lhsSpan, rhsSpan, ctx.Flags))
                            break;
                    }

                    rcrec = lineClass.Next;
                }
                while (rcrec >= 0);
            }

            if (rcrec < 0)
            {
                rcrec = _rcrecs.Add(new LineClass()
                {
                    LineOffset = lineOffset,
                    LineLength = lineLength,
                    Hash = lineHash,
                    Pass = pass,
                    Next = -1
                });

                _rcrecs[rcrec].Index = rcrec;
                _rcrecs[rcrec].Next = rcHash[hi];
                rcHash[hi] = rcrec;
            }

            if (pass == 1)
                _rcrecs[rcrec].Length1 += 1;
            else
                _rcrecs[rcrec].Length2 += 1;

            rec.Hash = (uint)_rcrecs[rcrec].Index;

            //hi = (int)XDL.HashNumber(rec.Hash, hbits);

            //rec.Next = rhash[hi];
            //rhash[hi] = recIndex;
        }

        private static bool DoLinesMatch<TChar>(ReadOnlySpan<TChar> line1, ReadOnlySpan<TChar> line2, XDiffFlags flags)
            where TChar : struct, INumberBase<TChar>
        {
            // xdl_recmatch()

            if (line1.SequenceEqual(line2))
                return true;

            if ((flags & XDL.WhitespaceOptionsMask) == 0)
                return false;

            int i1 = 0, i2 = 0;

            /*
             * -w matches everything that matches with -b, and -b in turn
             * matches everything that matches with --ignore-space-at-eol,
             * which in turn matches everything that matches with --ignore-cr-at-eol.
             *
             * Each flavor of ignoring needs different logic to skip whitespaces
             * while we have both sides to compare.
             */
            if ((flags & XDiffFlags.IgnoreWhitespace) != 0)
            {
                while (true)
                {
                    while ((uint)i1 < (uint)line1.Length && XDL.IsSpace(line1[i1]))
                        i1++;

                    while ((uint)i2 < (uint)line2.Length && XDL.IsSpace(line2[i2]))
                        i2++;

                    if ((uint)i1 >= (uint)line1.Length || (uint)i2 >= (uint)line2.Length)
                    {
                        break;
                    }

                    if (line1[i1] != line2[i2])
                        return false;

                    i1 += 1;
                    i2 += 1;
                }
            }
            else if ((flags & XDiffFlags.IgnoreWhitespaceChange) != 0)
            {
                while ((uint)i1 < (uint)line1.Length && (uint)i2 < (uint)line2.Length)
                {
                    if (XDL.IsSpace(line1[i1]) && XDL.IsSpace(line2[i2]))
                    {
                        i1 += 1;
                        i2 += 1;

                        /* Skip matching spaces and try again */
                        while ((uint)i1 < (uint)line1.Length && XDL.IsSpace(line1[i1]))
                            i1++;

                        while ((uint)i2 < (uint)line2.Length && XDL.IsSpace(line2[i2]))
                            i2++;

                        continue;
                    }

                    if (line1[i1] != line2[i2])
                        return false;

                    i1 += 1;
                    i2 += 1;
                }
            }
            else if ((flags & (XDiffFlags.IgnoreWhitespaceAtEOL | XDiffFlags.IgnoreCRAtEOL)) != 0) // if either flag is specified, take this path
            {
                while ((uint)i1 < (uint)line1.Length && (uint)i2 < (uint)line2.Length && line1[i1] == line2[i2])
                {
                    i1++;
                    i2++;
                }

                if ((flags & XDiffFlags.IgnoreWhitespaceAtEOL) == 0) // Ignore CR at EOL is flagged
                {
                    return EndsWithOptionalCR(line1, i1)
                        && EndsWithOptionalCR(line2, i2);
                }
            }

            /*
             * After running out of one side, the remaining side must have
             * nothing but whitespace for the lines to match.  Note that
             * ignore-whitespace-at-eol case may break out of the loop
             * while there still are characters remaining on both lines.
             */
            if (i1 < line1.Length)
            {
                while (i1 < line1.Length && XDL.IsSpace(line1[i1]))
                    i1++;

                if (i1 < line1.Length)
                    return false;
            }

            if (i2 < line2.Length)
            {
                while (i2 < line2.Length && XDL.IsSpace(line2[i2]))
                    i2++;

                if (i2 < line2.Length)
                    return false;
            }

            return true;

            static bool EndsWithOptionalCR(ReadOnlySpan<TChar> line, int index)
            {
                // ends_with_optional_cr()

                bool complete = line.Length > 0 && line[^1] == TChar.CreateChecked('\n');

                int size = line.Length;

                if (complete)
                    size--;

                if (size == index)
                    return true;

                /* do not ignore CR at the end of an incomplete line */
                if (complete && size == index + 1 && line[index] == TChar.CreateChecked('\r'))
                    return true;

                return false;
            }
        }

        public struct LineClass
        {
            public int Next;
            public uint Hash;
            public int Pass;
            public int LineOffset;
            public int LineLength;
            public int Index;
            public int Length1, Length2;
        }
    }

    public class Utf8Comparer : IEqualityComparer<ImmutableArray<byte>>,
        IAlternateEqualityComparer<ReadOnlySpan<byte>, ImmutableArray<byte>>
    {
        public bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
        {
            return x.AsSpan().SequenceEqual(y.AsSpan());
        }

        public int GetHashCode([DisallowNull] ImmutableArray<byte> obj)
        {
            return HashCode<byte>.Combine(obj.AsSpan());
        }

        public bool Equals(ReadOnlySpan<byte> alternate, ImmutableArray<byte> other)
        {
            return alternate.SequenceEqual(other.AsSpan());
        }

        public int GetHashCode(ReadOnlySpan<byte> alternate)
        {
            return HashCode<byte>.Combine(alternate);
        }

        public ImmutableArray<byte> Create(ReadOnlySpan<byte> alternate)
        {
            return ImmutableArray.Create(alternate);
        }
    }
}
