namespace Zootify_Backend;

/// <summary>
/// Provides case-insensitive fuzzy string matching based on the Levenshtein edit distance,
/// used to resolve a taxonomy species by an approximate latin name.
/// </summary>
public static class FuzzyMatch
{
    /// <summary>
    /// Returns a normalized similarity between two strings in the range 0..1, where 1 means equal
    /// (ignoring case and surrounding whitespace) and 0 means completely different.
    /// </summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>The similarity score between 0 and 1.</returns>
    public static double Similarity(string a, string b)
    {
        var left = (a ?? string.Empty).Trim().ToLowerInvariant();
        var right = (b ?? string.Empty).Trim().ToLowerInvariant();

        if (left.Length == 0 && right.Length == 0)
        {
            return 1.0;
        }

        var distance = LevenshteinDistance(left, right);
        var maxLength = Math.Max(left.Length, right.Length);

        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Returns the candidate whose selected text is most similar to the query, provided the best
    /// similarity reaches the given threshold; otherwise returns <c>default</c>.
    /// </summary>
    /// <typeparam name="T">The candidate type.</typeparam>
    /// <param name="query">The query string to match against.</param>
    /// <param name="candidates">The candidates to search.</param>
    /// <param name="selector">Selects the text to compare for each candidate.</param>
    /// <param name="threshold">The minimum similarity (0..1) required for a match.</param>
    /// <returns>The best matching candidate, or <c>default</c> when none reaches the threshold.</returns>
    public static T? BestMatch<T>(
        string query,
        IEnumerable<T> candidates,
        Func<T, string> selector,
        double threshold)
    {
        T? best = default;
        var bestScore = 0.0;

        foreach (var candidate in candidates)
        {
            var score = Similarity(query, selector(candidate));
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return bestScore >= threshold ? best : default;
    }

    /// <summary>
    /// Computes the Levenshtein edit distance (minimum single-character insertions, deletions, or
    /// substitutions) between two strings.
    /// </summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>The edit distance.</returns>
    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
