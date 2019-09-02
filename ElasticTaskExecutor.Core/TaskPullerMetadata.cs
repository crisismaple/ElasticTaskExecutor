using ElasticTaskExecutor.Core.Common;

namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Internal;

    public abstract class TaskPullerMetadata : ExecutorMetadataBase<TaskPuller>
    {
        internal readonly SemaphoreSlim OperationSemaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Only be evaluated when context trying to activate a suspended puller
        /// </summary>
        public virtual bool ShouldBeReactivate => true;

        /// <summary>
        /// Running puller count will be greater or equals to this count
        /// 0 means the puller could be suspended and would be re-activated by context
        /// </summary>
        /// <returns>Minimum puller instance count</returns>
        public abstract long GetMinExecutorCount();

        public abstract long GetMaxExecutorCount();

        public override long GetExecutorCounter()
        {
            OperationSemaphoreSlim.Wait();
            try
            {
                return Interlocked.Read(ref RunningExecutorCounter);
            }
            finally
            {
                OperationSemaphoreSlim.Release();
            }
        }

        internal override async Task CreateNewTaskExecutor()
        {
            try
            {
                if (await TryAllocateNewTaskExecutorIndexAsync().ConfigureAwait(false))
                {

                    TaskPuller puller = null;
                    try
                    {
                        puller = ExecutorActivator();
                    }
                    catch (Exception e)
                    {
                        ExecutorActivationException?.Invoke(this, e);
                        Interlocked.Decrement(ref RunningExecutorCounter);
                        return;
                    }

                    LinkNewExecutor(puller);
                    await puller.PullTaskAsync().ConfigureAwait(false);
                    puller?.Dispose();

                }
            }
            catch (Exception e)
            {
                PullerExecutionException?.Invoke(this, e);
            }
        }

        internal volatile Func<bool> GlobalApproveNewExecutorCreationCriteriaInContext;

        public virtual bool IsExecutorEnabled { get; } = true;

        internal async Task<bool> TryPerformLogoutAsync()
        {
            await OperationSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (DecrementExecutorCounter() >= GetMinExecutorCount())
                {
                    return true;
                }
                IncrementExecutorCounter();
                return false;
            }
            finally
            {
                OperationSemaphoreSlim.Release();
            }
        }

        internal async Task ForceLogoutAsync()
        {
            await OperationSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                DecrementExecutorCounter();
            }
            finally
            {
                OperationSemaphoreSlim.Release();
            }
        }

        private void LinkNewExecutor(TaskPuller puller)
        {
            puller.LinkedMetadata = this;
        }

        private async Task<bool> TryAllocateNewTaskExecutorIndexAsync()
        {
            await OperationSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                var maxExecutorCnt = GetMaxExecutorCount();
                if (maxExecutorCnt <= 0 || IncrementExecutorCounter() <= maxExecutorCnt) return true;
                DecrementExecutorCounter();
                return false;
            }
            finally
            {
                OperationSemaphoreSlim.Release();
            }
        }


        public event ExceptionEventHandler ExecutorActivationException;
        public event ExceptionEventHandler PullerExecutionException;
    }
}