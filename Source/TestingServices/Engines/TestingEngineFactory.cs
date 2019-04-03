// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
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
        /// <param name="configuration">Configuration</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration)
        {
            return BugFindingEngine.Create(configuration);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateBugFindingEngine(
            Configuration configuration, Assembly assembly)
        {
            return BugFindingEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateBugFindingEngine(
            Configuration configuration, Action<PSharpRuntime> action)
        {
            return BugFindingEngine.Create(configuration, action);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateReplayEngine(Configuration configuration)
        {
            return ReplayEngine.Create(configuration);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Assembly assembly)
        {
            return ReplayEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Action<PSharpRuntime> action)
        {
            return ReplayEngine.Create(configuration, action);
        }
    }
}
