using System.Reflection;
using System.Windows.Forms;
using Forge.Next.Shared.UI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Forge.Next.Shared.Tests;

/// <summary>
/// A fake WinForms control with an instance handler method. Deriving from the fake
/// <c>System.Windows.Forms.Control</c> (declared in <c>UIReflectionExtensionsTests</c>) makes
/// <see cref="UIExtensions.IsWinFormsControl"/> return true, and the inherited <c>Invoke</c>
/// records every delegate that is marshalled onto the "UI thread".
/// </summary>
public sealed class EventWinFormsControl : Control
{
    /// <summary>An instance handler used as an event subscriber; returns the value multiplied by ten.</summary>
    /// <param name="value">The input value.</param>
    /// <returns><paramref name="value"/> * 10.</returns>
    public int Handle(int value) => value * 10;
}

/// <summary>
/// A fake WPF dependency object exposing a recording <c>Dispatcher</c> and an instance handler method.
/// Deriving from the fake <c>System.Windows.DependencyObject</c> makes
/// <see cref="UIExtensions.IsWPFDependency"/> return true.
/// </summary>
public sealed class EventWpfControl : System.Windows.DependencyObject
{
    /// <summary>The dispatcher reflected on (by property name) and invoked by <c>InvokeOnWPFDependency</c>.</summary>
    public FakeDispatcher Dispatcher { get; } = new FakeDispatcher();

    /// <summary>An instance handler used as an event subscriber; returns the value multiplied by ten.</summary>
    /// <param name="value">The input value.</param>
    /// <returns><paramref name="value"/> * 10.</returns>
    public int Handle(int value) => value * 10;
}

/// <summary>
/// Unit tests for <see cref="Event"/>.
///
/// The class exposes a single public method, <see cref="Event.Fire"/>; the tests below are split per
/// behaviour (core invocation, null event, exception capture, void handlers, parameter forwarding, the two
/// UI control-invoke paths, and logging). The private helpers (<c>InvokeHandler</c>, <c>GetTargetName</c> and
/// the three <c>Log*</c> methods) are not visible from the test assembly; they are covered indirectly via
/// these scenarios.
/// </summary>
public class EventTests
{
    #region Fire — core behaviour

    /// <summary>
    /// Tests <see cref="Event.Fire"/>: every handler runs in order and its return value is collected.
    /// </summary>
    [Fact]
    public void FireTest()
    {
        // A multicast delegate with two subscribers.
        Func<int, int> handlers = x => x + 1;
        handlers += x => x * 2;

        IReadOnlyCollection<object?> results = Event.Fire(handlers, new object?[] { 10 });

        // Results are in subscriber order, each holding the corresponding return value.
        results.Count.ShouldBe(2);
        results.ElementAt(0).ShouldBe(11);   // 10 + 1
        results.ElementAt(1).ShouldBe(20);   // 10 * 2
    }

