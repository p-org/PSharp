//-----------------------------------------------------------------------
// <copyright file="DataFlowAnalysis.cs">
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
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A data flow analysis API.
    /// </summary>
    internal static class DataFlowAnalysis
    {
        #region public API

        /// <summary>
        /// Returns true if the given expression flows in the target.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        internal static bool FlowsIntoTarget(ExpressionSyntax expr, ISymbol target,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SyntaxNode targetSyntaxNode,
            ControlFlowGraphNode targetCfgNode, SemanticModel model, AnalysisContext context)
        {
            ISymbol reference = null;
            if (!context.TryGetSymbolFromExpression(out reference, expr, model))
            {
                return false;
            }

            return DataFlowAnalysis.FlowsIntoTarget(reference, target, syntaxNode,
                cfgNode, targetSyntaxNode, targetCfgNode);
        }

        /// <summary>
        /// Returns true if the given expression flows in the target.
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean</returns>
        internal static bool FlowsIntoTarget(VariableDeclaratorSyntax variable, ISymbol target,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SyntaxNode targetSyntaxNode,
            ControlFlowGraphNode targetCfgNode, SemanticModel model)
        {
            ISymbol reference = model.GetDeclaredSymbol(variable);
            return DataFlowAnalysis.FlowsIntoTarget(reference, target, syntaxNode,
                cfgNode, targetSyntaxNode, targetCfgNode);
        }

        /// <summary>
        /// Returns true if the given symbol flows into the target symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        internal static bool FlowsIntoTarget(ISymbol symbol, ISymbol target, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
            if (!targetCfgNode.Summary.DataFlowMap.TryGetMapForSyntaxNode(targetSyntaxNode,
                targetCfgNode, out dataFlowMap) || !dataFlowMap.ContainsKey(symbol))
            {
                return false;
            }

            if (symbol.Equals(target) && cfgNode.Summary.DataFlowMap.DoesSymbolReset(
                symbol, syntaxNode, cfgNode, targetSyntaxNode, targetCfgNode))
            {
                return false;
            }

            Dictionary<ISymbol, HashSet<ISymbol>> reachabilityMap = null;
            if (targetCfgNode.Summary.DataFlowMap.TryGetReachabilityMapForSyntaxNode(targetSyntaxNode,
                targetCfgNode, out reachabilityMap) && reachabilityMap.ContainsKey(symbol))
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
                    if (!cfgNode.Summary.DataFlowMap.DoesSymbolReset(symbol, syntaxNode,
                        cfgNode, targetSyntaxNode, targetCfgNode))
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
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        internal static bool FlowsFromTarget(ExpressionSyntax expr, ISymbol target,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SyntaxNode targetSyntaxNode,
            ControlFlowGraphNode targetCfgNode, SemanticModel model, AnalysisContext context)
        {

            ISymbol reference = null;
            if (!context.TryGetSymbolFromExpression(out reference, expr, model))
            {
                return false;
            }

            return DataFlowAnalysis.FlowsFromTarget(reference, target, syntaxNode,
                cfgNode, targetSyntaxNode, targetCfgNode);
        }

        /// <summary>
        /// Returns true if the given expression flows from the target.
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean</returns>
        internal static bool FlowsFromTarget(VariableDeclaratorSyntax variable, ISymbol target,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SyntaxNode targetSyntaxNode,
            ControlFlowGraphNode targetCfgNode, SemanticModel model)
        {
            ISymbol reference = model.GetDeclaredSymbol(variable);
            return DataFlowAnalysis.FlowsFromTarget(reference, target, syntaxNode,
                cfgNode, targetSyntaxNode, targetCfgNode);
        }

        /// <summary>
        /// Returns true if the given symbol flows from the target symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        internal static bool FlowsFromTarget(ISymbol symbol, ISymbol target, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
            if (!cfgNode.Summary.DataFlowMap.TryGetMapForSyntaxNode(syntaxNode,
                cfgNode, out dataFlowMap) || !dataFlowMap.ContainsKey(symbol))
            {
                return false;
            }

            if (symbol.Equals(target) && cfgNode.Summary.DataFlowMap.DoesSymbolReset(
                symbol, targetSyntaxNode, targetCfgNode, syntaxNode, cfgNode))
            {
                return false;
            }
            
            Dictionary<ISymbol, HashSet<ISymbol>> reachabilityMap = null;
            if (targetCfgNode.Summary.DataFlowMap.TryGetReachabilityMapForSyntaxNode(syntaxNode,
                cfgNode, out reachabilityMap) && reachabilityMap.ContainsKey(symbol))
            {
                foreach (var field in reachabilityMap[symbol])
                {
                    foreach (var reference in dataFlowMap[field])
                    {
                        dataFlowMap[symbol].Add(reference);
                    }
                }
            }

            var targetAliases = DataFlowAnalysis.GetAliases(target, targetSyntaxNode, targetCfgNode);
            if (targetAliases.Contains(symbol))
            {
                return false;
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
                    if (!cfgNode.Summary.DataFlowMap.DoesSymbolReset(symbol, targetSyntaxNode,
                        targetCfgNode, syntaxNode, cfgNode))
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
        /// Returns true if the given expression resets when flowing from the
        /// target in the given loop body control flow graph nodes. The given
        /// control flow graph nodes must be equal.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        internal static bool DoesResetInLoop(ExpressionSyntax expr, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, SyntaxNode targetSyntaxNode,
            ControlFlowGraphNode targetCfgNode, SemanticModel model, AnalysisContext context)
        {
            ISymbol reference = null;
            if (!cfgNode.Equals(targetCfgNode) ||
                !context.TryGetSymbolFromExpression(out reference, expr, model))
            {
                return false;
            }

            return DataFlowAnalysis.DoesResetInLoop(reference, syntaxNode, cfgNode,
                targetSyntaxNode, targetCfgNode);
        }

        /// <summary>
        /// Returns true if the given symbol resets when flowing from the
        /// target in the given loop body control flow graph nodes. The
        /// given control flow graph nodes must be equal.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        internal static bool DoesResetInLoop(ISymbol symbol, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, SyntaxNode targetSyntaxNode,
            ControlFlowGraphNode targetCfgNode)
        {
            if (cfgNode.ISuccessors.Count == 0)
            {
                return false;
            }

            var successor = cfgNode.ISuccessors.First();
            while (!successor.IsLoopHeadNode)
            {
                if (!successor.IsLoopHeadNode &&
                    successor.ISuccessors.Count == 0)
                {
                    return false;
                }
                else
                {
                    successor = successor.ISuccessors.First();
                }
            }

            var backwards = cfgNode.Summary.DataFlowMap.DoesSymbolReset(symbol,
                syntaxNode, cfgNode, successor.SyntaxNodes.First(), successor);
            var forwards = cfgNode.Summary.DataFlowMap.DoesSymbolReset(symbol,
                successor.SyntaxNodes.First(), successor, syntaxNode, cfgNode);

            return backwards || forwards;
        }

        /// <summary>
        /// Returns true if the given expression resets in successor
        /// control flow graph nodes.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        internal static bool DoesResetInSuccessors(ExpressionSyntax expr, ISymbol target,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SemanticModel model,
            AnalysisContext context)
        {
            ISymbol reference = null;
            if (!context.TryGetSymbolFromExpression(out reference, expr, model))
            {
                return false;
            }

            return DataFlowAnalysis.DoesResetInSuccessors(reference, target, syntaxNode, cfgNode);
        }

        /// <summary>
        /// Returns true if the given symbol resets in successor
        /// control flow graph nodes.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        internal static bool DoesResetInSuccessors(ISymbol symbol, ISymbol target,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            return DataFlowAnalysis.DoesResetInSuccessors(symbol, target, syntaxNode,
                cfgNode, cfgNode, new HashSet<ControlFlowGraphNode>());
        }

        /// <summary>
        /// Returns all possible aliases of the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <returns>Set of aliases</returns>
        internal static HashSet<ISymbol> GetAliases(ISymbol symbol, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            HashSet<ISymbol> aliases = new HashSet<ISymbol>();

            Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
            if (!cfgNode.Summary.DataFlowMap.TryGetMapForSyntaxNode(syntaxNode,
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

        /// <summary>
        /// Returns the data flow map for the given control flow graph.
        /// </summary>
        /// <param name="summary">MethodSummary</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>DataFlowMap</returns>
        internal static DataFlowMap AnalyseControlFlowGraph(MethodSummary summary, AnalysisContext context)
        {
            var dataFlowMap = new DataFlowMap();
            var model = context.Compilation.GetSemanticModel(summary.Method.SyntaxTree);

            foreach (var param in summary.Method.ParameterList.Parameters)
            {
                var declType = model.GetTypeInfo(param.Type).Type;
                if (context.IsTypeAllowedToBeSend(declType) ||
                    context.IsMachineType(declType, model))
                {
                    continue;
                }

                var paramSymbol = model.GetDeclaredSymbol(param);
                dataFlowMap.MapRefToSymbol(paramSymbol, paramSymbol, summary.Method.ParameterList,
                    summary.Node, false);
            }

            DataFlowAnalysis.AnalyseControlFlowGraphNode(summary.Node, summary.Node,
                summary.Method.ParameterList, dataFlowMap, model, context);

            return dataFlowMap;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Performs data flow analysis on the given control flow graph node.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="previousCfgNode">Previous controlFlowGraphNode</param>
        /// <param name="previousSyntaxNode">Previous syntaxNode</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        private static void AnalyseControlFlowGraphNode(ControlFlowGraphNode cfgNode, ControlFlowGraphNode previousCfgNode,
            SyntaxNode previousSyntaxNode, DataFlowMap dataFlowMap, SemanticModel model, AnalysisContext context)
        {
            if (!cfgNode.IsJumpNode && !cfgNode.IsLoopHeadNode)
            {
                foreach (var syntaxNode in cfgNode.SyntaxNodes)
                {
                    dataFlowMap.Transfer(previousSyntaxNode, previousCfgNode, syntaxNode, cfgNode);

                    var stmt = syntaxNode as StatementSyntax;
                    var localDecl = stmt.DescendantNodesAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                    var expr = stmt.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
                    var ret = stmt.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>().FirstOrDefault();

                    if (localDecl != null)
                    {
                        var varDecl = (stmt as LocalDeclarationStatementSyntax).Declaration;
                        foreach (var variable in varDecl.Variables)
                        {
                            if (variable.Initializer == null)
                            {
                                continue;
                            }

                            DataFlowAnalysis.TryCaptureParameterAccess(variable.Initializer.Value,
                                syntaxNode, cfgNode, dataFlowMap, model, context);
                            DataFlowAnalysis.TryCaptureFieldAccess(variable.Initializer.Value,
                                syntaxNode, cfgNode, dataFlowMap, model, context);

                            ITypeSymbol declType = null;
                            if (variable.Initializer.Value is LiteralExpressionSyntax &&
                                variable.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                declType = model.GetTypeInfo(varDecl.Type).Type;
                            }
                            else
                            {
                                declType = model.GetTypeInfo(variable.Initializer.Value).Type;
                            }

                            if (context.IsTypeAllowedToBeSend(declType) ||
                                context.IsMachineType(declType, model))
                            {
                                continue;
                            }

                            var declSymbol = model.GetDeclaredSymbol(variable);

                            if (variable.Initializer.Value is IdentifierNameSyntax ||
                                variable.Initializer.Value is MemberAccessExpressionSyntax)
                            {
                                ISymbol varSymbol = null;
                                if (variable.Initializer.Value is IdentifierNameSyntax)
                                {
                                    varSymbol = model.GetSymbolInfo(variable.Initializer.Value
                                        as IdentifierNameSyntax).Symbol;
                                }
                                else if (variable.Initializer.Value is MemberAccessExpressionSyntax)
                                {
                                    varSymbol = model.GetSymbolInfo((variable.Initializer.Value
                                        as MemberAccessExpressionSyntax).Name).Symbol;
                                }

                                dataFlowMap.MapRefToSymbol(varSymbol, declSymbol, syntaxNode, cfgNode);

                                HashSet<ITypeSymbol> objectTypes = null;
                                if (DataFlowAnalysis.ResolveObjectType(out objectTypes, varSymbol, syntaxNode,
                                    cfgNode, dataFlowMap))
                                {
                                    dataFlowMap.MapObjectTypesToSymbol(objectTypes, declSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowMap.EraseObjectTypesFromSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (variable.Initializer.Value is LiteralExpressionSyntax &&
                                variable.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                dataFlowMap.ResetSymbol(declSymbol, syntaxNode, cfgNode);
                            }
                            else if (variable.Initializer.Value is InvocationExpressionSyntax)
                            {
                                var invocation = variable.Initializer.Value as InvocationExpressionSyntax;
                                var summary = MethodSummary.TryGetSummary(invocation, model, context);
                                var reachableSymbols = DataFlowAnalysis.ResolveSideEffectsInCall(invocation,
                                    summary, syntaxNode, cfgNode, model, dataFlowMap);
                                var returnSymbols = DataFlowAnalysis.GetReturnSymbols(invocation, summary, model);

                                if (returnSymbols.Count == 0)
                                {
                                    dataFlowMap.ResetSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                                else if (returnSymbols.Contains(declSymbol))
                                {
                                    dataFlowMap.MapRefsToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode, false);
                                }
                                else
                                {
                                    dataFlowMap.MapRefsToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode);
                                }

                                if (reachableSymbols.Count > 0)
                                {
                                    dataFlowMap.MapReachableFieldsToSymbol(reachableSymbols, declSymbol,
                                        syntaxNode, cfgNode);
                                }

                                if (summary != null && summary.ReturnTypeSet.Count > 0)
                                {
                                    dataFlowMap.MapObjectTypesToSymbol(summary.ReturnTypeSet,
                                        declSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowMap.EraseObjectTypesFromSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (variable.Initializer.Value is ObjectCreationExpressionSyntax)
                            {
                                var objCreation = variable.Initializer.Value as ObjectCreationExpressionSyntax;
                                var summary = MethodSummary.TryGetSummary(objCreation, model, context);
                                var reachableSymbols = DataFlowAnalysis.ResolveSideEffectsInCall(objCreation,
                                    summary, syntaxNode, cfgNode, model, dataFlowMap);
                                var returnSymbols = DataFlowAnalysis.GetReturnSymbols(objCreation, summary, model);

                                if (returnSymbols.Count == 0)
                                {
                                    dataFlowMap.ResetSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                                else if (returnSymbols.Contains(declSymbol))
                                {
                                    dataFlowMap.MapRefsToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode, false);
                                }
                                else
                                {
                                    dataFlowMap.MapRefsToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode);
                                }

                                if (reachableSymbols.Count > 0)
                                {
                                    dataFlowMap.MapReachableFieldsToSymbol(reachableSymbols, declSymbol,
                                        syntaxNode, cfgNode);
                                }

                                var typeSymbol = model.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                                if (typeSymbol != null)
                                {
                                    dataFlowMap.MapObjectTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                                        declSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowMap.EraseObjectTypesFromSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                            }
                        }
                    }
                    else if (expr != null)
                    {
                        if (expr.Expression is BinaryExpressionSyntax)
                        {
                            var binaryExpr = expr.Expression as BinaryExpressionSyntax;

                            DataFlowAnalysis.TryCaptureParameterAccess(binaryExpr.Left,
                                syntaxNode, cfgNode, dataFlowMap, model, context);
                            DataFlowAnalysis.TryCaptureParameterAccess(binaryExpr.Right,
                                syntaxNode, cfgNode, dataFlowMap, model, context);
                            DataFlowAnalysis.TryCaptureFieldAccess(binaryExpr.Left,
                                syntaxNode, cfgNode, dataFlowMap, model, context);
                            DataFlowAnalysis.TryCaptureFieldAccess(binaryExpr.Right,
                                syntaxNode, cfgNode, dataFlowMap, model, context);

                            IdentifierNameSyntax lhs = null;
                            ISymbol lhsFieldSymbol = null;
                            if (binaryExpr.Left is IdentifierNameSyntax)
                            {
                                lhs = binaryExpr.Left as IdentifierNameSyntax;
                                var lhsType = model.GetTypeInfo(lhs).Type;
                                if (context.IsTypeAllowedToBeSend(lhsType) ||
                                    context.IsMachineType(lhsType, model))
                                {
                                    previousSyntaxNode = syntaxNode;
                                    previousCfgNode = cfgNode;
                                    continue;
                                }
                            }
                            else if (binaryExpr.Left is MemberAccessExpressionSyntax)
                            {
                                var name = (binaryExpr.Left as MemberAccessExpressionSyntax).Name;
                                var lhsType = model.GetTypeInfo(name).Type;
                                if (context.IsTypeAllowedToBeSend(lhsType) ||
                                    context.IsMachineType(lhsType, model))
                                {
                                    previousSyntaxNode = syntaxNode;
                                    previousCfgNode = cfgNode;
                                    continue;
                                }

                                lhs = context.GetFirstNonMachineIdentifier(binaryExpr.Left, model);
                                lhsFieldSymbol = model.GetSymbolInfo(name as IdentifierNameSyntax).Symbol;
                            }
                            else if (binaryExpr.Left is ElementAccessExpressionSyntax)
                            {
                                var memberAccess = (binaryExpr.Left as ElementAccessExpressionSyntax);
                                if (memberAccess.Expression is IdentifierNameSyntax)
                                {
                                    lhs = memberAccess.Expression as IdentifierNameSyntax;
                                    var lhsType = model.GetTypeInfo(lhs).Type;
                                    if (context.IsTypeAllowedToBeSend(lhsType) ||
                                        context.IsMachineType(lhsType, model))
                                    {
                                        previousSyntaxNode = syntaxNode;
                                        previousCfgNode = cfgNode;
                                        continue;
                                    }
                                }
                                else if (memberAccess.Expression is MemberAccessExpressionSyntax)
                                {
                                    var name = (memberAccess.Expression as MemberAccessExpressionSyntax).Name;
                                    var lhsType = model.GetTypeInfo(name).Type;
                                    if (context.IsTypeAllowedToBeSend(lhsType) ||
                                        context.IsMachineType(lhsType, model))
                                    {
                                        previousSyntaxNode = syntaxNode;
                                        previousCfgNode = cfgNode;
                                        continue;
                                    }

                                    lhs = context.GetFirstNonMachineIdentifier(memberAccess.Expression, model);
                                    lhsFieldSymbol = model.GetSymbolInfo(name as IdentifierNameSyntax).Symbol;
                                }
                            }

                            var leftSymbol = model.GetSymbolInfo(lhs).Symbol;

                            if (binaryExpr.Right is IdentifierNameSyntax ||
                                binaryExpr.Right is MemberAccessExpressionSyntax)
                            {
                                IdentifierNameSyntax rhs = null;
                                if (binaryExpr.Right is IdentifierNameSyntax)
                                {
                                    rhs = binaryExpr.Right as IdentifierNameSyntax;
                                }
                                else if (binaryExpr.Right is MemberAccessExpressionSyntax)
                                {
                                    rhs = context.GetFirstNonMachineIdentifier(binaryExpr.Right, model);
                                }

                                var rightSymbol = model.GetSymbolInfo(rhs).Symbol;
                                dataFlowMap.MapRefToSymbol(rightSymbol, leftSymbol, syntaxNode, cfgNode);
                                if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                {
                                    dataFlowMap.MapRefToSymbol(rightSymbol, lhsFieldSymbol, syntaxNode, cfgNode);
                                }

                                HashSet<ITypeSymbol> objectTypes = null;
                                if (DataFlowAnalysis.ResolveObjectType(out objectTypes, rightSymbol, syntaxNode,
                                    cfgNode, dataFlowMap))
                                {
                                    dataFlowMap.MapObjectTypesToSymbol(objectTypes, leftSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowMap.EraseObjectTypesFromSymbol(leftSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (binaryExpr.Right is LiteralExpressionSyntax &&
                                binaryExpr.Right.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                dataFlowMap.ResetSymbol(leftSymbol, syntaxNode, cfgNode);
                                if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                {
                                    dataFlowMap.ResetSymbol(lhsFieldSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (binaryExpr.Right is InvocationExpressionSyntax)
                            {
                                var invocation = binaryExpr.Right as InvocationExpressionSyntax;
                                var summary = MethodSummary.TryGetSummary(invocation, model, context);
                                var reachableSymbols = DataFlowAnalysis.ResolveSideEffectsInCall(invocation,
                                    summary, syntaxNode, cfgNode, model, dataFlowMap);
                                var returnSymbols = DataFlowAnalysis.GetReturnSymbols(invocation,
                                    summary, model);
                                DataFlowAnalysis.CheckForNonMappedFieldSymbol(invocation, cfgNode,
                                    dataFlowMap, model, context);

                                if (returnSymbols.Count == 0)
                                {
                                    dataFlowMap.ResetSymbol(leftSymbol, syntaxNode, cfgNode);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowMap.ResetSymbol(lhsFieldSymbol, syntaxNode, cfgNode);
                                    }
                                }
                                else if (returnSymbols.Contains(leftSymbol))
                                {
                                    dataFlowMap.MapRefsToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode, false);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowMap.MapRefsToSymbol(returnSymbols, lhsFieldSymbol,
                                            syntaxNode, cfgNode, false);
                                    }
                                }
                                else
                                {
                                    dataFlowMap.MapRefsToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowMap.MapRefsToSymbol(returnSymbols, lhsFieldSymbol,
                                            syntaxNode, cfgNode);
                                    }
                                }

                                if (lhsFieldSymbol != null && reachableSymbols.Count > 0)
                                {
                                    dataFlowMap.MapReachableFieldsToSymbol(reachableSymbols, lhsFieldSymbol,
                                        syntaxNode, cfgNode);
                                }

                                if (summary != null && summary.ReturnTypeSet.Count > 0)
                                {
                                    dataFlowMap.MapObjectTypesToSymbol(summary.ReturnTypeSet,
                                        leftSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowMap.EraseObjectTypesFromSymbol(leftSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (binaryExpr.Right is ObjectCreationExpressionSyntax)
                            {
                                var objCreation = binaryExpr.Right as ObjectCreationExpressionSyntax;
                                var summary = MethodSummary.TryGetSummary(objCreation, model, context);
                                var reachableSymbols = DataFlowAnalysis.ResolveSideEffectsInCall(objCreation,
                                    summary, syntaxNode, cfgNode, model, dataFlowMap);
                                var returnSymbols = DataFlowAnalysis.GetReturnSymbols(objCreation, summary, model);

                                if (returnSymbols.Count == 0)
                                {
                                    dataFlowMap.ResetSymbol(leftSymbol, syntaxNode, cfgNode);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowMap.ResetSymbol(lhsFieldSymbol, syntaxNode, cfgNode);
                                    }
                                }
                                else if (returnSymbols.Contains(leftSymbol))
                                {
                                    dataFlowMap.MapRefsToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode, false);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowMap.MapRefsToSymbol(returnSymbols, lhsFieldSymbol,
                                            syntaxNode, cfgNode, false);
                                    }
                                }
                                else
                                {
                                    dataFlowMap.MapRefsToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowMap.MapRefsToSymbol(returnSymbols, lhsFieldSymbol,
                                            syntaxNode, cfgNode);
                                    }
                                }

                                if (lhsFieldSymbol != null && reachableSymbols.Count > 0)
                                {
                                    dataFlowMap.MapReachableFieldsToSymbol(reachableSymbols, lhsFieldSymbol,
                                        syntaxNode, cfgNode);
                                }

                                var typeSymbol = model.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                                if (typeSymbol != null)
                                {
                                    dataFlowMap.MapObjectTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                                        leftSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowMap.EraseObjectTypesFromSymbol(leftSymbol, syntaxNode, cfgNode);
                                }
                            }
                        }
                        else if (expr.Expression is InvocationExpressionSyntax)
                        {
                            var invocation = expr.Expression as InvocationExpressionSyntax;
                            var summary = MethodSummary.TryGetSummary(invocation, model, context);
                            DataFlowAnalysis.ResolveSideEffectsInCall(invocation, summary,
                                syntaxNode, cfgNode, model, dataFlowMap);
                            DataFlowAnalysis.GetReturnSymbols(invocation, summary, model);
                            DataFlowAnalysis.CheckForNonMappedFieldSymbol(invocation, cfgNode,
                                dataFlowMap, model, context);
                        }
                    }
                    else if (ret != null)
                    {
                        HashSet<ISymbol> returnSymbols = null;
                        if (ret.Expression is IdentifierNameSyntax ||
                            ret.Expression is MemberAccessExpressionSyntax)
                        {
                            IdentifierNameSyntax rhs = null;
                            if (ret.Expression is IdentifierNameSyntax)
                            {
                                rhs = ret.Expression as IdentifierNameSyntax;
                            }
                            else if (ret.Expression is MemberAccessExpressionSyntax)
                            {
                                rhs = context.GetFirstNonMachineIdentifier(ret.Expression, model);
                            }

                            var rightSymbol = model.GetSymbolInfo(rhs).Symbol;
                            returnSymbols = new HashSet<ISymbol> { rightSymbol };

                            HashSet<ITypeSymbol> objectTypes = null;
                            if (DataFlowAnalysis.ResolveObjectType(out objectTypes, rightSymbol,
                                syntaxNode, cfgNode, dataFlowMap))
                            {
                                foreach (var objectType in objectTypes)
                                {
                                    cfgNode.Summary.ReturnTypeSet.Add(objectType);
                                }
                            }
                        }
                        else if (ret.Expression is InvocationExpressionSyntax)
                        {
                            var invocation = ret.Expression as InvocationExpressionSyntax;
                            var summary = MethodSummary.TryGetSummary(invocation, model, context);
                            DataFlowAnalysis.ResolveSideEffectsInCall(invocation, summary,
                                syntaxNode, cfgNode, model, dataFlowMap);
                            returnSymbols = DataFlowAnalysis.GetReturnSymbols(invocation, summary, model);

                            if (summary != null)
                            {
                                foreach (var objectType in summary.ReturnTypeSet)
                                {
                                    cfgNode.Summary.ReturnTypeSet.Add(objectType);
                                }
                            }
                        }
                        else if (ret.Expression is ObjectCreationExpressionSyntax)
                        {
                            var objCreation = ret.Expression as ObjectCreationExpressionSyntax;
                            var summary = MethodSummary.TryGetSummary(objCreation, model, context);
                            DataFlowAnalysis.ResolveSideEffectsInCall(objCreation, summary,
                                syntaxNode, cfgNode, model, dataFlowMap);
                            returnSymbols = DataFlowAnalysis.GetReturnSymbols(objCreation, summary, model);

                            var objectType = model.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                            if (objectType != null)
                            {
                                cfgNode.Summary.ReturnTypeSet.Add(objectType);
                            }
                        }

                        DataFlowAnalysis.TryCaptureReturnSymbols(returnSymbols, syntaxNode,
                            cfgNode, model, dataFlowMap);
                    }

                    previousSyntaxNode = syntaxNode;
                    previousCfgNode = cfgNode;
                }
            }
            else
            {
                dataFlowMap.Transfer(previousSyntaxNode, previousCfgNode, cfgNode.SyntaxNodes[0], cfgNode);
                previousSyntaxNode = cfgNode.SyntaxNodes[0];
                previousCfgNode = cfgNode;
            }

            foreach (var successor in cfgNode.ISuccessors)
            {
                if (DataFlowAnalysis.ReachedFixpoint(previousSyntaxNode, cfgNode, successor, dataFlowMap))
                {
                    continue;
                }

                DataFlowAnalysis.AnalyseControlFlowGraphNode(successor, cfgNode,
                    previousSyntaxNode, dataFlowMap, model, context);
            }
        }

        /// <summary>
        /// Checks if the data flow analysis reached a fixpoint regarding the given successor.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="successorCfgNode">Successor controlFlowGraphNode</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <returns>Boolean</returns>
        private static bool ReachedFixpoint(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            ControlFlowGraphNode successorCfgNode, DataFlowMap dataFlowMap)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> currentMap = null;
            if (!dataFlowMap.TryGetMapForSyntaxNode(syntaxNode, cfgNode, out currentMap))
            {
                return false;
            }

            Dictionary<ISymbol, HashSet<ISymbol>> successorMap = null;
            if (!dataFlowMap.TryGetMapForSyntaxNode(successorCfgNode.SyntaxNodes.First(),
                successorCfgNode, out successorMap))
            {
                return false;
            }

            foreach (var pair in currentMap)
            {
                if (!successorMap.ContainsKey(pair.Key) ||
                    !successorMap[pair.Key].SetEquals(pair.Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the return symbols fromt he given object creation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of return symbols</returns>
        private static HashSet<ISymbol> GetReturnSymbols(ObjectCreationExpressionSyntax call, MethodSummary summary,
            SemanticModel model)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            return summary.GetResolvedReturnSymbols(call.ArgumentList, model);
        }

        /// <summary>
        /// Returns the return symbols fromt he given invocation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of return symbols</returns>
        private static HashSet<ISymbol> GetReturnSymbols(InvocationExpressionSyntax call, MethodSummary summary,
            SemanticModel model)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            return summary.GetResolvedReturnSymbols(call.ArgumentList, model);
        }

        /// <summary>
        /// Checks if the gives up call has a field symbol argument that is not
        /// already mapped and, if yes, maps it.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        private static void CheckForNonMappedFieldSymbol(InvocationExpressionSyntax call,
            ControlFlowGraphNode cfgNode, DataFlowMap dataFlowMap, SemanticModel model,
            AnalysisContext context)
        {
            if (!cfgNode.IsGivesUpNode)
            {
                return;
            }

            List<MemberAccessExpressionSyntax> accesses;
            if (cfgNode.Summary.GivesUpSet.Count == 0 && call.Expression.DescendantNodesAndSelf().
                OfType<IdentifierNameSyntax>().Last().ToString().Equals("Send"))
            {
                accesses = call.ArgumentList.Arguments[1].DescendantNodesAndSelf().
                    OfType<MemberAccessExpressionSyntax>().ToList();
            }
            else
            {
                accesses = call.ArgumentList.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().ToList();
            }

            foreach (var access in accesses)
            {
                IdentifierNameSyntax id = context.GetFirstNonMachineIdentifier(access, model);
                if (id == null)
                {
                    continue;
                }

                var accessSymbol = model.GetSymbolInfo(id).Symbol;
                dataFlowMap.MapSymbolIfNotExists(accessSymbol, cfgNode.SyntaxNodes[0], cfgNode);
            }
        }

        /// <summary>
        /// Resolves the side effects from the given object creation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <returns>Set of reachable field symbols</returns>
        private static HashSet<ISymbol> ResolveSideEffectsInCall(ObjectCreationExpressionSyntax call, MethodSummary summary,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SemanticModel model, DataFlowMap dataFlowMap)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            HashSet<ISymbol> reachableFields = new HashSet<ISymbol>();
            var sideEffects = summary.GetResolvedSideEffects(call.ArgumentList, model);
            foreach (var sideEffect in sideEffects)
            {
                dataFlowMap.MapRefsToSymbol(sideEffect.Value, sideEffect.Key, syntaxNode, cfgNode);
                reachableFields.Add(sideEffect.Key);
            }

            foreach (var fieldAccess in summary.FieldAccessSet)
            {
                foreach (var access in fieldAccess.Value)
                {
                    if (cfgNode.Summary.FieldAccessSet.ContainsKey(fieldAccess.Key as IFieldSymbol))
                    {
                        cfgNode.Summary.FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
                    }
                    else
                    {
                        cfgNode.Summary.FieldAccessSet.Add(fieldAccess.Key as IFieldSymbol, new HashSet<SyntaxNode>());
                        cfgNode.Summary.FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
                    }
                }
            }

            return reachableFields;
        }

        /// <summary>
        /// Resolves the side effects from the given invocation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <returns>Set of reachable field symbols</returns>
        private static HashSet<ISymbol> ResolveSideEffectsInCall(InvocationExpressionSyntax call, MethodSummary summary,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SemanticModel model, DataFlowMap dataFlowMap)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            HashSet<ISymbol> reachableFields = new HashSet<ISymbol>();
            var sideEffects = summary.GetResolvedSideEffects(call.ArgumentList, model);
            foreach (var sideEffect in sideEffects)
            {
                dataFlowMap.MapRefsToSymbol(sideEffect.Value, sideEffect.Key, syntaxNode, cfgNode);
                reachableFields.Add(sideEffect.Key);
            }

            foreach (var fieldAccess in summary.FieldAccessSet)
            {
                foreach (var access in fieldAccess.Value)
                {
                    if (cfgNode.Summary.FieldAccessSet.ContainsKey(fieldAccess.Key as IFieldSymbol))
                    {
                        cfgNode.Summary.FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
                    }
                    else
                    {
                        cfgNode.Summary.FieldAccessSet.Add(fieldAccess.Key as IFieldSymbol, new HashSet<SyntaxNode>());
                        cfgNode.Summary.FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
                    }
                }
            }

            return reachableFields;
        }

        /// <summary>
        /// Resolves the object type of the given symbol on the given syntax node.
        /// </summary>
        /// <param name="types">Set of object type symbols</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <returns>Boolean</returns>
        private static bool ResolveObjectType(out HashSet<ITypeSymbol> types, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, DataFlowMap dataFlowMap)
        {
            types = new HashSet<ITypeSymbol>();

            Dictionary<ISymbol, HashSet<ITypeSymbol>> objectTypeMap = null;
            if (!dataFlowMap.TryGetObjectTypeMapForSyntaxNode(syntaxNode, cfgNode,
                out objectTypeMap))
            {
                return false;
            }

            if (!objectTypeMap.ContainsKey(symbol))
            {
                return false;
            }

            types = objectTypeMap[symbol];
            return true;
        }

        /// <summary>
        /// Tries to capture a potential parameter acccess in the given expression.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        private static void TryCaptureParameterAccess(ExpressionSyntax expr, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, DataFlowMap dataFlowMap, SemanticModel model,
            AnalysisContext context)
        {
            if (!(expr is MemberAccessExpressionSyntax))
            {
                return;
            }

            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = context.GetFirstNonMachineIdentifier(expr, model);
            if (identifier == null || name == null)
            {
                return;
            }

            var type = model.GetTypeInfo(identifier).Type;
            if (context.IsTypeAllowedToBeSend(type) ||
                context.IsMachineType(type, model) ||
                name.Equals(identifier))
            {
                return;
            }

            var symbol = model.GetSymbolInfo(identifier).Symbol;
            if (symbol == null)
            {
                return;
            }

            Dictionary<ISymbol, HashSet<ISymbol>> map = null;
            if (!dataFlowMap.TryGetMapForSyntaxNode(syntaxNode, cfgNode, out map))
            {
                return;
            }

            Dictionary<ISymbol, int> indexMap = new Dictionary<ISymbol, int>();
            var parameterList = cfgNode.Summary.Method.ParameterList.Parameters;
            for (int idx = 0; idx < parameterList.Count; idx++)
            {
                var paramSymbol = model.GetDeclaredSymbol(parameterList[idx]);
                indexMap.Add(paramSymbol, idx);
            }

            if (map.ContainsKey(symbol))
            {
                foreach (var reference in map[symbol])
                {
                    if (reference.Kind == SymbolKind.Parameter)
                    {
                        if (reference.Equals(symbol) && dataFlowMap.DoesSymbolReset(
                            reference, cfgNode.Summary.Node.SyntaxNodes.First(),
                            cfgNode.Summary.Node, syntaxNode, cfgNode, true))
                        {
                            continue;
                        }

                        int index = indexMap[reference];
                        if (cfgNode.Summary.AccessSet.ContainsKey(index))
                        {
                            cfgNode.Summary.AccessSet[index].Add(syntaxNode);
                        }
                        else
                        {
                            cfgNode.Summary.AccessSet.Add(index, new HashSet<SyntaxNode>());
                            cfgNode.Summary.AccessSet[index].Add(syntaxNode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to capture a potential field acccess in the given expression.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        private static void TryCaptureFieldAccess(ExpressionSyntax expr, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, DataFlowMap dataFlowMap, SemanticModel model,
            AnalysisContext context)
        {
            if (!(expr is MemberAccessExpressionSyntax))
            {
                return;
            }

            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = context.GetFirstNonMachineIdentifier(expr, model);
            if (identifier == null || name == null)
            {
                return;
            }

            var type = model.GetTypeInfo(identifier).Type;
            if (context.IsTypeAllowedToBeSend(type) ||
                context.IsMachineType(type, model) ||
                name.Equals(identifier))
            {
                return;
            }

            var symbol = model.GetSymbolInfo(identifier).Symbol;
            var definition = SymbolFinder.FindSourceDefinitionAsync(symbol,
                context.Solution).Result;
            if (!(definition is IFieldSymbol))
            {
                return;
            }
            
            var fieldDecl = definition.DeclaringSyntaxReferences.First().GetSyntax().
                AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();
            if (dataFlowMap.DoesSymbolReset(symbol, cfgNode.Summary.Node.SyntaxNodes.First(),
                cfgNode.Summary.Node, syntaxNode, cfgNode, true))
            {
                return;
            }

            if (cfgNode.Summary.FieldAccessSet.ContainsKey(symbol as IFieldSymbol))
            {
                cfgNode.Summary.FieldAccessSet[symbol as IFieldSymbol].Add(syntaxNode);
            }
            else
            {
                cfgNode.Summary.FieldAccessSet.Add(symbol as IFieldSymbol, new HashSet<SyntaxNode>());
                cfgNode.Summary.FieldAccessSet[symbol as IFieldSymbol].Add(syntaxNode);
            }
        }

        /// <summary>
        /// Tries to capture potential return symbols.
        /// </summary>
        /// <param name="returnSymbols">Set of return symbols</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        private static void TryCaptureReturnSymbols(HashSet<ISymbol> returnSymbols, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, SemanticModel model, DataFlowMap dataFlowMap)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> map = null;
            if (returnSymbols == null ||
                !dataFlowMap.TryGetMapForSyntaxNode(syntaxNode, cfgNode, out map))
            {
                return;
            }

            Dictionary<ISymbol, int> indexMap = new Dictionary<ISymbol, int>();
            var parameterList = cfgNode.Summary.Method.ParameterList.Parameters;
            for (int idx = 0; idx < parameterList.Count; idx++)
            {
                var paramSymbol = model.GetDeclaredSymbol(parameterList[idx]);
                indexMap.Add(paramSymbol, idx);
            }

            foreach (var symbol in returnSymbols)
            {
                if (symbol.Kind == SymbolKind.Parameter)
                {
                    if (dataFlowMap.DoesSymbolReset(symbol, cfgNode.Summary.Node.SyntaxNodes.
                        First(), cfgNode.Summary.Node, syntaxNode, cfgNode, true))
                    {
                        continue;
                    }

                    cfgNode.Summary.ReturnSet.Item1.Add(indexMap[symbol]);
                }
                else if (symbol.Kind == SymbolKind.Field)
                {
                    cfgNode.Summary.ReturnSet.Item2.Add(symbol as IFieldSymbol);
                }
                else if (map.ContainsKey(symbol))
                {
                    foreach (var reference in map[symbol].Where(v => v.Kind == SymbolKind.Field))
                    {
                        cfgNode.Summary.ReturnSet.Item2.Add(reference as IFieldSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the given symbol resets in successor
        /// control flow graph nodes.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <returns>Boolean</returns>
        private static bool DoesResetInSuccessors(ISymbol symbol, ISymbol target,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, ControlFlowGraphNode targetCfgNode,
            HashSet<ControlFlowGraphNode> visited, bool track = false)
        {
            visited.Add(targetCfgNode);

            foreach (var node in targetCfgNode.SyntaxNodes)
            {
                if (track)
                {
                    if (targetCfgNode.Summary.DataFlowMap.DoesSymbolReset(symbol,
                        syntaxNode, cfgNode, node, targetCfgNode) &&
                        !DataFlowAnalysis.FlowsIntoTarget(symbol, target, syntaxNode, cfgNode,
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
            
            if (targetCfgNode.ISuccessors.Count == 0)
            {
                return false;
            }

            List<bool> results = new List<bool>();
            foreach (var successor in targetCfgNode.ISuccessors.Where(v => !visited.Contains(v)))
            {
                results.Add(DataFlowAnalysis.DoesResetInSuccessors(symbol, target,
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

        #endregion
    }
}
