// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# replaying process.
    /// </summary>
    internal sealed class ReplayingProcess
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// Creates a P# replaying process.
        /// </summary>
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
            Output.WriteLine(engine.GetReport());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayingProcess"/> class.
        /// </summary>
        private ReplayingProcess(Configuration configuration)
        {
            configuration.EnableColoredConsoleOutput = true;
            configuration.DisableEnvironmentExit = false;
            this.Configuration = configuration;
        }
    }
}
