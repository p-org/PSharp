namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;

    public interface IReactiveReliableQueueManager
    {
        IReactiveReliableQueue<T> GetOrCreateAsync<T>(IReliableQueue<T> queue);
    }
}
