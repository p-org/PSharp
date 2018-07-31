#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IReactiveReliableQueue<T>
    {
        Task EnqueueAsync(ITransaction tx, T item, CancellationToken cancellationToken);

        Task EnqueueAsync(ITransaction tx, T item, TimeSpan timeout, CancellationToken cancellationToken);

        Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx, CancellationToken cancellationToken);

        Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx, TimeSpan timeout, CancellationToken cancellationToken);

        Task<long> GetCountAsync(ITransaction tx);
    }
}
