namespace ElasticTaskExecutor.Core.Utils
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class AsyncAutoResetEvent
    {
        private static readonly Task<bool> Tcs = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> _eventQueue = new Queue<TaskCompletionSource<bool>>();

        private volatile bool _alreadySet = false;

        public void Set()
        {
            lock (_eventQueue)
            {
                if (_eventQueue.Any())
                {
                    var firstEvent = _eventQueue.Dequeue();
                    firstEvent.SetResult(true);
                }

                if (!_alreadySet)
                {
                    _alreadySet = true;
                }
            }
        }

        public Task WaitAsync()
        {
            lock (_eventQueue)
            {
                if (_alreadySet)
                {
                    _alreadySet = false;
                    return Tcs;
                }
                var currentTask = new TaskCompletionSource<bool>();
                _eventQueue.Enqueue(currentTask);
                return currentTask.Task;
            }
        }
    }
}