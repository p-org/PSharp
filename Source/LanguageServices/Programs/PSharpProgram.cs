//-----------------------------------------------------------------------
// <copyright file="PSharpProgram.cs">
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

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Syntax;
using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A P# program.
    /// </summary>
    public sealed class PSharpProgram : AbstractPSharpProgram
    {
        #region fields
        
        /// <summary>
        /// List of using declarations.
        /// </summary>
        internal List<UsingDeclaration> UsingDeclarations;

        /// <summary>
        /// List of namespace declarations.
        /// </summary>
        internal List<NamespaceDeclaration> NamespaceDeclarations;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        public PSharpProgram(PSharpProject project, SyntaxTree tree)
            : base(project, tree)
        {
            this.UsingDeclarations = new List<UsingDeclaration>();
            this.NamespaceDeclarations = new List<NamespaceDeclaration>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        public override void Rewrite()
        {
            var text = "";
            if (base.Project.CompilationContext.ActiveCompilationTarget == CompilationTarget.Testing)
            {
                foreach (var node in this.UsingDeclarations)
                {
                    node.Model();
                    text += node.TextUnit.Text;
                }

                foreach (var node in this.NamespaceDeclarations)
                {
                    node.Model();
                    text += node.TextUnit.Text;
                }
            }
            else
            {
                foreach (var node in this.UsingDeclarations)
                {
                    node.Rewrite();
                    text += node.TextUnit.Text;
                }

                foreach (var node in this.NamespaceDeclarations)
                {
                    node.Rewrite();
                    text += node.TextUnit.Text;
                }
            }

            base.UpdateSyntaxTree(text);

            this.RewriteTypes();
            this.RewriteStatements();
            this.RewriteExpressions();

            this.InsertLibraries();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the P# types to C#.
        /// </summary>
        private void RewriteTypes()
        {
            this.SyntaxTree = new MachineTypeRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new HaltEventRewriter(this.Project).Rewrite(this.SyntaxTree);
        }

        /// <summary>
        /// Rewrites the P# statements to C#.
        /// </summary>
        private void RewriteStatements()
        {
            this.SyntaxTree = new CreateMachineRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new CreateRemoteMachineRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new SendRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new MonitorRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new RaiseRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new PopRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new AssertRewriter(this.Project).Rewrite(this.SyntaxTree);
        }

        /// <summary>
        /// Rewrites the P# expressions to C#.
        /// </summary>
        private void RewriteExpressions()
        {
            this.SyntaxTree = new TriggerRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new FieldAccessRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new ThisRewriter(this.Project).Rewrite(this.SyntaxTree);
            this.SyntaxTree = new NondeterministicChoiceRewriter(this.Project).Rewrite(this.SyntaxTree);
        }

        /// <summary>
        /// Inserts the P# libraries.
        /// </summary>
        private void InsertLibraries()
        {
            var list = new List<UsingDirectiveSyntax>();

            var systemLib = base.CreateLibrary("System");
            var psharpLib = base.CreateLibrary("Microsoft.PSharp");

            list.Add(systemLib);
            list.Add(psharpLib);

            list.AddRange(base.SyntaxTree.GetCompilationUnitRoot().Usings);

            var root = base.SyntaxTree.GetCompilationUnitRoot().WithUsings(SyntaxFactory.List(list));

            this.UpdateSyntaxTree(root.SyntaxTree.ToString());
        }

        #endregion
    }
}
