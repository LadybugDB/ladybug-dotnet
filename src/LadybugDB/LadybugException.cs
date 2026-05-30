using System;

namespace LadybugDB;

/// <summary>Base type for all errors raised by the Ladybug binding.</summary>
public class LadybugException : Exception
{
    public LadybugException(string message)
        : base(message)
    {
    }

    public LadybugException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Raised when a Cypher query or prepared statement fails to execute.</summary>
public sealed class LadybugQueryException : LadybugException
{
    public LadybugQueryException(string message)
        : base(message)
    {
    }
}
