namespace Zootify_Backend.Enums;

/// <summary>
/// The lifecycle state of a collection.
/// </summary>
public enum CollectionStatus
{
    /// <summary>Active and usable.</summary>
    Active = 1,

    /// <summary>Soft-deleted; retained until the purge job removes it permanently.</summary>
    Deleted = 2
}
