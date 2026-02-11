namespace VictorNovember.Extensions;

public static class RandomExtension
{
    public static T PickRandom<T>(this Random rng, IReadOnlyList<T> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("Collection cannot be empty.", nameof(items));

        return items[rng.Next(items.Count)];
    }
}
