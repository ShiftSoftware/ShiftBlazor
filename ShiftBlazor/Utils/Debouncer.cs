
namespace ShiftSoftware.ShiftBlazor.Utils;

// lock is probably not needed here since this class is used in blazor only
// blazor runs in a single thread context
// https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-10.0
// the class is re-written by Claude Opus 4.5
public class Debouncer : IDisposable
{
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();
    private bool _disposed;

    public void Debounce(int delayMilliseconds, Action action)
    {
        Debounce(delayMilliseconds, () => { action(); return Task.CompletedTask; });
    }

    public void Debounce(int delayMilliseconds, Func<Task> action)
    {
        CancellationTokenSource cts;

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            cts = _cts;
        }

        _ = ExecuteAsync(delayMilliseconds, action, cts.Token);
    }

    private static async Task ExecuteAsync(
        int delayMs, Func<Task> action, CancellationToken token)
    {
        try
        {
            await Task.Delay(delayMs, token);
            await action();
        }
        catch (TaskCanceledException) { }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}