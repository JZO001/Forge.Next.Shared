using System.Collections;
using Forge.Next.Shared;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Forge.Next.Shared.Tests;

/// <summary>
/// Unit tests for <see cref="IEnumerableExtensions"/> — the <c>ForEach</c> helper and both the
/// non-generic and generic <c>IsDeepEqual</c> overloads.
///
/// The private <c>ElementsDeepEqual</c> helper is not visible from the test assembly, so it is
/// exercised indirectly through the public <c>IsDeepEqual</c> methods (nested-sequence and
/// string-as-scalar scenarios specifically target the branches inside that helper).
/// </summary>
public class IEnumerableExtensionsTests
{
    #region ForEach

    /// <summary>
    /// Tests <see cref="IEnumerableExtensions.ForEach{T}(IEnumerable{T}, Action{T})"/>: the action must run
    /// once per element, in order, and a <see langword="null"/> source must be a safe no-op.
    /// </summary>
    [Fact]
    public void ForEachTest()
    {
        // Arrange: a small sequence and an NSubstitute action so we can verify the per-element invocations.
        int[] items = { 1, 2, 3 };
        Action<int> action = Substitute.For<Action<int>>();

        // Act
        items.ForEach(action);

        // Assert: the action was invoked exactly once for each element, with the expected argument.
        action.Received(1).Invoke(1);
        action.Received(1).Invoke(2);
        action.Received(1).Invoke(3);
        action.Received(3).Invoke(Arg.Any<int>());

        // A null source must NOT throw and must NOT invoke the action at all.
        Action<int> actionForNull = Substitute.For<Action<int>>();
        IEnumerable<int>? nullItems = null;
        Should.NotThrow(() => nullItems!.ForEach(actionForNull));
        actionForNull.DidNotReceive().Invoke(Arg.Any<int>());
    }

    #endregion

    #region IsDeepEqual (non-generic)

    /// <summary>
    /// Tests <see cref="IEnumerableExtensions.IsDeepEqual(IEnumerable, IEnumerable)"/> — the non-generic
    /// overload that requires both operands to share the same runtime type and recurses into nested
    /// <see cref="IEnumerable"/> elements.
    ///
    /// IMPORTANT: every operand is cast to the non-generic <see cref="IEnumerable"/>. Without the cast,
    /// strongly-typed collections (e.g. <c>int[]</c>, <c>List&lt;int&gt;</c>) would bind to the more specific
    /// generic overload, and this test would silently exercise the wrong method.
    /// </summary>
    [Fact]
    public void IsDeepEqualTest()
    {
        // Both null -> equal (ReferenceEquals(null, null) is true).
        ((IEnumerable?)null).IsDeepEqual((IEnumerable?)null).ShouldBeTrue();

        // Same reference -> equal.
        IEnumerable same = new[] { 1, 2, 3 };
        same.IsDeepEqual(same).ShouldBeTrue();

        // Exactly one null -> not equal.
        same.IsDeepEqual(null).ShouldBeFalse();
        ((IEnumerable?)null).IsDeepEqual(same).ShouldBeFalse();

        // Different runtime types (List<int> vs int[]) -> not equal, even with identical contents.
        ((IEnumerable)new List<int> { 1, 2, 3 }).IsDeepEqual((IEnumerable)new[] { 1, 2, 3 }).ShouldBeFalse();

        // Same type, equal flat contents -> equal.
        ((IEnumerable)new[] { 1, 2, 3 }).IsDeepEqual((IEnumerable)new[] { 1, 2, 3 }).ShouldBeTrue();

        // Same type, different length -> not equal (fast ICollection.Count path).
        ((IEnumerable)new[] { 1, 2, 3 }).IsDeepEqual((IEnumerable)new[] { 1, 2 }).ShouldBeFalse();

        // Same type and length, differing element -> not equal.
        ((IEnumerable)new[] { 1, 2, 3 }).IsDeepEqual((IEnumerable)new[] { 1, 9, 3 }).ShouldBeFalse();

        // Nested sequences are compared deeply: object[] containing inner int[] arrays.
        IEnumerable nestedA = new object[] { new[] { 1, 2 }, new[] { 3, 4 } };
        IEnumerable nestedB = new object[] { new[] { 1, 2 }, new[] { 3, 4 } };
        IEnumerable nestedDiff = new object[] { new[] { 1, 2 }, new[] { 3, 5 } };
        nestedA.IsDeepEqual(nestedB).ShouldBeTrue();
        nestedA.IsDeepEqual(nestedDiff).ShouldBeFalse();

        // Strings are treated as SCALAR values (not char sequences): equal strings compare equal by value.
        ((IEnumerable)new object[] { "ab" }).IsDeepEqual((IEnumerable)new object[] { "ab" }).ShouldBeTrue();
        ((IEnumerable)new object[] { "ab" }).IsDeepEqual((IEnumerable)new object[] { "ac" }).ShouldBeFalse();

        // Element-wise null handling: both null elements are equal, one null element is not.
        ((IEnumerable)new object?[] { null, 1 }).IsDeepEqual((IEnumerable)new object?[] { null, 1 }).ShouldBeTrue();
        ((IEnumerable)new object?[] { null, 1 }).IsDeepEqual((IEnumerable)new object?[] { 0, 1 }).ShouldBeFalse();
    }

