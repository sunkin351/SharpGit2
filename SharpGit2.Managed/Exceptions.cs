namespace SharpGit2.Managed;

public class Git2Exception : Exception
{
    public Git2Exception(string message) : base(message)
    {
    }

    public Git2Exception(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class Git2OSException : Git2Exception
{
    public Git2OSException(string message) : base(message)
    {
    }

    public Git2OSException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class Git2ConfigException : Git2Exception
{
    public Git2ConfigException(string message) : base(message)
    {
    }

    public Git2ConfigException(string message, Exception innerException) : base(message, innerException)
    {
    }
}