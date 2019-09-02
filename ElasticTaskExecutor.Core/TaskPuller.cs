namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Internal;

    public abstract class TaskPuller : ExecutorBase
    {
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
            TaskPullerStarted?.Invoke(this);
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
                    ExecutionStarting?.Invoke(this);
                    await Execution(cts).ConfigureAwait(false);
                    ExecutionFinished?.Invoke(this);
                }
                catch (OperationCanceledException)
                {
                    if (localCts?.IsCancellationRequested ?? false)
                    {
                        ExecutionTimeoutEvent?.Invoke(this);
                    }
                    else
                    {
                        ExecutionCancelled?.Invoke(this);
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
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
                    CreatingNewPuller?.Invoke(this);
#pragma warning disable 4014
                    LinkedMetadata.CreateNewTaskExecutor();
#pragma warning restore 4014
                    NewPullerCreated?.Invoke(this);
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

            TaskPullerFinalized?.Invoke(this);
        }

        public event ExecutorEventHandler TaskPullerStarted;
        public event ExecutorEventHandler TaskPullerFinalized;
        public event ExecutorEventHandler ExecutionStarting;
        public event ExecutorEventHandler ExecutionFinished;
        public event ExecutorEventHandler ExecutionCancelled;
        public event ExecutorEventHandler CreatingNewPuller;
        public event ExecutorEventHandler NewPullerCreated;
        public event ExecutorEventHandler ExecutionTimeoutEvent;
        public event ExceptionEventHandler ExecutionExceptionHandler;
    }
}