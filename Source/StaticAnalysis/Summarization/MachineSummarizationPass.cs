// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.DataFlowAnalysis;
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
        /// <summary>
        /// Creates a new machine summarization pass.
        /// </summary>
        internal static MachineSummarizationPass Create(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
        {
            return new MachineSummarizationPass(context, configuration, logger, errorReporter);
        }

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        internal override void Run(ISet<StateMachine> machines)
        {
            // Starts profiling the summarization.
            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }

            this.SummarizeStateMachines(machines);
            this.ComputeStateMachineInheritanceInformation(machines);

            // Stops profiling the summarization.
            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StopMeasuringExecutionTime();
                this.PrintProfilingResults();
            }

            this.PrintSummarizationInformation(machines);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineSummarizationPass"/> class.
        /// </summary>
        private MachineSummarizationPass(AnalysisContext context, Configuration configuration, ILogger logger, ErrorReporter errorReporter)
            : base(context, configuration, logger, errorReporter)
        {
        }

        /// <summary>
        /// Summarizes the state-machines in the project.
        /// </summary>
        private void SummarizeStateMachines(ISet<StateMachine> machines)
        {
            // Iterate the syntax trees for each project file.
            foreach (var tree in this.AnalysisContext.Compilation.SyntaxTrees)
            {
                // Get the tree's semantic model.
                var model = this.AnalysisContext.Compilation.GetSemanticModel(tree);

                // Get the tree's root node compilation unit.
                var root = (CompilationUnitSyntax)tree.GetRoot();

                // Iterate the class declerations only if they are machines.
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (Querying.IsMachine(this.AnalysisContext.Compilation, classDecl))
                    {
                        StateMachine stateMachine = new StateMachine(classDecl, this.AnalysisContext);
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
                    var inheritedMachine = availableMachines.FirstOrDefault(
                        m => AnalysisContext.GetFullClassName(m.Declaration).Equals(type.ToString()));
                    if (inheritedMachine is null)
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
        private void PrintSummarizationInformation(ISet<StateMachine> machines)
        {
            foreach (var machine in machines)
            {
                foreach (var summary in machine.MethodSummaries.Values)
                {
                    if (this.Configuration.ShowControlFlowInformation)
                    {
                        summary.PrintControlFlowGraph();
                    }

                    if (this.Configuration.ShowFullDataFlowInformation)
                    {
                        summary.PrintDataFlowInformation(true);
                    }
                    else if (this.Configuration.ShowDataFlowInformation)
                    {
                        summary.PrintDataFlowInformation();
                    }
                }
            }
        }

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            this.Logger.WriteLine($"... Data-flow analysis runtime: '{this.Profiler.Results()}' seconds.");
        }
    }
}
