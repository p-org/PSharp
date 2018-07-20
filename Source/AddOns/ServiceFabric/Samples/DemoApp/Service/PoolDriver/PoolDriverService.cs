namespace PoolDriver
{
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class PoolDriverService : StatefulService
    {
        public PoolDriverService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext)
        {
            this.Logger = logger;
        }

        public PoolDriverService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext, reliableStateManagerReplica)
        {
            this.Logger = logger;
        }

        public ILogger Logger { get; }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            //IReactiveReliableQueue<int> queue = await this.StateManager.GetOrAddReactiveReliableQueue<int>("QUEUE1");
            //using (ITransaction tx = this.StateManager.CreateTransaction())
            //{
            //    await queue.EnqueueAsync(tx, 1);
            //    await queue.EnqueueAsync(tx, 2);
            //    await tx.CommitAsync();
            //}

            QueueTask q1 = new QueueTask(this.StateManager, "QUEUE1");
            // QueueTask q1 = new QueueTask(this.StateManager, "QUEUE1", true);
            DequeueTask dq1 = new DequeueTask(this.StateManager, "QUEUE1");

            Task t1 = dq1.Start(cancellationToken);
            await Task.Delay(10000);
            Task t2 = q1.Start(cancellationToken);
            await Task.WhenAll(t1, t2);
        }
    }
}
