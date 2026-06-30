using System.Collections;

namespace Forge.Next.Shared;

/// <summary>
/// Extension methods for <see cref="IEnumerable"/> and <see cref="IEnumerable{T}"/>.
/// </summary>
public static class IEnumerableExtensions
{

    /// <summary>
    /// ForEach extension method for IEnumerable. This is not a LINQ method, but it is a convenient way to perform an action on each item in a collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The items.</param>
    /// <param name="action">The action.</param>
    public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        if (items is null)
        {
            return;
        }

        foreach (T item in items.ToList())
        {
            action(item);
        }
    }

    /// <summary>
    /// Determines whether two sequences are equal by comparing their elements in order,
    /// recursing into nested <see cref="IEnumerable"/> elements (deep comparison).
    /// Strings are treated as scalar values rather than character sequences.
    /// </summary>
    /// <param name="first">The first sequence to compare. May be <see langword="null"/>.</param>
    /// <param name="second">The second sequence to compare. May be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if both sequences are <see langword="null"/>, the same reference,
    /// or contain deeply equal elements in the same order; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsDeepEqual(this IEnumerable? first, IEnumerable? second)
    {
        // Covers (null, null) and identical references in one shot.
        if (ReferenceEquals(first, second))
            return true;

        if (first is null || second is null)
            return false;

        if (first.GetType() != second.GetType())
            return false;

        // Fast path: a differing count rules out equality without enumerating.
        if (first is ICollection collectionA && second is ICollection collectionB &&
            collectionA.Count != collectionB.Count)
            return false;

        IEnumerator enumeratorA = first.GetEnumerator();
        IEnumerator enumeratorB = second.GetEnumerator();
        try
        {
            bool hasNextLeft;
            bool hasNextRight;

            // Hint: Single '&' (not '&&') so both enumerators always advance together;
            // otherwise the length check below would read a stale hasNextRight.
            while ((hasNextLeft = enumeratorA.MoveNext()) & (hasNextRight = enumeratorB.MoveNext()))
            {
                if (!ElementsDeepEqual(enumeratorA.Current, enumeratorB.Current))
                    return false;
            }

            // Both ran out at the same time => same length.
            return hasNextLeft == hasNextRight;
        }
        finally
        {
            (enumeratorA as IDisposable)?.Dispose();
            (enumeratorB as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Determines whether two sequences are equal by comparing their elements in order,
    /// recursing into nested <see cref="IEnumerable"/> elements (deep comparison).
    /// Non-enumerable elements are compared with <see cref="EqualityComparer{T}.Default"/>;
    /// strings are treated as scalar values rather than character sequences.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="first">The first sequence to compare. May be <see langword="null"/>.</param>
    /// <param name="second">The second sequence to compare. May be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if both sequences are <see langword="null"/>, the same reference,
    /// or contain deeply equal elements in the same order; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsDeepEqual<T>(this IEnumerable<T>? first, IEnumerable<T>? second)
    {
        if (ReferenceEquals(first, second))
            return true;

        if (first is null || second is null)
            return false;

        // Fast path: a differing count rules out equality without enumerating.
        if (first is ICollection<T> collectionA && second is ICollection<T> collectionB &&
            collectionA.Count != collectionB.Count)
            return false;

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        using IEnumerator<T> enumeratorA = first.GetEnumerator();
        using IEnumerator<T> enumeratorB = second.GetEnumerator();

        bool hasNextLeft;
        bool hasNextRight;

        while ((hasNextLeft = enumeratorA.MoveNext()) & (hasNextRight = enumeratorB.MoveNext()))
        {
            T left = enumeratorA.Current;
            T right = enumeratorB.Current;

            // Recurse into nested sequences, but keep strings as scalar values.
            if (left is IEnumerable nestedLeft and not string &&
                right is IEnumerable nestedRight and not string)
            {
                if (!nestedLeft.IsDeepEqual(nestedRight))
                    return false;
            }
            else if (!comparer.Equals(left, right))
            {
                return false;
            }
        }

        return hasNextLeft == hasNextRight;
    }

    private static bool ElementsDeepEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        // Recurse into nested sequences, but keep strings as scalar values.
        if (left is IEnumerable nestedLeft and not string &&
            right is IEnumerable nestedRight and not string)
            return nestedLeft.IsDeepEqual(nestedRight);

        return left.Equals(right);
    }

}
