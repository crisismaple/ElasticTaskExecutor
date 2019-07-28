namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    public abstract class TaskExecutor : IDisposable
    {
        protected TaskExecutor(ILogger logger)
        {
            Logger = logger;
        }

        internal CancellationTokenSource TaskManagerCancellationToken { get; set; }

        protected readonly ILogger Logger;

        protected abstract Task Execution(CancellationTokenSource cts);

        protected abstract bool ShouldTryToCreateNewExecutor();

        protected abstract bool ShouldTryTerminateCurrentExecutor();

        internal Func<TimeSpan?> GetExecutionTimeout { get; set; }
        internal Func<long> IncrementExecutorCounter { get; set; }
        internal Func<long> DecrementExecutorCounter { get; set; }
        internal Func<Task> CreateNewTaskExecutor { get; set; }
        internal Func<long> GetMinExecutorCount { get; set; }
        internal Func<bool> GlobalApproveNewExecutorCreationCriteria { get; set; }
        internal Func<bool> IsExecutorEnabled { get; set; }

        public async Task RunTaskAsync()
        {
            var notDec = true;
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
                        Logger.LogWarning(
                            $"Execution been cancelled due to exceed timeout {timeout?.TotalSeconds ?? -1} seconds");
                    }
                    else
                    {
                        Logger.LogWarning($"Execution been cancelled");
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    Logger.LogWarning($"ServiceCancellationTokenSource disposed, execution been cancelled");
                    break;
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Meet exceptions in {nameof(RunTaskAsync)}: {e}");
                }

                if (TaskManagerCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!IsExecutorEnabled())
                {
                    break;
                }

                if (GlobalApproveNewExecutorCreationCriteria() && ShouldTryToCreateNewExecutor())
                {
#pragma warning disable 4014
                    CreateNewTaskExecutor();
#pragma warning restore 4014
                }

                if (!ShouldTryTerminateCurrentExecutor()) continue;

                //Try kill itself
                if (DecrementExecutorCounter() >= GetMinExecutorCount())
                {
                    notDec = false;
                    break;
                }

                // add back counter because we will not exist
                IncrementExecutorCounter();
            }

            if (notDec)
            {
                DecrementExecutorCounter();
            }

            Logger.LogInfo($"Execution safely exited");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}