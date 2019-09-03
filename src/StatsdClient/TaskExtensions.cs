using System;
using System.Threading;
using System.Threading.Tasks;

namespace StatsdClient
{
    public static class TaskExtensions
    {
        private static readonly Action<Task> IgnoreTaskContinuation;

        public static void Ignore(this Task task)
        {
            if (task.IsCompleted)
            {
                AggregateException exception = task.Exception;
            }
            else
                task.ContinueWith(TaskExtensions.IgnoreTaskContinuation, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        static TaskExtensions()
        {
            AggregateException exception;
            TaskExtensions.IgnoreTaskContinuation = (Action<Task>) (t => exception = t.Exception);
        }
    }
}