namespace Zootify_Backend.Transaction;

/// <summary>
/// A per-scope queue of side effects (e.g. emails, push notifications) that must run only AFTER the
/// surrounding database transaction has committed, so callers are never notified about work that was
/// subsequently rolled back. One instance exists per dependency-injection scope (a web request or a
/// background-job batch); it is drained by <see cref="TransactionInterceptor"/> after a successful
/// commit and is never drained for a read-only (rolled-back) transaction.
/// </summary>
public interface IPostCommitQueue
{
    /// <summary>
    /// Registers an action to run after the current transaction commits successfully.
    /// </summary>
    /// <param name="action">The deferred side effect to run post-commit.</param>
    void Enqueue(Func<Task> action);

    /// <summary>
    /// Runs and clears all queued actions. Failures are isolated so a single failing side effect can
    /// neither fail the (already committed) request nor prevent the remaining actions from running.
    /// </summary>
    Task DrainAsync();
}
