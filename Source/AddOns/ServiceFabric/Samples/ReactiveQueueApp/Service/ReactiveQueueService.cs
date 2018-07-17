namespace ReactiveQueueTest
{
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class ReactiveQueueService : StatefulService
    {
        public ReactiveQueueService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext)
        {
            this.Logger = logger;
        }

        public ReactiveQueueService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext, reliableStateManagerReplica)
        {
            this.Logger = logger;
        }

        public ILogger Logger { get; }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            QueueTask q1 = new QueueTask(this.StateManager, "QUEUE1", true);
            DequeueTask dq1 = new DequeueTask(this.StateManager, "QUEUE1");

            Task t1 = dq1.Start(cancellationToken);
            await Task.Delay(10000);
            Task t2 = q1.Start(cancellationToken);
            await Task.WhenAll(t1, t2);
        }
    }
}
