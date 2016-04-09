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

        /// <summary>
        /// Dictionary containing information about
        /// gives-up ownership methods.
        /// </summary>
        internal Dictionary<string, HashSet<int>> GivesUpOwnershipMethods;

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
        /// Caches the given summary.
        /// </summary>
        /// <param name="methodSummary">MethodSummary</param>
        public void CacheSummary(MethodSummary methodSummary)
        {
            this.Summaries.Add(methodSummary.Method, methodSummary);
        }

        /// <summary>
        /// Tries to get the method summary of the given object creation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="constructor">ConstructorDeclarationSyntax</param>
        /// <returns>MethodSummary</returns>
        public MethodSummary TryGetCachedSummary(ConstructorDeclarationSyntax constructor)
        {
            MethodSummary methodSummary;
            if (this.Summaries.TryGetValue(constructor, out methodSummary))
            {
                return null;
            }

            return methodSummary;
        }

        /// <summary>
        /// Tries to get the method summary of the given method declaration.
        /// Returns null if such summary cannot be found.
        /// </summary>
        /// <param name="method">MethodDeclarationSyntax</param>
        /// <returns>MethodSummary</returns>
        public MethodSummary TryGetCachedSummary(MethodDeclarationSyntax method)
        {
            MethodSummary methodSummary;
            if (!this.Summaries.TryGetValue(method, out methodSummary))
            {
                return null;
            }

            return methodSummary;
        }

        /// <summary>
        /// Tries to get the method from the given type and call.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="type">Type</param>
        /// <param name="call">Call</param>
        /// <returns>Boolean</returns>
        public bool TryGetMethodFromType(out MethodDeclarationSyntax method, ITypeSymbol type,
            InvocationExpressionSyntax call)
        {
            method = null;

            var definition = SymbolFinder.FindSourceDefinitionAsync(type, this.Solution).Result;
            if (definition == null)
            {
                return false;
            }

            var calleeClass = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ClassDeclarationSyntax;
            foreach (var m in calleeClass.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                if (m.Identifier.ValueText.Equals(AnalysisContext.GetCalleeOfInvocation(call)))
                {
                    method = m;
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Registers the gives-up ownership method, and its gives-up parameter indexes.
        /// The method name should include the full namespace.
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="givesUpParamIndexes">Set of indexes</param>
        public void RegisterGivesUpOwnershipMethod(string methodName, ISet<int> givesUpParamIndexes)
        {
            if (this.GivesUpOwnershipMethods.ContainsKey(methodName))
            {
                this.GivesUpOwnershipMethods[methodName].Clear();
            }
            else
            {
                this.GivesUpOwnershipMethods.Add(methodName, new HashSet<int>());
            }

            this.GivesUpOwnershipMethods[methodName].UnionWith(givesUpParamIndexes);
        }

        #endregion

        #region public helper methods

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
        /// Returns true if the given type is an enum.
        /// Returns false if not.
        /// </summary>
        /// <param name="type">ITypeSymbol</param>
        /// <returns>Boolean</returns>
        public bool IsTypeEnum(ITypeSymbol type)
        {
            var typeDef = SymbolFinder.FindSourceDefinitionAsync(type, this.Solution).Result;
            if (typeDef != null && typeDef.DeclaringSyntaxReferences.First().
                GetSyntax().IsKind(SyntaxKind.EnumDeclaration))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region public static API

        /// <summary>
        /// Returns the callee of the given call expression.
        /// </summary>
        /// <param name="invocation">Invocation</param>
        /// <returns>Callee</returns>
        public static string GetCalleeOfInvocation(InvocationExpressionSyntax invocation)
        {
            string callee = "";

            if (invocation.Expression is MemberAccessExpressionSyntax)
            {
                var memberAccessExpr = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccessExpr.Name is IdentifierNameSyntax)
                {
                    callee = (memberAccessExpr.Name as IdentifierNameSyntax).Identifier.ValueText;
                }
                else if (memberAccessExpr.Name is GenericNameSyntax)
                {
                    callee = (memberAccessExpr.Name as GenericNameSyntax).Identifier.ValueText;
                }
            }
            else
            {
                callee = invocation.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().
                    First().Identifier.ValueText;
            }

            return callee;
        }

        /// <summary>
        /// Returns the identifier from the expression.
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
        /// Returns the top-level identifier.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <returns>Identifier</returns>
        public static IdentifierNameSyntax GetTopLevelIdentifier(ExpressionSyntax expr)
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
        /// Returns all identifiers.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <returns>Identifiers</returns>
        public static HashSet<IdentifierNameSyntax> GetIdentifiers(ExpressionSyntax expr)
        {
            var identifiers = new HashSet<IdentifierNameSyntax>();
            if (expr is IdentifierNameSyntax)
            {
                identifiers.Add(expr as IdentifierNameSyntax);
            }
            else if (expr is MemberAccessExpressionSyntax)
            {
                identifiers.UnionWith((expr as MemberAccessExpressionSyntax).DescendantNodes().
                    OfType<IdentifierNameSyntax>());
            }

            return identifiers;
        }

        /// <summary>
        /// Returns the argument list after resolving
        /// the given call expression.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <returns>ArgumentListSyntax</returns>
        public ArgumentListSyntax GetArgumentList(ExpressionSyntax call)
        {
            ArgumentListSyntax argumentList = null;
            if (call is InvocationExpressionSyntax)
            {
                argumentList = (call as InvocationExpressionSyntax).ArgumentList;
            }
            else if (call is ObjectCreationExpressionSyntax)
            {
                argumentList = (call as ObjectCreationExpressionSyntax).ArgumentList;
            }

            return argumentList;
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
            this.GivesUpOwnershipMethods = new Dictionary<string, HashSet<int>>();
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
