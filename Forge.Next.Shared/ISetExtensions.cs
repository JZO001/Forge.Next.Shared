namespace Forge.Next.Shared;

/// <summary>
/// Extension methods for <see cref="ISet{T}"/>.
/// </summary>
public static class ISetExtensions
{

    /// <summary>
    /// Adds every element of <paramref name="collection"/> to <paramref name="set"/>. Elements already present
    /// are ignored (normal set semantics), and a <see langword="null"/> <paramref name="collection"/> is treated
    /// as empty (no-op).
    /// </summary>
    /// <typeparam name="T">The element type of the set.</typeparam>
    /// <param name="set">The set to add the elements to.</param>
    /// <param name="collection">The elements to add; may be <see langword="null"/> (treated as no elements).</param>
    /// <exception cref="ArgumentNullException"><paramref name="set"/> is <see langword="null"/>.</exception>
    public static void AddRange<T>(this ISet<T> set, IEnumerable<T> collection)
    {
        if (set is null)
        {
            throw new ArgumentNullException(nameof(set));
        }

        if (collection is not null)
        {
            foreach (T item in collection)
            {
                set.Add(item);
            }
        }
    }

}
