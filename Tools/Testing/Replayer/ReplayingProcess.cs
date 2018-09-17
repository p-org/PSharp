// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# replaying process.
    /// </summary>
    internal sealed class ReplayingProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        #endregion

        #region public methods

        /// <summary>
        /// Creates a P# replaying process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>ReplayingProcess</returns>
        public static ReplayingProcess Create(Configuration configuration)
        {
            return new ReplayingProcess(configuration);
        }

        /// <summary>
        /// Starts the P# replaying process.
        /// </summary>
        public void Start()
        {
            Output.WriteLine(". Reproducing trace in " + this.Configuration.AssemblyToBeAnalyzed);

            // Creates a new P# replay engine to reproduce a bug.
            ITestingEngine engine = TestingEngineFactory.CreateReplayEngine(this.Configuration);

            engine.Run();
            Output.WriteLine(engine.Report());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private ReplayingProcess(Configuration configuration)
        {
            configuration.EnableColoredConsoleOutput = true;
            configuration.DisableEnvironmentExit = false;
            this.Configuration = configuration;
        }

        #endregion
    }
}
