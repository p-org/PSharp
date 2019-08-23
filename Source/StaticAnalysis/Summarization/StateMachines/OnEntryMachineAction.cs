// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PSharp.DataFlowAnalysis;

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
