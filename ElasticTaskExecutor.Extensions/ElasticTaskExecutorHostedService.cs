using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElasticTaskExecutor.Core;
using Microsoft.Extensions.Hosting;

namespace ElasticTaskExecutor.Extensions
{
    public class ElasticTaskExecutorHostedService :IHostedService
    {
        private TaskExecutionContext _executionContext;
        private List<ITaskSubscriberMetadata> _subscribers;
        private bool _waitForExecutorTaskComplete;

        public ElasticTaskExecutorHostedService(
            TaskExecutionContext executionContext,
            ElasticTaskExecutorHostServiceOptions option)
        {
            _executionContext = executionContext;
            _waitForExecutorTaskComplete = option.WaitForExecutorTaskComplete;
            _subscribers = option.Subscribers;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_executionContext != null)
            {
                await _executionContext.StartAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_waitForExecutorTaskComplete)
            {
                var tasks = new List<Task>();
                if (_executionContext != null)
                {
                    tasks.Add(_executionContext.FinalizeAsync(cancellationToken));
                }
                if (_subscribers?.Any() ?? false)
                {
                    tasks.AddRange(_subscribers.Select(async s => await s.StopSubscriptionAsync(cancellationToken).ConfigureAwait(false)));
                }
                if (tasks.Any())
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                _executionContext?.Dispose();
            }

        }
    }
}