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
        /// Tries to get the method summary of the given invocation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>MethodSummary</returns>
        internal static MethodSummary TryGetCachedSummary(InvocationExpressionSyntax invocation,
            IDataFlowNode node)
        {
            var method = MethodSummaryResolver.ResolveMethod(invocation, node);
            if (method == null)
            {
                return null;
            }

            return node.Summary.AnalysisContext.TryGetCachedSummary(method);
        }

        /// <summary>
        /// Tries to get the method summary of the given object creation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="objCreation">ObjectCreationExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>MethodSummary</returns>
        internal static MethodSummary TryGetCachedSummary(ObjectCreationExpressionSyntax objCreation,
            IDataFlowNode node)
        {
            var constructor = MethodSummaryResolver.ResolveConstructor(objCreation, node);
            if (constructor == null)
            {
                return null;
            }

            return node.Summary.AnalysisContext.TryGetCachedSummary(constructor);
        }

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
                var candidateCallees = MethodSummaryResolver.ResolveCandidateMethodsAtCallSite(
                    invocation, node.Statement, node);
                foreach (var candidateCallee in candidateCallees)
                {
                    var calleeSummary = MethodSummary.Create(node.Summary.AnalysisContext, candidateCallee);
                    if (calleeSummary != null)
                    {
                        summaries.Add(calleeSummary);
                    }
                }
            }
            else
            {
                var constructor = MethodSummaryResolver.ResolveCallee(
                    objCreation, node) as ConstructorDeclarationSyntax;
                if (constructor != null)
                {
                    var calleeSummary = MethodSummary.Create(
                        node.Summary.AnalysisContext, constructor);
                    if (calleeSummary != null)
                    {
                        summaries.Add(calleeSummary);
                    }
                }
            }

            return summaries;
        }

        /// <summary>
        /// Returns the candidate callees after resolving the given invocation.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>Set of candidate callees</returns>
        private static HashSet<MethodDeclarationSyntax> ResolveCandidateMethodsAtCallSite(
            InvocationExpressionSyntax invocation, Statement statement, IDataFlowNode node)
        {
            var candidateCallees = new HashSet<MethodDeclarationSyntax>();
            var potentialCallee = MethodSummaryResolver.ResolveCallee(
                invocation, node) as MethodDeclarationSyntax;
            if (potentialCallee == null)
            {
                return candidateCallees;
            }

            if (potentialCallee.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                potentialCallee.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
                potentialCallee.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                HashSet<MethodDeclarationSyntax> overriders = null;
                if (!MethodSummaryResolver.TryGetCandidateMethodOverriders(out overriders,
                    invocation, statement, node))
                {
                    return candidateCallees;
                }

                candidateCallees.UnionWith(overriders);
            }

            if (candidateCallees.Count == 0)
            {
                candidateCallees.Add(potentialCallee);
            }

            return candidateCallees;
        }

        /// <summary>
        /// Returns the candidate callees after resolving the given invocation.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>MethodDeclarationSyntax</returns>
        private static MethodDeclarationSyntax ResolveMethod(InvocationExpressionSyntax invocation,
            IDataFlowNode node)
        {
            return MethodSummaryResolver.ResolveCallee(invocation, node) as MethodDeclarationSyntax;
        }

        /// <summary>
        /// Returns the constructor after resolving the given object creation.
        /// </summary>
        /// <param name="objCreation">ObjectCreationExpressionSyntax</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>ConstructorDeclarationSyntax</returns>
        private static ConstructorDeclarationSyntax ResolveConstructor(ObjectCreationExpressionSyntax objCreation,
            IDataFlowNode node)
        {
            return MethodSummaryResolver.ResolveCallee(objCreation, node) as ConstructorDeclarationSyntax;
        }

        /// <summary>
        /// Returns the candidate callees after resolving the given call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>Set of candidate callees</returns>
        private static BaseMethodDeclarationSyntax ResolveCallee(ExpressionSyntax call, IDataFlowNode node)
        {
            var calleeSymbol = node.Summary.SemanticModel.GetSymbolInfo(call).Symbol;
            if (calleeSymbol == null)
            {
                return null;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(calleeSymbol,
                node.Summary.AnalysisContext.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                return null;
            }

            var invocation = call as InvocationExpressionSyntax;
            var objCreation = call as ObjectCreationExpressionSyntax;
            if (invocation == null && objCreation == null)
            {
                return null;
            }

            return definition.DeclaringSyntaxReferences.First().GetSyntax()
                as BaseMethodDeclarationSyntax;
        }

        /// <summary>
        /// Tries to get the list of candidate methods that can override the given virtual call.
        /// If it cannot find such methods then it returns false.
        /// </summary>
        /// <param name="overriders">List of overrider methods</param>
        /// <param name="virtualCall">Virtual call</param>
        /// <param name="statement">Statement</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>Boolean</returns>
        private static bool TryGetCandidateMethodOverriders(out HashSet<MethodDeclarationSyntax> overriders,
            InvocationExpressionSyntax virtualCall, Statement statement, IDataFlowNode node)
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
                var typeDeclaration = statement.Summary.TypeDeclaration;
                foreach (var method in typeDeclaration.Members.OfType<MethodDeclarationSyntax>())
                {
                    if (method.Identifier.ToString().Equals(callee.Identifier.ToString()))
                    {
                        overriders.Add(method);
                        return true;
                    }
                }

                return false;
            }

            Dictionary<ISymbol, HashSet<ITypeSymbol>> referenceTypeMap = null;
            //if (calleeSymbol == null ||
            //    !cfgNode.GetMethodSummary().DataFlowAnalysis.TryGetReferenceTypeMapForSyntaxNode(
            //        syntaxNode, cfgNode, out referenceTypeMap) ||
            //    !referenceTypeMap.ContainsKey(calleeSymbol))
            //{
            //    return false;
            //}

            //foreach (var objectType in referenceTypeMap[calleeSymbol])
            //{
            //    MethodDeclarationSyntax m = null;
            //    if (this.TryGetMethodFromType(out m, objectType, virtualCall))
            //    {
            //        overriders.Add(m);
            //    }
            //}

            return true;
        }

        #endregion
    }
}
