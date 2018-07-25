using Microsoft.ServiceFabric.Data;
using System;
using System.Threading;

namespace Microsoft.PSharp.ServiceFabric
{
    /// <summary>
    /// Runtime factory
    /// </summary>
    public static class ServiceFabricRuntimeFactory
    {
        internal static ServiceFabricPSharpRuntime Current { get; private set; }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="config">P# Configuration</param>
        /// <returns></returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, Configuration config)
        {
            return Create(stateManager, config, new SingleProcessMachineManager());
        }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="config">P# Configuration</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <returns></returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, Configuration config,
            IRemoteMachineManager remoteMachineManager)
        {
            Current = new ServiceFabricPSharpRuntime(stateManager, remoteMachineManager, config);
            Current.SetRsmNetworkProvider(new Net.DefaultRsmNetworkProvider(Current));
            return Current;
        }

        /// <summary>
        /// Creates the ServiceFabric runtime for P#
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="config">P# Configuration</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <param name="networkProviderFunc">Network provider</param>
        /// <returns></returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, CancellationToken cancellationToken, Configuration config,
            IRemoteMachineManager remoteMachineManager, Func<PSharpRuntime, Net.IRsmNetworkProvider> networkProviderFunc)
        {
            Current = new ServiceFabricPSharpRuntime(stateManager, cancellationToken, remoteMachineManager, config);
            Current.SetRsmNetworkProvider(networkProviderFunc(Current));
            return Current;
        }

    }
}