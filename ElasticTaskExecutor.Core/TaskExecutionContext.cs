namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Internal;

    public sealed class TaskExecutionContext : IDisposable
    {
        private volatile bool _isFinalizing = false;

        private readonly SemaphoreSlim _operationSemaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly Dictionary<int, TaskPullerMetadata> _executorRegistry =
            new Dictionary<int, TaskPullerMetadata>();

        private readonly ILogger _logger;
        public TimeSpan ExecutionMonitoringInterval;
        public TimeSpan ExitMonitoringInterval;
        public bool PrintMonitorInfo;

        public TaskExecutionContext(ILogger logger, TimeSpan executionMonitoringInterval,
            TimeSpan exitMonitoringInterval, bool printMonitorInfo = false)
        {
            _logger = logger;
            ExecutionMonitoringInterval = executionMonitoringInterval;
            ExitMonitoringInterval = exitMonitoringInterval;
            PrintMonitorInfo = printMonitorInfo;
            var daemonExecutorMetadata = new DaemonExecutorMetadata(() =>
            {
                return new DaemonExecutor(
                    logger,
                    _executorRegistry,
                    () => executionMonitoringInterval,
                    () => printMonitorInfo);
            });
            TryRegisterNewExecutorInternalAsync(daemonExecutorMetadata, CancellationToken.None).Wait();
#pragma warning disable 4014
            daemonExecutorMetadata.CreateNewTaskExecutor();
#pragma warning restore 4014
        }

        public async Task<bool> TryRegisterNewExecutorAsync(TaskPullerMetadata metadata, CancellationToken cancellation)
        {
            if (metadata.TaskExecutorTypeId == Constraint.DaemonExecutorId)
            {
                return false;
            }

            return await TryRegisterNewExecutorInternalAsync(metadata, cancellation).ConfigureAwait(false);
        }

        private async Task<bool> TryRegisterNewExecutorInternalAsync(TaskPullerMetadata metadata,
            CancellationToken cancellation)
        {
            try
            {
                await _operationSemaphoreSlim.WaitAsync(cancellation).ConfigureAwait(false);

            }
            catch (OperationCanceledException)
            {
                return false;
            }

            try
            {
                if (!_isFinalizing)
                {
                    var key = metadata.TaskExecutorTypeId;
                    if (_executorRegistry.ContainsKey(key))
                    {
                        return false;
                    }

                    HandleMetadataRegistration(metadata);
                    _executorRegistry.Add(key, metadata);
                    return true;
                }

                return false;
            }
            finally
            {
                _operationSemaphoreSlim.Release();

            }
        }

        public async Task<bool> TryUnRegisterExecutorAsync(int taskExecutorTypeId, CancellationToken token)
        {
            if (taskExecutorTypeId == Constraint.DaemonExecutorId)
            {
                return false;
            }

            try
            {
                await _operationSemaphoreSlim.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            try
            {
                if (_isFinalizing)
                {
                    return false;
                }

                if (_executorRegistry.TryGetValue(taskExecutorTypeId, out var metadataInstance))
                {
                    _executorRegistry.Remove(taskExecutorTypeId);
                    HandleMetadataUnRegistration(metadataInstance);
                }

                return true;
            }
            finally
            {
                _operationSemaphoreSlim.Release();
            }
        }

        public async Task FinalizeAsync()
        {
            _cts.Cancel();
            await _operationSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                _isFinalizing = true;
            }
            finally
            {
                _operationSemaphoreSlim.Release();
            }

            while (true)
            {
                var currentRunningStatus = _executorRegistry.ToDictionary(kv => kv.Key,
                    kv => (kv.Value.TaskExecutorName, kv.Value.GetExecutorCounter()));
                var currentRunningTaskCnt = currentRunningStatus.Values.Select(m => m.Item2).Sum();
                if (currentRunningTaskCnt <= 0)
                {
                    if (PrintMonitorInfo)
                    {
                        _logger?.LogInfo("All executors exited gracefully");
                    }

                    break;
                }

                if (PrintMonitorInfo)
                {
                    _logger?.LogInfo(
                        $"Waiting safe exit. Pending executor info: {currentRunningTaskCnt} task is running.");
                    foreach (var kv in currentRunningStatus)
                    {
                        if (kv.Value.Item2 > 0)
                        {
                            _logger?.LogInfo($"{kv.Key}: ({kv.Value.TaskExecutorName}) -> {kv.Value.Item2} running;");
                        }
                    }
                }

                await Task.Delay(ExitMonitoringInterval).ConfigureAwait(false);
            }

            foreach (var taskExecutorMetadata in _executorRegistry)
            {
                HandleMetadataUnRegistration(taskExecutorMetadata.Value);
            }
        }

        private void HandleMetadataRegistration(TaskPullerMetadata metadata)
        {
            if (metadata.TaskManagerCancellationToken != null)
            {
                throw new Exception("Metadata was already bind to an execution context");
            }

            metadata.TaskManagerCancellationToken = _cts;
            metadata.GlobalApproveNewExecutorCreationCriteriaInContext = GlobalApproveNewExecutorCreationCriteria;
        }

        private void HandleMetadataUnRegistration(TaskPullerMetadata metadata)
        {
            if (metadata != null)
            {
                metadata.TaskManagerCancellationToken = null;
                metadata.GlobalApproveNewExecutorCreationCriteriaInContext = null;
            }
        }

        public Func<bool> GlobalApproveNewExecutorCreationCriteria = () => true;

        public void Dispose()
        {
            _operationSemaphoreSlim?.Dispose();
            _cts?.Dispose();
        }
    }
}