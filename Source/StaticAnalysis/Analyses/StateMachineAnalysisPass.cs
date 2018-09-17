// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Implementation of an abstract state-machine analysis pass.
    /// </summary>
    internal abstract class StateMachineAnalysisPass
    {
        #region fields

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

        #endregion

        #region internal methods

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        internal abstract void Run(ISet<StateMachine> machines);

        #endregion

        #region protected methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        /// <param name="errorReporter">ErrorReporter</param>
        protected StateMachineAnalysisPass(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
        {
            this.Logger = logger;
            this.Profiler = new Profiler();
            this.AnalysisContext = context;
            this.Configuration = configuration;
            this.ErrorReporter = errorReporter;
        }

        #endregion

        #region profiling

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected abstract void PrintProfilingResults();

        #endregion
    }
}
