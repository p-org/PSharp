// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// The P# on exit machine action.
    /// </summary>
    internal sealed class OnExitMachineAction : MachineAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnExitMachineAction"/> class.
        /// </summary>
        internal OnExitMachineAction(MethodDeclarationSyntax methodDecl, MachineState state)
            : base(methodDecl, state)
        {
        }
    }
}
