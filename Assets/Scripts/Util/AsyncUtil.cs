using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Util {
    /// <summary>
    /// Utility methods for working with async tasks
    /// </summary>
    public static class AsyncUtil {
        /// <summary>
        /// Blocks until condition is true or timeout occurs. Checks for the condition on a background thread. 
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1) {
            Task waitTask = Task.Run(async () => {
                while (!condition()) {
                    await Task.Delay(frequency);
                }
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout))) 
                throw new TimeoutException();
        }

        /// <summary>
        /// Blocks until condition is true. Blocks indefinitely. Runs on the caller thread. 
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        public static async Task WaitUntilOnCallerThread(Func<bool> condition, int frequency = 25) {
            while (!condition()) {
                await Task.Delay(frequency);
            }
        }

        /// <summary>
        /// Fire off a task without caring whether it completes. Useful for running a task on the current thread from a
        /// non-async context.
        /// </summary>
        public static async void FireAndForget(this Task task) {
            try {
                await task;
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }
}
