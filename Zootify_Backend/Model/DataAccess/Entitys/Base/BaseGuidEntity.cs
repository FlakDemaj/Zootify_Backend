namespace Zootify_Backend.Model.DataAccess.Entitys.Base;

public abstract class BaseGuidEntity
{
    // Version 7 UUIDs are time-ordered, which keeps B-tree index inserts sequential
    // and avoids the page-split churn caused by random (v4) GUIDs.
    public Guid Guid { get; private set; } = Guid.CreateVersion7();

}