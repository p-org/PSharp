// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Class implementing a call trace step.
    /// </summary>
    internal class CallTraceStep
    {
        /// <summary>
        /// The method declaration.
        /// </summary>
        internal readonly BaseMethodDeclarationSyntax Method;

        /// <summary>
        /// The invocation expression.
        /// </summary>
        internal readonly ExpressionSyntax Invocation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallTraceStep"/> class.
        /// </summary>
        internal CallTraceStep(BaseMethodDeclarationSyntax method, ExpressionSyntax invocation)
        {
            this.Method = method;
            this.Invocation = invocation;
        }
    }
}
