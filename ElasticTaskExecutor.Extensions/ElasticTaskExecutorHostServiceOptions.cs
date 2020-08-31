using ElasticTaskExecutor.Core;
using System.Collections.Generic;

namespace ElasticTaskExecutor.Extensions
{
    public class ElasticTaskExecutorHostServiceOptions
    {
        public bool WaitForExecutorTaskComplete { get; set; } = true;

        public bool EnableTaskPullerContext { get; set; } = true;

        internal List<ITaskSubscriberMetadata> Subscribers { get; set; } = new List<ITaskSubscriberMetadata>();

        public void AddSubscriberMetadataMetadata<T>(TaskSubscriberMetadata<T> metadata)
        {
            if(metadata != null)
            {
                Subscribers.Add(metadata);
            }
        }
    }
}
