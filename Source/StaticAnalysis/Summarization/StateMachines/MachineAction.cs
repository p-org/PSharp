//-----------------------------------------------------------------------
// <copyright file="MachineAction.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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
