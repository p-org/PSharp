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

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.StaticAnalysis;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# static analysis process.
    /// </summary>
    internal sealed class StaticAnalysisProcess
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# static analysis process.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>StaticAnalysisProcess</returns>
        public static StaticAnalysisProcess Create(CompilationContext context)
        {
            return new StaticAnalysisProcess(context);
        }

        /// <summary>
        /// Starts the P# static analysis process.
        /// </summary>
        public void Start()
        {
            if (!this.CompilationContext.Configuration.RunStaticAnalysis)
            {
                return;
            }

            foreach (var project in this.CompilationContext.GetSolution().Projects)
            {
                IO.PrintLine(". Analyzing " + project.Name);
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
        /// <param name="context">CompilationContext</param>
        private StaticAnalysisProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        /// <summary>
        /// Analyzes the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        private void AnalyzeProject(Project project)
        {
            // Starts profiling the analysis.
            if (this.CompilationContext.Configuration.ShowRuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }

            // Create a P# static analysis context.
            var context = AnalysisContext.Create(this.CompilationContext.Configuration, project);

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
            if (this.CompilationContext.Configuration.ShowGivesUpInformation)
            {
                methodSummaryAnalysis.PrintGivesUpResults();
            }

            // Creates and runs an analysis that constructs the
            // state transition graph for each machine.
            if (this.CompilationContext.Configuration.DoStateTransitionAnalysis)
            {
                StateTransitionAnalysis.Create(context).Run();
            }

            // Creates and runs an analysis that detects if all methods
            // in each machine respect given up ownerships.
            RespectsOwnershipAnalysis.Create(context).Run();

            // Stops profiling the analysis.
            if (this.CompilationContext.Configuration.ShowRuntimeResults)
            {
                Profiler.StopMeasuringExecutionTime();
            }

            if (this.CompilationContext.Configuration.ShowRuntimeResults ||
                this.CompilationContext.Configuration.ShowDFARuntimeResults ||
                this.CompilationContext.Configuration.ShowROARuntimeResults)
            {
                Profiler.PrintResults();
            }
        }

        #endregion
    }
}
