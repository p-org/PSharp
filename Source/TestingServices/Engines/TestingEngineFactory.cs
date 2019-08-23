// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# testing engine factory.
    /// </summary>
    public static class TestingEngineFactory
    {
        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration)
        {
            return BugFindingEngine.Create(configuration);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(
            Configuration configuration, Assembly assembly)
        {
            return BugFindingEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(
            Configuration configuration, Action<IMachineRuntime> action)
        {
            return BugFindingEngine.Create(configuration, action);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration)
        {
            return ReplayEngine.Create(configuration);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Assembly assembly)
        {
            return ReplayEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Action<IMachineRuntime> action)
        {
            return ReplayEngine.Create(configuration, action);
        }
    }
}
