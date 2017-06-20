//-----------------------------------------------------------------------
// <copyright file="MethodSummaryResolver.cs">
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
    /// Class implementing a method summary resolver.
    /// </summary>
    internal static class MethodSummaryResolver
    {
        #region internal methods
        
        /// <summary>
        /// Returns all cached method summaries for the specified invocation.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>MethodSummarys</returns>
        internal static ISet<MethodSummary> ResolveMethodSummaries(
            InvocationExpressionSyntax invocation, IDataFlowNode node)
        {
            return MethodSummaryResolver.ResolveMethodSummaries(invocation as ExpressionSyntax, node);
        }

        /// <summary>
        /// Returns all cached method summaries for the specified object creation.
        /// </summary>
        /// <param name="objCreation">ObjectCreationExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>MethodSummarys</returns>
        internal static ISet<MethodSummary> ResolveMethodSummaries(
            ObjectCreationExpressionSyntax objCreation, IDataFlowNode node)
        {
            return MethodSummaryResolver.ResolveMethodSummaries(objCreation as ExpressionSyntax, node);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns all cached method summaries for the specified call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>MethodSummarys</returns>
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
                var parameterTypes = MethodSummaryResolver.GetCandidateParameterTypes(
                    invocation.ArgumentList, node);
                var candidateCalleeDeclarations = MethodSummaryResolver.
                    ResolveCandidateMethodsAtCallSite(invocation, node);
                foreach (var candidateCalleeDeclaration in candidateCalleeDeclarations)
                {
                    var calleeSummary = MethodSummary.Create(node.Summary.AnalysisContext,
                        candidateCalleeDeclaration, parameterTypes);
                    if (calleeSummary != null)
                    {
                        summaries.Add(calleeSummary);
                    }
                }
            }
            else
            {
                var parameterTypes = MethodSummaryResolver.GetCandidateParameterTypes(
                    objCreation.ArgumentList, node);
                var constructor = MethodSummaryResolver.ResolveMethodDeclaration(
                    objCreation, node) as ConstructorDeclarationSyntax;
                if (constructor != null)
                {
                    var calleeSummary = MethodSummary.Create(node.Summary.AnalysisContext,
                        constructor, parameterTypes);
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
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>Set of candidate callees</returns>
        private static HashSet<MethodDeclarationSyntax> ResolveCandidateMethodsAtCallSite(
            InvocationExpressionSyntax invocation, IDataFlowNode node)
        {
            var candidateCallees = new HashSet<MethodDeclarationSyntax>();
            var candidateMethodDeclaration = MethodSummaryResolver.ResolveMethodDeclaration(
                invocation, node) as MethodDeclarationSyntax;
            if (candidateMethodDeclaration == null)
            {
                return candidateCallees;
            }
            
            if (candidateMethodDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                candidateMethodDeclaration.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
                candidateMethodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                HashSet<MethodDeclarationSyntax> overriders = null;
                if (!MethodSummaryResolver.TryGetCandidateMethodOverriders(out overriders,
                    invocation, node))
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
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>BaseMethodDeclarationSyntax</returns>
        private static BaseMethodDeclarationSyntax ResolveMethodDeclaration(
            ExpressionSyntax call, IDataFlowNode node)
        {
            var calleeSymbol = node.Summary.SemanticModel.GetSymbolInfo(call).Symbol;
            if (calleeSymbol == null)
            {
                return null;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(calleeSymbol,
                node.Summary.AnalysisContext.Solution).Result as IMethodSymbol;
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
        /// <param name="overriders">List of overrider methods</param>
        /// <param name="virtualCall">Virtual call</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>Boolean</returns>
        private static bool TryGetCandidateMethodOverriders(out HashSet<MethodDeclarationSyntax> overriders,
            InvocationExpressionSyntax virtualCall, IDataFlowNode node)
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
                MethodDeclarationSyntax method = null;
                if (MethodSummaryResolver.TryGetMethodDeclarationFromType(
                    out method, calleeType, virtualCall, node))
                {
                    overriders.Add(method);
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to get the method declaration from the
        /// specified type and invocation.
        /// </summary>
        /// <param name="method">MethodDeclarationSyntax</param>
        /// <param name="type">Type</param>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>Boolean</returns>
        private static bool TryGetMethodDeclarationFromType(out MethodDeclarationSyntax method,
            ITypeSymbol type, InvocationExpressionSyntax invocation, IDataFlowNode node)
        {
            method = null;

            var definition = SymbolFinder.FindSourceDefinitionAsync(type,
                node.Summary.AnalysisContext.Solution).Result;
            if (definition == null)
            {
                return false;
            }

            var calleeClass = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ClassDeclarationSyntax;
            foreach (var m in calleeClass.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                if (m.Identifier.ValueText.Equals(node.Summary.AnalysisContext.
                    GetCalleeOfInvocation(invocation)))
                {
                    method = m;
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the candidate parameter types from the
        /// specified argument list.
        /// </summary>
        /// <param name="argumentList">ArgumentListSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>ITypeSymbols</returns>
        private static IDictionary<int, ISet<ITypeSymbol>> GetCandidateParameterTypes(
            ArgumentListSyntax argumentList, IDataFlowNode node)
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

        #endregion
    }
}
