﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass computes the summaries for
    /// each machine of a P# program.
    /// </summary>
    internal sealed class MachineSummarizationPass : StateMachineAnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new machine summarization pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        /// <param name="errorReporter">ErrorReporter</param>
        /// <returns>MachineSummarizationPass</returns>
        internal static MachineSummarizationPass Create(AnalysisContext context,
            Configuration configuration, ILogger logger, ErrorReporter errorReporter)
        {
            return new MachineSummarizationPass(context, configuration, logger, errorReporter);
        }

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        internal override void Run(ISet<StateMachine> machines)
        {
            // Starts profiling the summarization.
            if (base.Configuration.EnableProfiling)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }

            this.SummarizeStateMachines(machines);
            this.ComputeStateMachineInheritanceInformation(machines);

            // Stops profiling the summarization.
            if (base.Configuration.EnableProfiling)
            {
                this.Profiler.StopMeasuringExecutionTime();
                this.PrintProfilingResults();
            }

            this.PrintSummarizationInformation(machines);
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        /// <param name="errorReporter">ErrorReporter</param>
        private MachineSummarizationPass(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
            : base(context, configuration, logger, errorReporter)
        {

        }

        #endregion

        #region private methods

        /// <summary>
        /// Summarizes the state-machines in the project.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        private void SummarizeStateMachines(ISet<StateMachine> machines)
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
                        StateMachine stateMachine = new StateMachine(classDecl, base.AnalysisContext);
                        if (this.Configuration.AnalyzeDataFlow)
                        {
                            stateMachine.Summarize();
                        }

                        machines.Add(stateMachine);
                    }
                }
            }
        }

        /// <summary>
        /// Computes the state-machine inheritance information for all
        /// state-machines in the project.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        private void ComputeStateMachineInheritanceInformation(ISet<StateMachine> machines)
        {
            foreach (var machine in machines)
            {
                IList<INamedTypeSymbol> baseTypes = this.AnalysisContext.GetBaseTypes(machine.Declaration);
                foreach (var type in baseTypes)
                {
                    if (type.ToString().Equals(typeof(Machine).FullName))
                    {
                        break;
                    }

                    var availableMachines = new List<StateMachine>(machines);
                    var inheritedMachine = availableMachines.FirstOrDefault(m
                        => base.AnalysisContext.GetFullClassName(m.Declaration).Equals(type.ToString()));
                    if (inheritedMachine == null)
                    {
                        break;
                    }

                    machine.BaseMachines.Add(inheritedMachine);
                }
            }
        }

        /// <summary>
        /// Prints summarization information.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        private void PrintSummarizationInformation(ISet<StateMachine> machines)
        {
            foreach (var machine in machines)
            {
                foreach (var summary in machine.MethodSummaries.Values)
                {
                    if (base.Configuration.ShowControlFlowInformation)
                    {
                        summary.PrintControlFlowGraph();
                    }

                    if (base.Configuration.ShowFullDataFlowInformation)
                    {
                        summary.PrintDataFlowInformation(true);
                    }
                    else if (base.Configuration.ShowDataFlowInformation)
                    {
                        summary.PrintDataFlowInformation();
                    }
                }
            }
        }

        #endregion

        #region profiling methods

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            base.Logger.WriteLine("... Data-flow analysis runtime: '" +
                base.Profiler.Results() + "' seconds.");
        }

        #endregion
    }
}
