
namespace ShiftSoftware.ShiftBlazor.Utils;

public class Debouncer
{
    private CancellationTokenSource? CancellationTokenSource = null;
    private readonly object _lock = new object();

    public void Debounce(int delayMilliseconds, Action action)
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = new();

        Task.Delay(delayMilliseconds, CancellationTokenSource.Token)
            .ContinueWith(task =>
            {
                if (!task.IsCanceled)
                {
                    action();
                }
            }, TaskScheduler.Default);
    }

    public void Debounce(int delayMilliseconds, Func<Task> action)
    {
        lock (_lock)
        {
            CancellationTokenSource?.Cancel();
            CancellationTokenSource = new();

            Task.Delay(delayMilliseconds, CancellationTokenSource.Token)
                .ContinueWith(async task =>
                {
                    if (!task.IsCanceled)
                    {
                        await action();
                    }
                }, TaskScheduler.Default);
        }
    }
}
