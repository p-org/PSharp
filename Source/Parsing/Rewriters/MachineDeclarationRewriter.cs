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

        /// <summary>
        /// Dictionary from machine to class declaration.
        /// </summary>
        private Dictionary<ClassDeclarationSyntax, ClassDeclarationSyntax> MachineDeclMap;

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
            this.MachineDeclMap = new Dictionary<ClassDeclarationSyntax, ClassDeclarationSyntax>();
        }

        /// <summary>
        /// Performs rewritting.
        /// </summary>
        /// <returns>CompilationUnitSyntax</returns>
        internal CompilationUnitSyntax Run()
        {
            this.ParseMachineDeclarations();
            this.RewriteMachineDeclarations();

            this.ParseMachineClassDeclarations();
            this.RewriteMachineClassDeclarations();

            return base.Result;
        }

        #endregion

        #region private parsing API

        /// <summary>
        /// Parses the root compilation unit for machine declarations.
        /// </summary>
        private void ParseMachineDeclarations()
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

        /// <summary>
        /// Parse a NamespaceDeclarationSyntax node.
        /// </summary>
        /// <param name="node">NamespaceDeclarationSyntax</param>
        private void ParseNamespaceDeclarationSyntax(NamespaceDeclarationSyntax node)
        {
            var nodes = node.ChildNodes().ToList();
            for (int idx = 0; idx < nodes.Count - 1; idx++)
            {
                if (nodes[idx] is PropertyDeclarationSyntax)
                {
                    this.ParsePropertyDeclarationSyntax(nodes[idx] as PropertyDeclarationSyntax);
                }
                else if (nodes[idx] is FieldDeclarationSyntax)
                {
                    this.ParseFieldDeclarationSyntax(nodes[idx] as FieldDeclarationSyntax);
                }
            }
        }

        /// <summary>
        /// Parse a PropertyDeclarationSyntax node.
        /// </summary>
        /// <param name="node">PropertyDeclarationSyntax</param>
        private void ParsePropertyDeclarationSyntax(PropertyDeclarationSyntax node)
        {
            var nodes = node.ChildNodes().ToList();

            var index = -1;
            for (int idx = 0; idx < nodes.Count - 1; idx++)
            {
                if (nodes[idx] is IdentifierNameSyntax)
                {
                    index = idx;
                    break;
                }
            }

            if (index < 0)
            {
                return;
            }

            var id = nodes[index] as IdentifierNameSyntax;
            if (!id.Identifier.ValueText.Equals("machine"))
            {
                return;
            }

            this.MachineDeclIds.Add(id);
            this.MachineNames.Add(node.Identifier.ValueText);
        }

        /// <summary>
        /// Parse a FieldDeclarationSyntax node. 
        /// </summary>
        /// <param name="node">FieldDeclarationSyntax</param>
        private void ParseFieldDeclarationSyntax(FieldDeclarationSyntax node)
        {
            if (!(node.Declaration.Type is IdentifierNameSyntax))
            {
                return;
            }

            var id = node.Declaration.Type as IdentifierNameSyntax;
            if (!id.Identifier.ValueText.Equals("machine"))
            {
                return;
            }

            this.MachineDeclIds.Add(id);
        }

        /// <summary>
        /// Parse machine class declarations.
        /// </summary>
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
                var rewrittenClassDecl = classDecl.WithIdentifier(classIdToken);

                var machineIdToken = SyntaxFactory.Identifier(base.CreateWhitespaceTriviaList(),
                    "Machine", base.CreateEndOfLineTriviaList());
                var machineId = SyntaxFactory.IdentifierName(machineIdToken);
                var nodeOrTokenList = SyntaxFactory.NodeOrTokenList(machineId);
                var seperatedList = SyntaxFactory.SeparatedList<TypeSyntax>(nodeOrTokenList);

                var colonToken = SyntaxFactory.Token(base.CreateWhitespaceTriviaList(1),
                    SyntaxKind.ColonToken, base.CreateWhitespaceTriviaList(1));
                var baseList = SyntaxFactory.BaseList(colonToken, seperatedList);

                rewrittenClassDecl = rewrittenClassDecl.WithBaseList(baseList);

                MachineDeclMap.Add(classDecl, rewrittenClassDecl);
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

        /// <summary>
        /// Rewrites machine declarations.
        /// </summary>
        private void RewriteMachineClassDeclarations()
        {
            base.Result = base.Result.ReplaceNodes(this.MachineDeclMap.Keys,
                (key, val) => this.MachineDeclMap[key]);
            base.Result = SyntaxFactory.ParseCompilationUnit(base.Result.ToFullString());
        }

        #endregion
    }
}
