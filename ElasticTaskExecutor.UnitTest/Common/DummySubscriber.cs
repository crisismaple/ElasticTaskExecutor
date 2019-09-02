namespace ElasticTaskExecutor.UnitTest.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;

    public class DummySubscriber<T>: TaskSubscriber<T>
    {
        protected override async Task Execution(T taskPayload, CancellationTokenSource cts)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine(taskPayload);
        }
    }
}