// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Static trace information.
    /// </summary>
    internal class TraceInfo
    {
        /// <summary>
        /// Error trace.
        /// </summary>
        internal List<ErrorTraceStep> ErrorTrace;

        /// <summary>
        /// Call trace.
        /// </summary>
        internal List<CallTraceStep> CallTrace;

        /// <summary>
        /// The method where the trace begins.
        /// </summary>
        internal string Method;

        /// <summary>
        /// The machine where the trace begins.
        /// </summary>
        internal string Machine;

        /// <summary>
        /// The state where the trace begins.
        /// </summary>
        internal string State;

        /// <summary>
        /// The corresponding payload of the trace.
        /// </summary>
        internal string Payload;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceInfo"/> class.
        /// Constructor.
        /// </summary>
        internal TraceInfo()
        {
            this.ErrorTrace = new List<ErrorTraceStep>();
            this.CallTrace = new List<CallTraceStep>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceInfo"/> class.
        /// </summary>
        internal TraceInfo(BaseMethodDeclarationSyntax method, StateMachine machine,
            MachineState state, ISymbol payload)
        {
            this.ErrorTrace = new List<ErrorTraceStep>();
            this.CallTrace = new List<CallTraceStep>();

            if (method == null)
            {
                this.Method = null;
            }
            else if (method is MethodDeclarationSyntax)
            {
                this.Method = (method as MethodDeclarationSyntax).Identifier.ValueText;
            }
            else if (method is ConstructorDeclarationSyntax)
            {
                this.Method = (method as ConstructorDeclarationSyntax).Identifier.ValueText;
            }

            if (machine == null)
            {
                this.Machine = null;
            }
            else
            {
                this.Machine = machine.Name;
            }

            if (state == null)
            {
                this.State = null;
            }
            else
            {
                this.State = state.Name;
            }

            if (payload == null)
            {
                this.Payload = null;
            }
            else
            {
                this.Payload = payload.Name;
            }
        }

        /// <summary>
        /// Adds new error trace to the trace for
        /// the specified syntax node.
        /// </summary>
        internal void AddErrorTrace(SyntaxNode syntaxNode)
        {
            var errorTraceStep = new ErrorTraceStep(syntaxNode.ToString(), syntaxNode.SyntaxTree.FilePath,
                syntaxNode.SyntaxTree.GetLineSpan(syntaxNode.Span).StartLinePosition.Line + 1);
            this.ErrorTrace.Add(errorTraceStep);
        }

        /// <summary>
        /// Adds new error trace to the trace for
        /// the specified syntax token.
        /// </summary>
        internal void AddErrorTrace(SyntaxToken syntaxToken)
        {
            var errorTraceStep = new ErrorTraceStep(syntaxToken.ToString(), syntaxToken.SyntaxTree.FilePath,
                syntaxToken.SyntaxTree.GetLineSpan(syntaxToken.Span).StartLinePosition.Line + 1);
            this.ErrorTrace.Add(errorTraceStep);
        }

        /// <summary>
        /// Inserts a new call to the trace.
        /// </summary>
        internal void InsertCall(BaseMethodDeclarationSyntax method, ExpressionSyntax call)
        {
            if (call is InvocationExpressionSyntax ||
                call is ObjectCreationExpressionSyntax)
            {
                this.CallTrace.Insert(0, new CallTraceStep(method, call));
            }
        }

        /// <summary>
        /// Merges the given trace to the current trace.
        /// </summary>
        internal void Merge(TraceInfo traceInfo)
        {
            this.ErrorTrace.AddRange(traceInfo.ErrorTrace);
            this.CallTrace.AddRange(traceInfo.CallTrace);
            this.Method = traceInfo.Method;
            this.Machine = traceInfo.Machine;
            this.State = traceInfo.State;
            this.Payload = traceInfo.Payload;
        }
    }
}
