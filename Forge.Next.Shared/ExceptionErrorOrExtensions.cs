using ErrorOr;

namespace Forge.Next.Shared;

/// <summary>
/// Extension methods that convert an <see cref="Exception"/> (together with its inner-exception chain)
/// into <see cref="Error"/> values consumable by the <c>ErrorOr</c> library.
/// </summary>
public static class ExceptionErrorOrExtensions
{

    /// <summary>
    /// Converts <paramref name="ex"/> and its inner-exception chain into a failed
    /// <see cref="ErrorOr{TValue}"/> carrying one <see cref="Error"/> per exception.
    /// </summary>
    /// <typeparam name="TValue">The value type of the resulting <see cref="ErrorOr{TValue}"/>.</typeparam>
    /// <param name="ex">The exception to convert.</param>
    /// <param name="expectedErrorType">The <see cref="ErrorType"/> applied to every produced error.</param>
    /// <returns>A failed <see cref="ErrorOr{TValue}"/> containing the errors derived from the exception chain.</returns>
    public static ErrorOr<TValue> ToErrorOrError<TValue>(this Exception ex, ErrorType expectedErrorType = ErrorType.Unexpected)
        => ex.ToErrorOrError(expectedErrorType)
             .ToErrorOr<TValue>();

    /// <summary>
    /// Walks <paramref name="ex"/> and its <see cref="Exception.InnerException"/> chain, producing one
    /// <see cref="Error"/> per exception. Each error's description is the exception message, and its metadata
    /// captures the exception's assembly-qualified type name, message, and stack trace.
    /// </summary>
    /// <param name="ex">The exception to convert.</param>
    /// <param name="expectedErrorType">
    /// The <see cref="ErrorType"/> that selects which <see cref="Error"/> factory is used for every exception
    /// in the chain. Unrecognized values fall back to <see cref="Error.Unexpected(string, string, System.Collections.Generic.Dictionary{string, object})"/>.
    /// </param>
    /// <returns>A list of errors ordered from the outermost exception to the innermost.</returns>
    public static List<Error> ToErrorOrError(this Exception ex, ErrorType expectedErrorType = ErrorType.Unexpected)
    {
        List<Error> errors = new List<Error>();

        void AddError(Exception exception)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>()
                {
                    {
                        "type", exception.GetType().AssemblyQualifiedName ?? string.Empty
                    },
                    {
                        "message", exception.Message
                    },
                    {
                        "stackTrace", exception.StackTrace ?? string.Empty
                    }
                };

            errors.Add(expectedErrorType switch
            {
                ErrorType.Validation => Error.Validation(description: exception.Message, metadata: dict),
                ErrorType.Forbidden => Error.Forbidden(description: exception.Message, metadata: dict),
                ErrorType.Unexpected => Error.Unexpected(description: exception.Message, metadata: dict),
                ErrorType.NotFound => Error.NotFound(description: exception.Message, metadata: dict),
                ErrorType.Unauthorized => Error.Unauthorized(description: exception.Message, metadata: dict),
                ErrorType.Conflict => Error.Conflict(description: exception.Message, metadata: dict),
                ErrorType.Failure => Error.Failure(description: exception.Message, metadata: dict),
                _ => Error.Unexpected(description: exception.Message, metadata: dict)
            });

            if (exception.InnerException is not null)
            {
                AddError(exception.InnerException);
            }
        }

        AddError(ex);

        return errors;
    }

}
