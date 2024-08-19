namespace SharpGit2;

public struct GitIndexTime(int seconds, uint nanoseconds)
{
    public int Seconds = seconds;
    public uint Nanoseconds = nanoseconds;
}