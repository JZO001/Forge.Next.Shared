using Forge.Next.Shared;
using Shouldly;
using Xunit;

namespace Forge.Next.Shared.Tests;

/// <summary>
/// Unit tests for <see cref="InitializationException"/>, covering its three constructors and confirming it
/// behaves as a regular <see cref="Exception"/> subtype.
/// </summary>
public class InitializationExceptionTests
{
    /// <summary>
    /// Tests the parameterless constructor: it produces a valid <see cref="Exception"/> with no inner
    /// exception. (The framework supplies a default, non-null message for parameterless exceptions.)
    /// </summary>
    [Fact]
    public void ConstructorTest()
    {
        InitializationException exception = new();

        exception.ShouldBeAssignableTo<Exception>();   // it is an Exception
        exception.InnerException.ShouldBeNull();        // no inner exception was provided
        exception.Message.ShouldNotBeNull();            // default framework message
    }

    /// <summary>
    /// Tests the message constructor: the supplied message is exposed via <see cref="Exception.Message"/>.
    /// </summary>
    [Fact]
    public void ConstructorWithMessageTest()
    {
        InitializationException exception = new("initialization failed");

        exception.Message.ShouldBe("initialization failed");
        exception.InnerException.ShouldBeNull();
    }

    /// <summary>
    /// Tests the message + inner-exception constructor: both the message and the inner exception are stored.
    /// </summary>
    [Fact]
    public void ConstructorWithMessageAndInnerExceptionTest()
    {
        InvalidOperationException inner = new("root cause");
        InitializationException exception = new("initialization failed", inner);

        exception.Message.ShouldBe("initialization failed");
        exception.InnerException.ShouldBeSameAs(inner);
    }
}
