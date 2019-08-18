namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Internal;

    public abstract class TaskPuller : ExecutorBase
    {
        protected TaskPuller(ILogger executorLogger) : base(executorLogger)
        {
        }

        internal Func<Task> CreateNewTaskExecutor { get; set; }

        internal Func<bool> GlobalApproveNewExecutorCreationCriteria { get; set; }

        internal Func<bool> IsExecutorEnabled { get; set; }


        protected abstract bool ShouldTryToCreateNewPuller();

        protected abstract bool ShouldTryTerminateCurrentPuller();

        internal Func<Task<bool>> TryPerformLogoutAsync { get; set; }
        internal Func<Task> ForceLogoutAsync { get; set; }

        protected abstract Task Execution(CancellationTokenSource cts);

        internal async Task PullTaskAsync()
        {
            var alreadyLogout = false;
            while (true)
            {
                if (TaskManagerCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!IsExecutorEnabled())
                {
                    break;
                }

                var cts = TaskManagerCancellationToken;
                CancellationTokenSource localCts = null;
                var timeout = GetExecutionTimeout();
                if (timeout.HasValue)
                {
                    localCts = new CancellationTokenSource(timeout.Value);
                    cts = CancellationTokenSource.CreateLinkedTokenSource(
                        TaskManagerCancellationToken.Token,
                        localCts.Token);
                }

                try
                {
                    await Execution(cts).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (localCts?.IsCancellationRequested ?? false)
                    {
                        ExecutorLogger.LogWarning(
                            $"Execution been cancelled due to exceed timeout {timeout?.TotalSeconds} seconds");
                    }
                    else
                    {
                        ExecutorLogger.LogWarning($"Execution been cancelled");
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    ExecutorLogger.LogWarning($"ServiceCancellationTokenSource disposed, execution been cancelled");
                    break;
                }
                catch (Exception e)
                {
                    ExecutorLogger.LogWarning($"Meet exceptions in {nameof(PullTaskAsync)}: {e}");
                }

                if (TaskManagerCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!IsExecutorEnabled())
                {
                    break;
                }

                if (GlobalApproveNewExecutorCreationCriteria() && ShouldTryToCreateNewPuller())
                {
#pragma warning disable 4014
                    CreateNewTaskExecutor();
#pragma warning restore 4014
                }

                if (!ShouldTryTerminateCurrentPuller()) continue;

                //Try kill itself
                if (await TryPerformLogoutAsync().ConfigureAwait(false))
                {
                    alreadyLogout = true;
                    break;
                }
            }

            if (!alreadyLogout)
            {
                await ForceLogoutAsync().ConfigureAwait(false);
            }

            ExecutorLogger.LogInfo($"Execution safely exited");
        }
    }
}