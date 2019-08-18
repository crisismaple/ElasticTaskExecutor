﻿namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    public abstract class ExecutorMetadataBase<T> where T: ExecutorBase
    {
        internal long RunningExecutorCounter = 0L;
        
        public abstract int TaskExecutorTypeId { get; }

        public string TaskExecutorName { get; set; }

        protected abstract ILogger Logger { get; }

        public TimeSpan? ExecutionTimeout { get; set; }

        public abstract long GetExecutorCounter();

        internal long IncrementExecutorCounter()
        {
            return Interlocked.Increment(ref RunningExecutorCounter);
        }

        internal long DecrementExecutorCounter()
        {
            return Interlocked.Decrement(ref RunningExecutorCounter);
        }

        internal volatile CancellationTokenSource TaskManagerCancellationToken;

        internal abstract Task CreateNewTaskExecutor();

        internal string GetTaskExecutorIndex()
        {
            return $"{TaskExecutorTypeId}:{TaskExecutorName}";
        }
        
        protected abstract T ExecutorActivator();
    }
}