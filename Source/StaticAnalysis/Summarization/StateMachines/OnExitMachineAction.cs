// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PSharp.DataFlowAnalysis;

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
