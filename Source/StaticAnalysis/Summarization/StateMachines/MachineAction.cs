using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PSharp.DataFlowAnalysis;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// An abstract P# machine action.
    /// </summary>
    internal abstract class MachineAction
    {
        /// <summary>
        /// The parent state.
        /// </summary>
        private readonly MachineState State;

        /// <summary>
        /// Name of the machine action.
        /// </summary>
        internal string Name;

        /// <summary>
        /// Underlying method declaration.
        /// </summary>
        internal MethodDeclarationSyntax MethodDeclaration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineAction"/> class.
        /// </summary>
        internal MachineAction(MethodDeclarationSyntax methodDecl, MachineState state)
        {
            this.State = state;
            this.Name = AnalysisContext.GetFullMethodName(methodDecl);
            this.MethodDeclaration = methodDecl;
        }
    }
}
