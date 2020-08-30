namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Common;
    using Internal;
    using Utils;
    
    public interface ITaskSubscriberMetadata
    {
        Task StopSubscriptionAsync(CancellationToken cts);

        Task ResumeSubscriptionAsync(CancellationToken cts);
    }

    public sealed class TaskSubscriberMetadata<T> : ExecutorMetadataBase<TaskSubscriber<T>>, ITaskSubscriberMetadata
    {
        private readonly SemaphoreSlim _stateChangeSemaphoreSlim = new SemaphoreSlim(1, 1);

        private volatile int _maxSubscriberCount = 0;

        internal TaskSubscriberMetadata(
            int maxSubscriberCount,
            Func<TaskSubscriber<T>> subscriberActivator,
            TimeSpan? executionTimeout,
            int? maxCacheLength = null,
            bool allowSynchronousContinuations = false)
        {
            if (maxCacheLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCacheLength));
            }

            if (maxCacheLength.HasValue && maxCacheLength.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCacheLength));
            }
            TaskManagerCancellationToken = new CancellationTokenSource();
            _subscriberActivator = subscriberActivator;
            ExecutionTimeout = executionTimeout;
            LocalCache = maxCacheLength == null
                ? Channel.CreateUnbounded<T>(new UnboundedChannelOptions()
                {
                    AllowSynchronousContinuations = allowSynchronousContinuations,
                })
                : Channel.CreateBounded<T>(new BoundedChannelOptions(maxCacheLength.Value)
                {
                    AllowSynchronousContinuations = allowSynchronousContinuations,
                    FullMode = BoundedChannelFullMode.Wait
                });
            _maxSubscriberCount = maxSubscriberCount;
            StartSubscribersInternal(_maxSubscriberCount);
        }

        internal Channel<T> LocalCache;

        internal AsyncAutoResetEvent InstanceCancelEvent = new AsyncAutoResetEvent();

        public static TaskSubscriberMetadata<T> CreateNewSubscription(
            int maxSubscriberCount,
            Func<TaskSubscriber<T>> subscriberActivator,
            TimeSpan? executionTimeout,
            int? maxCacheLength = null)
        {
            return new TaskSubscriberMetadata<T>(
                maxSubscriberCount,
                subscriberActivator,
                executionTimeout,
                maxCacheLength);
        }

        internal override async Task CreateNewTaskExecutor(CancellationToken token)
        {
            IncrementExecutorCounter();
            try
            {
                TaskSubscriber<T> subscriber = null;
                try
                {
                    subscriber = ExecutorActivator();
                }
                catch (Exception e)
                {
                    ExecutorActivationException?.Invoke(this, e);
                    return;
                }
                LinkNewExecutor(subscriber);
                SubscriberExecutionStarting?.Invoke(subscriber);
                await subscriber.StartSubscribe().ConfigureAwait(false);
                SubscriberFinalized?.Invoke(subscriber);
                subscriber?.Dispose();
            }
            catch (Exception e)
            {
                SubscriberExecutionException?.Invoke(this, e);
            }
            finally
            {
                DecrementExecutorCounter();
            }
        }

        private readonly Func<TaskSubscriber<T>> _subscriberActivator;

        protected override TaskSubscriber<T> ExecutorActivator()
        {
            return _subscriberActivator();
        }

        private void LinkNewExecutor(TaskSubscriber<T> subscriber)
        {
            subscriber.LinkedMetadata = this;
            var cts = new CancellationTokenSource();
            InstanceCancelEvent.WaitAsync().ContinueWith(t => cts.Cancel(), CancellationToken.None);
            subscriber.TaskManagerCancellationToken =
                CancellationTokenSource.CreateLinkedTokenSource(TaskManagerCancellationToken.Token, cts.Token);
        }

        private void StartSubscribersInternal(int count)
        {
            Parallel.ForEach(Enumerable.Range(0, count),
                i =>
                {
                    // ReSharper disable once MethodSupportsCancellation
                    Task.Factory.StartNew(async () =>
                        await CreateNewTaskExecutor(CancellationToken.None).ConfigureAwait(false));
                });
        }

        public async Task IncreaseSubscriberCountAsync(int count, CancellationToken ctx)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (TaskManagerCancellationToken.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(TaskSubscriberMetadata<T>));
            }

            await _stateChangeSemaphoreSlim.WaitAsync(ctx).ConfigureAwait(false);
            try
            {
                Interlocked.Add(ref _maxSubscriberCount, count);
                StartSubscribersInternal(count);
            }
            finally
            {
                _stateChangeSemaphoreSlim.Release();
            }

        }

        public async Task DecreaseSubscriberCountAsync(int count, CancellationToken ctx)
        {
            if (TaskManagerCancellationToken.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(TaskSubscriberMetadata<T>));
            }

            await _stateChangeSemaphoreSlim.WaitAsync(ctx).ConfigureAwait(false);
            try
            {
                if (count <= 0 || _maxSubscriberCount < count)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                while (count > 0)
                {
                    InstanceCancelEvent.Set();
                    count--;
                }
            }
            finally
            {
                _stateChangeSemaphoreSlim.Release();
            }
        }

        public async Task StopSubscriptionAsync(CancellationToken cts)
        {
            try
            {
                await _stateChangeSemaphoreSlim.WaitAsync(cts).ConfigureAwait(false);
                try
                {
                    if (!TaskManagerCancellationToken.IsCancellationRequested)
                    {
                        TaskManagerCancellationToken.Cancel();
                        return;
                    }
                }
                finally
                {
                    _stateChangeSemaphoreSlim.Release();
                }
                throw new InvalidOperationException();
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        public async Task ResumeSubscriptionAsync(CancellationToken cts)
        {
            try
            {
                await _stateChangeSemaphoreSlim.WaitAsync(cts).ConfigureAwait(false);
                try
                {
                    if (TaskManagerCancellationToken.IsCancellationRequested)
                    {
                        Interlocked.Exchange(ref TaskManagerCancellationToken, new CancellationTokenSource());
                        Interlocked.Exchange(ref InstanceCancelEvent, new AsyncAutoResetEvent());
                        StartSubscribersInternal(_maxSubscriberCount);
                        return;
                    }
                }
                finally
                {
                    _stateChangeSemaphoreSlim.Release();
                }
                throw new InvalidOperationException();
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        internal override int TaskExecutorTypeId => 0;

        public override TimeSpan? ExecutionTimeout { get; }

        public override long GetExecutorCounter()
        {
            return Interlocked.Read(ref RunningExecutorCounter);
        }

        public async Task PublishTask(T task, CancellationToken ctx)
        {
            if (TaskManagerCancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException();
            }

            await LocalCache.Writer.WriteAsync(task, ctx).ConfigureAwait(false);
        }


        public event ExceptionEventHandler ExecutorActivationException;
        public event ExceptionEventHandler SubscriberExecutionException;
        public event ExecutorEventHandler SubscriberFinalized;
        public event ExecutorEventHandler SubscriberExecutionStarting;
    }
}