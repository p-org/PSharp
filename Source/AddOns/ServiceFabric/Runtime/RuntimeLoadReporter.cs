namespace Microsoft.PSharp.ServiceFabric
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;

    internal class RuntimeLoadReporter : BackgroundTask
    {
        private const string MetricName = "PSharpStateMachineCount";
        private IPSharpService service;
        private IPSharpEventSourceLogger logger;
        private IStatefulServicePartition partition;

        public RuntimeLoadReporter(IPSharpService service, IPSharpEventSourceLogger logger, IStatefulServicePartition partition)
        {
            this.service = service;
            this.logger = logger;
            this.partition = partition;
        }

        protected override bool IsEnabled()
        {
            return true;
        }

        protected override async Task Run(CancellationToken token)
        {
            var resources = await this.service.ListResourceTypesAsync();
            ulong count = 0;
            foreach (var item in resources)
            {
                this.logger.Message($"{item.ResourceType} = {item.Count}");
                count += item.Count;
            }

            this.logger.Message($"{this.partition.PartitionInfo.Id} = {count}");
            this.partition.ReportLoad(new List<LoadMetric> { new LoadMetric(MetricName, (int)count) });
        }

        protected override TimeSpan WaitTime()
        {
            return TimeSpan.FromSeconds(30);
        }
    }

}
