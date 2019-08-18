namespace ElasticTaskExecutor.UnitTest.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Core.Common;

    public class DummySubscriber<T>: TaskSubscriber<T>
    {
        public DummySubscriber(ILogger executorLogger) : base(executorLogger)
        {
        }

        protected override async Task Execution(T taskPayload, CancellationTokenSource cts)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine(taskPayload);
        }
    }
}