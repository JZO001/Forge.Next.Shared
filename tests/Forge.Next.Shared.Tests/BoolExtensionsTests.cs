using Forge.Next.Shared;
using NSubstitute;
using Shouldly;
using Xunit;

// Verifying a delegate that returns a Task with NSubstitute (e.g. onTrue.Received(1).Invoke()) produces a
// Task we deliberately do not await — the call exists only to record the received/not-received assertion.
// Inside the async test methods that would raise CS4014, so we suppress it for this file.
#pragma warning disable CS4014

namespace Forge.Next.Shared.Tests;

/// <summary>
/// Unit tests for <see cref="BoolExtensions"/>.
///
/// Every public method gets one test method named after it (with a <c>Test</c> suffix and, for the
/// overloads, a <c>WithValue</c> discriminator so the two same-named overloads do not collide).
/// Each test covers the three branches the production code can take:
///   1. the "matching" branch (the callback selected by the condition),
///   2. the "non-matching" branch when the optional callback IS supplied, and
///   3. the "non-matching" branch when the optional callback is NOT supplied (default / no-op).
///
/// NSubstitute is used to create substitutes for the delegate parameters so we can both stub their
/// return values and verify exactly which callback was (and was not) invoked.
/// </summary>
public class BoolExtensionsTests
{
    // A couple of fixed values reused across the tests so the assertions read clearly:
    // ON_TRUE_RESULT is what the "true" callback returns, ON_FALSE_RESULT what the "false" callback returns.
    private const int ON_TRUE_RESULT = 111;
    private const int ON_FALSE_RESULT = 222;

    // An arbitrary input value passed to the WithValue overloads, so we can assert the value is forwarded.
    private const string INPUT_VALUE = "input";

