// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass checks if any P# machine contains
    /// states that are declared as generic.
    /// </summary>
    internal sealed class NoGenericStatesAnalysisPass : StateMachineAnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new generic machine analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        /// <param name="errorReporter">ErrorReporter</param>
        /// <returns>NoGenericStatesAnalysisPass</returns>
        internal static NoGenericStatesAnalysisPass Create(AnalysisContext context,
            Configuration configuration, ILogger logger, ErrorReporter errorReporter)
        {
            return new NoGenericStatesAnalysisPass(context, configuration, logger, errorReporter);
        }

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        internal override void Run(ISet<StateMachine> machines)
        {
            this.CheckStates(machines);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        /// <param name="errorReporter">ErrorReporter</param>
        private NoGenericStatesAnalysisPass(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
            : base(context, configuration, logger, errorReporter)
        {

        }

        /// <summary>
        /// Checks the states of each machine and report warnings if
        /// any state is declared as generic.
        /// </summary>
        /// <param name="machines">StateMachines</param>
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
                        base.ErrorReporter.Report(trace, $"State '{state.Name}' was" +
                            $" declared as generic, which is not allowed by P#.");
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
            base.Logger.WriteLine("... No generic states analysis runtime: '" +
                base.Profiler.Results() + "' seconds.");
        }

        #endregion
    }
}
