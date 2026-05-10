namespace Zootify_Backend.Transaction;

/// <summary>
/// Marks a method to be executed within a transaction.
/// The transaction can optionally be set as read-only.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TransactionAttribute : Attribute
{
    /// <summary>
    /// Indicates whether the transaction is read-only.
    /// If set to true, no write operations are allowed.
    /// </summary>
    public bool ReadOnly { get; set; }
}