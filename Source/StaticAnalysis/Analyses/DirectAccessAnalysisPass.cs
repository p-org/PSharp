// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.DataFlowAnalysis;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass checks if any P# machine contains fields
    /// or methods that can be publicly accessed.
    /// </summary>
    internal sealed class DirectAccessAnalysisPass : StateMachineAnalysisPass
    {
        /// <summary>
        /// Creates a new direct access analysis pass.
        /// </summary>
        internal static DirectAccessAnalysisPass Create(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
        {
            return new DirectAccessAnalysisPass(context, configuration, logger, errorReporter);
        }

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        internal override void Run(ISet<StateMachine> machines)
        {
            this.CheckFields(machines);
            this.CheckMethods(machines);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectAccessAnalysisPass"/> class.
        /// </summary>
        private DirectAccessAnalysisPass(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
            : base(context, configuration, logger, errorReporter)
        {
        }

        /// <summary>
        /// Checks the fields of each machine and report warnings if
        /// any field is not private or protected.
        /// </summary>
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
                        this.ErrorReporter.ReportWarning(trace, "Field '{0}' of machine '{1}' is " +
                            "declared as 'public'.", field.Declaration.ToString(), machine.Name);
                    }
                    else if (field.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        TraceInfo trace = new TraceInfo();
                        trace.AddErrorTrace(field);
                        this.ErrorReporter.ReportWarning(trace, "Field '{0}' of machine '{1}' is " +
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
                        this.ErrorReporter.ReportWarning(trace, "Method '{0}' of machine '{1}' " +
                            "is declared as 'public'.", method.Identifier.ValueText, machine.Name);
                    }
                    else if (method.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        TraceInfo trace = new TraceInfo();
                        trace.AddErrorTrace(method.Identifier);
                        this.ErrorReporter.ReportWarning(trace, "Method '{0}' of machine '{1}' " +
                            "is declared as 'internal'.", method.Identifier.ValueText, machine.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            this.Logger.WriteLine($"... Direct access analysis runtime: '{this.Profiler.Results()}' seconds.");
        }
    }
}
