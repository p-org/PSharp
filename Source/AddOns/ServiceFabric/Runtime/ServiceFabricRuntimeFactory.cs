using System;
using System.Threading;
using Microsoft.ServiceFabric.Data;

namespace Microsoft.PSharp.ServiceFabric
{
    /// <summary>
    /// Runtime factory.
    /// </summary>
    public static class ServiceFabricRuntimeFactory
    {
        internal static ServiceFabricPSharpRuntime Current { get; private set; }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager)
        {
            return Create(stateManager, new SingleProcessMachineManager(), Configuration.Create(), CancellationToken.None);
        }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, IRemoteMachineManager remoteMachineManager)
        {
            return Create(stateManager, remoteMachineManager, Configuration.Create(), CancellationToken.None);
        }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="configuration">P# Configuration</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, Configuration configuration)
        {
            return Create(stateManager, new SingleProcessMachineManager(), configuration, CancellationToken.None);
        }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <param name="configuration">P# Configuration</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, IRemoteMachineManager remoteMachineManager, Configuration configuration)
        {
            return Create(stateManager, remoteMachineManager, configuration, CancellationToken.None);
        }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <param name="configuration">P# Configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, IRemoteMachineManager remoteMachineManager,
            Configuration configuration, CancellationToken cancellationToken)
        {
            Current = new ServiceFabricPSharpRuntime(stateManager, remoteMachineManager, configuration, cancellationToken);
            Current.SetRsmNetworkProvider(new Net.DefaultRsmNetworkProvider(Current));
            return Current;
        }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <param name="configuration">P# Configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="networkProviderFunc">Network provider</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, IRemoteMachineManager remoteMachineManager,
            Configuration configuration, CancellationToken cancellationToken, Func<PSharpRuntime, Net.IRsmNetworkProvider> networkProviderFunc)
        {
            Current = new ServiceFabricPSharpRuntime(stateManager, remoteMachineManager, configuration, cancellationToken);
            Current.SetRsmNetworkProvider(networkProviderFunc(Current));
            return Current;
        }
    }
}
