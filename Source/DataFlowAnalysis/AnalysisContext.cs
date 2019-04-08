// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// A static analysis context.
    /// </summary>
    public sealed class AnalysisContext
    {
        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        public readonly Solution Solution;

        /// <summary>
        /// The project compilation for this analysis context.
        /// </summary>
        public readonly Compilation Compilation;

        /// <summary>
        /// Set of registered immutable types.
        /// </summary>
        private readonly ISet<Type> RegisteredImmutableTypes;

        /// <summary>
        /// Dictionary containing information about
        /// gives-up ownership methods.
        /// </summary>
        internal IDictionary<string, ISet<int>> GivesUpOwnershipMethods;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalysisContext"/> class.
        /// </summary>
        /// <param name="project">The project to analyze.</param>
        private AnalysisContext(Project project)
        {
            this.Solution = project.Solution;
            this.Compilation = project.GetCompilationAsync().Result;
            this.RegisteredImmutableTypes = new HashSet<Type>();
            this.GivesUpOwnershipMethods = new Dictionary<string, ISet<int>>();
        }

        /// <summary>
        /// Create a new static analysis context.
        /// </summary>
        public static AnalysisContext Create(Project project)
        {
            return new AnalysisContext(project);
        }

        /// <summary>
        /// Registers the specified immutable type.
        /// </summary>
        public void RegisterImmutableType(Type type)
        {
            this.RegisteredImmutableTypes.Add(type);
        }

        /// <summary>
        /// Registers the gives-up ownership method, and its gives-up parameter indexes.
        /// The method name should include the full namespace.
        /// </summary>
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

        /// <summary>
        /// Returns the full name of the given class.
        /// </summary>
        public static string GetFullClassName(ClassDeclarationSyntax node)
        {
            string name = node.Identifier.ValueText;
            return GetFullQualifierNameOfSyntaxNode(node) + name;
        }

        /// <summary>
        /// Returns the full name of the given struct.
        /// </summary>
        public static string GetFullStructName(StructDeclarationSyntax node)
        {
            string name = node.Identifier.ValueText;
            return GetFullQualifierNameOfSyntaxNode(node) + name;
        }

        /// <summary>
        /// Returns the full name of the given method.
        /// </summary>
        public static string GetFullMethodName(BaseMethodDeclarationSyntax node)
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

            return GetFullQualifierNameOfSyntaxNode(node) + name;
        }

        /// <summary>
        /// Returns the base type symbols of the given class.
        /// </summary>
        public IList<INamedTypeSymbol> GetBaseTypes(ClassDeclarationSyntax node)
        {
            var baseTypes = new List<INamedTypeSymbol>();

            var model = this.Compilation.GetSemanticModel(node.SyntaxTree);
            INamedTypeSymbol typeSymbol = model.GetDeclaredSymbol(node);
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
        public bool IsTypePassedByValueOrImmutable(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Array)
            {
                return false;
            }
            else if (type.TypeKind == TypeKind.Enum)
            {
                return true;
            }

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

            if (this.RegisteredImmutableTypes.Any(t => t.FullName.Equals(typeName)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the identifier from the expression.
        /// </summary>
        public static IdentifierNameSyntax GetIdentifier(ExpressionSyntax expr)
        {
            IdentifierNameSyntax identifier = null;
            ExpressionSyntax exprToParse = expr;
            while (identifier is null)
            {
                if (exprToParse is IdentifierNameSyntax)
                {
                    identifier = exprToParse as IdentifierNameSyntax;
                }
                else if (exprToParse is MemberAccessExpressionSyntax)
                {
                    exprToParse = (exprToParse as MemberAccessExpressionSyntax).Name;
                }
                else if (exprToParse is ElementAccessExpressionSyntax)
                {
                    exprToParse = (exprToParse as ElementAccessExpressionSyntax).Expression;
                }
                else if (exprToParse is BinaryExpressionSyntax &&
                    (exprToParse as BinaryExpressionSyntax).IsKind(SyntaxKind.AsExpression))
                {
                    exprToParse = (exprToParse as BinaryExpressionSyntax).Left;
                }
                else
                {
                    break;
                }
            }

            return identifier;
        }

        /// <summary>
        /// Returns the root identifier from the expression.
        /// </summary>
        public static IdentifierNameSyntax GetRootIdentifier(ExpressionSyntax expr)
        {
            IdentifierNameSyntax identifier = null;
            ExpressionSyntax exprToParse = expr;
            while (identifier is null)
            {
                if (exprToParse is IdentifierNameSyntax)
                {
                    identifier = exprToParse as IdentifierNameSyntax;
                }
                else if (exprToParse is MemberAccessExpressionSyntax)
                {
                    exprToParse = (exprToParse as MemberAccessExpressionSyntax).DescendantNodes().
                        OfType<IdentifierNameSyntax>().FirstOrDefault();
                }
                else if (exprToParse is ElementAccessExpressionSyntax)
                {
                    exprToParse = (exprToParse as ElementAccessExpressionSyntax).Expression;
                }
                else if (exprToParse is BinaryExpressionSyntax &&
                    (exprToParse as BinaryExpressionSyntax).IsKind(SyntaxKind.AsExpression))
                {
                    exprToParse = (exprToParse as BinaryExpressionSyntax).Left;
                }
                else
                {
                    break;
                }
            }

            return identifier;
        }

        /// <summary>
        /// Returns all identifiers.
        /// </summary>
        public static HashSet<IdentifierNameSyntax> GetIdentifiers(ExpressionSyntax expr) =>
            new HashSet<IdentifierNameSyntax>(expr.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>());

        /// <summary>
        /// Returns the callee of the given call expression.
        /// </summary>
        public static string GetCalleeOfInvocation(InvocationExpressionSyntax invocation)
        {
            string callee = string.Empty;

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
        /// Returns the argument list after resolving the given call expression.
        /// </summary>
        public static ArgumentListSyntax GetArgumentList(ExpressionSyntax call)
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

        /// <summary>
        /// Returns the full qualifier name of the given syntax node.
        /// </summary>
        private static string GetFullQualifierNameOfSyntaxNode(SyntaxNode syntaxNode)
        {
            string result = string.Empty;

            if (syntaxNode is null)
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
    }
}
