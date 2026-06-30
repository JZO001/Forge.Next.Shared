using ErrorOr;
using Forge.Next.Shared;
using Shouldly;
using Xunit;

namespace Forge.Next.Shared.Tests;

/// <summary>
/// Unit tests for <see cref="ExceptionErrorOrExtensions"/>: turning an <see cref="Exception"/> (and its
/// inner-exception chain) into ErrorOr <see cref="Error"/> values.
///
/// The local <c>AddError</c> recursion inside the production method is a private local function; it is
/// covered indirectly here by feeding exceptions that have inner-exception chains and asserting that one
/// error is produced per exception, in outermost-to-innermost order.
/// </summary>
public class ExceptionErrorOrExtensionsTests
{
    #region ToErrorOrError -> List<Error>

    /// <summary>
    /// Tests <see cref="ExceptionErrorOrExtensions.ToErrorOrError(Exception, ErrorType)"/>: a single
    /// exception yields one error with message/metadata populated, and an inner-exception chain yields one
    /// ordered error per exception.
    /// </summary>
    [Fact]
    public void ToErrorOrErrorTest()
    {
        // Throw-and-catch so the exception actually carries a stack trace in its metadata.
        Exception thrown;
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        List<Error> errors = thrown.ToErrorOrError();

        // A single exception (no inner) -> exactly one error.
        errors.Count.ShouldBe(1);

        Error error = errors[0];
        error.Description.ShouldBe("boom");                 // description == exception message
        error.Type.ShouldBe(ErrorType.Unexpected);          // default expectedErrorType

        // Metadata captures the exception's type (assembly-qualified), message and stack trace.
        error.Metadata.ShouldNotBeNull();
        error.Metadata!["type"].ShouldBe(typeof(InvalidOperationException).AssemblyQualifiedName);
        error.Metadata!["message"].ShouldBe("boom");
        error.Metadata!.ShouldContainKey("stackTrace");
        ((string)error.Metadata!["stackTrace"]).ShouldNotBeNullOrEmpty();

        // Inner-exception chain -> one error per exception, ordered outermost -> innermost.
        Exception withInner = new("outer", new InvalidOperationException("inner"));
        List<Error> chain = withInner.ToErrorOrError();
        chain.Count.ShouldBe(2);
        chain[0].Description.ShouldBe("outer");
        chain[1].Description.ShouldBe("inner");
    }

    /// <summary>
    /// Verifies that every supported <see cref="ErrorType"/> is mapped to an <see cref="Error"/> of the same
    /// type by the internal switch expression.
    /// </summary>
    /// <param name="errorType">The error type requested by the caller.</param>
    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Unexpected)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    public void ToErrorOrErrorErrorTypeMappingTest(ErrorType errorType)
    {
        // The produced error's Type must match exactly the requested ErrorType.
        Error error = new InvalidOperationException("x").ToErrorOrError(errorType).Single();
        error.Type.ShouldBe(errorType);
    }

    #endregion

    #region ToErrorOrError<TValue> -> ErrorOr<TValue>

    /// <summary>
    /// Tests <see cref="ExceptionErrorOrExtensions.ToErrorOrError{TValue}(Exception, ErrorType)"/>: the
    /// generic overload produces a failed <see cref="ErrorOr{TValue}"/> wrapping the same error list, in the
    /// same order, with the requested error type.
    /// </summary>
    [Fact]
    public void ToErrorOrErrorGenericTest()
    {
        Exception ex = new("outer", new InvalidOperationException("inner"));

        ErrorOr<int> result = ex.ToErrorOrError<int>(ErrorType.Validation);

        result.IsError.ShouldBeTrue();                      // it is a failure, never a value
        result.Errors.Count.ShouldBe(2);                    // one error per exception in the chain
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Description.ShouldBe("outer");     // ordered outermost-first...
        result.Errors[1].Description.ShouldBe("inner");      // ...then the inner exception
    }

    #endregion
}
