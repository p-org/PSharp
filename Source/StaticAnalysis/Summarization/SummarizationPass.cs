//-----------------------------------------------------------------------
// <copyright file="SummarizationPass.cs">
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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass computes the summaries for
    /// each machine of a P# program.
    /// </summary>
    internal sealed class SummarizationPass : AnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new summarization pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>SummarizationPass</returns>
        internal static SummarizationPass Create(PSharpAnalysisContext context)
        {
            return new SummarizationPass(context);
        }

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        internal override void Run()
        {
            // Starts profiling the summarization.
            if (this.AnalysisContext.Configuration.ShowDFARuntimeResults &&
                !this.AnalysisContext.Configuration.ShowRuntimeResults &&
                !this.AnalysisContext.Configuration.ShowROARuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }
            
            foreach (var machine in this.AnalysisContext.Machines)
            {
                this.SummarizeStateMachine(machine);
            }

            // Stops profiling the summarization.
            if (this.AnalysisContext.Configuration.ShowDFARuntimeResults &&
                !this.AnalysisContext.Configuration.ShowRuntimeResults &&
                !this.AnalysisContext.Configuration.ShowROARuntimeResults)
            {
                Profiler.StopMeasuringExecutionTime();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private SummarizationPass(PSharpAnalysisContext context)
            : base(context)
        {

        }

        /// <summary>
        /// Analyses all eligible methods of the given state-machine to compute the
        /// method summaries. This process repeats until it reaches a fix-point.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void SummarizeStateMachine(StateMachine machine)
        {
            int fixPoint = 0;
            
            foreach (var method in this.GetMachineMethods(machine))
            {
                if (this.AnalysisContext.Summaries.ContainsKey(method) ||
                    method.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    continue;
                }

                this.SummarizeMethod(method, machine);
                if (!this.AnalysisContext.Summaries.ContainsKey(method))
                {
                    fixPoint++;
                }
            }

            if (fixPoint > 0)
            {
                // If fix-point has not been reached, repeat.
                this.SummarizeStateMachine(machine);
            }
        }

        /// <summary>
        /// Computes the summary for the given method.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        private void SummarizeMethod(MethodDeclarationSyntax method, StateMachine machine)
        {
            foreach (var call in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var model = this.AnalysisContext.Compilation.GetSemanticModel(call.SyntaxTree);
                var callSymbol = model.GetSymbolInfo(call).Symbol;
                if (callSymbol == null)
                {
                    continue;
                }

                var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                    this.AnalysisContext.Solution).Result;
                if (definition == null)
                {
                    continue;
                }

                var callee = CodeAnalysis.CSharp.DataFlowAnalysis.AnalysisContext.GetCalleeOfInvocation(call);
                var calleeMethod = definition.DeclaringSyntaxReferences.First().GetSyntax()
                    as BaseMethodDeclarationSyntax;

                if (machine.Declaration.ChildNodes().OfType<BaseMethodDeclarationSyntax>().
                    Contains(calleeMethod) &&
                    !this.AnalysisContext.Summaries.ContainsKey(calleeMethod) &&
                    !calleeMethod.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    return;
                }
            }
            
            var summary = MethodSummary.Create(this.AnalysisContext, method);
            this.AnalysisContext.CacheSummary(summary);

            if (this.AnalysisContext.Configuration.ShowControlFlowInformation)
            {
                summary.PrintControlFlowGraph();
            }

            if (this.AnalysisContext.Configuration.ShowFullDataFlowInformation)
            {
                summary.PrintDataFlowInformation(true);
            }
            else if (this.AnalysisContext.Configuration.ShowDataFlowInformation)
            {
                summary.PrintDataFlowInformation();
            }
        }

        /// <summary>
        /// Returns all available machine method declarations.
        /// </summary>
        /// <param name="machine">StateMachine</param>
        /// <returns>MethodDeclarationSyntaxs</returns>
        private ISet<MethodDeclarationSyntax> GetMachineMethods(StateMachine machine)
        {
            var methods = new HashSet<MethodDeclarationSyntax>(
                machine.Declaration.ChildNodes().OfType<MethodDeclarationSyntax>());

            //HashSet<StateMachine> baseMachines;
            //if (this.AnalysisContext.MachineInheritanceMap.TryGetValue(machine, out baseMachines))
            //{
            //    foreach (var baseMachine in baseMachines)
            //    {
            //        methods.UnionWith(baseMachine.Declaration.ChildNodes().OfType<MethodDeclarationSyntax>().
            //            Where(method => !method.Modifiers.Any(SyntaxKind.AbstractKeyword)));
            //    }
            //}

            return methods;
        }

        #endregion
    }
}
