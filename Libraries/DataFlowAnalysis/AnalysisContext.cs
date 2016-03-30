//-----------------------------------------------------------------------
// <copyright file="AnalysisContext.cs">
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// A static analysis context.
    /// </summary>
    public class AnalysisContext
    {
        #region fields

        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        public readonly Solution Solution;

        /// <summary>
        /// The project compilation for this analysis context.
        /// </summary>
        public readonly Compilation Compilation;

        /// <summary>
        /// Dictionary of method summaries in the project.
        /// </summary>
        public Dictionary<BaseMethodDeclarationSyntax, MethodSummary> Summaries;

        #endregion

        #region public API

        /// <summary>
        /// Create a new static analysis context.
        /// </summary>
        /// <param name="project">Project</param>
        /// <returns>AnalysisContext</returns>
        public static AnalysisContext Create(Project project)
        {
            return new AnalysisContext(project);
        }

        /// <summary>
        /// Tries to get the method summary of the given object creation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>MethodSummary</returns>
        public virtual MethodSummary TryGetSummary(ObjectCreationExpressionSyntax call, SemanticModel model)
        {
            return MethodSummary.TryGet(call, model, this);
        }

        /// <summary>
        /// Tries to get the method summary of the given invocation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>MethodSummary</returns>
        public virtual MethodSummary TryGetSummary(InvocationExpressionSyntax call, SemanticModel model)
        {
            return MethodSummary.TryGet(call, model, this);
        }

        /// <summary>
        /// Returns the full name of the given class.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <returns>string</returns>
        public string GetFullClassName(ClassDeclarationSyntax node)
        {
            string name = node.Identifier.ValueText;
            return this.GetFullQualifierNameOfSyntaxNode(node) + name;
        }

        /// <summary>
        /// Returns the full name of the given struct.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <returns>string</returns>
        public string GetFullStructName(StructDeclarationSyntax node)
        {
            string name = node.Identifier.ValueText;
            return this.GetFullQualifierNameOfSyntaxNode(node) + name;
        }

        /// <summary>
        /// Returns the full name of the given method.
        /// </summary>
        /// <param name="node">SyntaxNode</param>
        /// <returns>string</returns>
        public string GetFullMethodName(BaseMethodDeclarationSyntax node)
        {
            string name = null;
            if (node is MethodDeclarationSyntax)
            {
                name = (node as MethodDeclarationSyntax).Identifier.ValueText;
            }
            else if (node is ConstructorDeclarationSyntax)
            {
                name = (node as ConstructorDeclarationSyntax).Identifier.ValueText;
            }
            
            return this.GetFullQualifierNameOfSyntaxNode(node) + name;
        }

        /// <summary>
        /// Returns the base type symbols of the given class.
        /// </summary>
        /// <param name="node">SyntaxNode</param>
        /// <returns>Base types</returns>
        public IList<INamedTypeSymbol> GetBaseTypes(ClassDeclarationSyntax node)
        {
            var baseTypes = new List<INamedTypeSymbol>();

            var model = this.Compilation.GetSemanticModel(node.SyntaxTree);
            string nodeName = this.GetFullClassName(node);

            INamedTypeSymbol typeSymbol = model.Compilation.GetTypeByMetadataName(nodeName);
            while (typeSymbol.BaseType != null)
            {
                baseTypes.Add(typeSymbol.BaseType);
                typeSymbol = typeSymbol.BaseType;
            }

            return baseTypes;
        }

        /// <summary>
        /// Returns true if the given type is passed by value or is immutable.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Boolean</returns>
        public virtual bool IsTypePassedByValueOrImmutable(ITypeSymbol type)
        {
            var typeName = type.ContainingNamespace.ToString() + "." + type.Name;
            if (typeName.Equals(typeof(bool).FullName) ||
                typeName.Equals(typeof(byte).FullName) ||
                typeName.Equals(typeof(sbyte).FullName) ||
                typeName.Equals(typeof(char).FullName) ||
                typeName.Equals(typeof(decimal).FullName) ||
                typeName.Equals(typeof(double).FullName) ||
                typeName.Equals(typeof(float).FullName) ||
                typeName.Equals(typeof(int).FullName) ||
                typeName.Equals(typeof(uint).FullName) ||
                typeName.Equals(typeof(long).FullName) ||
                typeName.Equals(typeof(ulong).FullName) ||
                typeName.Equals(typeof(short).FullName) ||
                typeName.Equals(typeof(ushort).FullName) ||
                typeName.Equals(typeof(string).FullName))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the identifier from the expression.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <returns>Identifier</returns>
        public IdentifierNameSyntax GetIdentifier(ExpressionSyntax expr)
        {
            IdentifierNameSyntax identifier = null;
            if (expr is IdentifierNameSyntax)
            {
                identifier = expr as IdentifierNameSyntax;
            }
            else if (expr is MemberAccessExpressionSyntax)
            {
                identifier = (expr as MemberAccessExpressionSyntax).Name
                    as IdentifierNameSyntax;
            }

            return identifier;
        }

        /// <summary>
        /// Gets the top-level identifier.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <returns>Identifier</returns>
        public IdentifierNameSyntax GetTopLevelIdentifier(ExpressionSyntax expr)
        {
            IdentifierNameSyntax identifier = null;
            if (expr is IdentifierNameSyntax)
            {
                identifier = expr as IdentifierNameSyntax;
            }
            else if (expr is MemberAccessExpressionSyntax)
            {
                identifier = (expr as MemberAccessExpressionSyntax).DescendantNodes().
                    OfType<IdentifierNameSyntax>().First();
            }

            return identifier;
        }

        /// <summary>
        /// Returns true if the type of the expression is an enum.
        /// Returns false if it is not
        /// </summary>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean</returns>
        public bool IsExprEnum(ExpressionSyntax expr, SemanticModel model)
        {
            var type = model.GetTypeInfo(expr).Type;
            var typeDef = SymbolFinder.FindSourceDefinitionAsync(type, this.Solution).Result;
            if (typeDef != null && typeDef.DeclaringSyntaxReferences.First().
                GetSyntax().IsKind(SyntaxKind.EnumDeclaration))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">Project</param>
        protected AnalysisContext(Project project)
        {
            this.Solution = project.Solution;
            this.Compilation = project.GetCompilationAsync().Result;
            this.Summaries = new Dictionary<BaseMethodDeclarationSyntax, MethodSummary>();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Returns the full qualifier name of the given syntax node.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <returns>string</returns>
        protected string GetFullQualifierNameOfSyntaxNode(SyntaxNode syntaxNode)
        {
            string result = "";

            if (syntaxNode == null)
            {
                return result;
            }

            SyntaxNode ancestor = null;
            while ((ancestor = syntaxNode.Ancestors().Where(val
                => val is ClassDeclarationSyntax).FirstOrDefault()) != null)
            {
                result = (ancestor as ClassDeclarationSyntax).Identifier.ValueText + "." + result;
                syntaxNode = ancestor;
            }

            ancestor = null;
            while ((ancestor = syntaxNode.Ancestors().Where(val
                => val is NamespaceDeclarationSyntax).FirstOrDefault()) != null)
            {
                result = (ancestor as NamespaceDeclarationSyntax).Name + "." + result;
                syntaxNode = ancestor;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the syntax tree belongs to the P# program.
        /// Else returns false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean</returns>
        protected bool IsProgramSyntaxTree(SyntaxTree tree)
        {
            if (tree.FilePath.Contains("\\AssemblyInfo.cs") ||
                    tree.FilePath.Contains(".NETFramework,"))
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
