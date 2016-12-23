using System;
using System.Threading;
using System.Threading.Tasks;

namespace BixPlugins
{
    public static class Extensions
    {
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task; // Very important in order to propagate exceptions
            }

            Log.Error("TimeoutException The operation has timed out.");
            //   throw new TimeoutException("The operation has timed out.");
            
            return default(TResult);
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                await task; // Very important in order to propagate exceptions
            }
            else
            {
                Log.Error("TimeoutException The operation has timed out.");
                //   throw new TimeoutException("The operation has timed out.");
            }
        }
    }
}