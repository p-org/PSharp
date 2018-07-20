namespace VMManager
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;

    public class DequeueTask : BackgroundTask
    {
        public DequeueTask(IReliableStateManager manager, string queueName)
        {
            this.Manager = manager;
            this.queueName = queueName;
        }

        public IReliableStateManager Manager { get; }

        private string queueName;

        protected override bool IsEnabled()
        {
            return true;
        }

        protected override async Task Run(CancellationToken token)
        {
            IReactiveReliableQueue<int> queue = await this.Manager.GetOrAddReactiveReliableQueue<int>(this.queueName);
            using (ITransaction tx = this.Manager.CreateTransaction())
            {
                var item = await queue.TryDequeueAsync(tx, token);
                if(item.HasValue)
                {
                    ServiceEventSource.Current.Message($"[DEQUEUE] {this.queueName}: Dequeue item has value {item.Value}");
                    await tx.CommitAsync();
                }
                else
                {
                    ServiceEventSource.Current.Message($"[DEQUEUE] {this.queueName}: Dequeue item does not have a value");
                }
            }
        }

        protected override TimeSpan WaitTime()
        {
            return TimeSpan.FromSeconds(1);
        }
    }

}
