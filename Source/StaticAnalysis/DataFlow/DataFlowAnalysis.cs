//-----------------------------------------------------------------------
// <copyright file="DataFlowAnalysis.cs">
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
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Class implementing data-flow analysis.
    /// </summary>
    internal class DataFlowAnalysis
    {
        #region fields

        /// <summary>
        /// DataFlowMap containing data-flow values.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ISymbol>>>> DataFlowMap;

        /// <summary>
        /// DataFlowMap containing object reachability values.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ISymbol>>>> ReachabilityMap;

        /// <summary>
        /// DataFlowMap containing reference types.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ITypeSymbol>>>> ReferenceTypeMap;

        /// <summary>
        /// DataFlowMap containing reference resets.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            HashSet<ISymbol>>> ReferenceResetMap;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        internal DataFlowAnalysis()
        {
            this.DataFlowMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ISymbol>>>>();
            this.ReachabilityMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ISymbol>>>>();
            this.ReferenceTypeMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ITypeSymbol>>>>();
            this.ReferenceResetMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                HashSet<ISymbol>>>();
        }

        #endregion

        #region public analysis API

        /// <summary>
        /// Analyzes the data-flow analysis on the given control flow graph node.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="previousCfgNode">Previous controlFlowGraphNode</param>
        /// <param name="previousSyntaxNode">Previous syntaxNode</param>
        /// <param name="dataFlowAnalysis">DataFlowAnalysis</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        public void Analyze(ControlFlowGraphNode cfgNode, ControlFlowGraphNode previousCfgNode,
            SyntaxNode previousSyntaxNode, DataFlowAnalysis dataFlowAnalysis, SemanticModel model,
            AnalysisContext context)
        {
            if (!cfgNode.IsJumpNode && !cfgNode.IsLoopHeadNode)
            {
                foreach (var syntaxNode in cfgNode.SyntaxNodes)
                {
                    this.Transfer(previousSyntaxNode, previousCfgNode, syntaxNode, cfgNode);

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

                            this.TryCaptureParameterAccess(variable.Initializer.Value, syntaxNode,
                                cfgNode, model, context);
                            this.TryCaptureFieldAccess(variable.Initializer.Value, syntaxNode,
                                cfgNode, model, context);

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
                                context.IsMachineIdType(declType, model))
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

                                dataFlowAnalysis.MapRefToSymbol(varSymbol, declSymbol, syntaxNode, cfgNode);

                                HashSet<ITypeSymbol> referenceTypes = null;
                                if (this.ResolveReferenceType(out referenceTypes, varSymbol, syntaxNode, cfgNode))
                                {
                                    dataFlowAnalysis.MapReferenceTypesToSymbol(referenceTypes, declSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowAnalysis.EraseReferenceTypesFromSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (variable.Initializer.Value is LiteralExpressionSyntax &&
                                variable.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                dataFlowAnalysis.ResetSymbol(declSymbol, syntaxNode, cfgNode);
                            }
                            else if (variable.Initializer.Value is InvocationExpressionSyntax)
                            {
                                var invocation = variable.Initializer.Value as InvocationExpressionSyntax;
                                var summary = MethodSummary.TryGetSummary(invocation, model, context);
                                var reachableSymbols = this.ResolveSideEffectsInCall(invocation,
                                    summary, syntaxNode, cfgNode, model);
                                var returnSymbols = this.GetReturnSymbols(invocation, summary, model);

                                if (returnSymbols.Count == 0)
                                {
                                    dataFlowAnalysis.ResetSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                                else if (returnSymbols.Contains(declSymbol))
                                {
                                    dataFlowAnalysis.MapRefsToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode, false);
                                }
                                else
                                {
                                    dataFlowAnalysis.MapRefsToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode);
                                }

                                if (reachableSymbols.Count > 0)
                                {
                                    dataFlowAnalysis.MapReachableFieldsToSymbol(reachableSymbols, declSymbol,
                                        syntaxNode, cfgNode);
                                }

                                if (summary != null && summary.ReturnTypeSet.Count > 0)
                                {
                                    dataFlowAnalysis.MapReferenceTypesToSymbol(summary.ReturnTypeSet,
                                        declSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowAnalysis.EraseReferenceTypesFromSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (variable.Initializer.Value is ObjectCreationExpressionSyntax)
                            {
                                var objCreation = variable.Initializer.Value as ObjectCreationExpressionSyntax;
                                var summary = MethodSummary.TryGetSummary(objCreation, model, context);
                                var reachableSymbols = this.ResolveSideEffectsInCall(objCreation,
                                    summary, syntaxNode, cfgNode, model);
                                var returnSymbols = this.GetReturnSymbols(objCreation, summary, model);

                                if (returnSymbols.Count == 0)
                                {
                                    dataFlowAnalysis.ResetSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                                else if (returnSymbols.Contains(declSymbol))
                                {
                                    dataFlowAnalysis.MapRefsToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode, false);
                                }
                                else
                                {
                                    dataFlowAnalysis.MapRefsToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode);
                                }

                                if (reachableSymbols.Count > 0)
                                {
                                    dataFlowAnalysis.MapReachableFieldsToSymbol(reachableSymbols, declSymbol,
                                        syntaxNode, cfgNode);
                                }

                                var typeSymbol = model.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                                if (typeSymbol != null)
                                {
                                    dataFlowAnalysis.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                                        declSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowAnalysis.EraseReferenceTypesFromSymbol(declSymbol, syntaxNode, cfgNode);
                                }
                            }
                        }
                    }
                    else if (expr != null)
                    {
                        if (expr.Expression is BinaryExpressionSyntax)
                        {
                            var binaryExpr = expr.Expression as BinaryExpressionSyntax;

                            this.TryCaptureParameterAccess(binaryExpr.Left, syntaxNode, cfgNode, model, context);
                            this.TryCaptureParameterAccess(binaryExpr.Right, syntaxNode, cfgNode, model, context);
                            this.TryCaptureFieldAccess(binaryExpr.Left, syntaxNode, cfgNode, model, context);
                            this.TryCaptureFieldAccess(binaryExpr.Right, syntaxNode, cfgNode, model, context);

                            IdentifierNameSyntax lhs = null;
                            ISymbol lhsFieldSymbol = null;
                            if (binaryExpr.Left is IdentifierNameSyntax)
                            {
                                lhs = binaryExpr.Left as IdentifierNameSyntax;
                                var lhsType = model.GetTypeInfo(lhs).Type;
                                if (context.IsTypeAllowedToBeSend(lhsType) ||
                                    context.IsMachineIdType(lhsType, model))
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
                                    context.IsMachineIdType(lhsType, model))
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
                                        context.IsMachineIdType(lhsType, model))
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
                                        context.IsMachineIdType(lhsType, model))
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
                                dataFlowAnalysis.MapRefToSymbol(rightSymbol, leftSymbol, syntaxNode, cfgNode);
                                if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                {
                                    dataFlowAnalysis.MapRefToSymbol(rightSymbol, lhsFieldSymbol, syntaxNode, cfgNode);
                                }

                                HashSet<ITypeSymbol> referenceTypes = null;
                                if (this.ResolveReferenceType(out referenceTypes, rightSymbol, syntaxNode, cfgNode))
                                {
                                    dataFlowAnalysis.MapReferenceTypesToSymbol(referenceTypes, leftSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowAnalysis.EraseReferenceTypesFromSymbol(leftSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (binaryExpr.Right is LiteralExpressionSyntax &&
                                binaryExpr.Right.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                dataFlowAnalysis.ResetSymbol(leftSymbol, syntaxNode, cfgNode);
                                if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                {
                                    dataFlowAnalysis.ResetSymbol(lhsFieldSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (binaryExpr.Right is InvocationExpressionSyntax)
                            {
                                var invocation = binaryExpr.Right as InvocationExpressionSyntax;
                                var summary = MethodSummary.TryGetSummary(invocation, model, context);
                                var reachableSymbols = this.ResolveSideEffectsInCall(invocation,
                                    summary, syntaxNode, cfgNode, model);
                                var returnSymbols = this.GetReturnSymbols(invocation,
                                    summary, model);
                                this.CheckForNonMappedFieldSymbol(invocation, cfgNode, model, context);

                                if (returnSymbols.Count == 0)
                                {
                                    dataFlowAnalysis.ResetSymbol(leftSymbol, syntaxNode, cfgNode);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowAnalysis.ResetSymbol(lhsFieldSymbol, syntaxNode, cfgNode);
                                    }
                                }
                                else if (returnSymbols.Contains(leftSymbol))
                                {
                                    dataFlowAnalysis.MapRefsToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode, false);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowAnalysis.MapRefsToSymbol(returnSymbols, lhsFieldSymbol,
                                            syntaxNode, cfgNode, false);
                                    }
                                }
                                else
                                {
                                    dataFlowAnalysis.MapRefsToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowAnalysis.MapRefsToSymbol(returnSymbols, lhsFieldSymbol,
                                            syntaxNode, cfgNode);
                                    }
                                }

                                if (lhsFieldSymbol != null && reachableSymbols.Count > 0)
                                {
                                    dataFlowAnalysis.MapReachableFieldsToSymbol(reachableSymbols, lhsFieldSymbol,
                                        syntaxNode, cfgNode);
                                }

                                if (summary != null && summary.ReturnTypeSet.Count > 0)
                                {
                                    dataFlowAnalysis.MapReferenceTypesToSymbol(summary.ReturnTypeSet,
                                        leftSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowAnalysis.EraseReferenceTypesFromSymbol(leftSymbol, syntaxNode, cfgNode);
                                }
                            }
                            else if (binaryExpr.Right is ObjectCreationExpressionSyntax)
                            {
                                var objCreation = binaryExpr.Right as ObjectCreationExpressionSyntax;
                                var summary = MethodSummary.TryGetSummary(objCreation, model, context);
                                var reachableSymbols = this.ResolveSideEffectsInCall(objCreation,
                                    summary, syntaxNode, cfgNode, model);
                                var returnSymbols = this.GetReturnSymbols(objCreation, summary, model);

                                if (returnSymbols.Count == 0)
                                {
                                    dataFlowAnalysis.ResetSymbol(leftSymbol, syntaxNode, cfgNode);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowAnalysis.ResetSymbol(lhsFieldSymbol, syntaxNode, cfgNode);
                                    }
                                }
                                else if (returnSymbols.Contains(leftSymbol))
                                {
                                    dataFlowAnalysis.MapRefsToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode, false);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowAnalysis.MapRefsToSymbol(returnSymbols, lhsFieldSymbol,
                                            syntaxNode, cfgNode, false);
                                    }
                                }
                                else
                                {
                                    dataFlowAnalysis.MapRefsToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                                    {
                                        dataFlowAnalysis.MapRefsToSymbol(returnSymbols, lhsFieldSymbol,
                                            syntaxNode, cfgNode);
                                    }
                                }

                                if (lhsFieldSymbol != null && reachableSymbols.Count > 0)
                                {
                                    dataFlowAnalysis.MapReachableFieldsToSymbol(reachableSymbols, lhsFieldSymbol,
                                        syntaxNode, cfgNode);
                                }

                                var typeSymbol = model.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                                if (typeSymbol != null)
                                {
                                    dataFlowAnalysis.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                                        leftSymbol, syntaxNode, cfgNode);
                                }
                                else
                                {
                                    dataFlowAnalysis.EraseReferenceTypesFromSymbol(leftSymbol, syntaxNode, cfgNode);
                                }
                            }
                        }
                        else if (expr.Expression is InvocationExpressionSyntax)
                        {
                            var invocation = expr.Expression as InvocationExpressionSyntax;
                            var summary = MethodSummary.TryGetSummary(invocation, model, context);
                            this.ResolveSideEffectsInCall(invocation, summary, syntaxNode, cfgNode, model);
                            this.GetReturnSymbols(invocation, summary, model);
                            this.CheckForNonMappedFieldSymbol(invocation, cfgNode, model, context);
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

                            HashSet<ITypeSymbol> referenceTypes = null;
                            if (this.ResolveReferenceType(out referenceTypes, rightSymbol, syntaxNode, cfgNode))
                            {
                                foreach (var referenceType in referenceTypes)
                                {
                                    cfgNode.Summary.ReturnTypeSet.Add(referenceType);
                                }
                            }
                        }
                        else if (ret.Expression is InvocationExpressionSyntax)
                        {
                            var invocation = ret.Expression as InvocationExpressionSyntax;
                            var summary = MethodSummary.TryGetSummary(invocation, model, context);
                            this.ResolveSideEffectsInCall(invocation, summary, syntaxNode, cfgNode, model);
                            returnSymbols = this.GetReturnSymbols(invocation, summary, model);

                            if (summary != null)
                            {
                                foreach (var referenceType in summary.ReturnTypeSet)
                                {
                                    cfgNode.Summary.ReturnTypeSet.Add(referenceType);
                                }
                            }
                        }
                        else if (ret.Expression is ObjectCreationExpressionSyntax)
                        {
                            var objCreation = ret.Expression as ObjectCreationExpressionSyntax;
                            var summary = MethodSummary.TryGetSummary(objCreation, model, context);
                            this.ResolveSideEffectsInCall(objCreation, summary, syntaxNode, cfgNode, model);
                            returnSymbols = this.GetReturnSymbols(objCreation, summary, model);

                            var referenceType = model.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                            if (referenceType != null)
                            {
                                cfgNode.Summary.ReturnTypeSet.Add(referenceType);
                            }
                        }

                        this.TryCaptureReturnSymbols(returnSymbols, syntaxNode, cfgNode, model);
                    }

                    previousSyntaxNode = syntaxNode;
                    previousCfgNode = cfgNode;
                }
            }
            else
            {
                dataFlowAnalysis.Transfer(previousSyntaxNode, previousCfgNode, cfgNode.SyntaxNodes[0], cfgNode);
                previousSyntaxNode = cfgNode.SyntaxNodes[0];
                previousCfgNode = cfgNode;
            }

            foreach (var successor in cfgNode.ISuccessors)
            {
                if (this.ReachedFixpoint(previousSyntaxNode, cfgNode, successor))
                {
                    continue;
                }

                this.Analyze(successor, cfgNode, previousSyntaxNode, dataFlowAnalysis, model, context);
            }
        }

        #endregion

        #region data-flow update methods

        /// <summary>
        /// Transfers the data-flow map from the previous node to the new node.
        /// </summary>
        /// <param name="previousSyntaxNode">Previous syntaxNode</param>
        /// <param name="previousCfgNode">Previous cfgNode</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void Transfer(SyntaxNode previousSyntaxNode, ControlFlowGraphNode previousCfgNode,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.DataFlowMap.ContainsKey(cfgNode))
            {
                this.DataFlowMap.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            Dictionary<ISymbol, HashSet<ISymbol>> previousMap = null;
            if (this.TryGetDataFlowMapForSyntaxNode(previousSyntaxNode, previousCfgNode, out previousMap))
            {
                foreach (var pair in previousMap)
                {
                    if (!this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(pair.Key))
                    {
                        this.DataFlowMap[cfgNode][syntaxNode].Add(pair.Key, new HashSet<ISymbol>());
                    }

                    foreach (var reference in pair.Value)
                    {
                        this.DataFlowMap[cfgNode][syntaxNode][pair.Key].Add(reference);
                    }
                }
            }

            if (!this.ReachabilityMap.ContainsKey(cfgNode))
            {
                this.ReachabilityMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.ReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.ReachabilityMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            Dictionary<ISymbol, HashSet<ISymbol>> previousReachabilityMap = null;
            if (this.TryGetReachabilityMapForSyntaxNode(previousSyntaxNode, previousCfgNode,
                out previousReachabilityMap))
            {
                foreach (var pair in previousReachabilityMap)
                {
                    if (!this.ReachabilityMap[cfgNode][syntaxNode].ContainsKey(pair.Key))
                    {
                        this.ReachabilityMap[cfgNode][syntaxNode].Add(pair.Key, new HashSet<ISymbol>());
                    }

                    foreach (var reference in pair.Value)
                    {
                        this.ReachabilityMap[cfgNode][syntaxNode][pair.Key].Add(reference);
                    }
                }
            }

            if (!this.ReferenceTypeMap.ContainsKey(cfgNode))
            {
                this.ReferenceTypeMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ITypeSymbol>>>());
                this.ReferenceTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ITypeSymbol>>());
            }
            else if (!this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReferenceTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ITypeSymbol>>());
            }

            Dictionary<ISymbol, HashSet<ITypeSymbol>> previousObjectTypeMap = null;
            if (this.TryGetReferenceTypeMapForSyntaxNode(previousSyntaxNode, previousCfgNode,
                out previousObjectTypeMap))
            {
                foreach (var pair in previousObjectTypeMap)
                {
                    if (!this.ReferenceTypeMap[cfgNode][syntaxNode].ContainsKey(pair.Key))
                    {
                        this.ReferenceTypeMap[cfgNode][syntaxNode].Add(pair.Key, new HashSet<ITypeSymbol>());
                    }

                    foreach (var type in pair.Value)
                    {
                        this.ReferenceTypeMap[cfgNode][syntaxNode][pair.Key].Add(type);
                    }
                }
            }
        }

        /// <summary>
        /// Resets the references of the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void ResetSymbol(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.DataFlowMap.ContainsKey(cfgNode))
            {
                this.DataFlowMap.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            if (this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol] = new HashSet<ISymbol> { symbol };
            }
            else
            {
                this.DataFlowMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol> { symbol });
            }

            this.MarkSymbolReassignment(symbol, syntaxNode, cfgNode);
        }

        /// <summary>
        /// Maps the given symbol if its not already mapped.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void MapSymbolIfNotExists(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.DataFlowMap.ContainsKey(cfgNode))
            {
                this.DataFlowMap.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            if (!this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol] = new HashSet<ISymbol> { symbol };
            }
        }

        /// <summary>
        /// Maps the given reference to the given symbol.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        /// <param name="markReset">Should mark symbol as reset</param>
        internal void MapRefToSymbol(ISymbol reference, ISymbol symbol, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, bool markReset = true)
        {
            if (!this.DataFlowMap.ContainsKey(cfgNode))
            {
                this.DataFlowMap.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            HashSet<ISymbol> additionalRefs = new HashSet<ISymbol>();
            if (this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(reference))
            {
                foreach (var r in this.DataFlowMap[cfgNode][syntaxNode][reference])
                {
                    if (!reference.Equals(r))
                    {
                        additionalRefs.Add(r);
                    }
                }
            }

            if (this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(symbol) && additionalRefs.Count > 0)
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol] = new HashSet<ISymbol>();
            }
            else if (additionalRefs.Count > 0)
            {
                this.DataFlowMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
            }
            else if (this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol] = new HashSet<ISymbol> { reference };
            }
            else
            {
                this.DataFlowMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol> { reference });
            }

            foreach (var r in additionalRefs)
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol].Add(r);
            }

            if (markReset)
            {
                this.MarkSymbolReassignment(symbol, syntaxNode, cfgNode);
            }
        }

        /// <summary>
        /// Maps the given set of references to the given symbol.
        /// </summary>
        /// <param name="references">Set of references</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        /// <param name="markReset">Should mark symbol as reset</param>
        private void MapRefsToSymbol(HashSet<ISymbol> references, ISymbol symbol, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, bool markReset = true)
        {
            if (!this.DataFlowMap.ContainsKey(cfgNode))
            {
                this.DataFlowMap.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            HashSet<ISymbol> additionalRefs = new HashSet<ISymbol>();
            foreach (var reference in references)
            {
                if (this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(reference))
                {
                    foreach (var r in this.DataFlowMap[cfgNode][syntaxNode][reference])
                    {
                        if (!reference.Equals(r))
                        {
                            additionalRefs.Add(r);
                        }
                    }
                }
            }

            if (this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(symbol) && additionalRefs.Count > 0)
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol].Clear();
            }
            else if (additionalRefs.Count > 0)
            {
                this.DataFlowMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
            }
            else if (this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol].Clear();
                foreach (var reference in references)
                {
                    this.DataFlowMap[cfgNode][syntaxNode][symbol].Add(reference);
                }
            }
            else
            {
                this.DataFlowMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
                foreach (var reference in references)
                {
                    this.DataFlowMap[cfgNode][syntaxNode][symbol].Add(reference);
                }
            }

            foreach (var r in additionalRefs)
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol].Add(r);
            }

            if (markReset)
            {
                this.MarkSymbolReassignment(symbol, syntaxNode, cfgNode);
            }
        }

        /// <summary>
        /// Maps the given set of reachable field symbols to the given symbol.
        /// </summary>
        /// <param name="fields">Set of field symbols</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void MapReachableFieldsToSymbol(HashSet<ISymbol> fields, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.ReachabilityMap.ContainsKey(cfgNode))
            {
                this.ReachabilityMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.ReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ISymbol>>());
            }
            else if (!this.ReachabilityMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ISymbol>>());
            }

            if (this.ReachabilityMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ReachabilityMap[cfgNode][syntaxNode][symbol].Clear();
                foreach (var reference in fields)
                {
                    this.ReachabilityMap[cfgNode][syntaxNode][symbol].Add(reference);
                }
            }
            else
            {
                this.ReachabilityMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
                foreach (var reference in fields)
                {
                    this.ReachabilityMap[cfgNode][syntaxNode][symbol].Add(reference);
                }
            }
        }

        /// <summary>
        /// Maps the given set of reference types to the given symbol.
        /// </summary>
        /// <param name="types">Set of reference types</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void MapReferenceTypesToSymbol(HashSet<ITypeSymbol> types, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.ReferenceTypeMap.ContainsKey(cfgNode))
            {
                this.ReferenceTypeMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ITypeSymbol>>>());
                this.ReferenceTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ITypeSymbol>>());
            }
            else if (!this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReferenceTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ITypeSymbol>>());
            }

            if (this.ReferenceTypeMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ReferenceTypeMap[cfgNode][syntaxNode][symbol].Clear();
                foreach (var type in types)
                {
                    this.ReferenceTypeMap[cfgNode][syntaxNode][symbol].Add(type);
                }
            }
            else
            {
                this.ReferenceTypeMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ITypeSymbol>());
                foreach (var type in types)
                {
                    this.ReferenceTypeMap[cfgNode][syntaxNode][symbol].Add(type);
                }
            }
        }

        /// <summary>
        /// Erases the set of reference types from the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void EraseReferenceTypesFromSymbol(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (this.ReferenceTypeMap.ContainsKey(cfgNode) &&
                this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode) &&
                this.ReferenceTypeMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ReferenceTypeMap[cfgNode][syntaxNode].Remove(symbol);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Resolves the side effects from the given object creation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of reachable field symbols</returns>
        private HashSet<ISymbol> ResolveSideEffectsInCall(ObjectCreationExpressionSyntax call, MethodSummary summary,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SemanticModel model)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            HashSet<ISymbol> reachableFields = new HashSet<ISymbol>();
            var sideEffects = summary.GetResolvedSideEffects(call.ArgumentList, model);
            foreach (var sideEffect in sideEffects)
            {
                this.MapRefsToSymbol(sideEffect.Value, sideEffect.Key, syntaxNode, cfgNode);
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
        /// <returns>Set of reachable field symbols</returns>
        private HashSet<ISymbol> ResolveSideEffectsInCall(InvocationExpressionSyntax call, MethodSummary summary,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, SemanticModel model)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            HashSet<ISymbol> reachableFields = new HashSet<ISymbol>();
            var sideEffects = summary.GetResolvedSideEffects(call.ArgumentList, model);
            foreach (var sideEffect in sideEffects)
            {
                this.MapRefsToSymbol(sideEffect.Value, sideEffect.Key, syntaxNode, cfgNode);
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
        /// Resolves the reference type of the given symbol on the given syntax node.
        /// </summary>
        /// <param name="types">Set of reference type symbols</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        private bool ResolveReferenceType(out HashSet<ITypeSymbol> types, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            types = new HashSet<ITypeSymbol>();

            Dictionary<ISymbol, HashSet<ITypeSymbol>> referenceTypeMap = null;
            if (!this.TryGetReferenceTypeMapForSyntaxNode(syntaxNode, cfgNode,
                out referenceTypeMap))
            {
                return false;
            }

            if (!referenceTypeMap.ContainsKey(symbol))
            {
                return false;
            }

            types = referenceTypeMap[symbol];
            return true;
        }

        /// <summary>
        /// Tries to capture a potential parameter acccess in the given expression.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        private void TryCaptureParameterAccess(ExpressionSyntax expr, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, SemanticModel model,
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
                context.IsMachineIdType(type, model) ||
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
            if (!this.TryGetDataFlowMapForSyntaxNode(syntaxNode, cfgNode, out map))
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
                        if (reference.Equals(symbol) && this.DoesSymbolReset(reference,
                            cfgNode.Summary.EntryNode.SyntaxNodes.First(),
                            cfgNode.Summary.EntryNode, syntaxNode, cfgNode, true))
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
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        private void TryCaptureFieldAccess(ExpressionSyntax expr, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, SemanticModel model, AnalysisContext context)
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
                context.IsMachineIdType(type, model) ||
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
            if (this.DoesSymbolReset(symbol, cfgNode.Summary.EntryNode.SyntaxNodes.First(),
                cfgNode.Summary.EntryNode, syntaxNode, cfgNode, true))
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
        private void TryCaptureReturnSymbols(HashSet<ISymbol> returnSymbols, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, SemanticModel model)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> map = null;
            if (returnSymbols == null ||
                !this.TryGetDataFlowMapForSyntaxNode(syntaxNode, cfgNode, out map))
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
                    if (this.DoesSymbolReset(symbol, cfgNode.Summary.EntryNode.SyntaxNodes.
                        First(), cfgNode.Summary.EntryNode, syntaxNode, cfgNode, true))
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
        /// Checks if the gives up call has a field symbol argument that is not
        /// already mapped and, if yes, maps it.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        private void CheckForNonMappedFieldSymbol(InvocationExpressionSyntax call,
            ControlFlowGraphNode cfgNode, SemanticModel model, AnalysisContext context)
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
                this.MapSymbolIfNotExists(accessSymbol, cfgNode.SyntaxNodes[0], cfgNode);
            }
        }

        /// <summary>
        /// Returns the return symbols fromt he given object creation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of return symbols</returns>
        private HashSet<ISymbol> GetReturnSymbols(ObjectCreationExpressionSyntax call, MethodSummary summary,
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
        private HashSet<ISymbol> GetReturnSymbols(InvocationExpressionSyntax call, MethodSummary summary,
            SemanticModel model)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            return summary.GetResolvedReturnSymbols(call.ArgumentList, model);
        }

        /// <summary>
        /// Marks that the symbol has been reassigned a reference.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void MarkSymbolReassignment(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.ReferenceResetMap.ContainsKey(cfgNode))
            {
                this.ReferenceResetMap.Add(cfgNode, new Dictionary<SyntaxNode, HashSet<ISymbol>>());
                this.ReferenceResetMap[cfgNode].Add(syntaxNode, new HashSet<ISymbol>());
            }
            else if (!this.ReferenceResetMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReferenceResetMap[cfgNode].Add(syntaxNode, new HashSet<ISymbol>());
            }

            this.ReferenceResetMap[cfgNode][syntaxNode].Add(symbol);
        }

        /// <summary>
        /// Returns true if the given symbol resets until it reaches the
        /// target control flow graph node.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="track">Tracking</param>
        /// <returns>Boolean</returns>
        private bool DoesSymbolReset(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode, HashSet<ControlFlowGraphNode> visited,
            bool track)
        {
            visited.Add(cfgNode);

            bool result = false;
            if (syntaxNode.Equals(targetSyntaxNode) && cfgNode.Equals(targetCfgNode) &&
                !track)
            {
                return result;
            }

            foreach (var node in cfgNode.SyntaxNodes)
            {
                if (track && this.ReferenceResetMap.ContainsKey(cfgNode) &&
                    this.ReferenceResetMap[cfgNode].ContainsKey(node) &&
                    this.ReferenceResetMap[cfgNode][node].Contains(symbol))
                {
                    result = true;
                    break;
                }

                if (!track && node.Equals(syntaxNode))
                {
                    track = true;
                }
            }

            if (!result)
            {
                foreach (var successor in cfgNode.ISuccessors.Where(v => !visited.Contains(v)))
                {
                    if ((successor.Equals(targetCfgNode) ||
                        successor.IsPredecessorOf(targetCfgNode)) &&
                        this.DoesSymbolReset(symbol, successor.SyntaxNodes.First(),
                        successor, targetSyntaxNode, targetCfgNode, visited, true))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the data-flow analysis reached a fixpoint regarding the given successor.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="successorCfgNode">Successor controlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        private bool ReachedFixpoint(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            ControlFlowGraphNode successorCfgNode)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> currentMap = null;
            if (!this.TryGetDataFlowMapForSyntaxNode(syntaxNode, cfgNode, out currentMap))
            {
                return false;
            }

            Dictionary<ISymbol, HashSet<ISymbol>> successorMap = null;
            if (!this.TryGetDataFlowMapForSyntaxNode(successorCfgNode.SyntaxNodes.First(),
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

        #endregion

        #region data-flow query methods

        /// <summary>
        /// Tries to return the map for the given syntax node. Returns false and
        /// a null map, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        internal bool TryGetDataFlowMapForSyntaxNode(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            out Dictionary<ISymbol, HashSet<ISymbol>> map)
        {
            if (cfgNode == null || syntaxNode == null)
            {
                map = null;
                return false;
            }

            if (this.DataFlowMap.ContainsKey(cfgNode))
            {
                if (this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
                {
                    map = this.DataFlowMap[cfgNode][syntaxNode];
                    return true;
                }
                else
                {
                    map = null;
                    return false;
                }
            }
            else
            {
                map = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to return the reachability map for the given syntax node. Returns false
        /// and a null map, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        internal bool TryGetReachabilityMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, out Dictionary<ISymbol, HashSet<ISymbol>> map)
        {
            if (cfgNode == null || syntaxNode == null)
            {
                map = null;
                return false;
            }

            if (this.ReachabilityMap.ContainsKey(cfgNode))
            {
                if (this.ReachabilityMap[cfgNode].ContainsKey(syntaxNode))
                {
                    map = this.ReachabilityMap[cfgNode][syntaxNode];
                    return true;
                }
                else
                {
                    map = null;
                    return false;
                }
            }
            else
            {
                map = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to return the reference type map for the given syntax node. Returns false
        /// and a null map, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        internal bool TryGetReferenceTypeMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, out Dictionary<ISymbol, HashSet<ITypeSymbol>> map)
        {
            if (cfgNode == null || syntaxNode == null)
            {
                map = null;
                return false;
            }

            if (this.ReferenceTypeMap.ContainsKey(cfgNode))
            {
                if (this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode))
                {
                    map = this.ReferenceTypeMap[cfgNode][syntaxNode];
                    return true;
                }
                else
                {
                    map = null;
                    return false;
                }
            }
            else
            {
                map = null;
                return false;
            }
        }

        /// <summary>
        /// Returns true if the given symbol resets until it reaches the
        /// target control flow graph node.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="track">Tracking</param>
        /// <returns>Boolean</returns>
        internal bool DoesSymbolReset(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode, bool track = false)
        {
            return this.DoesSymbolReset(symbol, syntaxNode, cfgNode, targetSyntaxNode,
                targetCfgNode, new HashSet<ControlFlowGraphNode>(), track);
        }

        #endregion

        #region data-flow summary printing methods

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        internal void PrintDataFlowMap()
        {
            if (this.DataFlowMap.Count > 0)
            {
                IO.PrintLine("... |");
                IO.PrintLine("... | . Data flow map");
                foreach (var cfgNode in this.DataFlowMap)
                {
                    foreach (var syntaxNode in cfgNode.Value)
                    {
                        IO.PrintLine("... | ... Statement: '{0}'", syntaxNode.Key);
                        foreach (var pair in syntaxNode.Value)
                        {
                            foreach (var symbol in pair.Value)
                            {
                                IO.PrintLine("... | ..... " + pair.Key.Name + " ::= " + symbol.Name);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the reachability map.
        /// </summary>
        internal void PrintReachabilityMap()
        {
            if (this.ReachabilityMap.Count > 0)
            {
                IO.PrintLine("... |");
                IO.PrintLine("... | . Reachability map");
                foreach (var cfgNode in this.ReachabilityMap)
                {
                    foreach (var syntaxNode in cfgNode.Value)
                    {
                        IO.PrintLine("... | ... Statement: '{0}'", syntaxNode.Key);
                        foreach (var pair in syntaxNode.Value)
                        {
                            foreach (var symbol in pair.Value)
                            {
                                IO.PrintLine("... | ..... " + pair.Key.Name + " ::= " + symbol.Name);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the type of references.
        /// </summary>
        internal void PrintReferenceTypes()
        {
            if (this.ReferenceTypeMap.Count > 0)
            {
                IO.PrintLine("... |");
                IO.PrintLine("... | . Types of references");
                foreach (var cfgNode in this.ReferenceTypeMap)
                {
                    foreach (var syntaxNode in cfgNode.Value)
                    {
                        IO.PrintLine("... | ... Statement: '{0}'", syntaxNode.Key);
                        foreach (var pair in syntaxNode.Value)
                        {
                            foreach (var symbol in pair.Value)
                            {
                                IO.PrintLine("... | ..... " + pair.Key.Name + " ::= " + symbol.Name);
                            }
                        }
                    }
                }
            } 
        }

        /// <summary>
        /// Prints statements that reset references.
        /// </summary>
        internal void PrintStatementsThatResetReferences()
        {
            if (this.ReferenceResetMap.Count > 0)
            {
                IO.PrintLine("... |");
                IO.PrintLine("... | . Statements that reset references");
                foreach (var cfgNode in this.ReferenceResetMap)
                {
                    foreach (var syntaxNode in cfgNode.Value)
                    {
                        IO.PrintLine("... | ... Statement: '{0}'", syntaxNode.Key);
                        foreach (var symbol in syntaxNode.Value)
                        {
                            IO.PrintLine("... | ..... Reference: '{0}'", symbol.Name);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
