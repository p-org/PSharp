//-----------------------------------------------------------------------
// <copyright file="MachineStateDeclarationParser.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// The P# machine state declaration parsing visitor.
    /// </summary>
    internal sealed class MachineStateDeclarationParser : BaseStateVisitor
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        internal MachineStateDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog)
            : base(project, errorLog)
        {

        }

        #endregion

        #region protected API
        
        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean value</returns>
        protected override bool IsState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            return Querying.IsMachineState(compilation, classDecl);
        }

        /// <summary>
        /// Returns the type of the state.
        /// </summary>
        /// <returns>Text</returns>
        protected override string GetTypeOfState()
        {
            return "MachineState";
        }

        /// <summary>
        /// Checks for special properties.
        /// </summary>
        /// <param name="state">State</param>
        /// <param name="compilation">Compilation</param>
        protected override void CheckForSpecialProperties(ClassDeclarationSyntax state,
            CodeAnalysis.Compilation compilation)
        {
            this.CheckForLivenessAttribute(state, compilation);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Checks that a state does not have a liveness attribute.
        /// </summary>
        /// <param name="state">State</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForLivenessAttribute(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(state.SyntaxTree);

            var hotAttributes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.Hot")).
                ToList();

            var coldAttributes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.Cold")).
                ToList();

            if (hotAttributes.Count > 0 || coldAttributes.Count > 0)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier, "A machine state cannot declare " +
                    "a liveness attribute."));
            }
        }

        #endregion
    }
}
