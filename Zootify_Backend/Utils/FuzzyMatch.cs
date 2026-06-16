namespace Zootify_Backend;

/// <summary>
/// Provides case-insensitive fuzzy string matching based on the Levenshtein edit distance,
/// used to resolve a taxonomy species by an approximate latin name.
/// </summary>
public static class FuzzyMatch
{
    /// <summary>
    /// Filters and ranks <paramref name="items"/> against a search query, augmenting plain substring
    /// matching with fuzzy (typo-tolerant) matching. Substring/exact matches are always kept and ranked
    /// first; additionally, items whose name is similar enough (best of whole-name and per-word
    /// similarity, above <paramref name="threshold"/>) are appended. Results are ordered by relevance.
    /// An empty query returns all items ordered by name.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="query">The search text; <c>null</c>/whitespace returns all items.</param>
    /// <param name="items">The candidate items.</param>
    /// <param name="selector">Selects the name to match for each item.</param>
    /// <param name="threshold">Minimum fuzzy similarity (0..1) for a non-substring item to be included.</param>
    /// <returns>The matching items ordered by relevance (substring first, then by similarity, then name).</returns>
    public static List<T> SearchAugmented<T>(
        string? query,
        IEnumerable<T> items,
        Func<T, string> selector,
        double threshold = 0.6)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return items.OrderBy(selector, StringComparer.OrdinalIgnoreCase).ToList();
        }

        var trimmedQuery = query.Trim();

        return items
            .Select(item =>
            {
                var name = selector(item) ?? string.Empty;
                var isSubstring = name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase);
                return (item, name, isSubstring, score: FuzzyScore(trimmedQuery, name));
            })
            .Where(x => x.isSubstring || x.score >= threshold)
            .OrderByDescending(x => x.isSubstring)
            .ThenByDescending(x => x.score)
            .ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.item)
            .ToList();
    }

    /// <summary>
    /// Returns the better of the whole-name similarity and the best single-word similarity, so a typo in
    /// one word of a multi-word name (e.g. "leoo" in "Panthera leo") still scores highly.
    /// </summary>
    /// <param name="query">The search text to compare.</param>
    /// <param name="name">The candidate name (possibly multiple words).</param>
    /// <returns>The similarity score between 0 and 1.</returns>
    private static double FuzzyScore(string query, string name)
    {
        var wholeSimilarity = Similarity(query, name);

        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var bestWordSimilarity = words.Length == 0
            ? 0.0
            : words.Max(word => Similarity(query, word));

        return Math.Max(wholeSimilarity, bestWordSimilarity);
    }

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
