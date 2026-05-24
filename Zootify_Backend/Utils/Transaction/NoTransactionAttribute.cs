namespace Zootify_Backend.Transaction;

/// <summary>
/// Marks a method to be executed without a transaction.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class NoTransactionAttribute : Attribute;