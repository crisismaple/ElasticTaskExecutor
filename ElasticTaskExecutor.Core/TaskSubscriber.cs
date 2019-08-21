namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Common;
    using Internal;

    public abstract class TaskSubscriber<T> : ExecutorBase
    {
        protected TaskSubscriber(ILogger executorLogger) : base(executorLogger)
        {
        }

        private ChannelReader<T> TaskQueueReader => LinkedMetadata.LocalCache.Reader;

        internal TaskSubscriberMetadata<T> LinkedMetadata;

        internal CancellationTokenSource TaskManagerCancellationToken { get; set; }

        private TimeSpan? ExecutionTimeout => LinkedMetadata.ExecutionTimeout;

        protected abstract Task Execution(T taskPayload, CancellationTokenSource cts);

        internal async Task StartSubscribe()
        {
            var reader = TaskQueueReader;
            SubscriberStarted?.Invoke(this, EventArgs.Empty);
            while (true)
            {
                T newPayload;
                try
                {
                    newPayload = await reader.ReadAsync(TaskManagerCancellationToken.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    ExecutorLogger.LogWarning($"Got exception when subscribing task :{e}");
                    continue;
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
                    await Execution(newPayload, cts).ConfigureAwait(false);
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
                    ExecutorLogger.LogWarning($"Meet exceptions in {nameof(StartSubscribe)}: {e}");
                    ExecutionExceptionHandler?.Invoke(this, e);
                }

                if (TaskManagerCancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            ExecutorLogger.LogInfo($"Execution safely exited");
            TaskSubscriberFinalized?.Invoke(this, EventArgs.Empty);
        }


        public event EventHandler SubscriberStarted;
        public event EventHandler TaskSubscriberFinalized;
        public event EventHandler ExecutionStarting;
        public event EventHandler ExecutionFinished;
        public event EventHandler ExecutionTimeoutEvent;
        public event ExceptionEventHandler ExecutionExceptionHandler;
    }
}