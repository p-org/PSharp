using System.Collections.Generic;

using Microsoft.PSharp.DataFlowAnalysis;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass checks if any P# machine contains
    /// states that are declared as generic.
    /// </summary>
    internal sealed class NoGenericStatesAnalysisPass : StateMachineAnalysisPass
    {
        /// <summary>
        /// Creates a new generic machine analysis pass.
        /// </summary>
        internal static NoGenericStatesAnalysisPass Create(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
        {
            return new NoGenericStatesAnalysisPass(context, configuration, logger, errorReporter);
        }

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        internal override void Run(ISet<StateMachine> machines)
        {
            this.CheckStates(machines);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoGenericStatesAnalysisPass"/> class.
        /// </summary>
        private NoGenericStatesAnalysisPass(AnalysisContext context, Configuration configuration, ILogger logger, ErrorReporter errorReporter)
            : base(context, configuration, logger, errorReporter)
        {
        }

        /// <summary>
        /// Checks the states of each machine and report warnings if
        /// any state is declared as generic.
        /// </summary>
        private void CheckStates(ISet<StateMachine> machines)
        {
            foreach (var machine in machines)
            {
                foreach (var state in machine.MachineStates)
                {
                    if (state.Declaration.Arity > 0)
                    {
                        TraceInfo trace = new TraceInfo();
                        trace.AddErrorTrace(state.Declaration.Identifier);
                        this.ErrorReporter.Report(trace, $"State '{state.Name}' was" +
                            $" declared as generic, which is not allowed by P#.");
                    }
                }
            }
        }

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            this.Logger.WriteLine($"... No generic states analysis runtime: '{this.Profiler.Results()}' seconds.");
        }
    }
}
