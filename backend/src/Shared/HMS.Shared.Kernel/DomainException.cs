namespace HMS.Shared.Kernel;

/// <summary>
/// Base type for exceptions raised when a domain invariant is violated (e.g. an
/// entity constructed with invalid state). Reserved for conditions that should
/// never happen if calling code is correct — expected business failures (not
/// found, conflict, validation) are represented with <see cref="Result"/> instead.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
