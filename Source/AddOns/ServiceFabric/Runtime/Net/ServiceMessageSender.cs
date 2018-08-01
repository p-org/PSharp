using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ServiceFabric.Net
{
    internal class ServicePartitionMessageSender : IRsmNetworkProvider
    {
        private string serviceName;
        private string partitionName;
        private string senderQueueName;
        private string creatorQueueName;
        private IReliableStateManager stateManager;
        private IServiceRemotingMessageSerializationProvider serializationProvider;
        private IPSharpService service;

        public ServicePartitionMessageSender(string serviceName, string partitionName, IReliableStateManager stateManager, IServiceRemotingMessageSerializationProvider serializationProvider)
        {
            this.serviceName = serviceName;
            this.partitionName = partitionName;
            this.senderQueueName = $"{serviceName}|||{partitionName}-SENDER";
            this.creatorQueueName = $"{serviceName}|||{partitionName}-CREATOR";
            this.stateManager = stateManager;
            this.serializationProvider = serializationProvider;
            this.service = GetService();
        }

        public Task Initialize(CancellationToken token)
        {
            Task.WaitAll(GetSenderQueue(), GetCreatorQueue());
            CreatorTask creatorTask = new CreatorTask(this.service, this.stateManager, this.creatorQueueName);
            SenderTask senderTask = new SenderTask(this.service, this.stateManager, this.senderQueueName);
            creatorTask.Start(token);
            senderTask.Start(token);
            return Task.CompletedTask;
        }

        private async Task<IReliableConcurrentQueue<Tuple<MachineId, Event>>> GetSenderQueue()
        {
            return await this.stateManager.GetOrAddAsync<IReliableConcurrentQueue<Tuple<MachineId, Event>>>(this.senderQueueName);
        }

        private async Task<IReliableConcurrentQueue<Tuple<MachineId, string, Event>>> GetCreatorQueue()
        {
            return await this.stateManager.GetOrAddAsync<IReliableConcurrentQueue<Tuple<MachineId, string, Event>>>(this.creatorQueueName);
        }

        private IPSharpService GetService()
        {
            IPSharpService service;

            var proxyFactory = new ServiceProxyFactory((c) =>
            {
                return new FabricTransportServiceRemotingClientFactory(
                    serializationProvider: serializationProvider
                    );
            },
            // retry several times (TODO: allow users to override policy)
            new Microsoft.ServiceFabric.Services.Communication.Client.OperationRetrySettings(
                        TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), 100)
            );

            service = proxyFactory.CreateServiceProxy<IPSharpService>(
                new Uri(serviceName),
                new ServicePartitionKey(partitionName),
                listenerName: "PSharpServiceEndpoint");

            return service;
        }

        public Task<MachineId> RemoteCreateMachineId(string machineType, string friendlyName, string endpoint)
        {
            return service.CreateMachineId(machineType, friendlyName);
        }

        public async Task RemoteCreateMachine(ITransaction tx, Type machineType, MachineId mid, Event e)
        {
            var createQueue = await GetCreatorQueue();
            // Assembly qualified name will break with versioning of assemblies - we need to move to string types
            await createQueue.EnqueueAsync(tx, new Tuple<MachineId, string, Event>(mid, machineType.AssemblyQualifiedName, e));
        }

        public async Task RemoteSend(ITransaction tx, MachineId target, Event e)
        {
            var senderQueue = await GetSenderQueue();
            await senderQueue.EnqueueAsync(tx, new Tuple<MachineId, Event>(target, e));
        }

        private class SenderTask : BackgroundTask
        {
            private IPSharpService service;
            private IReliableStateManager stateManager;
            private string queueName;

            public SenderTask(IPSharpService service, IReliableStateManager stateManager, string queueName)
            {
                this.service = service;
                this.stateManager = stateManager;
                this.queueName = queueName;
            }

            protected override bool IsEnabled()
            {
                return true;
            }

            protected override async Task Run(CancellationToken token)
            {
                IReliableConcurrentQueue<Tuple<MachineId, Event>> senderQueue = await this.stateManager.GetOrAddAsync<IReliableConcurrentQueue<Tuple<MachineId, Event>>>(this.queueName);
                List<Tuple<MachineId, Event>> events = new List<Tuple<MachineId, Event>>();
                using (ITransaction tx = this.stateManager.CreateTransaction())
                {
                    bool shouldCommit = false;
                    bool itemExists = false;
                    do
                    {
                        itemExists = false;
                        var cv = await senderQueue.TryDequeueAsync(tx, token);
                        if (cv.HasValue)
                        {
                            itemExists = true;
                            events.Add(cv.Value);
                        }
                        else
                        {
                            if (events.Count > 0)
                            {
                                await service.BulkSendEvent(events);
                                shouldCommit = true;
                            }
                        }
                    } while (itemExists);

                    if (shouldCommit)
                    {
                        await tx.CommitAsync();
                    }
                }
            }

            protected override TimeSpan WaitTime()
            {
                return TimeSpan.FromSeconds(0);
            }
        }

        private class CreatorTask : BackgroundTask
        {
            private IPSharpService service;
            private IReliableStateManager stateManager;
            private string queueName;

            public CreatorTask(IPSharpService service, IReliableStateManager stateManager, string queueName)
            {
                this.service = service;
                this.stateManager = stateManager;
                this.queueName = queueName;
            }

            protected override bool IsEnabled()
            {
                return true;
            }

            protected override async Task Run(CancellationToken token)
            {
                IReliableConcurrentQueue<Tuple<MachineId, string, Event>> creator = await this.stateManager.GetOrAddAsync<IReliableConcurrentQueue<Tuple<MachineId, string, Event>>>(this.queueName);
                List<Tuple<MachineId, string, Event>> events = new List<Tuple<MachineId, string, Event>>();
                using (ITransaction tx = this.stateManager.CreateTransaction())
                {
                    bool shouldCommit = false;
                    bool itemExists = false;
                    do
                    {
                        itemExists = false;
                        var cv = await creator.TryDequeueAsync(tx, token);
                        if (cv.HasValue)
                        {
                            itemExists = true;
                            events.Add(cv.Value);
                        }
                        else
                        {
                            if (events.Count > 0)
                            {
                                await service.BulkCreateMachine(events);
                                shouldCommit = true;
                            }
                        }
                    } while (itemExists);

                    if (shouldCommit)
                    {
                        await tx.CommitAsync();
                    }
                }
            }

            protected override TimeSpan WaitTime()
            {
                return TimeSpan.FromSeconds(0);
            }
        }
    }
}
