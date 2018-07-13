namespace Microsoft.ServiceFabric.Grpc
{
    using global::Grpc.Core;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using System;
    using System.Fabric;

    public class GrpcCommunicationClient<TClient> : ICommunicationClient
        where TClient : ClientBase<TClient>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public TClient Client { get; }
        public ChannelCache.ChannelEntry ChannelEntry { get; }
        public string ConnectionAddress { get; }

        internal GrpcCommunicationClient(string connectionAddress, ChannelCache.ChannelEntry channelEntry, TClient client)
        {
            Client = client;
            ChannelEntry = channelEntry;
            ConnectionAddress = connectionAddress;
        }

        public ResolvedServicePartition ResolvedServicePartition { get; set; }
        public string ListenerName { get; set; }
        public ResolvedServiceEndpoint Endpoint { get; set; }
    }
}
