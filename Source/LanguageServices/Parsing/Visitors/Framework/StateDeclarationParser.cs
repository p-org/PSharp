//-----------------------------------------------------------------------
// <copyright file="StateDeclarationParser.cs">
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

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// The P# state declaration parsing visitor.
    /// </summary>
    internal sealed class StateDeclarationParser : BaseVisitor
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        internal StateDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog)
            : base(project, errorLog)
        {

        }

        /// <summary>
        /// Parses the syntax tree for errors.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        internal void Parse(SyntaxTree tree)
        {
            var compilation = base.Project.Project.GetCompilationAsync().Result;

            var states = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => Querying.IsMachineState(compilation, val)).
                ToList();

            foreach (var state in states)
            {
                this.CheckForFields(state);
                this.CheckForMethods(state);
            }
        }

        #endregion

        #region private API

        /// <summary>
        /// Checks that the state is not declaring fields.
        /// </summary>
        /// <param name="state">State</param>
        private void CheckForFields(ClassDeclarationSyntax state)
        {
            var fields = state.DescendantNodes().OfType<FieldDeclarationSyntax>().
                ToList();

            if (fields.Count > 0)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier,
                    "A state cannot declare fields."));
            }
        }

        /// <summary>
        /// Checks that the state is not declaring methods (beside the P# API ones).
        /// </summary>
        /// <param name="state">State</param>
        private void CheckForMethods(ClassDeclarationSyntax state)
        {
            var methods = state.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => !val.Modifiers.Any(SyntaxKind.OverrideKeyword)).
                ToList();

            if (methods.Count > 0)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier,
                    "A state cannot declare methods (besides the P# API ones)."));
            }
        }

        #endregion
    }
}
