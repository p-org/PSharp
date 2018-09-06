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
        #region fields

        /// <summary>
        /// The method declaration.
        /// </summary>
        internal readonly BaseMethodDeclarationSyntax Method;

        /// <summary>
        /// The invocation expression.
        /// </summary>
        internal readonly ExpressionSyntax Invocation;

        #endregion

        #region methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="invocation">Invocation</param>
        internal CallTraceStep(BaseMethodDeclarationSyntax method, ExpressionSyntax invocation)
        {
            this.Method = method;
            this.Invocation = invocation;
        }

        #endregion
    }
}
