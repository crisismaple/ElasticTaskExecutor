namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using System.Threading;
    using Common;

    public abstract class ExecutorBase : IDisposable
    {
        protected ExecutorBase(ILogger executorLogger)
        {
            ExecutorLogger = executorLogger;
        }

        internal CancellationTokenSource TaskManagerCancellationToken { get; set; }

        protected ILogger ExecutorLogger { get; }


        internal Func<TimeSpan?> GetExecutionTimeout { get; set; }


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