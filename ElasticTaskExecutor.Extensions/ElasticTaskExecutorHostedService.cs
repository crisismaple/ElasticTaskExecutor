using System.Threading;
using System.Threading.Tasks;
using ElasticTaskExecutor.Core;
using Microsoft.Extensions.Hosting;

namespace ElasticTaskExecutor.Extensions
{
    public class ElasticTaskExecutorHostedService :IHostedService
    {
        private TaskExecutionContext _executionContext;
        private bool _waitForExecutorTaskComplete;

        public ElasticTaskExecutorHostedService(TaskExecutionContext executionContext, 
            bool waitForExecutorTaskComplete)
        {
            _executionContext = executionContext;
            _waitForExecutorTaskComplete = waitForExecutorTaskComplete;
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
            if (_waitForExecutorTaskComplete && _executionContext != null)
            {
                await _executionContext.FinalizeAsync(cancellationToken).ConfigureAwait(false);
                _executionContext.Dispose();
            }
        }
    }
}