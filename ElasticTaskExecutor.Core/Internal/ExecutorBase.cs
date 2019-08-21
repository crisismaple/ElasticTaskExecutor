namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using Common;

    public abstract class ExecutorBase : IDisposable
    {
        public delegate void ExceptionEventHandler(object sender, Exception e);

        protected ExecutorBase(ILogger executorLogger)
        {
            ExecutorLogger = executorLogger;
        }
        
        protected ILogger ExecutorLogger { get; }

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