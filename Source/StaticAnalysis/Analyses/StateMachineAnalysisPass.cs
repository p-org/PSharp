// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.PSharp.DataFlowAnalysis;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Implementation of an abstract state-machine analysis pass.
    /// </summary>
    internal abstract class StateMachineAnalysisPass
    {
        /// <summary>
        /// The analysis context.
        /// </summary>
        protected AnalysisContext AnalysisContext;

        /// <summary>
        /// Configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// The installed logger.
        /// </summary>
        protected ILogger Logger;

        /// <summary>
        /// The error reporter.
        /// </summary>
        protected ErrorReporter ErrorReporter;

        /// <summary>
        /// The analysis pass profiler.
        /// </summary>
        protected Profiler Profiler;

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        internal abstract void Run(ISet<StateMachine> machines);

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineAnalysisPass"/> class.
        /// </summary>
        protected StateMachineAnalysisPass(AnalysisContext context, Configuration configuration, ILogger logger, ErrorReporter errorReporter)
        {
            this.Logger = logger;
            this.Profiler = new Profiler();
            this.AnalysisContext = context;
            this.Configuration = configuration;
            this.ErrorReporter = errorReporter;
        }

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected abstract void PrintProfilingResults();
    }
}
