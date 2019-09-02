namespace ElasticTaskExecutor.Core.Internal
{
    using System;

    public abstract class ExecutorBase : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();

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