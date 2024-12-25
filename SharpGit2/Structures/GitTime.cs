namespace SharpGit2;

public readonly struct GitTime(ulong time, int offset) : IEquatable<GitTime>
{
    /// <summary>
    /// Seconds that have passed since 1970-01-01T00:00:00Z (UTC)
    /// </summary>
    public readonly ulong Time = time;
    /// <summary>
    /// Gets the total number of minutes representing the time's offset from Coordinated Universal Time (UTC).
    /// </summary>
    public readonly int Offset = offset;
    /// <summary>
    /// '+' or '-' depending on the sign of <see cref="Offset"/>
    /// </summary>
    public readonly byte Sign = (byte)(offset < 0 ? '-' : '+');

    public override bool Equals(object? obj)
    {
        return obj is GitTime other && Equals(other);
    }

    public bool Equals(GitTime other)
    {
        return Time == other.Time && Offset == other.Offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Time, Offset);
    }

    public override string ToString()
    {
        return ((DateTimeOffset)this).ToString();
    }

    public static GitTime Now => (GitTime)DateTimeOffset.Now;

    public static explicit operator GitTime(DateTimeOffset time)
    {
        return new((ulong)time.ToUnixTimeSeconds(), time.TotalOffsetMinutes);
    }

    public static implicit operator DateTimeOffset(GitTime time)
    {
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds((long)time.Time).ToOffset(new TimeSpan(0, time.Offset, 0));
        }
        catch
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "GitTime value does not convert to a valid DateTimeOffset!");
        }
    }

    public static bool operator ==(GitTime left, GitTime right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GitTime left, GitTime right)
    {
        return !left.Equals(right);
    }
}