    #endregion

    #region IsDeepEqual<T> (generic)

    /// <summary>
    /// Tests <see cref="IEnumerableExtensions.IsDeepEqual{T}(IEnumerable{T}, IEnumerable{T})"/> — the generic
    /// overload that compares elements with <see cref="EqualityComparer{T}.Default"/> and, unlike the
    /// non-generic overload, does NOT require both operands to share the same concrete container type.
    /// </summary>
    [Fact]
    public void IsDeepEqualGenericTest()
    {
        // Both null -> equal.
        ((IEnumerable<int>?)null).IsDeepEqual((IEnumerable<int>?)null).ShouldBeTrue();

        // Same reference -> equal.
        List<int> same = new() { 1, 2, 3 };
        same.IsDeepEqual(same).ShouldBeTrue();

        // Exactly one null -> not equal.
        same.IsDeepEqual(null).ShouldBeFalse();
        ((IEnumerable<int>?)null).IsDeepEqual(same).ShouldBeFalse();

        // Equal contents -> equal.
        new List<int> { 1, 2, 3 }.IsDeepEqual(new List<int> { 1, 2, 3 }).ShouldBeTrue();

        // Different length -> not equal (ICollection<T>.Count fast path).
        new List<int> { 1, 2, 3 }.IsDeepEqual(new List<int> { 1, 2 }).ShouldBeFalse();

        // Same length, differing element -> not equal.
        new List<int> { 1, 2, 3 }.IsDeepEqual(new List<int> { 1, 9, 3 }).ShouldBeFalse();

        // The generic overload is container-agnostic: an int[] equals a List<int> with the same contents
        // (this is the documented behavioural difference versus the non-generic overload above).
        IEnumerable<int> asArray = new[] { 1, 2, 3 };
        IEnumerable<int> asList = new List<int> { 1, 2, 3 };
        asArray.IsDeepEqual(asList).ShouldBeTrue();

        // Nested sequences (List<int[]>) are compared deeply through the non-generic recursion.
        IEnumerable<int[]> nestedA = new List<int[]> { new[] { 1, 2 }, new[] { 3, 4 } };
        IEnumerable<int[]> nestedB = new List<int[]> { new[] { 1, 2 }, new[] { 3, 4 } };
        IEnumerable<int[]> nestedDiff = new List<int[]> { new[] { 1, 2 }, new[] { 3, 5 } };
        nestedA.IsDeepEqual(nestedB).ShouldBeTrue();
        nestedA.IsDeepEqual(nestedDiff).ShouldBeFalse();

        // Strings are scalars: a sequence of equal strings is deeply equal.
        new List<string> { "ab", "cd" }.IsDeepEqual(new List<string> { "ab", "cd" }).ShouldBeTrue();
        new List<string> { "ab", "cd" }.IsDeepEqual(new List<string> { "ab", "ce" }).ShouldBeFalse();

        // Non-collection sequences (IEnumerable<T> that is not ICollection<T>) still enumerate correctly,
        // covering the path where the Count fast-path is skipped. We build such sequences lazily.
        static IEnumerable<int> Lazy(params int[] values)
        {
            foreach (int v in values)
            {
                yield return v;
            }
        }

        Lazy(1, 2, 3).IsDeepEqual(Lazy(1, 2, 3)).ShouldBeTrue();
        Lazy(1, 2, 3).IsDeepEqual(Lazy(1, 2)).ShouldBeFalse();   // shorter right side
        Lazy(1, 2).IsDeepEqual(Lazy(1, 2, 3)).ShouldBeFalse();   // shorter left side
    }

    #endregion
}
