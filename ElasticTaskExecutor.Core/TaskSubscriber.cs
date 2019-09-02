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
        private ChannelReader<T> TaskQueueReader => LinkedMetadata.LocalCache.Reader;

        internal TaskSubscriberMetadata<T> LinkedMetadata;

        internal CancellationTokenSource TaskManagerCancellationToken { get; set; }

        private TimeSpan? ExecutionTimeout => LinkedMetadata.ExecutionTimeout;

        protected abstract Task Execution(T taskPayload, CancellationTokenSource cts);

        internal async Task StartSubscribe()
        {
            var reader = TaskQueueReader;
            SubscriberStarted?.Invoke(this);
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
                    TaskPayloadFetchException?.Invoke(this, e);
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
                    ExecutionStarting?.Invoke(this);
                    await Execution(newPayload, cts).ConfigureAwait(false);
                    ExecutionFinished?.Invoke(this);
                }
                catch (OperationCanceledException ex)
                {
                    if (localCts?.IsCancellationRequested ?? false)
                    {
                        ExecutionTimeoutEvent?.Invoke(this);
                    }
                    else
                    {
                        SubscriberCancelled?.Invoke(this, ex);
                        break;
                    }
                }
                catch (ObjectDisposedException ed)
                {
                    SubscriberCancelled?.Invoke(this, ed);
                    break;
                }
                catch (Exception e)
                {
                    ExecutionException?.Invoke(this, e);
                }

                if (TaskManagerCancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
            TaskSubscriberFinalized?.Invoke(this);
        }

        public event ExecutorEventHandler SubscriberStarted;
        public event ExecutorEventHandler TaskSubscriberFinalized;
        public event ExecutorEventHandler ExecutionStarting;
        public event ExecutorEventHandler ExecutionFinished;
        public event ExecutorEventHandler ExecutionTimeoutEvent;
        public event ExceptionEventHandler SubscriberCancelled;
        public event ExceptionEventHandler ExecutionException;
        public event ExceptionEventHandler TaskPayloadFetchException;
    }
}