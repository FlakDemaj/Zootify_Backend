namespace Zootify_Backend.Transaction;

/// <inheritdoc />
public class PostCommitQueue : IPostCommitQueue
{
    private readonly List<Func<Task>> _actions = [];

    /// <inheritdoc />
    public void Enqueue(Func<Task> action)
    {
        _actions.Add(action);
    }

    /// <inheritdoc />
    public async Task DrainAsync()
    {
        // Snapshot and clear first so any action that itself enqueues more work is not run in this
        // same pass (avoids re-entrancy / infinite loops).
        var actions = _actions.ToList();
        _actions.Clear();

        foreach (var action in actions)
        {
            try
            {
                await action();
            }
            catch
            {
                // Best-effort: the transaction has already committed, so a failed post-commit side
                // effect (e.g. an undeliverable email) must not fail the request or stop the others.
            }
        }
    }
}
