// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// The P# on entry machine action.
    /// </summary>
    internal sealed class OnEntryMachineAction : MachineAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnEntryMachineAction"/> class.
        /// </summary>
        internal OnEntryMachineAction(MethodDeclarationSyntax methodDecl, MachineState state)
            : base(methodDecl, state)
        {
        }
    }
}
