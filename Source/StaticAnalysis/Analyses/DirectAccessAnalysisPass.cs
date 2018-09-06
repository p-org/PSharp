// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass checks if any P# machine contains fields
    /// or methods that can be publicly accessed.
    /// </summary>
    internal sealed class DirectAccessAnalysisPass : StateMachineAnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new direct access analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        /// <param name="errorReporter">ErrorReporter</param>
        /// <returns>DirectAccessAnalysisPass</returns>
        internal static DirectAccessAnalysisPass Create(AnalysisContext context,
            Configuration configuration, ILogger logger, ErrorReporter errorReporter)
        {
            return new DirectAccessAnalysisPass(context, configuration, logger, errorReporter);
        }

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        internal override void Run(ISet<StateMachine> machines)
        {
            this.CheckFields(machines);
            this.CheckMethods(machines);
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
        private DirectAccessAnalysisPass(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
            : base(context, configuration, logger, errorReporter)
        {

        }

        /// <summary>
        /// Checks the fields of each machine and report warnings if
        /// any field is not private or protected.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        private void CheckFields(ISet<StateMachine> machines)
        {
            foreach (var machine in machines)
            {
                foreach (var field in machine.Declaration.ChildNodes().OfType<FieldDeclarationSyntax>())
                {
                    if (field.Modifiers.Any(SyntaxKind.PublicKeyword))
                    {
                        TraceInfo trace = new TraceInfo();
                        trace.AddErrorTrace(field);
                        base.ErrorReporter.ReportWarning(trace, "Field '{0}' of machine '{1}' is " +
                            "declared as 'public'.", field.Declaration.ToString(), machine.Name);
                    }
                    else if (field.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        TraceInfo trace = new TraceInfo();
                        trace.AddErrorTrace(field);
                        base.ErrorReporter.ReportWarning(trace, "Field '{0}' of machine '{1}' is " +
                            "declared as 'internal'.", field.Declaration.ToString(), machine.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the methods of each machine and report warnings if
        /// any method is directly accessed by anything else than the
        /// P# runtime.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        private void CheckMethods(ISet<StateMachine> machines)
        {
            foreach (var machine in machines)
            {
                foreach (var method in machine.Declaration.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
                    {
                        TraceInfo trace = new TraceInfo();
                        trace.AddErrorTrace(method.Identifier);
                        base.ErrorReporter.ReportWarning(trace, "Method '{0}' of machine '{1}' " +
                            "is declared as 'public'.", method.Identifier.ValueText, machine.Name);
                    }
                    else if (method.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        TraceInfo trace = new TraceInfo();
                        trace.AddErrorTrace(method.Identifier);
                        base.ErrorReporter.ReportWarning(trace, "Method '{0}' of machine '{1}' " +
                            "is declared as 'internal'.", method.Identifier.ValueText, machine.Name);
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
            base.Logger.WriteLine("... Direct access analysis runtime: '" +
                base.Profiler.Results() + "' seconds.");
        }

        #endregion
    }
}
