namespace PoolManager
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;

    public class QueueTask : BackgroundTask
    {
        int count = 100;

        public QueueTask(IReliableStateManager manager, string queueName, bool shouldAbort = false)
        {
            this.Manager = manager;
            this.queueName = queueName;
            this.shouldAbort = shouldAbort;
        }

        public IReliableStateManager Manager { get; }

        private string queueName;
        private bool shouldAbort;

        protected override bool IsEnabled()
        {
            return true;
        }

        protected override async Task Run(CancellationToken token)
        {
            IReactiveReliableQueue<int> queue = await this.Manager.GetOrAddReactiveReliableQueue<int>(this.queueName);
            using (ITransaction tx = this.Manager.CreateTransaction())
            {
                ServiceEventSource.Current.Message($"[QUEUE] {this.queueName}: ADD STARTING DELAY");
                await Task.Delay(TimeSpan.FromSeconds(4));
                ServiceEventSource.Current.Message($"[QUEUE] {this.queueName}: Enqueue item has value {count}");
                await queue.EnqueueAsync(tx, count++, token);
                if (!this.shouldAbort)
                {
                    ServiceEventSource.Current.Message($"[QUEUE] {this.queueName}: Delaying commit by few seconds");
                    await Task.Delay(TimeSpan.FromSeconds(4));
                    await tx.CommitAsync();
                    ServiceEventSource.Current.Message($"[QUEUE] {this.queueName}: Committing");
                }
                else
                {
                    ServiceEventSource.Current.Message($"[QUEUE] {this.queueName}: Delaying Abort by few seconds");
                    await Task.Delay(TimeSpan.FromSeconds(4));
                    ServiceEventSource.Current.Message($"[QUEUE] {this.queueName}: Aborting");
                }
            }
        }

        protected override TimeSpan WaitTime()
        {
            return TimeSpan.FromSeconds(10);
        }
    }

}
