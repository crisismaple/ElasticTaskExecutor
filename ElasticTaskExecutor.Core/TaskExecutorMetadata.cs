﻿namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    public abstract class TaskExecutorMetadata
    {
        private long _runningExecutorCounter = 0;
        public abstract bool IsEnabled { get; }
        public abstract int TaskExecutorTypeId { get; }

        public abstract string TaskExecutorName { get; }

        protected abstract ILogger Logger { get; }

        public abstract long GetMinExecutorCount();

        public abstract long GetMaxExecutorCount();

        public abstract TimeSpan? GetExecutionTimeout();

        internal long GetExecutorCounter()
        {
            return Interlocked.Read(ref _runningExecutorCounter);
        }

        internal long IncrementExecutorCounter()
        {
            return Interlocked.Increment(ref _runningExecutorCounter);
        }

        internal long DecrementExecutorCounter()
        {
            return Interlocked.Decrement(ref _runningExecutorCounter);
        }

        internal async Task CreateNewTaskExecutor()
        {
            if (TryAllocateNewTaskExecutorIndex())
            {
                try
                {
                    using (var executor = ExecutorActivator())
                    {
                        LinkNewExecutor(executor);
                        await executor.RunTaskAsync().ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException)
                {
                    Logger?.LogInfo(
                        $"Task execution been cancelled in {nameof(CreateNewTaskExecutor)} for {TaskExecutorTypeId} executor {TaskExecutorName}");
                }
                catch (Exception e)
                {
                    Logger?.LogError(
                        $"Met exception in {nameof(CreateNewTaskExecutor)} for {TaskExecutorTypeId} executor {TaskExecutorName}:{e}");
                }
            }
        }

        internal Func<bool> GlobalApproveNewExecutorCreationCriteriaInContext { get; set; }

        internal string GetTaskExecutorIndex()
        {
            return $"{TaskExecutorTypeId}:{TaskExecutorName}";
        }

        protected abstract TaskExecutor ExecutorActivator();

        private void LinkNewExecutor(TaskExecutor executor)
        {
            executor.GetExecutionTimeout = GetExecutionTimeout;
            executor.CreateNewTaskExecutor = CreateNewTaskExecutor;
            executor.DecrementExecutorCounter = DecrementExecutorCounter;
            executor.IncrementExecutorCounter = IncrementExecutorCounter;
            executor.GetMinExecutorCount = GetMinExecutorCount;
            executor.GlobalApproveNewExecutorCreationCriteria = GlobalApproveNewExecutorCreationCriteriaInContext;
        }

        private bool TryAllocateNewTaskExecutorIndex()
        {
            var taskId = Interlocked.Increment(ref _runningExecutorCounter);
            var maxExecutorCnt = GetMaxExecutorCount();
            if (maxExecutorCnt <= 0 || taskId <= maxExecutorCnt) return true;
            Interlocked.Decrement(ref _runningExecutorCounter);
            return false;
        }
    }
}