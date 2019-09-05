# Elastic Task Executor
[Elastic Task Executor](https://github.com/crisismaple/ElasticTaskExecutor) is a configuration-oriented offline task management library for .net written in C#. It fully adopts .net [Task-based asynchronous pattern (TAP)](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap) and supports consuming tasks in both Pull/Push and Pub/Sub model with hot-swappable task management and elastic capability control with execution monitoring and exception handling.
# Implementation
The core library targets on [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) and direct uses CLR build thread pool for task execution and depends on following .NET extension libraries:
+ [System.Threading.Channels](https://www.nuget.org/packages/System.Threading.Channels/)
+ [System.Threading.Tasks.Extensions](https://www.nuget.org/packages/System.Threading.Tasks.Extensions/)
# Concepts
## Task Executor
Indicate a logic task runner instance, it could work in Pull(Task Puller) or Subscribe(Task Subscriper) model.
### Task Puller
A task puller is a type of executor that continous pulling task from external task source and execute it locally.
### Task Subscriber
A subsciber will only be activated once new task been published in the subscription.
## Task Execution Context
The execution context is used to control the lifetime of all task executors, it will manage different type of task executors based on their corresponding configuration in metadata. Each execution context has a build-in deamon task to monitor and subscribe the change of registered task metadata.
## Task Metadata
Metadata describe the runtime strategy and configuration of a particular type of task executor including max/min executor count, capeblity adjustment strategy, task timeout etc.

# Usage Samples
## Task Puller Example
``` CSharp
    /// <summary>
    /// Step 1: Define your own puller class derive from TaskPuller
    /// </summary>
    public class DummyPuller : TaskPuller
    {
        protected override async Task Execution(CancellationTokenSource cts)
        {
            cts.Token.ThrowIfCancellationRequested();
            Console.WriteLine($"Executing in {this.Id}");
            await Task.Delay(1000).ConfigureAwait(false);
        }

        protected override bool ShouldTryToCreateNewPuller()
        {
            Console.WriteLine($"Should try to create new puller?");
            return false;
        }

        protected override bool ShouldTryTerminateCurrentPuller()
        {
            Console.WriteLine($"Should try to terminate current puller?");
            return false;
        }
    }
    /// <summary>
    /// Step 2: Define your customized metadata class derive from TaskPullerMetadata
    /// </summary>
    public class DummyPullerMetadata : TaskPullerMetadata
    {
        public bool IsEnabled { private get; set; }

        public override bool IsExecutorEnabled => IsEnabled;

        public override int TaskExecutorTypeId => 123;

        public override TimeSpan? ExecutionTimeout => new TimeSpan(1,0,0); //Timeout for a single task execution is set to 1 hour

        public override long GetMinExecutorCount()
        {
            // We will always keep at least 2 executor running
            return 2;
        }

        public override long GetMaxExecutorCount()
        {
            // We will at most allow at most 3 executor running
            return 3;
        }

        protected override TaskPuller ExecutorActivator()
        {
            return new DummyPuller();
        }
    }

    //Step 3: Config the execution context in your process and regist the metadata
    var monitorTimespan = TimeSpan.FromSeconds(10);
    var exitWaitingTimespan = TimeSpan.FromSeconds(5);
    var dummyMetadata = new DummyPullerMetadata();
    var logger = new DummyLogger();
    using (var context = new TaskExecutionContext(logger, monitorTimespan, exitWaitingTimespan, true))
    {
        await context.TryRegisterNewExecutorAsync(dummyMetadata, CancellationToken.None).ConfigureAwait(false);
        await Task.Delay(5000).ConfigureAwait(false);
        dummyMetadata.IsEnabled = false; //Pause the task puller suite
        await Task.Delay(5000).ConfigureAwait(false);
        dummyMetadata.IsEnabled = true; //Resume the task puller suite
        await Task.Delay(5000).ConfigureAwait(false);
        await context.FinalizeAsync().ConfigureAwait(false); //Wait till all executor exit gracefully
    }
```
## Task Subscriper Example
``` CSharp
    /// <summary>
    /// Step 1: Define your own subscriber class derive from TaskSubscriber
    /// </summary>
    public class DummySubscriber<T>: TaskSubscriber<T>
    {
        protected override async Task Execution(T taskPayload, CancellationTokenSource cts)
        {
            await Task.Delay(1000).ConfigureAwait(false);
        }
    }

    //Step 2: Just use it!
    //Task Executor Type Id is 2
    //Subscriber count is set to 10
    //Task execution timeout is set to 30 min
    var subscription = TaskSubscriberMetadata<long>.CreateNewSubscription(
        2,
        10,
        () => new DummySubscriber<long>(),
        TimeSpan.FromMinutes(30));

    //Increase subscriber count to 12
    await subscription.IncreaseSubscriberCountAsync(2, CancellationToken.None);

    //Publish task to the subscription
    await subscription.PublishTask(1000, CancellationToken.None);

    //Decrease subscriber count to 7
    await subscription.IncreaseSubscriberCountAsync(5, CancellationToken.None);

    //Pause subscription
    await subscription.StopSubscriptionAsync(CancellationToken.None);

    await Task.Delay(3000).ConfigureAwait(false);

    //Resume Subscription
    await subscription.ResumeSubscriptionAsync(CancellationToken.None);
```