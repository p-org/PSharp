// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PSharp.DataFlowAnalysis;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// The P# on event do machine action.
    /// </summary>
    internal sealed class OnEventDoMachineAction : MachineAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnEventDoMachineAction"/> class.
        /// </summary>
        internal OnEventDoMachineAction(MethodDeclarationSyntax methodDecl, MachineState state)
            : base(methodDecl, state)
        {
        }
    }
}
