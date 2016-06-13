//-----------------------------------------------------------------------
// <copyright file="PSharpProgram.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Syntax;
using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;
using Microsoft.PSharp.Utilities;

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

            base.UpdateSyntaxTree(text);

            this.RewriteTypes();
            this.RewriteStatements();
            this.RewriteExpressions();

            this.InsertLibraries();

            if (IO.Debugging)
            {
                base.GetProject().CompilationContext.PrintSyntaxTree(base.GetSyntaxTree());
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the P# types to C#.
        /// </summary>
        private void RewriteTypes()
        {
            new MachineTypeRewriter(this).Rewrite();
            new HaltEventRewriter(this).Rewrite();
        }

        /// <summary>
        /// Rewrites the P# statements to C#.
        /// </summary>
        private void RewriteStatements()
        {
            new CreateMachineRewriter(this).Rewrite();
            new CreateRemoteMachineRewriter(this).Rewrite();
            new SendRewriter(this).Rewrite();
            new MonitorRewriter(this).Rewrite();
            new RaiseRewriter(this).Rewrite();
            new GotoStateRewriter(this).Rewrite();
            new PopRewriter(this).Rewrite();
            new AssertRewriter(this).Rewrite();

            var qualifiedMethods = this.GetResolvedRewrittenQualifiedMethods();
            new TypeofRewriter(this).Rewrite(qualifiedMethods);
        }

        /// <summary>
        /// Rewrites the P# expressions to C#.
        /// </summary>
        private void RewriteExpressions()
        {
            new TriggerRewriter(this).Rewrite();
            new CurrentStateRewriter(this).Rewrite();
            new ThisRewriter(this).Rewrite();
            new NondeterministicChoiceRewriter(this).Rewrite();
        }

        /// <summary>
        /// Inserts the P# libraries.
        /// </summary>
        private void InsertLibraries()
        {
            var list = new List<UsingDirectiveSyntax>();
            var psharpLib = base.CreateLibrary("Microsoft.PSharp");
            
            list.Add(psharpLib);
            list.AddRange(base.GetSyntaxTree().GetCompilationUnitRoot().Usings);

            var root = base.GetSyntaxTree().GetCompilationUnitRoot().
                WithUsings(SyntaxFactory.List(list));
            base.UpdateSyntaxTree(root.SyntaxTree.ToString());
        }

        /// <summary>
        /// Resolves and returns the rewritten qualified methods of this program.
        /// </summary>
        /// <returns>QualifiedMethods</returns>
        private HashSet<QualifiedMethod> GetResolvedRewrittenQualifiedMethods()
        {
            var qualifiedMethods = new HashSet<QualifiedMethod>();
            foreach (var ns in NamespaceDeclarations)
            {
                foreach (var machine in ns.MachineDeclarations)
                {
                    var allQualifiedNames = new HashSet<string>();
                    foreach (var state in machine.GetAllStateDeclarations())
                    {
                        allQualifiedNames.Add(state.GetFullyQualifiedName('.'));
                    }

                    foreach (var method in machine.RewrittenMethods)
                    {
                        method.MachineQualifiedStateNames.UnionWith(allQualifiedNames);
                        qualifiedMethods.Add(method);
                    }
                }
            }

            return qualifiedMethods;
        }

        #endregion
    }
}
