// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The runtime for creating and executing machines.
    /// </summary>
    public static class PSharpRuntime
    {
        /// <summary>
        /// Creates a new runtime.
        /// </summary>
        /// <returns>The created runtime.</returns>
        public static IMachineRuntime Create()
        {
            return new ProductionRuntime(Configuration.Create());
        }

        /// <summary>
        /// Creates a new runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The created runtime.</returns>
        public static IMachineRuntime Create(Configuration configuration)
        {
            return new ProductionRuntime(configuration ?? Configuration.Create());
        }
    }
}
