//-----------------------------------------------------------------------
// <copyright file="DataFlowQuerying.cs">
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

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A data-flow querying API.
    /// </summary>
    internal static class DataFlowQuerying
    {
        #region data-flow querying methods

        /// <summary>
        /// Returns true if the given expression flows in the target.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        public static bool FlowsIntoTarget(ExpressionSyntax expr, ISymbol target,
            SyntaxNode syntaxNode, CFGNode cfgNode, SyntaxNode targetSyntaxNode,
            CFGNode targetCfgNode, SemanticModel model, AnalysisContext context)
        {
            IdentifierNameSyntax identifier = context.GetTopLevelIdentifier(expr);
            if (identifier == null)
            {
                return false;
            }

            ISymbol reference = model.GetSymbolInfo(identifier).Symbol;
            return DataFlowQuerying.FlowsIntoTarget(reference, target, syntaxNode,
                cfgNode, targetSyntaxNode, targetCfgNode);
        }

        /// <summary>
        /// Returns true if the given variable flows in the target.
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean</returns>
        public static bool FlowsIntoTarget(VariableDeclaratorSyntax variable, ISymbol target,
            SyntaxNode syntaxNode, CFGNode cfgNode, SyntaxNode targetSyntaxNode,
            CFGNode targetCfgNode, SemanticModel model)
        {
            ISymbol reference = model.GetDeclaredSymbol(variable);
            return DataFlowQuerying.FlowsIntoTarget(reference, target, syntaxNode,
                cfgNode, targetSyntaxNode, targetCfgNode);
        }

        /// <summary>
        /// Returns true if the given symbol flows into the target symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        public static bool FlowsIntoTarget(ISymbol symbol, ISymbol target, SyntaxNode syntaxNode,
            CFGNode cfgNode, SyntaxNode targetSyntaxNode, CFGNode targetCfgNode)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
            if (!targetCfgNode.GetMethodSummary().DataFlowAnalysis.TryGetDataFlowMapForSyntaxNode(targetSyntaxNode,
                targetCfgNode, out dataFlowMap) || !dataFlowMap.ContainsKey(symbol))
            {
                return false;
            }

            if (symbol.Equals(target) && cfgNode.GetMethodSummary().DataFlowAnalysis.DoesReferenceResetUntilSyntaxNode(
                symbol, syntaxNode, cfgNode, targetSyntaxNode, targetCfgNode))
            {
                return false;
            }

            Dictionary<ISymbol, HashSet<ISymbol>> reachabilityMap = null;
            if (targetCfgNode.GetMethodSummary().DataFlowAnalysis.TryGetFieldReachabilityMapForSyntaxNode(
                targetSyntaxNode, targetCfgNode, out reachabilityMap) && reachabilityMap.ContainsKey(symbol))
            {
                foreach (var field in reachabilityMap[symbol])
                {
                    foreach (var reference in dataFlowMap[field])
                    {
                        dataFlowMap[symbol].Add(reference);
                    }
                }
            }

            foreach (var reference in dataFlowMap[symbol])
            {
                if (reference.Equals(target))
                {
                    return true;
                }
            }

            if (dataFlowMap.ContainsKey(target))
            {
                foreach (var reference in dataFlowMap[target])
                {
                    if (!cfgNode.GetMethodSummary().DataFlowAnalysis.DoesReferenceResetUntilSyntaxNode(
                        symbol, syntaxNode, cfgNode, targetSyntaxNode, targetCfgNode))
                    {
                        if (reference.Equals(symbol))
                        {
                            return true;
                        }

                        foreach (var symbolRef in dataFlowMap[symbol])
                        {
                            if (reference.Equals(symbolRef))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given expression flows from the target.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        public static bool FlowsFromTarget(ExpressionSyntax expr, ISymbol target, SyntaxNode syntaxNode,
            CFGNode cfgNode, SyntaxNode targetSyntaxNode, CFGNode targetCfgNode, SemanticModel model,
            AnalysisContext context)
        {
            IdentifierNameSyntax identifier = context.GetTopLevelIdentifier(expr);
            if (identifier == null)
            {
                return false;
            }

            ISymbol symbol = model.GetSymbolInfo(identifier).Symbol;
            return DataFlowQuerying.FlowsFromTarget(symbol, target, syntaxNode,
                cfgNode, targetSyntaxNode, targetCfgNode);
        }

        /// <summary>
        /// Returns true if the given symbol flows from the target symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        public static bool FlowsFromTarget(ISymbol symbol, ISymbol target, SyntaxNode syntaxNode,
            CFGNode cfgNode, SyntaxNode targetSyntaxNode, CFGNode targetCfgNode)
        {            
            Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
            if (!cfgNode.GetMethodSummary().DataFlowAnalysis.TryGetDataFlowMapForSyntaxNode(syntaxNode,
                cfgNode, out dataFlowMap) || !dataFlowMap.ContainsKey(symbol))
            {
                return false;
            }

            var aliases = new HashSet<ISymbol> { target };
            aliases.UnionWith(DataFlowQuerying.GetAliases(target, targetSyntaxNode, targetCfgNode));

            foreach (var alias in aliases)
            {
                if (symbol.Equals(alias) &&
                    cfgNode.GetMethodSummary().DataFlowAnalysis.DoesReferenceResetUntilSyntaxNode(
                    symbol, targetSyntaxNode, targetCfgNode, syntaxNode, cfgNode))
                {
                    continue;
                }
                
                Dictionary<ISymbol, HashSet<ISymbol>> reachabilityMap = null;
                if (targetCfgNode.GetMethodSummary().DataFlowAnalysis.TryGetFieldReachabilityMapForSyntaxNode(
                    syntaxNode, cfgNode, out reachabilityMap) && reachabilityMap.ContainsKey(symbol))
                {
                    foreach (var field in reachabilityMap[symbol])
                    {
                        foreach (var reference in dataFlowMap[field])
                        {
                            dataFlowMap[symbol].Add(reference);
                        }
                    }
                }

                foreach (var reference in dataFlowMap[symbol])
                {
                    if (reference.Equals(alias))
                    {
                        return true;
                    }
                }

                if (dataFlowMap.ContainsKey(alias))
                {
                    foreach (var reference in dataFlowMap[alias])
                    {
                        if (!cfgNode.GetMethodSummary().DataFlowAnalysis.DoesReferenceResetUntilSyntaxNode(
                            symbol, targetSyntaxNode, targetCfgNode, syntaxNode, cfgNode))
                        {
                            if (reference.Equals(symbol))
                            {
                                return true;
                            }

                            foreach (var symbolRef in dataFlowMap[symbol])
                            {
                                if (reference.Equals(symbolRef))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given symbol resets
        /// in the control-flow graph node.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <returns>Boolean</returns>
        public static bool DoesResetInCFGNode(ISymbol symbol, SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            return cfgNode.GetMethodSummary().DataFlowAnalysis.DoesReferenceResetInCFGNode(
                symbol, syntaxNode, cfgNode);
        }

        /// <summary>
        /// Returns true if the given expression resets in
        /// successor control-flow graph nodes.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        public static bool DoesResetInSuccessorCFGNodes(ExpressionSyntax expr, ISymbol target,
            SyntaxNode syntaxNode, CFGNode cfgNode, SemanticModel model, AnalysisContext context)
        {
            IdentifierNameSyntax identifier = context.GetTopLevelIdentifier(expr);
            if (identifier == null)
            {
                return false;
            }

            ISymbol reference = model.GetSymbolInfo(identifier).Symbol;
            return DataFlowQuerying.DoesResetInSuccessorCFGNodes(reference, target, syntaxNode, cfgNode);
        }

        /// <summary>
        /// Returns true if the given symbol resets
        /// in successor control-flow graph nodes.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <returns>Boolean</returns>
        public static bool DoesResetInSuccessorCFGNodes(ISymbol symbol, ISymbol target,
            SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            return DataFlowQuerying.DoesSymbolResetInSuccessorCFGNodes(symbol, target, syntaxNode,
                cfgNode, cfgNode, new HashSet<CFGNode>());
        }

        /// <summary>
        /// Returns true if the given expression resets when flowing from the
        /// target in the given loop body control-flow graph nodes.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target CFGNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        public static bool DoesResetInLoop(ExpressionSyntax expr, SyntaxNode syntaxNode,
            CFGNode cfgNode, SyntaxNode targetSyntaxNode, CFGNode targetCfgNode,
            SemanticModel model, AnalysisContext context)
        {
            IdentifierNameSyntax identifier = context.GetTopLevelIdentifier(expr);
            if (!cfgNode.Equals(targetCfgNode) || identifier == null)
            {
                return false;
            }

            ISymbol reference = model.GetSymbolInfo(identifier).Symbol;
            return DataFlowQuerying.DoesResetInLoop(reference, syntaxNode, cfgNode,
                targetSyntaxNode, targetCfgNode);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns true if the given symbol resets
        /// in successor control-flow graph nodes.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <returns>Boolean</returns>
        private static bool DoesSymbolResetInSuccessorCFGNodes(ISymbol symbol, ISymbol target,
            SyntaxNode syntaxNode, CFGNode cfgNode, CFGNode targetCfgNode,
            HashSet<CFGNode> visited, bool track = false)
        {
            visited.Add(targetCfgNode);

            foreach (var node in targetCfgNode.SyntaxNodes)
            {
                if (track)
                {
                    if (targetCfgNode.GetMethodSummary().DataFlowAnalysis.DoesReferenceResetUntilSyntaxNode(
                        symbol, syntaxNode, cfgNode, node, targetCfgNode) &&
                        !DataFlowQuerying.FlowsIntoTarget(symbol, target, syntaxNode, cfgNode,
                        node, targetCfgNode))
                    {
                        return true;
                    }
                }

                if (node.Equals(syntaxNode))
                {
                    track = true;
                }
            }
            
            if (targetCfgNode.GetImmediateSuccessors().Count() == 0)
            {
                return false;
            }

            List<bool> results = new List<bool>();
            foreach (var successor in targetCfgNode.GetImmediateSuccessors().Where(v => !visited.Contains(v)))
            {
                results.Add(DataFlowQuerying.DoesSymbolResetInSuccessorCFGNodes(symbol, target,
                    syntaxNode, cfgNode, successor, visited, true));
            }

            if (results.Contains(true))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the given symbol resets when flowing from the
        /// target in the given loop body control-flow graph nodes.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="targetSyntaxNode">Target SyntaxNode</param>
        /// <param name="targetCfgNode">Target CFGNode</param>
        /// <returns>Boolean</returns>
        private static bool DoesResetInLoop(ISymbol symbol, SyntaxNode syntaxNode,
            CFGNode cfgNode, SyntaxNode targetSyntaxNode, CFGNode targetCfgNode)
        {
            if (cfgNode.GetImmediateSuccessors().Count() == 0)
            {
                return false;
            }

            var successor = cfgNode.GetImmediateSuccessors().First();
            while (!successor.IsLoopHeadNode)
            {
                if (!successor.IsLoopHeadNode &&
                    successor.GetImmediateSuccessors().Count() == 0)
                {
                    return false;
                }
                else
                {
                    successor = successor.GetImmediateSuccessors().First();
                }
            }

            var backwards = cfgNode.GetMethodSummary().DataFlowAnalysis.DoesReferenceResetUntilSyntaxNode(
                symbol, syntaxNode, cfgNode, successor.SyntaxNodes.First(), successor);
            var forwards = cfgNode.GetMethodSummary().DataFlowAnalysis.DoesReferenceResetUntilSyntaxNode(
                symbol, successor.SyntaxNodes.First(), successor, syntaxNode, cfgNode);

            return backwards || forwards;
        }

        /// <summary>
        /// Returns all possible aliases of the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <returns>Set of aliases</returns>
        private static HashSet<ISymbol> GetAliases(ISymbol symbol, SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            HashSet<ISymbol> aliases = new HashSet<ISymbol>();

            Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
            if (!cfgNode.GetMethodSummary().DataFlowAnalysis.TryGetDataFlowMapForSyntaxNode(syntaxNode,
                cfgNode, out dataFlowMap) || !dataFlowMap.ContainsKey(symbol))
            {
                return aliases;
            }

            foreach (var reference in dataFlowMap[symbol].Where(v => !v.Equals(symbol)))
            {
                aliases.Add(reference);
            }

            return aliases;
        }

        #endregion
    }
}
