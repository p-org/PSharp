//-----------------------------------------------------------------------
// <copyright file="TypeofRewriter.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Rewrite typeof statements to fully qualify state names.
    /// </summary>
    internal sealed class TypeofRewriter : PSharpRewriter
    {
        #region fields

        /// <summary>
        /// Set of all qualified state names in the current machine.
        /// </summary>
        private HashSet<string> CurrentAllQualifiedStateNames;

        /// <summary>
        /// Qualified state name corresponding to the procedure
        /// currently being rewritten.
        /// </summary>
        private List<string> CurrentQualifiedStateName;

        /// <summary>
        /// Set of rewritten qualified methods.
        /// </summary>
        private HashSet<QualifiedMethod> RewrittenQualifiedMethods;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal TypeofRewriter(IPSharpProgram program)
            : base(program)
        {
            this.CurrentAllQualifiedStateNames = new HashSet<string>();
            this.CurrentQualifiedStateName = new List<string>();
            this.RewrittenQualifiedMethods = new HashSet<QualifiedMethod>();
        }

        /// <summary>
        /// Rewrites the typeof statements in the program.
        /// </summary>
        /// <param name="rewrittenQualifiedMethods">QualifiedMethods</param>
        internal void Rewrite(HashSet<QualifiedMethod> rewrittenQualifiedMethods)
        {
            this.RewrittenQualifiedMethods = rewrittenQualifiedMethods;

            var typeofnodes = base.Program.GetSyntaxTree().GetRoot().DescendantNodes()
                .OfType<TypeOfExpressionSyntax>().
                ToList();

            if (typeofnodes.Count == 0)
                return;

            var root = base.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: typeofnodes,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            base.UpdateSyntaxTree(root.ToString());
        }

        #endregion

        #region private methods
        
        /// <summary>
        /// Rewrites the type inside typeof.
        /// </summary>
        /// <param name="node">ExpressionStatementSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(TypeOfExpressionSyntax node)
        {
            // Gets containing method.
            var methoddecl = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methoddecl == null)
            {
                return node;
            }

            // Gets containing class.
            var classdecl = methoddecl.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classdecl == null)
            {
                return node;
            }

            // Gets containing namespace.
            var namespacedecl = classdecl.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (namespacedecl == null)
            {
                return node;
            }

            var key = Tuple.Create(methoddecl.Identifier.ValueText, classdecl.Identifier.ValueText,
                namespacedecl.Name.ToString());

            var rewrittenMethod = this.RewrittenQualifiedMethods.SingleOrDefault(
                val => val.Name.Equals(methoddecl.Identifier.ValueText) &&
                val.MachineName.Equals(classdecl.Identifier.ValueText) &&
                val.NamespaceName.Equals(namespacedecl.Name.ToString()));
            if (rewrittenMethod == null)
            {
                return node;
            }
            
            this.CurrentAllQualifiedStateNames = rewrittenMethod.MachineQualifiedStateNames;
            this.CurrentQualifiedStateName = rewrittenMethod.QualifiedStateName;

            var typeUsed = node.Type.ToString();
            var fullyQualifiedName = this.GetFullyQualifiedStateName(typeUsed);
            if (fullyQualifiedName == typeUsed)
            {
                return node;
            }

            var tokenizedName = this.ToTokens(fullyQualifiedName);

            var rewritten = SyntaxFactory.ParseExpression("typeof(" + fullyQualifiedName + ")");
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }

        /// <summary>
        /// Given a partially-qualified state name, return the
        /// fully qualified state name.
        /// </summary>
        /// <param name="state">Partially qualified state name</param>
        /// <returns>Fully qualified state name</returns>
        private string GetFullyQualifiedStateName(string state)
        {
            if (this.CurrentQualifiedStateName.Count < 1 ||
                CurrentAllQualifiedStateNames.Count == 0)
            {
                return state;
            }

            for (int i = this.CurrentQualifiedStateName.Count - 2; i >= 0; i--)
            {
                var prefix = this.CurrentQualifiedStateName[0];
                for (int j = 1; j <= i; j++)
                {
                    prefix += "." + this.CurrentQualifiedStateName[j];
                }

                if (this.CurrentAllQualifiedStateNames.Contains(prefix + "." + state))
                {
                    return prefix + "." + state;
                }  
            }

            return state;
        }

        /// <summary>
        /// Tokenizes a qualified name.
        /// </summary>
        /// <param name="state">Qualified name</param>
        /// <returns>Tokenized name</returns>
        private List<string> ToTokens(string state)
        {
            return state.Split('.').ToList();
        }

        /// <summary>
        /// Collapses a tokenized qualified name.
        /// </summary>
        /// <param name="state">Tokenized qualified name</param>
        /// <returns>Qualified name</returns>
        private string FromTokens(List<string> state)
        {
            return state.Aggregate("", (acc, name) => acc == "" ? name : acc + "." + name);
        }

        #endregion
    }
}
