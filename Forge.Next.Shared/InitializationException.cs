namespace Forge.Next.Shared;

/// <summary>
/// Exception for initialization fail scenarios
/// </summary>
public class InitializationException : Exception
{

    #region Constructor(s)

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializationException"/> class.
    /// </summary>
    public InitializationException()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializationException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public InitializationException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializationException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The innerException.</param>
    public InitializationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    #endregion

}
