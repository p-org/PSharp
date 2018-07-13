namespace Microsoft.ServiceFabric.Grpc
{
    using global::Grpc.Core;
    using Microsoft.Extensions.Logging;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class GrpcCommunicationClientFactory<TClient> : CommunicationClientFactoryBase<GrpcCommunicationClient<TClient>>
        where TClient : ClientBase<TClient>
    {
        private ILogger Log { get; }
        private readonly ChannelCache _channelCache;
        private readonly Func<Channel, TClient> _creator;

        public GrpcCommunicationClientFactory(
            ILogger logger, ChannelCache channelCache,
            Func<Channel, TClient> creator,
            IServicePartitionResolver servicePartitionResolver = null,
            IEnumerable<IExceptionHandler> exceptionHandlers = null,
            string traceId = null) : base(servicePartitionResolver, GetExceptionHandlers(logger, exceptionHandlers), traceId)
        {
            Log = logger;
            _channelCache = channelCache;
            _creator = creator;

            ClientConnected += GrpcCommunicationClientFactory_ClientConnected;
            ClientDisconnected += GrpcCommunicationClientFactory_ClientDisconnected;
        }

        private static IEnumerable<IExceptionHandler> GetExceptionHandlers(
                        ILogger logger,
                      IEnumerable<IExceptionHandler> exceptionHandlers)
        {
            var handlers = new List<IExceptionHandler>();
            if (exceptionHandlers != null)
            {
                handlers.AddRange(exceptionHandlers);
            }

            handlers.Add(new GrpcExceptionHandler(logger));
            handlers.Add(new ServiceRemotingExceptionHandler());
            return handlers;
        }

        private void GrpcCommunicationClientFactory_ClientDisconnected(object sender, CommunicationClientEventArgs<GrpcCommunicationClient<TClient>> e)
        {
            var channel = e.Client.ChannelEntry.Channel;
            Log.LogDebug("Client {Id} disconnected: {Target}, {Resolved}, {State}", e.Client.Id, channel.Target, channel.ResolvedTarget, channel.State);
        }

        private void GrpcCommunicationClientFactory_ClientConnected(object sender, CommunicationClientEventArgs<GrpcCommunicationClient<TClient>> e)
        {
            var channel = e.Client.ChannelEntry.Channel;
            Log.LogDebug("Client {Id} connected: {Target}, {Resolved}, {State}", e.Client.Id, channel.Target, channel.ResolvedTarget, channel.State);
        }

        protected override void AbortClient(GrpcCommunicationClient<TClient> client)
        {
            var channel = client.ChannelEntry.Channel;
            Log.LogDebug("Abort client {Id} for: {Target}, {Resolved}, {State}", client.Id, channel.Target, channel.ResolvedTarget, channel.State);
            client.ChannelEntry.ShutdownAsync().Wait();
        }

        protected override Task<GrpcCommunicationClient<TClient>> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            Log.LogTrace("Create client for {Endpoint}", endpoint);
            var channelEntry = _channelCache.GetOrCreate(endpoint);
            var client = _creator(channelEntry.Channel);
            var communicationClient = new GrpcCommunicationClient<TClient>(endpoint, channelEntry, client);
            Log.LogTrace("Created client {Id} for {Endpoint}", communicationClient.Id, endpoint);
            return Task.FromResult(communicationClient);
        }

        protected override bool ValidateClient(GrpcCommunicationClient<TClient> client) => client.ChannelEntry.Validate(Log);

        protected override bool ValidateClient(string endpoint, GrpcCommunicationClient<TClient> client) => endpoint == client.ConnectionAddress && ValidateClient(client);
    }
}
