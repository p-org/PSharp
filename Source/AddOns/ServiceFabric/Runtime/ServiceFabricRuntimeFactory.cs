using Microsoft.ServiceFabric.Data;

namespace Microsoft.PSharp.ServiceFabric
{
    /// <summary>
    /// Runtime factory
    /// </summary>
    public static class ServiceFabricRuntimeFactory
    {
        /// <summary>
        /// Creates the ServiceFabric runtime for P#
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="config">P# Configuration</param>
        /// <returns></returns>
        public static PSharpRuntime Create(IReliableStateManager stateManager, Configuration config)
        {
            return new ServiceFabricPSharpRuntime(stateManager, config);
        }

    }
}