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
        
        internal ChannelReader<T> TaskQueueReader { get; set; }

        protected abstract Task Execution(T taskPayload, CancellationTokenSource cts);

        internal async Task StartSubscribe()
        {
            while (true)
            {
                T newPayload;
                try
                {
                    newPayload = await TaskQueueReader.ReadAsync(TaskManagerCancellationToken.Token).ConfigureAwait(false);
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
                    await Execution(newPayload, cts).ConfigureAwait(false);
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
                    ExecutorLogger.LogWarning($"Meet exceptions in {nameof(StartSubscribe)}: {e}");
                }

                if (TaskManagerCancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            ExecutorLogger.LogInfo($"Execution safely exited");
        }
    }
}