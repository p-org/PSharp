//-----------------------------------------------------------------------
// <copyright file="PProgram.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Syntax;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A P program.
    /// </summary>
    public sealed class PProgram : AbstractPSharpProgram
    {
        #region fields

        /// <summary>
        /// List of event declarations.
        /// </summary>
        internal List<EventDeclaration> EventDeclarations;

        /// <summary>
        /// List of machine declarations.
        /// </summary>
        internal List<MachineDeclaration> MachineDeclarations;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="filePath">SyntaxTree</param>
        public PProgram(PSharpProject project, SyntaxTree tree)
            : base(project, tree)
        {
            this.EventDeclarations = new List<EventDeclaration>();
            this.MachineDeclarations = new List<MachineDeclaration>();
        }

        /// <summary>
        /// Rewrites the P program to the C#-IR.
        /// </summary>
        public override void Rewrite()
        {
            var text = "";
            if (base.Project.Configuration.CompileForTesting)
            {
                foreach (var node in this.EventDeclarations)
                {
                    node.Model();
                    text += node.TextUnit.Text;
                }

                foreach (var node in this.MachineDeclarations)
                {
                    node.Model();
                    text += node.TextUnit.Text;
                }
            }
            else
            {
                foreach (var node in this.EventDeclarations)
                {
                    node.Rewrite();
                    text += node.TextUnit.Text;
                }

                foreach (var node in this.MachineDeclarations)
                {
                    node.Rewrite();
                    text += node.TextUnit.Text;
                }
            }

            base.UpdateSyntaxTree(text);

            this.InsertLibraries();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Inserts the P# libraries.
        /// </summary>
        private void InsertLibraries()
        {
            var list = new List<UsingDirectiveSyntax>();

            var systemLib = base.CreateLibrary("System");
            var psharpLib = base.CreateLibrary("Microsoft.PSharp");
            var psharpColletionsLib = base.CreateLibrary("Microsoft.PSharp.Collections");

            list.Add(systemLib);
            list.Add(psharpLib);
            list.Add(psharpColletionsLib);

            list.AddRange(base.SyntaxTree.GetCompilationUnitRoot().Usings);

            var root = base.SyntaxTree.GetCompilationUnitRoot().WithUsings(SyntaxFactory.List(list));

            base.UpdateSyntaxTree(root.SyntaxTree.ToString());
        }

        #endregion
    }
}
