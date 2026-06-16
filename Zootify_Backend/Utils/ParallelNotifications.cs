namespace Zootify_Backend;

/// <summary>
/// Helper for dispatching best-effort notifications (emails, push) to many recipients concurrently.
/// </summary>
public static class ParallelNotifications
{
    /// <summary>
    /// Runs <paramref name="send"/> for every item concurrently and waits for all of them. Each send is
    /// isolated: a failure (e.g. an undeliverable email) is swallowed so it cannot prevent the other
    /// sends from running or surface as an unobserved task exception.
    /// </summary>
    /// <typeparam name="T">The recipient/item type.</typeparam>
    /// <param name="items">The items to send a notification for.</param>
    /// <param name="send">The asynchronous send to perform per item.</param>
    public static Task RunAsync<T>(IEnumerable<T> items, Func<T, Task> send)
    {
        return Task.WhenAll(items.Select(async item =>
        {
            try
            {
                await send(item);
            }
            catch
            {
                // Best-effort: a single failed notification must not abort the rest of the batch.
            }
        }));
    }
}
