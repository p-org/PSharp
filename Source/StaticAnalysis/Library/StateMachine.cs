//-----------------------------------------------------------------------
// <copyright file="MachineDStateMachineeclaration.cs">
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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# state-machine.
    /// </summary>
    internal sealed class StateMachine
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private PSharpAnalysisContext AnalysisContext;

        /// <summary>
        /// Name of the state-machine.
        /// </summary>
        internal string Name;

        /// <summary>
        /// The underlying declaration.
        /// </summary>
        internal ClassDeclarationSyntax Declaration;

        /// <summary>
        /// List of states in the machine.
        /// </summary>
        internal List<MachineState> MachineStates;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="classDecl">ClassDeclarationSyntax</param>
        /// <param name="context">AnalysisContext</param>
        internal StateMachine(ClassDeclarationSyntax classDecl, PSharpAnalysisContext context)
        {
            this.AnalysisContext = context;
            this.Name = this.AnalysisContext.GetFullClassName(classDecl);
            this.Declaration = classDecl;
            this.MachineStates = new List<MachineState>();
            this.FindAllStates();
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Returns true if the given method is an action of the machine.
        /// </summary>
        /// <param name="method">MethodDeclarationSyntax</param>
        /// <returns>Boolean</returns>
        internal bool ContainsMachineAction(MethodDeclarationSyntax method)
        {
            foreach (var state in this.MachineStates)
            {
                if (state.MachineActions.Any(action => action.MethodDeclaration.Equals(method)))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Finds all states in them machine.
        /// </summary>
        private void FindAllStates()
        {
            foreach (var classDecl in this.Declaration.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (Querying.IsMachineState(this.AnalysisContext.Compilation, classDecl))
                {
                    this.MachineStates.Add(new MachineState(classDecl, this, this.AnalysisContext));
                }
            }
        }

        #endregion
    }
}
