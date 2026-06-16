namespace Zootify_Backend.Enums;

/// <summary>
/// The biological rank of a taxonomy node, ordered from broadest (phylum) to most specific (species).
/// </summary>
public enum TaxonomyRank
{
    /// <summary>Phylum (Stamm) — the broadest supported rank.</summary>
    Phylum = 0,

    /// <summary>Class (Klasse).</summary>
    Class = 1,

    /// <summary>Order (Ordnung).</summary>
    Order = 2,

    /// <summary>Family (Familie).</summary>
    Family = 3,

    /// <summary>Genus (Gattung).</summary>
    Genus = 4,

    /// <summary>Species (Art) — the most specific rank; specimens are linked to this rank.</summary>
    Species = 5
}
