//-----------------------------------------------------------------------
// <copyright file="StateMachine.cs">
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
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# state-machine.
    /// </summary>
    internal sealed class StateMachine
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

        /// <summary>
        /// Name of the state-machine.
        /// </summary>
        internal string Name;

        /// <summary>
        /// The underlying declaration.
        /// </summary>
        internal ClassDeclarationSyntax Declaration;

        /// <summary>
        /// Set of base machines.
        /// </summary>
        internal ISet<StateMachine> BaseMachines;

        /// <summary>
        /// Set of states in the machine.
        /// </summary>
        internal ISet<MachineState> MachineStates;

        /// <summary>
        /// Map of method summaries in the machine.
        /// </summary>
        internal IDictionary<BaseMethodDeclarationSyntax, MethodSummary> MethodSummaries;

        /// <summary>
        /// True if the machine is abstract.
        /// </summary>
        internal bool IsAbstract;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="classDecl">ClassDeclarationSyntax</param>
        /// <param name="context">AnalysisContext</param>
        internal StateMachine(ClassDeclarationSyntax classDecl, AnalysisContext context)
        {
            this.AnalysisContext = context;
            this.Name = this.AnalysisContext.GetFullClassName(classDecl);
            this.Declaration = classDecl;
            this.BaseMachines = new HashSet<StateMachine>();
            this.MachineStates = new HashSet<MachineState>();
            this.MethodSummaries = new Dictionary<BaseMethodDeclarationSyntax, MethodSummary>();

            if (this.Declaration.Modifiers.Any(SyntaxKind.AbstractKeyword))
            {
                this.IsAbstract = true;
            }

            this.FindAllStates();
            this.AnalyzeAllStates();
            this.Summarize();
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Returns all the successor summaries.
        /// </summary>
        /// <param name="summary">MethodSummary</param>
        /// <returns>MethodSummarys</returns>
        internal ISet<MethodSummary> GetSuccessorSummaries(MethodSummary summary)
        {
            var summaries = new HashSet<MethodSummary>();
            var fromStates = new HashSet<MachineState>();

            foreach (var state in this.MachineStates)
            {
                var summaryActions = state.MachineActions.FindAll(action
                    => action.MethodDeclaration.Equals(summary.Method));
                if (summaryActions.Count == 0)
                {
                    continue;
                }

                foreach (var summaryAction in summaryActions)
                {
                    if (!(summaryAction is OnExitMachineAction))
                    {
                        summaries.UnionWith(state.MachineActions.FindAll(action
                            => action is OnEventDoMachineAction || action is OnEventGotoMachineAction ||
                            action is OnEventPushMachineAction || action is OnExitMachineAction).
                            Select(action => action.MethodDeclaration).
                            Select(method => this.MethodSummaries[method]));
                    }
                }

                fromStates.Add(state);
            }

            foreach (var state in fromStates)
            {
                foreach (var successor in state.GetSuccessorStates())
                {
                    summaries.UnionWith(successor.MachineActions.FindAll(action
                        => action is OnEntryMachineAction || action is OnEventDoMachineAction ||
                        action is OnEventGotoMachineAction || action is OnEventPushMachineAction ||
                        action is OnExitMachineAction).
                        Select(action => action.MethodDeclaration).
                        Select(method => this.MethodSummaries[method]));
                }
            }

            return summaries;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Finds all states in the machine.
        /// </summary>
        private void FindAllStates()
        {
            foreach (var classDecl in this.Declaration.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (Querying.IsMachineState(this.AnalysisContext.Compilation, classDecl))
                {
                    this.MachineStates.Add(new MachineState(classDecl, this, this.AnalysisContext));
                }
            }
        }

        /// <summary>
        /// Analyzes all states in the machine.
        /// </summary>
        private void AnalyzeAllStates()
        {
            foreach (var state in this.MachineStates)
            {
                state.Analyze();
            }
        }

        /// <summary>
        /// Summarizes the state-machine.
        /// </summary>
        private void Summarize()
        {
            foreach (var method in this.GetMethodDeclarations())
            {
                if (method.Body == null ||
                    this.MethodSummaries.ContainsKey(method))
                {
                    continue;
                }

                this.SummarizeMethod(method);
            }
        }

        /// <summary>
        /// Computes the summary for the specified method.
        /// </summary>
        /// <param name="method">Method</param>
        private void SummarizeMethod(MethodDeclarationSyntax method)
        {
            var summary = MethodSummary.Create(this.AnalysisContext, method);
            this.MethodSummaries.Add(method, summary);
        }

        /// <summary>
        /// Returns all available method declarations.
        /// </summary>
        /// <returns>MethodDeclarationSyntaxs</returns>
        private ISet<MethodDeclarationSyntax> GetMethodDeclarations()
        {
            var methods = new HashSet<MethodDeclarationSyntax>(
                this.Declaration.ChildNodes().OfType<MethodDeclarationSyntax>());

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
