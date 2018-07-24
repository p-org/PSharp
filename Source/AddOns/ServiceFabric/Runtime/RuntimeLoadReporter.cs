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
        private IStatefulServicePartition partition;

        public RuntimeLoadReporter(IPSharpService service, IStatefulServicePartition partition)
        {
            this.service = service;
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
                count += item.Count;
            }

            this.partition.ReportLoad(new List<LoadMetric> { new LoadMetric(MetricName, (int)count) });
        }

        protected override TimeSpan WaitTime()
        {
            return TimeSpan.FromMinutes(1);
        }
    }

}
