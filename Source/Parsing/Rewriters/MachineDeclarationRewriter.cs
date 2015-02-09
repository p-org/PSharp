//-----------------------------------------------------------------------
// <copyright file="MachineDeclarationRewriter.cs">
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

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// Implements a machine declaration rewriter.
    /// </summary>
    internal class MachineDeclarationRewriter : BaseRewriter
    {
        #region fields

        /// <summary>
        /// Machine declaration identifiers to be rewritten.
        /// </summary>
        private HashSet<IdentifierNameSyntax> MachineDeclIds;

        /// <summary>
        /// Names of machines without inheritance.
        /// </summary>
        private HashSet<string> MachineNames;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="root">CompilationUnitSyntax</param>
        internal MachineDeclarationRewriter(CompilationUnitSyntax root)
            : base(root)
        {
            this.MachineDeclIds = new HashSet<IdentifierNameSyntax>();
            this.MachineNames = new HashSet<string>();
        }

        /// <summary>
        /// Performs rewritting.
        /// </summary>
        /// <returns>CompilationUnitSyntax</returns>
        internal CompilationUnitSyntax Run()
        {
            this.ParseRootCompilationUnit();
            this.RewriteMachineDeclarations();
            this.ParseMachineClassDeclarations();

            return base.Result;
        }

        #endregion

        #region private parsing API

        /// <summary>
        /// Parses the root compilation unit.
        /// </summary>
        private void ParseRootCompilationUnit()
        {
            var nodes = base.Root.ChildNodes().ToList();
            for (int idx = 0; idx < nodes.Count - 1; idx++)
            {
                if (nodes[idx] is NamespaceDeclarationSyntax)
                {
                    this.ParseNamespaceDeclarationSyntax(nodes[idx] as NamespaceDeclarationSyntax);
                }
            }
        }

        private void ParseNamespaceDeclarationSyntax(NamespaceDeclarationSyntax node)
        {
            var nodes = node.ChildNodes().ToList();
            for (int idx = 0; idx < nodes.Count - 1; idx++)
            {
                if (nodes[idx] is PropertyDeclarationSyntax)
                {
                    this.ParsePropertyDeclarationSyntax(nodes[idx] as PropertyDeclarationSyntax);
                }
            }
        }

        private void ParsePropertyDeclarationSyntax(PropertyDeclarationSyntax node)
        {
            var nodes = node.ChildNodes().ToList();
            if (nodes.Count == 0 || !(nodes[0] is IdentifierNameSyntax))
            {
                return;
            }

            var id = nodes[0] as IdentifierNameSyntax;
            if (!id.Identifier.ValueText.Equals("machine"))
            {
                return;
            }

            this.MachineDeclIds.Add(id);
            this.MachineNames.Add(node.Identifier.ValueText);
        }

        private void ParseMachineClassDeclarations()
        {
            foreach (var classDecl in base.Result.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (!this.MachineNames.Contains(classDecl.Identifier.ValueText))
                {
                    continue;
                }

                var classIdToken = SyntaxFactory.Identifier(base.CreateWhitespaceTriviaList(),
                    classDecl.Identifier.ValueText, base.CreateWhitespaceTriviaList());
                var newClassDecl = classDecl.WithIdentifier(classIdToken);

                var machineIdToken = SyntaxFactory.Identifier(base.CreateWhitespaceTriviaList(),
                    "Machine", base.CreateEndOfLineTriviaList());
                var machineId = SyntaxFactory.IdentifierName(machineIdToken);
                var nodeOrTokenList = SyntaxFactory.NodeOrTokenList(machineId);
                var seperatedList = SyntaxFactory.SeparatedList<TypeSyntax>(nodeOrTokenList);

                var colonToken = SyntaxFactory.Token(base.CreateWhitespaceTriviaList(1),
                    SyntaxKind.ColonToken, base.CreateWhitespaceTriviaList(1));
                var baseList = SyntaxFactory.BaseList(colonToken, seperatedList);

                newClassDecl = newClassDecl.WithBaseList(baseList);
                Console.WriteLine(newClassDecl.ToFullString());
            }
        }

        #endregion

        #region private rewriting API

        /// <summary>
        /// Rewrites machine declarations.
        /// </summary>
        private void RewriteMachineDeclarations()
        {
            var idToken = SyntaxFactory.Identifier(base.CreateWhitespaceTriviaList(),
                    "class", base.CreateWhitespaceTriviaList(1));
            var id = SyntaxFactory.IdentifierName(idToken);

            base.Result = base.Root.ReplaceNodes(this.MachineDeclIds, (key, val) => id);
            base.Result = SyntaxFactory.ParseCompilationUnit(base.Result.ToFullString());
        }

        #endregion
    }
}