    #region IfTrue

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfTrue{TNextValue}(bool, Func{TNextValue}, Func{TNextValue}?)"/>.
    /// </summary>
    [Fact]
    public void IfTrueTest()
    {
        // Substitutes for the two callbacks. We stub their return values up front; NSubstitute does not
        // count these stubbing calls as "received" invocations, so the Received(...) assertions stay accurate.
        Func<int> onTrue = Substitute.For<Func<int>>();
        Func<int> onFalse = Substitute.For<Func<int>>();
        onTrue.Invoke().Returns(ON_TRUE_RESULT);
        onFalse.Invoke().Returns(ON_FALSE_RESULT);

        // 1. condition == true -> onTrue is invoked and its result returned; onFalse must stay untouched.
        true.IfTrue(onTrue, onFalse).ShouldBe(ON_TRUE_RESULT);
        onTrue.Received(1).Invoke();
        onFalse.DidNotReceive().Invoke();

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 2. condition == false WITH an onFalse callback -> onFalse is invoked and its result returned.
        false.IfTrue(onTrue, onFalse).ShouldBe(ON_FALSE_RESULT);
        onFalse.Received(1).Invoke();
        onTrue.DidNotReceive().Invoke();

        onTrue.ClearReceivedCalls();

        // 3. condition == false WITHOUT an onFalse callback -> the default of TNextValue (0 for int) is returned
        //    and onTrue is never invoked.
        false.IfTrue(onTrue).ShouldBe(default);
        onTrue.DidNotReceive().Invoke();
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfTrue{TValue, TNextValue}(bool, TValue, Func{TValue, TNextValue}, Func{TValue, TNextValue}?)"/>,
    /// the overload that forwards an input <c>value</c> to the selected callback.
    /// </summary>
    [Fact]
    public void IfTrueWithValueTest()
    {
        Func<string, int> onTrue = Substitute.For<Func<string, int>>();
        Func<string, int> onFalse = Substitute.For<Func<string, int>>();
        onTrue.Invoke(INPUT_VALUE).Returns(ON_TRUE_RESULT);
        onFalse.Invoke(INPUT_VALUE).Returns(ON_FALSE_RESULT);

        // 1. true -> onTrue invoked WITH the supplied value.
        true.IfTrue(INPUT_VALUE, onTrue, onFalse).ShouldBe(ON_TRUE_RESULT);
        onTrue.Received(1).Invoke(INPUT_VALUE);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 2. false WITH onFalse -> onFalse invoked WITH the supplied value.
        false.IfTrue(INPUT_VALUE, onTrue, onFalse).ShouldBe(ON_FALSE_RESULT);
        onFalse.Received(1).Invoke(INPUT_VALUE);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());

        onTrue.ClearReceivedCalls();

        // 3. false WITHOUT onFalse -> default returned, onTrue untouched.
        false.IfTrue(INPUT_VALUE, onTrue).ShouldBe(default);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfTrueAsync{TNextValue}(bool, Func{Task{TNextValue}}, Func{Task{TNextValue}}?)"/>.
    /// </summary>
    [Fact]
    public async Task IfTrueAsyncTest()
    {
        Func<Task<int>> onTrue = Substitute.For<Func<Task<int>>>();
        Func<Task<int>> onFalse = Substitute.For<Func<Task<int>>>();
        // The substitutes return already-completed tasks carrying the canned results.
        onTrue.Invoke().Returns(Task.FromResult(ON_TRUE_RESULT));
        onFalse.Invoke().Returns(Task.FromResult(ON_FALSE_RESULT));

        // 1. true -> awaits onTrue.
        (await true.IfTrueAsync(onTrue, onFalse)).ShouldBe(ON_TRUE_RESULT);
        onTrue.Received(1).Invoke();
        onFalse.DidNotReceive().Invoke();

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 2. false WITH onFalse -> awaits onFalse.
        (await false.IfTrueAsync(onTrue, onFalse)).ShouldBe(ON_FALSE_RESULT);
        onFalse.Received(1).Invoke();
        onTrue.DidNotReceive().Invoke();

        onTrue.ClearReceivedCalls();

        // 3. false WITHOUT onFalse -> default returned without awaiting anything.
        (await false.IfTrueAsync(onTrue)).ShouldBe(default);
        onTrue.DidNotReceive().Invoke();
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfTrueAsync{TValue, TNextValue}(bool, TValue, Func{TValue, Task{TNextValue}}, Func{TValue, Task{TNextValue}}?)"/>.
    /// </summary>
    [Fact]
    public async Task IfTrueAsyncWithValueTest()
    {
        Func<string, Task<int>> onTrue = Substitute.For<Func<string, Task<int>>>();
        Func<string, Task<int>> onFalse = Substitute.For<Func<string, Task<int>>>();
        onTrue.Invoke(INPUT_VALUE).Returns(Task.FromResult(ON_TRUE_RESULT));
        onFalse.Invoke(INPUT_VALUE).Returns(Task.FromResult(ON_FALSE_RESULT));

        // 1. true -> awaits onTrue with the value.
        (await true.IfTrueAsync(INPUT_VALUE, onTrue, onFalse)).ShouldBe(ON_TRUE_RESULT);
        onTrue.Received(1).Invoke(INPUT_VALUE);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 2. false WITH onFalse -> awaits onFalse with the value.
        (await false.IfTrueAsync(INPUT_VALUE, onTrue, onFalse)).ShouldBe(ON_FALSE_RESULT);
        onFalse.Received(1).Invoke(INPUT_VALUE);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());

        onTrue.ClearReceivedCalls();

        // 3. false WITHOUT onFalse -> default returned.
        (await false.IfTrueAsync(INPUT_VALUE, onTrue)).ShouldBe(default);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfTrueDo(bool, Action, Action?)"/>: side-effect variant that returns
    /// the original condition.
    /// </summary>
    [Fact]
    public void IfTrueDoTest()
    {
        Action onTrue = Substitute.For<Action>();
        Action onFalse = Substitute.For<Action>();

        // 1. true -> onTrue executed; the method returns the unchanged condition (true).
        true.IfTrueDo(onTrue, onFalse).ShouldBeTrue();
        onTrue.Received(1).Invoke();
        onFalse.DidNotReceive().Invoke();

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 2. false WITH onFalse -> onFalse executed; returns the unchanged condition (false).
        false.IfTrueDo(onTrue, onFalse).ShouldBeFalse();
        onFalse.Received(1).Invoke();
        onTrue.DidNotReceive().Invoke();

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 3. false WITHOUT onFalse -> nothing executed; still returns false.
        false.IfTrueDo(onTrue).ShouldBeFalse();
        onTrue.DidNotReceive().Invoke();
        onFalse.DidNotReceive().Invoke();
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfTrueDo{TValue}(bool, TValue, Action{TValue}, Action{TValue}?)"/>.
    /// </summary>
    [Fact]
    public void IfTrueDoWithValueTest()
    {
        Action<string> onTrue = Substitute.For<Action<string>>();
        Action<string> onFalse = Substitute.For<Action<string>>();

        // 1. true -> onTrue executed with the value; returns true.
        true.IfTrueDo(INPUT_VALUE, onTrue, onFalse).ShouldBeTrue();
        onTrue.Received(1).Invoke(INPUT_VALUE);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 2. false WITH onFalse -> onFalse executed with the value; returns false.
        false.IfTrueDo(INPUT_VALUE, onTrue, onFalse).ShouldBeFalse();
        onFalse.Received(1).Invoke(INPUT_VALUE);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 3. false WITHOUT onFalse -> nothing executed; returns false.
        false.IfTrueDo(INPUT_VALUE, onTrue).ShouldBeFalse();
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfTrueDoAsync(bool, Func{Task}, Func{Task}?)"/>.
    /// </summary>
    [Fact]
    public async Task IfTrueDoAsyncTest()
    {
        Func<Task> onTrue = Substitute.For<Func<Task>>();
        Func<Task> onFalse = Substitute.For<Func<Task>>();
        // Return completed tasks so awaiting them does not block.
        onTrue.Invoke().Returns(Task.CompletedTask);
        onFalse.Invoke().Returns(Task.CompletedTask);

        // 1. true -> awaits onTrue; returns true.
        (await true.IfTrueDoAsync(onTrue, onFalse)).ShouldBeTrue();
        onTrue.Received(1).Invoke();
        onFalse.DidNotReceive().Invoke();

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 2. false WITH onFalse -> awaits onFalse; returns false.
        (await false.IfTrueDoAsync(onTrue, onFalse)).ShouldBeFalse();
        onFalse.Received(1).Invoke();
        onTrue.DidNotReceive().Invoke();

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 3. false WITHOUT onFalse -> nothing executed; returns false.
        (await false.IfTrueDoAsync(onTrue)).ShouldBeFalse();
        onTrue.DidNotReceive().Invoke();
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfTrueDoAsync{TValue}(bool, TValue, Func{TValue, Task}, Func{TValue, Task}?)"/>.
    /// </summary>
    [Fact]
    public async Task IfTrueDoAsyncWithValueTest()
    {
        Func<string, Task> onTrue = Substitute.For<Func<string, Task>>();
        Func<string, Task> onFalse = Substitute.For<Func<string, Task>>();
        onTrue.Invoke(INPUT_VALUE).Returns(Task.CompletedTask);
        onFalse.Invoke(INPUT_VALUE).Returns(Task.CompletedTask);

        // 1. true -> awaits onTrue with the value; returns true.
        (await true.IfTrueDoAsync(INPUT_VALUE, onTrue, onFalse)).ShouldBeTrue();
        onTrue.Received(1).Invoke(INPUT_VALUE);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 2. false WITH onFalse -> awaits onFalse with the value; returns false.
        (await false.IfTrueDoAsync(INPUT_VALUE, onTrue, onFalse)).ShouldBeFalse();
        onFalse.Received(1).Invoke(INPUT_VALUE);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());

        onTrue.ClearReceivedCalls();
        onFalse.ClearReceivedCalls();

        // 3. false WITHOUT onFalse -> nothing executed; returns false.
        (await false.IfTrueDoAsync(INPUT_VALUE, onTrue)).ShouldBeFalse();
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());
    }

    #endregion

    #region IfFalse

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfFalse{TNextValue}(bool, Func{TNextValue}, Func{TNextValue}?)"/>.
    /// Here the PRIMARY branch fires when the condition is <see langword="false"/>.
    /// </summary>
    [Fact]
    public void IfFalseTest()
    {
        Func<int> onFalse = Substitute.For<Func<int>>();
        Func<int> onTrue = Substitute.For<Func<int>>();
        onFalse.Invoke().Returns(ON_FALSE_RESULT);
        onTrue.Invoke().Returns(ON_TRUE_RESULT);

        // 1. condition == false -> onFalse invoked, its result returned; onTrue untouched.
        false.IfFalse(onFalse, onTrue).ShouldBe(ON_FALSE_RESULT);
        onFalse.Received(1).Invoke();
        onTrue.DidNotReceive().Invoke();

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 2. condition == true WITH an onTrue callback -> onTrue invoked, its result returned.
        true.IfFalse(onFalse, onTrue).ShouldBe(ON_TRUE_RESULT);
        onTrue.Received(1).Invoke();
        onFalse.DidNotReceive().Invoke();

        onFalse.ClearReceivedCalls();

        // 3. condition == true WITHOUT an onTrue callback -> default returned; onFalse untouched.
        true.IfFalse(onFalse).ShouldBe(default);
        onFalse.DidNotReceive().Invoke();
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfFalse{TValue, TNextValue}(bool, TValue, Func{TValue, TNextValue}, Func{TValue, TNextValue}?)"/>.
    /// </summary>
    [Fact]
    public void IfFalseWithValueTest()
    {
        Func<string, int> onFalse = Substitute.For<Func<string, int>>();
        Func<string, int> onTrue = Substitute.For<Func<string, int>>();
        onFalse.Invoke(INPUT_VALUE).Returns(ON_FALSE_RESULT);
        onTrue.Invoke(INPUT_VALUE).Returns(ON_TRUE_RESULT);

        // 1. false -> onFalse invoked with the value.
        false.IfFalse(INPUT_VALUE, onFalse, onTrue).ShouldBe(ON_FALSE_RESULT);
        onFalse.Received(1).Invoke(INPUT_VALUE);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 2. true WITH onTrue -> onTrue invoked with the value.
        true.IfFalse(INPUT_VALUE, onFalse, onTrue).ShouldBe(ON_TRUE_RESULT);
        onTrue.Received(1).Invoke(INPUT_VALUE);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());

        onFalse.ClearReceivedCalls();

        // 3. true WITHOUT onTrue -> default returned.
        true.IfFalse(INPUT_VALUE, onFalse).ShouldBe(default);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfFalseAsync{TNextValue}(bool, Func{Task{TNextValue}}, Func{Task{TNextValue}}?)"/>.
    /// </summary>
    [Fact]
    public async Task IfFalseAsyncTest()
    {
        Func<Task<int>> onFalse = Substitute.For<Func<Task<int>>>();
        Func<Task<int>> onTrue = Substitute.For<Func<Task<int>>>();
        onFalse.Invoke().Returns(Task.FromResult(ON_FALSE_RESULT));
        onTrue.Invoke().Returns(Task.FromResult(ON_TRUE_RESULT));

        // 1. false -> awaits onFalse.
        (await false.IfFalseAsync(onFalse, onTrue)).ShouldBe(ON_FALSE_RESULT);
        onFalse.Received(1).Invoke();
        onTrue.DidNotReceive().Invoke();

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 2. true WITH onTrue -> awaits onTrue.
        (await true.IfFalseAsync(onFalse, onTrue)).ShouldBe(ON_TRUE_RESULT);
        onTrue.Received(1).Invoke();
        onFalse.DidNotReceive().Invoke();

        onFalse.ClearReceivedCalls();

        // 3. true WITHOUT onTrue -> default returned.
        (await true.IfFalseAsync(onFalse)).ShouldBe(default);
        onFalse.DidNotReceive().Invoke();
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfFalseAsync{TValue, TNextValue}(bool, TValue, Func{TValue, Task{TNextValue}}, Func{TValue, Task{TNextValue}}?)"/>.
    /// </summary>
    [Fact]
    public async Task IfFalseAsyncWithValueTest()
    {
        Func<string, Task<int>> onFalse = Substitute.For<Func<string, Task<int>>>();
        Func<string, Task<int>> onTrue = Substitute.For<Func<string, Task<int>>>();
        onFalse.Invoke(INPUT_VALUE).Returns(Task.FromResult(ON_FALSE_RESULT));
        onTrue.Invoke(INPUT_VALUE).Returns(Task.FromResult(ON_TRUE_RESULT));

        // 1. false -> awaits onFalse with the value.
        (await false.IfFalseAsync(INPUT_VALUE, onFalse, onTrue)).ShouldBe(ON_FALSE_RESULT);
        onFalse.Received(1).Invoke(INPUT_VALUE);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 2. true WITH onTrue -> awaits onTrue with the value.
        (await true.IfFalseAsync(INPUT_VALUE, onFalse, onTrue)).ShouldBe(ON_TRUE_RESULT);
        onTrue.Received(1).Invoke(INPUT_VALUE);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());

        onFalse.ClearReceivedCalls();

        // 3. true WITHOUT onTrue -> default returned.
        (await true.IfFalseAsync(INPUT_VALUE, onFalse)).ShouldBe(default);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfFalseDo(bool, Action, Action?)"/>.
    /// </summary>
    [Fact]
    public void IfFalseDoTest()
    {
        Action onFalse = Substitute.For<Action>();
        Action onTrue = Substitute.For<Action>();

        // 1. false -> onFalse executed; returns the unchanged condition (false).
        false.IfFalseDo(onFalse, onTrue).ShouldBeFalse();
        onFalse.Received(1).Invoke();
        onTrue.DidNotReceive().Invoke();

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 2. true WITH onTrue -> onTrue executed; returns the unchanged condition (true).
        true.IfFalseDo(onFalse, onTrue).ShouldBeTrue();
        onTrue.Received(1).Invoke();
        onFalse.DidNotReceive().Invoke();

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 3. true WITHOUT onTrue -> nothing executed; returns true.
        true.IfFalseDo(onFalse).ShouldBeTrue();
        onFalse.DidNotReceive().Invoke();
        onTrue.DidNotReceive().Invoke();
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfFalseDo{TValue}(bool, TValue, Action{TValue}, Action{TValue}?)"/>.
    /// </summary>
    [Fact]
    public void IfFalseDoWithValueTest()
    {
        Action<string> onFalse = Substitute.For<Action<string>>();
        Action<string> onTrue = Substitute.For<Action<string>>();

        // 1. false -> onFalse executed with the value; returns false.
        false.IfFalseDo(INPUT_VALUE, onFalse, onTrue).ShouldBeFalse();
        onFalse.Received(1).Invoke(INPUT_VALUE);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 2. true WITH onTrue -> onTrue executed with the value; returns true.
        true.IfFalseDo(INPUT_VALUE, onFalse, onTrue).ShouldBeTrue();
        onTrue.Received(1).Invoke(INPUT_VALUE);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 3. true WITHOUT onTrue -> nothing executed; returns true.
        true.IfFalseDo(INPUT_VALUE, onFalse).ShouldBeTrue();
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfFalseDoAsync(bool, Func{Task}, Func{Task}?)"/>.
    /// </summary>
    [Fact]
    public async Task IfFalseDoAsyncTest()
    {
        Func<Task> onFalse = Substitute.For<Func<Task>>();
        Func<Task> onTrue = Substitute.For<Func<Task>>();
        onFalse.Invoke().Returns(Task.CompletedTask);
        onTrue.Invoke().Returns(Task.CompletedTask);

        // 1. false -> awaits onFalse; returns false.
        (await false.IfFalseDoAsync(onFalse, onTrue)).ShouldBeFalse();
        onFalse.Received(1).Invoke();
        onTrue.DidNotReceive().Invoke();

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 2. true WITH onTrue -> awaits onTrue; returns true.
        (await true.IfFalseDoAsync(onFalse, onTrue)).ShouldBeTrue();
        onTrue.Received(1).Invoke();
        onFalse.DidNotReceive().Invoke();

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 3. true WITHOUT onTrue -> nothing executed; returns true.
        (await true.IfFalseDoAsync(onFalse)).ShouldBeTrue();
        onFalse.DidNotReceive().Invoke();
    }

    /// <summary>
    /// Tests <see cref="BoolExtensions.IfFalseDoAsync{TValue}(bool, TValue, Func{TValue, Task}, Func{TValue, Task}?)"/>.
    /// </summary>
    [Fact]
    public async Task IfFalseDoAsyncWithValueTest()
    {
        Func<string, Task> onFalse = Substitute.For<Func<string, Task>>();
        Func<string, Task> onTrue = Substitute.For<Func<string, Task>>();
        onFalse.Invoke(INPUT_VALUE).Returns(Task.CompletedTask);
        onTrue.Invoke(INPUT_VALUE).Returns(Task.CompletedTask);

        // 1. false -> awaits onFalse with the value; returns false.
        (await false.IfFalseDoAsync(INPUT_VALUE, onFalse, onTrue)).ShouldBeFalse();
        onFalse.Received(1).Invoke(INPUT_VALUE);
        onTrue.DidNotReceive().Invoke(Arg.Any<string>());

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 2. true WITH onTrue -> awaits onTrue with the value; returns true.
        (await true.IfFalseDoAsync(INPUT_VALUE, onFalse, onTrue)).ShouldBeTrue();
        onTrue.Received(1).Invoke(INPUT_VALUE);
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());

        onFalse.ClearReceivedCalls();
        onTrue.ClearReceivedCalls();

        // 3. true WITHOUT onTrue -> nothing executed; returns true.
        (await true.IfFalseDoAsync(INPUT_VALUE, onFalse)).ShouldBeTrue();
        onFalse.DidNotReceive().Invoke(Arg.Any<string>());
    }

    #endregion
}

#pragma warning restore CS4014
