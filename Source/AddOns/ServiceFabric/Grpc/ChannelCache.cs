namespace Microsoft.ServiceFabric.Grpc
{
    using global::Grpc.Core;
    using System;
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public sealed class ChannelCache
    {
        public static ChannelCache CreateDefault(ILogger log) =>
            new ChannelCache(log, new MemoryCache(Options.Create(new MemoryCacheOptions())));

        public IMemoryCache Cache { get; }
        public ILogger Log { get; }

        public class ChannelEntry
        {
            public Guid Id { get; } = Guid.NewGuid();
            public Channel Channel { get; }
            public bool Invalid { get; private set; }

            public ChannelEntry(Channel channel)
            {
                Channel = channel;
            }

            public bool Validate(ILogger log)
            {
                if (Invalid)
                {
                    return false;
                }
                else
                {
                    switch (Channel.State)
                    {
                        case ChannelState.Shutdown:
                        case ChannelState.TransientFailure:
                            log.LogInformation("Channel {Id} for {Target} has state of {State}",
                                Id, Channel.Target, Channel.State);
                            Invalid = true;
                            return false;

                        default:
                            return true;
                    }
                }
            }

            internal Task ShutdownAsync()
            {
                Invalid = true;
                return Channel.ShutdownAsync();
            }
        }

        public static bool ValidateChannel(ChannelEntry channelEntry)
        {
            if (channelEntry.Invalid)
            {
                return false;
            }
            else
            {
                var channel = channelEntry.Channel;
                switch (channel.State)
                {
                    case ChannelState.Shutdown:
                    case ChannelState.TransientFailure:
                        return false;

                    default:
                        return true;
                }
            }
        }

        public Guid Id { get; } = Guid.NewGuid();

        public ChannelCache(ILogger logger, IMemoryCache memoryCache)
        {
            Log = logger;
            Cache = memoryCache;
            Log.LogInformation("New Channel cache {Id}", Id);
        }

        private readonly object _getLock = new object();

        private MemoryCacheEntryOptions CreateOptions() =>
            new MemoryCacheEntryOptions()
            .RegisterPostEvictionCallback(HandleEvicted)
            .SetPriority(CacheItemPriority.NeverRemove);

        private void HandleEvicted(object key, object value, EvictionReason reason, object _)
        {
            Task.Factory.StartNew(() => HandleEvicted((ChannelEntry)value, reason));
        }

        private async Task HandleEvicted(ChannelEntry value, EvictionReason reason)
        {
            var target = value.Channel.Target;
            Log.LogInformation("Evicted {Address} due to {Reason}", target, reason);
            try
            {
                await PerformEvictionActions(value).ConfigureAwait(false);
                Log.LogInformation("Evicted ok for {Address} due to {Reason}",
                    target, reason);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Error evicting {Address} due to {Reason}: {Exception}",
                    target, reason, ex.Message);
            }
        }

        public ChannelEntry GetOrCreate(string endpoint)
        {
            var attempt = 0;
            while (attempt++ < 2)
            {
                lock (_getLock)
                {
                    var entry = Cache.GetOrCreate(endpoint, cacheEntry =>
                    {
                        Log.LogDebug("{CacheId}: Creating a new channel for {Endpoint}", Id, endpoint);
                        cacheEntry.SetOptions(CreateOptions());
                        var channelAddress = endpoint.Replace("http://", string.Empty);
                        var channel = new Channel(channelAddress, ChannelCredentials.Insecure);
                        var newEntry = new ChannelEntry(channel);
                        Log.LogDebug("{CacheId}: Created new channel for {Endpoint} as {Id}", Id, endpoint, newEntry.Id);
                        return newEntry;
                    });
                    if (entry.Invalid)
                    {
                        Cache.Remove(endpoint);
                    }
                    else
                    {
                        return entry;
                    }
                }
            }
            throw new FabricCannotConnectException($"Failed to connect to {endpoint}");
        }

        private Task PerformEvictionActions(ChannelEntry value)
        {
            Log.LogDebug("Shutting down channel {Id} for {Address}, {State}",
                value.Id, value.Channel.Target, value.Channel.State);
            return value.ShutdownAsync();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
