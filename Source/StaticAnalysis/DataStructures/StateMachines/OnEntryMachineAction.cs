//-----------------------------------------------------------------------
// <copyright file="OnEntryMachineAction.cs">
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

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// The P# on entry machine action.
    /// </summary>
    internal sealed class OnEntryMachineAction : MachineAction
    {
        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="methodDecl">MethodDeclarationSyntax</param>
        /// <param name="state">MachineState</param>
        /// <param name="context">AnalysisContext</param>
        internal OnEntryMachineAction(MethodDeclarationSyntax methodDecl, MachineState state,
            PSharpAnalysisContext context)
            : base(methodDecl, state, context)
        {

        }

        #endregion
    }
}
