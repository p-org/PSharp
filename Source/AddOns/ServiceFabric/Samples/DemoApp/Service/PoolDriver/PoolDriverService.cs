namespace PoolDriver
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.PSharp;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using PoolDriver.Controllers;
    using PoolServicesContract;

    public class PoolDriverService : PSharpService
    {
        public static List<Type> KnownTypes = new List<Type>()
        {
            // Events
            typeof(ePoolDeletionRequestEvent),
            typeof(ePoolDriverConfigChangeEvent),
            typeof(ePoolResizeRequestEvent),

            // Contracts
            typeof(PoolDriverConfig)
        };

        private IReliableDictionary<string, MachineId> dict;

        public PoolDriverService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext, KnownTypes)
        {
            foreach (var item in KnownTypes)
            {
                ServiceEventSource.Current.Message($"KnownType {item.FullName}");
            }
            this.Logger = logger;
        }

        public PoolDriverService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext,  KnownTypes, reliableStateManagerReplica)
        {
            this.Logger = logger;
        }

        public ILogger Logger { get; }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            List<ServiceReplicaListener> listeners = new List<ServiceReplicaListener>(base.CreateServiceReplicaListeners());
            ServiceReplicaListener additional = new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Suffix is {listener.UrlSuffix}");
                        
                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatefulServiceContext>(serviceContext)
                                            .AddSingleton<IReliableStateManager>(this.StateManager)
                                            .AddSingleton<PoolDriverService>(this))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    .UseUrls(url)
                                    .Build();
                    }));


            listeners.Add(additional);
            return listeners;
        }

        protected override Dictionary<Type, ulong> GetMachineTypesWithMaxLoad()
        {
            return new Dictionary<Type, ulong>() { { typeof(PoolDriverMachine), 1 } };
        }

        protected override IRemoteMachineManager GetMachineManager()
        {
            return new ResourceBasedRemoteMachineManager(this, this.Partition, this.PSharpLogger);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("Starting PoolDriverService");
            await base.RunAsync(cancellationToken);
            this.dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, MachineId>>("PoolDriverSingleton");
            await Task.Delay(-1, cancellationToken);
        }

        protected override IPSharpEventSourceLogger GetPSharpRuntimeLogger()
        {
            return new MyLogger();
        }

        public async Task SubmitRequest(Config value)
        {
            var runtime = await this.RuntimeTcs.Task;
            try
            {
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    var cv = await this.dict.AddOrUpdateAsync(tx, "DATA", (key) =>
                    {
                        return runtime.CreateMachine(typeof(PoolDriverMachine), MakeConfig(value));
                    },
                    (key, macId) =>
                    {
                        runtime.SendEvent(macId, MakeConfig(value));
                        return macId;
                    });

                    await tx.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message($"Hit exception - {ex}");
            }
        }

        private ePoolDriverConfigChangeEvent MakeConfig(Config value)
        {
            ePoolDriverConfigChangeEvent change = new ePoolDriverConfigChangeEvent();
            change.Configuration = new PoolDriverConfig();
            change.Configuration.PoolData = new Dictionary<string, int>();
            for (int i = 0; i < value.TotalPools; i++)
            {
                change.Configuration.PoolData.Add($"Pool{i + 1}", i + 1);
            }

            return change;
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