    /// <summary>
    /// Tests that a <see langword="null"/> delegate yields an empty collection and never throws.
    /// </summary>
    [Fact]
    public void FireWithNullEventTest()
    {
        IReadOnlyCollection<object?> results = Event.Fire(null!, new object?[] { 1 });

        results.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a throwing handler's exception is captured into the result (not propagated) and that the
    /// remaining handlers still run.
    /// </summary>
    [Fact]
    public void FireCapturesHandlerExceptionTest()
    {
        InvalidOperationException boom = new("bad handler");

        Func<int, int> handlers = x => x + 1;   // succeeds
        handlers += _ => throw boom;            // throws
        handlers += x => x * 2;                 // still runs

        IReadOnlyCollection<object?> results = Event.Fire(handlers, new object?[] { 10 });

        results.Count.ShouldBe(3);
        results.ElementAt(0).ShouldBe(11);
        results.ElementAt(2).ShouldBe(20);      // the handler after the failing one still executed

        // The handler is invoked via reflection (MethodInfo.Invoke), which wraps a thrown exception in a
        // TargetInvocationException; the original exception is its InnerException.
        object? captured = results.ElementAt(1);
        captured.ShouldBeOfType<TargetInvocationException>();
        ((TargetInvocationException)captured!).InnerException.ShouldBeSameAs(boom);
    }

    /// <summary>
    /// Tests that a void (<see cref="Action{T}"/>) handler runs (observable side effect) and contributes a
    /// <see langword="null"/> result element.
    /// </summary>
    [Fact]
    public void FireWithVoidHandlerTest()
    {
        int sideEffect = 0;
        Action<int> handler = x => sideEffect = x;

        IReadOnlyCollection<object?> results = Event.Fire(handler, new object?[] { 42 });

        sideEffect.ShouldBe(42);                // the handler executed
        results.Count.ShouldBe(1);
        results.ElementAt(0).ShouldBeNull();    // void handler -> null return value
    }

    /// <summary>
    /// Tests that all supplied parameters are forwarded, in order, to each handler.
    /// </summary>
    [Fact]
    public void FireForwardsParametersTest()
    {
        Func<int, int, int> handler = (a, b) => a + b;

        IReadOnlyCollection<object?> results = Event.Fire(handler, new object?[] { 3, 4 });

        results.Count.ShouldBe(1);
        results.ElementAt(0).ShouldBe(7);
    }

    #endregion

    #region Fire — UI control invoke

    /// <summary>
    /// Tests that with <c>controlInvoke: true</c> a handler whose target is a WinForms control is routed
    /// through the control's <c>Invoke</c>, whereas with <c>controlInvoke: false</c> it is invoked directly.
    /// </summary>
    [Fact]
    public void FireOnWinFormsControlTest()
    {
        EventWinFormsControl control = new();
        Func<int, int> handler = control.Handle;   // the delegate's target is the control

        // controlInvoke: true -> marshalled through control.Invoke(...).
        IReadOnlyCollection<object?> marshalled = Event.Fire(handler, new object?[] { 5 }, controlInvoke: true);
        marshalled.ElementAt(0).ShouldBe(50);
        control.InvokedDelegates.Count.ShouldBe(1);    // the UI path was used

        // controlInvoke: false -> invoked directly; the control's Invoke is NOT used.
        control.InvokedDelegates.Clear();
        IReadOnlyCollection<object?> direct = Event.Fire(handler, new object?[] { 5 }, controlInvoke: false);
        direct.ElementAt(0).ShouldBe(50);
        control.InvokedDelegates.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that with <c>controlInvoke: true</c> a handler whose target is a WPF dependency object is routed
    /// through the object's dispatcher, whereas with <c>controlInvoke: false</c> it is invoked directly.
    /// </summary>
    [Fact]
    public void FireOnWPFDependencyTest()
    {
        EventWpfControl control = new();
        Func<int, int> handler = control.Handle;

        // controlInvoke: true -> marshalled through control.Dispatcher.Invoke(...).
        IReadOnlyCollection<object?> marshalled = Event.Fire(handler, new object?[] { 5 }, controlInvoke: true);
        marshalled.ElementAt(0).ShouldBe(50);
        control.Dispatcher.InvokedDelegates.Count.ShouldBe(1);   // the WPF dispatch path was used

        // controlInvoke: false -> invoked directly; the dispatcher is NOT used.
        control.Dispatcher.InvokedDelegates.Clear();
        IReadOnlyCollection<object?> direct = Event.Fire(handler, new object?[] { 5 }, controlInvoke: false);
        direct.ElementAt(0).ShouldBe(50);
        control.Dispatcher.InvokedDelegates.ShouldBeEmpty();
    }

    #endregion

    #region Fire — logging

    /// <summary>
    /// Tests that, when a logger is supplied, the debug guard runs before and after each invocation and the
    /// error guard runs when a handler throws.
    ///
    /// NSubstitute cannot easily verify the <c>ILogger.Log&lt;TState&gt;</c> call because the actual state type is
    /// an internal framework type, so we assert on the <c>IsEnabled</c> gate that the production code checks
    /// immediately before every log write — its call counts pin down which logging branches executed.
    /// </summary>
    [Fact]
    public void FireLogsDiagnosticsTest()
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // A successful handler -> Debug checked before AND after; the Error path is never reached.
        Func<int, int> ok = x => x + 1;
        Event.Fire(ok, new object?[] { 1 }, controlInvoke: false, logger: logger);
        logger.Received(2).IsEnabled(LogLevel.Debug);    // before + after
        logger.DidNotReceive().IsEnabled(LogLevel.Error);

        logger.ClearReceivedCalls();

        // A failing handler -> Debug still checked before + after (finally always runs), plus Error once.
        Func<int, int> bad = _ => throw new InvalidOperationException("x");
        Event.Fire(bad, new object?[] { 1 }, controlInvoke: false, logger: logger);
        logger.Received(2).IsEnabled(LogLevel.Debug);
        logger.Received(1).IsEnabled(LogLevel.Error);
    }

    #endregion
}
