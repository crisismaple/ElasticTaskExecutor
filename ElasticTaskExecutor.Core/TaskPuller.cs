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

        internal TaskPullerMetadata LinkedMetadata { get; set; }
        
        protected abstract bool ShouldTryToCreateNewPuller();

        protected abstract bool ShouldTryTerminateCurrentPuller();
        
        protected abstract Task Execution(CancellationTokenSource cts);

        private CancellationTokenSource TaskManagerCancellationToken =>
            LinkedMetadata.TaskManagerCancellationToken;

        private TimeSpan? ExecutionTimeout => LinkedMetadata.ExecutionTimeout;

        internal async Task PullTaskAsync()
        {
            var alreadyLogout = false;
            TaskPullerStarted?.Invoke(this, EventArgs.Empty);
            while (true)
            {
                if (TaskManagerCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!LinkedMetadata.IsExecutorEnabled)
                {
                    break;
                }

                var cts = TaskManagerCancellationToken;
                CancellationTokenSource localCts = null;
                var timeout = ExecutionTimeout;
                if (timeout.HasValue)
                {
                    localCts = new CancellationTokenSource(timeout.Value);
                    cts = CancellationTokenSource.CreateLinkedTokenSource(
                        TaskManagerCancellationToken.Token,
                        localCts.Token);
                }

                try
                {
                    ExecutionStarting?.Invoke(this, EventArgs.Empty);
                    await Execution(cts).ConfigureAwait(false);
                    ExecutionFinished?.Invoke(this, EventArgs.Empty);
                }
                catch (OperationCanceledException)
                {
                    if (localCts?.IsCancellationRequested ?? false)
                    {
                        ExecutorLogger.LogWarning(
                            $"Execution been cancelled due to exceed timeout {timeout?.TotalSeconds} seconds");
                        ExecutionTimeoutEvent?.Invoke(this, EventArgs.Empty);
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
                    ExecutionExceptionHandler?.Invoke(this, e);
                }

                if (TaskManagerCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!LinkedMetadata.IsExecutorEnabled)
                {
                    break;
                }

                if (LinkedMetadata.GlobalApproveNewExecutorCreationCriteriaInContext() && ShouldTryToCreateNewPuller())
                {
                    CreatingNewPuller?.Invoke(this, EventArgs.Empty);
#pragma warning disable 4014
                    LinkedMetadata.CreateNewTaskExecutor();
#pragma warning restore 4014
                    NewPullerCreated?.Invoke(this, EventArgs.Empty);
                }

                if (!ShouldTryTerminateCurrentPuller()) continue;

                //Try kill itself
                if (await LinkedMetadata.TryPerformLogoutAsync().ConfigureAwait(false))
                {
                    alreadyLogout = true;
                    break;
                }
            }

            if (!alreadyLogout)
            {
                await LinkedMetadata.ForceLogoutAsync().ConfigureAwait(false);
            }

            TaskPullerFinalized?.Invoke(this, EventArgs.Empty);
            ExecutorLogger.LogInfo($"Execution safely exited");
        }

        public event EventHandler TaskPullerStarted;
        public event EventHandler TaskPullerFinalized;
        public event EventHandler ExecutionStarting;
        public event EventHandler ExecutionFinished;
        public event EventHandler CreatingNewPuller;
        public event EventHandler NewPullerCreated;
        public event EventHandler ExecutionTimeoutEvent;
        public event ExceptionEventHandler ExecutionExceptionHandler;
    }
}