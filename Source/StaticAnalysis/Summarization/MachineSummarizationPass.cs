//-----------------------------------------------------------------------
// <copyright file="MachineSummarizationPass.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass computes the summaries for
    /// each machine of a P# program.
    /// </summary>
    internal sealed class MachineSummarizationPass : AnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new machine summarization pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>MachineSummarizationPass</returns>
        internal static MachineSummarizationPass Create(PSharpAnalysisContext context)
        {
            return new MachineSummarizationPass(context);
        }

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        internal override void Run()
        {
            // Starts profiling the summarization.
            if (this.AnalysisContext.Configuration.TimeStaticAnalysis)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }

            this.SummarizeStateMachines();
            this.ComputeStateMachineInheritanceInformation();

            // Stops profiling the summarization.
            if (this.AnalysisContext.Configuration.TimeStaticAnalysis)
            {
                this.Profiler.StopMeasuringExecutionTime();
                this.PrintProfilingResults();
            }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private MachineSummarizationPass(PSharpAnalysisContext context)
            : base(context)
        {

        }

        #endregion

        #region private methods

        /// <summary>
        /// Summarizes the state-machines in the project.
        /// </summary>
        private void SummarizeStateMachines()
        {
            // Iterate the syntax trees for each project file.
            foreach (var tree in base.AnalysisContext.Compilation.SyntaxTrees)
            {
                // Get the tree's semantic model.
                var model = base.AnalysisContext.Compilation.GetSemanticModel(tree);

                // Get the tree's root node compilation unit.
                var root = (CompilationUnitSyntax)tree.GetRoot();

                // Iterate the class declerations only if they are machines.
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (Querying.IsMachine(base.AnalysisContext.Compilation, classDecl))
                    {
                        base.AnalysisContext.Machines.Add(new StateMachine(classDecl, base.AnalysisContext));
                    }
                }
            }
        }

        /// <summary>
        /// Computes the state-machine inheritance information for all
        /// state-machines in the project.
        /// </summary>
        private void ComputeStateMachineInheritanceInformation()
        {
            foreach (var machine in base.AnalysisContext.Machines)
            {
                machine.ComputeInheritanceInformation();
            }
        }

        #endregion

        #region profiling methods

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            IO.PrintLine("... Data-flow analysis runtime: '" +
                base.Profiler.Results() + "' seconds.");
        }

        #endregion
    }
}
