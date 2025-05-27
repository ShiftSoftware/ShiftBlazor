using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Utils;

public class Debouncer
{
    private CancellationTokenSource? CancellationTokenSource = null;

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
}
