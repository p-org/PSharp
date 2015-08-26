//-----------------------------------------------------------------------
// <copyright file="StaticAnalysisProcess.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using Microsoft.PSharp.StaticAnalysis;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# static analysis process.
    /// </summary>
    internal sealed class StaticAnalysisProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private LanguageServicesConfiguration Configuration;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# static analysis process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>StaticAnalysisProcess</returns>
        public static StaticAnalysisProcess Create(LanguageServicesConfiguration configuration)
        {
            return new StaticAnalysisProcess(configuration);
        }

        /// <summary>
        /// Starts the P# static analysis process.
        /// </summary>
        public void Start()
        {
            if (!this.Configuration.RunStaticAnalysis)
            {
                return;
            }

            foreach (var project in ProgramInfo.Solution.Projects)
            {
                Output.PrintLine(". Analyzing " + project.Name);
                this.AnalyzeProject(project);
            }

            // Prints error statistics and profiling results.
            AnalysisErrorReporter.PrintStats();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private StaticAnalysisProcess(LanguageServicesConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Analyzes the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        private void AnalyzeProject(Project project)
        {
            // Starts profiling the analysis.
            if (this.Configuration.ShowRuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }

            // Create a P# static analysis context.
            var context = AnalysisContext.Create(this.Configuration, project);

            // Creates and runs an analysis that performs an initial sanity
            // checking to see if machine code behaves as it should.
            SanityCheckingAnalysis.Create(context).Run();

            // Creates and runs an analysis that finds if a machine exposes
            // any fields or methods to other machines.
            RuntimeOnlyDirectAccessAnalysis.Create(context).Run();

            // Creates and runs an analysis that computes the summary
            // for every method in each machine.
            var methodSummaryAnalysis = MethodSummaryAnalysis.Create(context);
            methodSummaryAnalysis.Run();
            if (this.Configuration.ShowGivesUpInformation)
            {
                methodSummaryAnalysis.PrintGivesUpResults();
            }

            // Creates and runs an analysis that constructs the
            // state transition graph for each machine.
            if (this.Configuration.DoStateTransitionAnalysis)
            {
                StateTransitionAnalysis.Create(context).Run();
            }

            // Creates and runs an analysis that detects if all methods
            // in each machine respect given up ownerships.
            RespectsOwnershipAnalysis.Create(context).Run();

            // Stops profiling the analysis.
            if (this.Configuration.ShowRuntimeResults)
            {
                Profiler.StopMeasuringExecutionTime();
            }

            if (this.Configuration.ShowRuntimeResults ||
                this.Configuration.ShowDFARuntimeResults ||
                this.Configuration.ShowROARuntimeResults)
            {
                Profiler.PrintResults();
            }
        }

        #endregion
    }
}
