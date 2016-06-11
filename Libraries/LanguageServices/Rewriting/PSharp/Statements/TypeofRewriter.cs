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

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Rewrite typeof statements to fully qualify state names.
    /// </summary>
    internal sealed class TypeofRewriter : PSharpRewriter
    {
        #region fields

        /// <summary>
        /// Set of all qualified state names in the current machine
        /// </summary>
        HashSet<string> CurrentAllQualifiedStateNames;

        /// <summary>
        /// Qualified state name corresponding to the procedure currently being rewritten
        /// </summary>
        List<string> CurrentQualifiedStateName;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal TypeofRewriter(IPSharpProgram program)
            : base(program)
        {
            CurrentAllQualifiedStateNames = new HashSet<string>();
            CurrentQualifiedStateName = new List<string>();
        }

        /// <summary>
        /// Rewrites the typeof statements in the program.
        /// </summary>
        internal void Rewrite(Dictionary<Tuple<string, string, string>, Tuple<HashSet<string>, List<string>>> GeneratedMethodsToQualifiedStateNames)
        {
            var methods = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().
                ToList();

            foreach (var method in methods)
            {
                // Get containing class 
                var classdecl = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classdecl == null) continue;

                // Get containing namespace
                var namespacedecl = classdecl.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                if (namespacedecl == null) continue;
                
                var key = Tuple.Create(method.Identifier.ValueText, classdecl.Identifier.ValueText, namespacedecl.Name.ToString());

                // Is this a generated method
                if (!GeneratedMethodsToQualifiedStateNames.ContainsKey(key)) continue;

                var value = GeneratedMethodsToQualifiedStateNames[key];
                CurrentAllQualifiedStateNames = value.Item1;
                CurrentQualifiedStateName = value.Item2;

                // lets visit typeof nodes now
                var typeofnodes = method.DescendantNodes().OfType<TypeOfExpressionSyntax>().ToList();

                var root = base.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                    nodes: typeofnodes,
                    computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

                base.UpdateSyntaxTree(root.ToString());

            }

        }

        #endregion

        #region private methods

        /// <summary>
        /// Given a partially-qualified state name, return the fully qualified
        /// state name.
        /// </summary>
        /// <param name="state">Partially qualified state name</param>
        /// <returns>Fully qualified state name</returns>
        private string GetFullyQualifiedStateName(string state)
        {
            if (CurrentQualifiedStateName.Count < 1 || CurrentAllQualifiedStateNames.Count == 0)
                return state;

            for (int i = CurrentQualifiedStateName.Count - 2; i >= 0; i--)
            {
                var prefix = CurrentQualifiedStateName[0];
                for (int j = 1; j <= i; j++) prefix += "." + CurrentQualifiedStateName[0];
                if (CurrentAllQualifiedStateNames.Contains(prefix + "." + state))
                    return prefix + "." + state;
            }

            return state;
        }

        /// <summary>
        /// Tokenize a qualified name.
        /// </summary>
        /// <param name="state">Qualified name</param>
        /// <returns>Tokenized name</returns>
        private static List<string> ToTokens(string state)
        {
            return state.Split('.').ToList();
        }

        /// <summary>
        /// Collapse a tokenized qualified name.
        /// </summary>
        /// <param name="state">Tokenized qualified name</param>
        /// <returns>Qualified name</returns>
        private static string FromTokens(List<string> state)
        {
            return state.Aggregate("", (acc, name) => acc == "" ? name : acc + "." + name);
        }


        /// <summary>
        /// Rewrites the type inside typeof
        /// </summary>
        /// <param name="node">ExpressionStatementSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(TypeOfExpressionSyntax node)
        {
            var typeUsed = node.Type.ToString();
            var fullyQualifiedName = GetFullyQualifiedStateName(typeUsed);
            if (fullyQualifiedName == typeUsed) return node;

            var tokenizedName = ToTokens(fullyQualifiedName);

            var rewritten  = SyntaxFactory.ParseExpression("typeof(" + fullyQualifiedName + ")");
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
