#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;
    using System.Fabric;

    public interface IReactiveReliableQueueManager
    {
        IReactiveReliableQueue<T> GetOrCreateAsync<T>(IReliableQueue<T> queue);
    }
}
