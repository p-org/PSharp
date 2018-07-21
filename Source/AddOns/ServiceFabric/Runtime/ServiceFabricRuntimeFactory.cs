using Microsoft.ServiceFabric.Data;

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
            Current = new ServiceFabricPSharpRuntime(stateManager,new SingleProcessMachineManager(stateManager), config);
            return Current;
        }

    }
}