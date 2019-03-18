// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# replaying process.
    /// </summary>
    internal sealed class MinimizingProcess
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
        public static MinimizingProcess Create(Configuration configuration)
        {
            return new MinimizingProcess(configuration);
        }

        /// <summary>
        /// Starts the P# replaying process.
        /// </summary>
        public void Start()
        {
            Output.WriteLine(". Reproducing trace in " + this.Configuration.AssemblyToBeAnalyzed);

            // Creates a new P# replay engine to reproduce a bug.
            ITestingEngine engine = TestingEngineFactory.CreateMinimizerEngine(this.Configuration);

            engine.Run();

            this.EmitTraces(engine);
            

            Output.WriteLine(engine.Report());
        }

        private void EmitTraces(ITestingEngine engine)
        {
            
            string file = Path.GetFileNameWithoutExtension(
                (this.Configuration.ScheduleFile.Equals("")) ? this.Configuration.AssemblyToBeAnalyzed : this.Configuration.ScheduleFile
            );
            string dir = Path.GetDirectoryName(this.Configuration.AssemblyToBeAnalyzed) + 
                Path.DirectorySeparatorChar + "MinimizedTraces" + Path.DirectorySeparatorChar +
                    Path.GetFileName(file) + Path.DirectorySeparatorChar; ;

            // If this is a separate (sub-)process, CodeCoverageInstrumentation.OutputDirectory may not have been set up.
            Directory.CreateDirectory(dir);

            file += "_" + this.Configuration.TestingProcessId + "_"+ DateTime.Now.Millisecond;
            Output.WriteLine($"... Emitting task {this.Configuration.TestingProcessId} traces to: " 
                + dir +  file + ".*" );
            engine.TryEmitTraces(dir, file);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private MinimizingProcess(Configuration configuration)
        {
            configuration.EnableColoredConsoleOutput = true;
            configuration.DisableEnvironmentExit = false;
            this.Configuration = configuration;
        }

        #endregion
    }
}
