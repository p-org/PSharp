namespace PoolManager
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;
    using PoolServicesContract;

    public class PoolManagerService : PSharpService
    {
        public static List<Type> KnownTypes = new List<Type>()
        {
            // Events
            typeof(ePoolDeletionRequestEvent),
            typeof(ePoolDriverConfigChangeEvent),
            typeof(ePoolResizeRequestEvent),
            typeof(eVMCreateRequestEvent),
            typeof(eVMDeleteRequestEvent),
            typeof(eVMRenewRequestEvent),
            typeof(eVMFailureEvent),

            // Contracts
            typeof(PoolDriverConfig)
        };

        public PoolManagerService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext, KnownTypes)
        {
            this.Logger = logger;
        }

        public PoolManagerService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext, KnownTypes, reliableStateManagerReplica)
        {
            this.Logger = logger;
        }

        public ILogger Logger { get; }

        protected override Dictionary<Type, ulong> GetMachineTypesWithMaxLoad()
        {
            return new Dictionary<Type, ulong>() { { typeof(PoolManagerMachine), 10 } };
        }

        protected override IRemoteMachineManager GetMachineManager()
        {
            return new ResourceBasedRemoteMachineManager(this, this.Partition, this.PSharpLogger);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
            await Task.Delay(-1, cancellationToken);
        }

        protected override IPSharpEventSourceLogger GetPSharpRuntimeLogger()
        {
            return new MyLogger();
        }

        private class MyLogger : IPSharpEventSourceLogger
        {
            public void Message(string message)
            {
                ServiceEventSource.Current.Message(message);
            }

            public void Message(string message, params object[] args)
            {
                ServiceEventSource.Current.Message(message, args);
            }
        }
    }
}
