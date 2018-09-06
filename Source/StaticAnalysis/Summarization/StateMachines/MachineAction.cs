// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// An abstract P# machine action.
    /// </summary>
    internal abstract class MachineAction
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

        /// <summary>
        /// The parent state.
        /// </summary>
        private MachineState State;

        /// <summary>
        /// Name of the machine action.
        /// </summary>
        internal string Name;

        /// <summary>
        /// Underlying method declaration.
        /// </summary>
        internal MethodDeclarationSyntax MethodDeclaration;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="methodDecl">MethodDeclarationSyntax</param>
        /// <param name="state">MachineState</param>
        /// <param name="context">AnalysisContext</param>
        internal MachineAction(MethodDeclarationSyntax methodDecl, MachineState state,
            AnalysisContext context)
        {
            this.AnalysisContext = context;
            this.State = state;
            this.Name = this.AnalysisContext.GetFullMethodName(methodDecl);
            this.MethodDeclaration = methodDecl;
        }

        #endregion
    }
}
