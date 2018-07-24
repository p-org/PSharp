using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.ServiceFabric.TestingServices
{
    /// <summary>
    /// Runtime factory
    /// </summary>
    internal static class TestingRuntimeFactory
    {
        /// <summary>
        /// Creates a new P# Service Fabric testing runtime.
        /// </summary>
        /// <returns>Runtime</returns>
        [TestRuntimeCreate]
        internal static PSharp.TestingServices.BugFindingRuntime Create(Configuration configuration,
            ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
        {
            return new BugFindingRuntime(configuration, strategy, reporter);
        }
    }
}