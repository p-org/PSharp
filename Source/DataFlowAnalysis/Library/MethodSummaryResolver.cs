// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a method summary resolver.
    /// </summary>
    internal static class MethodSummaryResolver
    {
        /// <summary>
        /// Returns all cached method summaries for the specified invocation.
        /// </summary>
        internal static ISet<MethodSummary> ResolveMethodSummaries(InvocationExpressionSyntax invocation, IDataFlowNode node)
        {
            return ResolveMethodSummaries(invocation as ExpressionSyntax, node);
        }

        /// <summary>
        /// Returns all cached method summaries for the specified object creation.
        /// </summary>
        internal static ISet<MethodSummary> ResolveMethodSummaries(ObjectCreationExpressionSyntax objCreation, IDataFlowNode node)
        {
            return ResolveMethodSummaries(objCreation as ExpressionSyntax, node);
        }

        /// <summary>
        /// Returns all cached method summaries for the specified call.
        /// </summary>
        private static ISet<MethodSummary> ResolveMethodSummaries(ExpressionSyntax call, IDataFlowNode node)
        {
            var summaries = new HashSet<MethodSummary>();

            var invocation = call as InvocationExpressionSyntax;
            var objCreation = call as ObjectCreationExpressionSyntax;
            var callSymbol = node.Summary.SemanticModel.GetSymbolInfo(call).Symbol;
            if (callSymbol == null ||
                (invocation == null && objCreation == null))
            {
                return summaries;
            }

            if (node.MethodSummaryCache.ContainsKey(callSymbol))
            {
                summaries.UnionWith(node.MethodSummaryCache[callSymbol]);
            }
            else if (invocation != null)
            {
                var parameterTypes = GetCandidateParameterTypes(invocation.ArgumentList, node);
                var candidateCalleeDeclarations = ResolveCandidateMethodsAtCallSite(invocation, node);
                foreach (var candidateCalleeDeclaration in candidateCalleeDeclarations)
                {
                    var calleeSummary = MethodSummary.Create(node.Summary.AnalysisContext, candidateCalleeDeclaration, parameterTypes);
                    if (calleeSummary != null)
                    {
                        summaries.Add(calleeSummary);
                    }
                }
            }
            else
            {
                var parameterTypes = GetCandidateParameterTypes(objCreation.ArgumentList, node);
                var constructor = ResolveMethodDeclaration(objCreation, node) as ConstructorDeclarationSyntax;
                if (constructor != null)
                {
                    var calleeSummary = MethodSummary.Create(node.Summary.AnalysisContext, constructor, parameterTypes);
                    if (calleeSummary != null)
                    {
                        summaries.Add(calleeSummary);
                    }
                }
            }

            return summaries;
        }

        /// <summary>
        /// Returns the candidate callees after resolving the specified invocation.
        /// </summary>
        private static HashSet<MethodDeclarationSyntax> ResolveCandidateMethodsAtCallSite(
            InvocationExpressionSyntax invocation, IDataFlowNode node)
        {
            var candidateCallees = new HashSet<MethodDeclarationSyntax>();
            var candidateMethodDeclaration = ResolveMethodDeclaration(invocation, node) as MethodDeclarationSyntax;
            if (candidateMethodDeclaration == null)
            {
                return candidateCallees;
            }

            if (candidateMethodDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                candidateMethodDeclaration.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
                candidateMethodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                if (!TryGetCandidateMethodOverriders(out HashSet<MethodDeclarationSyntax> overriders, invocation, node))
                {
                    return candidateCallees;
                }

                candidateCallees.UnionWith(overriders);
            }

            if (candidateCallees.Count == 0)
            {
                candidateCallees.Add(candidateMethodDeclaration);
            }

            return candidateCallees;
        }

        /// <summary>
        /// Returns the method declaration after resolving the specified call.
        /// </summary>
        private static BaseMethodDeclarationSyntax ResolveMethodDeclaration(
            ExpressionSyntax call, IDataFlowNode node)
        {
            var calleeSymbol = node.Summary.SemanticModel.GetSymbolInfo(call).Symbol;
            if (calleeSymbol == null)
            {
                return null;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(calleeSymbol, node.Summary.AnalysisContext.Solution).Result as IMethodSymbol;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                return null;
            }

            BaseMethodDeclarationSyntax methodDeclaration;
            if (definition.PartialImplementationPart != null)
            {
                methodDeclaration = definition.PartialImplementationPart.DeclaringSyntaxReferences.
                    First().GetSyntax() as BaseMethodDeclarationSyntax;
            }
            else
            {
                methodDeclaration = definition.DeclaringSyntaxReferences.First().
                    GetSyntax() as BaseMethodDeclarationSyntax;
            }

            return methodDeclaration;
        }

        /// <summary>
        /// Tries to get the list of candidate methods that can
        /// override the specified virtual call.
        /// </summary>
        private static bool TryGetCandidateMethodOverriders(
            out HashSet<MethodDeclarationSyntax> overriders,
            InvocationExpressionSyntax virtualCall,
            IDataFlowNode node)
        {
            overriders = new HashSet<MethodDeclarationSyntax>();

            ISymbol calleeSymbol = null;
            SimpleNameSyntax callee = null;
            bool isThis = false;

            if (virtualCall.Expression is MemberAccessExpressionSyntax)
            {
                var expr = virtualCall.Expression as MemberAccessExpressionSyntax;
                var identifier = expr.Expression.DescendantNodesAndSelf().
                    OfType<IdentifierNameSyntax>().Last();
                calleeSymbol = node.Summary.SemanticModel.GetSymbolInfo(identifier).Symbol;

                if (expr.Expression is ThisExpressionSyntax)
                {
                    callee = expr.Name;
                    isThis = true;
                }
            }
            else
            {
                callee = virtualCall.Expression as IdentifierNameSyntax;
                isThis = true;
            }

            if (isThis)
            {
                var typeDeclaration = node.Summary.Method.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (typeDeclaration != null)
                {
                    foreach (var method in typeDeclaration.Members.OfType<MethodDeclarationSyntax>())
                    {
                        if (method.Identifier.ToString().Equals(callee.Identifier.ToString()))
                        {
                            overriders.Add(method);
                            return true;
                        }
                    }
                }

                return false;
            }

            var calleeDefinitions = node.DataFlowInfo.ResolveOutputAliases(calleeSymbol);
            var calleeTypes = calleeDefinitions.SelectMany(def => def.CandidateTypes);
            if (!calleeTypes.Any())
            {
                return false;
            }

            foreach (var calleeType in calleeTypes)
            {
                if (TryGetMethodDeclarationFromType(out MethodDeclarationSyntax method, calleeType, virtualCall, node))
                {
                    overriders.Add(method);
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to get the method declaration from the specified type and invocation.
        /// </summary>
        private static bool TryGetMethodDeclarationFromType(
            out MethodDeclarationSyntax method,
            ITypeSymbol type,
            InvocationExpressionSyntax invocation,
            IDataFlowNode node)
        {
            method = null;

            var definition = SymbolFinder.FindSourceDefinitionAsync(type, node.Summary.AnalysisContext.Solution).Result;
            if (definition == null)
            {
                return false;
            }

            var calleeClass = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ClassDeclarationSyntax;
            foreach (var m in calleeClass.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                if (m.Identifier.ValueText.Equals(AnalysisContext.GetCalleeOfInvocation(invocation)))
                {
                    method = m;
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the candidate parameter types from the specified argument list.
        /// </summary>
        private static IDictionary<int, ISet<ITypeSymbol>> GetCandidateParameterTypes(ArgumentListSyntax argumentList, IDataFlowNode node)
        {
            var candidateTypes = new Dictionary<int, ISet<ITypeSymbol>>();
            if (argumentList == null)
            {
                return candidateTypes;
            }

            for (int idx = 0; idx < argumentList.Arguments.Count; idx++)
            {
                var argSymbol = node.Summary.SemanticModel.GetSymbolInfo(
                    argumentList.Arguments[idx].Expression).Symbol;
                if (argSymbol != null)
                {
                    candidateTypes.Add(idx, node.DataFlowInfo.GetCandidateTypesOfSymbol(argSymbol));
                }
            }

            return candidateTypes;
        }
    }
}
