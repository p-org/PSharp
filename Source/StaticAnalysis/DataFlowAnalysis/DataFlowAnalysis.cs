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
        /// The analysis context.
        /// </summary>
        protected AnalysisContext AnalysisContext;

        /// <summary>
        /// The semantic model.
        /// </summary>
        protected SemanticModel SemanticModel;

        /// <summary>
        /// DataFlowMap containing data-flow values.
        /// </summary>
        protected Dictionary<CFGNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ISymbol>>>> DataFlowMap;

        /// <summary>
        /// DataFlowMap containing object reachability values.
        /// </summary>
        protected Dictionary<CFGNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ISymbol>>>> ReachabilityMap;

        /// <summary>
        /// DataFlowMap containing reference types.
        /// </summary>
        protected Dictionary<CFGNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ITypeSymbol>>>> ReferenceTypeMap;

        /// <summary>
        /// DataFlowMap containing reference resets.
        /// </summary>
        protected Dictionary<CFGNode, Dictionary<SyntaxNode,
            HashSet<ISymbol>>> ReferenceResetMap;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="model">SemanticModel</param>
        protected DataFlowAnalysis(AnalysisContext context, SemanticModel model)
        {
            this.DataFlowMap = new Dictionary<CFGNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ISymbol>>>>();
            this.ReachabilityMap = new Dictionary<CFGNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ISymbol>>>>();
            this.ReferenceTypeMap = new Dictionary<CFGNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ITypeSymbol>>>>();
            this.ReferenceResetMap = new Dictionary<CFGNode, Dictionary<SyntaxNode,
                HashSet<ISymbol>>>();

            this.AnalysisContext = context;
            this.SemanticModel = model;
        }

        #endregion

        #region public analysis API

        /// <summary>
        /// Analyzes the data-flow of the given method.
        /// </summary>
        /// <param name="methodSummary">MethodSummary</param>
        /// <param name="context">AnalysisContext</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>DataFlowAnalysis</returns>
        public static DataFlowAnalysis Analyze(MethodSummary methodSummary, AnalysisContext context, SemanticModel model)
        {
            var dataFlowAnalysis = new DataFlowAnalysis(context, model);
            dataFlowAnalysis.Analyze(methodSummary);
            return dataFlowAnalysis;
        }

        /// <summary>
        /// Tries to return the map for the given syntax node.
        /// Returns false and a null, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        public bool TryGetDataFlowMapForSyntaxNode(SyntaxNode syntaxNode, CFGNode cfgNode,
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
        /// Tries to return the reachability map for the given syntax node.
        /// Returns false and a null, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        public bool TryGetReachabilityMapForSyntaxNode(SyntaxNode syntaxNode,
            CFGNode cfgNode, out Dictionary<ISymbol, HashSet<ISymbol>> map)
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
        /// Tries to return the reference type map for the given syntax node.
        /// Returns false and a null, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        public bool TryGetReferenceTypeMapForSyntaxNode(SyntaxNode syntaxNode,
            CFGNode cfgNode, out Dictionary<ISymbol, HashSet<ITypeSymbol>> map)
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
        /// Returns true if the given reference resets until it
        /// reaches the target control-flow graph node.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="refSyntaxNode">Reference SyntaxNode</param>
        /// <param name="refCfgNode">Reference CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target CFGNode</param>
        /// <returns>Boolean</returns>
        public bool DoesReferenceResetUntilCFGNode(ISymbol reference, SyntaxNode refSyntaxNode,
            CFGNode refCfgNode, SyntaxNode targetSyntaxNode, CFGNode targetCfgNode)
        {
            return this.DoesReferenceResetUntilCFGNode(reference, refSyntaxNode, refCfgNode,
                targetSyntaxNode, targetCfgNode, false);
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Analyzes the data-flow of the given method.
        /// </summary>
        /// <param name="methodSummary">MethodSummary</param>
        /// <returns>DataFlowAnalysis</returns>
        protected void Analyze(MethodSummary methodSummary)
        {
            foreach (var param in methodSummary.Method.ParameterList.Parameters)
            {
                ITypeSymbol declType = this.SemanticModel.GetTypeInfo(param.Type).Type;
                if (this.AnalysisContext.IsTypePassedByValueOrImmutable(declType))
                {
                    continue;
                }

                IParameterSymbol paramSymbol = this.SemanticModel.GetDeclaredSymbol(param);
                this.MapReferenceToSymbol(paramSymbol, paramSymbol,
                    methodSummary.Method.ParameterList, methodSummary.EntryNode, false);
            }

            this.Analyze(methodSummary.EntryNode, methodSummary.EntryNode,
                methodSummary.Method.ParameterList);
        }

        #endregion

        #region data-flow analysis methods

        /// <summary>
        /// Analyzes the data-flow of the given control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="previousCfgNode">Previous CFGNode</param>
        /// <param name="previousSyntaxNode">Previous SyntaxNode</param>
        private void Analyze(CFGNode cfgNode, CFGNode previousCfgNode, SyntaxNode previousSyntaxNode)
        {
            if (!cfgNode.IsJumpNode && !cfgNode.IsLoopHeadNode)
            {
                this.AnalyzeRegularNode(cfgNode, previousCfgNode, previousSyntaxNode);
            }
            else
            {
                this.Transfer(previousSyntaxNode, previousCfgNode, cfgNode.SyntaxNodes[0], cfgNode);
                previousSyntaxNode = cfgNode.SyntaxNodes[0];
                previousCfgNode = cfgNode;
            }

            foreach (var successor in cfgNode.GetImmediateSuccessors())
            {
                if (this.ReachedFixpoint(previousSyntaxNode, cfgNode, successor))
                {
                    continue;
                }

                this.Analyze(successor, cfgNode, previousSyntaxNode);
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the given regular control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="previousCfgNode">Previous CFGNode</param>
        /// <param name="previousSyntaxNode">Previous SyntaxNode</param>
        private void AnalyzeRegularNode(CFGNode cfgNode, CFGNode previousCfgNode, SyntaxNode previousSyntaxNode)
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
                    this.AnalyzeVariableDeclaration(varDecl, syntaxNode, cfgNode);
                }
                else if (expr != null)
                {
                    if (expr.Expression is AssignmentExpressionSyntax)
                    {
                        var assignment = expr.Expression as AssignmentExpressionSyntax;
                        this.AnalyzeAssignmentExpression(assignment, syntaxNode, cfgNode);
                    }
                    else if (expr.Expression is InvocationExpressionSyntax)
                    {
                        var invocation = expr.Expression as InvocationExpressionSyntax;
                        this.AnalyzeInvocationExpression(invocation, syntaxNode, cfgNode);
                    }
                }
                else if (ret != null)
                {
                    this.AnalyzeReturnStatement(ret, syntaxNode, cfgNode);
                }

                previousSyntaxNode = syntaxNode;
                previousCfgNode = cfgNode;
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the given variable declaration.
        /// </summary>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        private void AnalyzeVariableDeclaration(VariableDeclarationSyntax varDecl, SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            foreach (var variable in varDecl.Variables)
            {
                if (variable.Initializer == null)
                {
                    continue;
                }

                this.CaptureParameterAccesses(variable.Initializer.Value, syntaxNode, cfgNode);
                this.CaptureFieldAccesses(variable.Initializer.Value, syntaxNode, cfgNode);

                ITypeSymbol declType = null;
                if (variable.Initializer.Value is LiteralExpressionSyntax &&
                    variable.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    declType = this.SemanticModel.GetTypeInfo(varDecl.Type).Type;
                }
                else
                {
                    declType = this.SemanticModel.GetTypeInfo(variable.Initializer.Value).Type;
                }

                if (this.AnalysisContext.IsTypePassedByValueOrImmutable(declType))
                {
                    continue;
                }

                var declSymbol = this.SemanticModel.GetDeclaredSymbol(variable);

                if (variable.Initializer.Value is IdentifierNameSyntax ||
                    variable.Initializer.Value is MemberAccessExpressionSyntax)
                {
                    ISymbol varSymbol = null;
                    if (variable.Initializer.Value is IdentifierNameSyntax)
                    {
                        varSymbol = this.SemanticModel.GetSymbolInfo(variable.Initializer.Value
                            as IdentifierNameSyntax).Symbol;
                    }
                    else if (variable.Initializer.Value is MemberAccessExpressionSyntax)
                    {
                        varSymbol = this.SemanticModel.GetSymbolInfo((variable.Initializer.Value
                            as MemberAccessExpressionSyntax).Name).Symbol;
                    }

                    this.MapReferenceToSymbol(varSymbol, declSymbol, syntaxNode, cfgNode);

                    HashSet<ITypeSymbol> referenceTypes = null;
                    if (this.ResolveReferenceType(out referenceTypes, varSymbol, syntaxNode, cfgNode))
                    {
                        this.MapReferenceTypesToSymbol(referenceTypes, declSymbol, syntaxNode, cfgNode);
                    }
                    else
                    {
                        this.EraseReferenceTypes(declSymbol, syntaxNode, cfgNode);
                    }
                }
                else if (variable.Initializer.Value is LiteralExpressionSyntax &&
                    variable.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    this.ResetReferences(declSymbol, syntaxNode, cfgNode);
                }
                else if (variable.Initializer.Value is InvocationExpressionSyntax)
                {
                    var invocation = variable.Initializer.Value as InvocationExpressionSyntax;
                    var summary = this.AnalysisContext.TryGetSummary(invocation, this.SemanticModel);
                    var reachableSymbols = this.ResolveSideEffectsInCall(invocation,
                        summary, syntaxNode, cfgNode);
                    var returnSymbols = this.GetReturnSymbols(invocation, summary);

                    if (returnSymbols.Count == 0)
                    {
                        this.ResetReferences(declSymbol, syntaxNode, cfgNode);
                    }
                    else if (returnSymbols.Contains(declSymbol))
                    {
                        this.MapReferencesToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode, false);
                    }
                    else
                    {
                        this.MapReferencesToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode);
                    }

                    if (reachableSymbols.Count > 0)
                    {
                        this.MapReachableFieldsToSymbol(reachableSymbols, declSymbol,
                            syntaxNode, cfgNode);
                    }

                    if (summary != null && summary.ReturnTypeSet.Count > 0)
                    {
                        this.MapReferenceTypesToSymbol(summary.ReturnTypeSet,
                            declSymbol, syntaxNode, cfgNode);
                    }
                    else
                    {
                        this.EraseReferenceTypes(declSymbol, syntaxNode, cfgNode);
                    }
                }
                else if (variable.Initializer.Value is ObjectCreationExpressionSyntax)
                {
                    var objCreation = variable.Initializer.Value as ObjectCreationExpressionSyntax;
                    var summary = this.AnalysisContext.TryGetSummary(objCreation, this.SemanticModel);
                    var reachableSymbols = this.ResolveSideEffectsInCall(objCreation,
                        summary, syntaxNode, cfgNode);
                    var returnSymbols = this.GetReturnSymbols(objCreation, summary);

                    if (returnSymbols.Count == 0)
                    {
                        this.ResetReferences(declSymbol, syntaxNode, cfgNode);
                    }
                    else if (returnSymbols.Contains(declSymbol))
                    {
                        this.MapReferencesToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode, false);
                    }
                    else
                    {
                        this.MapReferencesToSymbol(returnSymbols, declSymbol, syntaxNode, cfgNode);
                    }

                    if (reachableSymbols.Count > 0)
                    {
                        this.MapReachableFieldsToSymbol(reachableSymbols, declSymbol,
                            syntaxNode, cfgNode);
                    }

                    var typeSymbol = this.SemanticModel.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                    if (typeSymbol != null)
                    {
                        this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                            declSymbol, syntaxNode, cfgNode);
                    }
                    else
                    {
                        this.EraseReferenceTypes(declSymbol, syntaxNode, cfgNode);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the given assignment expression.
        /// </summary>
        /// <param name="binaryExpr">BinaryExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        private void AnalyzeAssignmentExpression(AssignmentExpressionSyntax assignment, SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            this.CaptureParameterAccesses(assignment.Left, syntaxNode, cfgNode);
            this.CaptureParameterAccesses(assignment.Right, syntaxNode, cfgNode);
            this.CaptureFieldAccesses(assignment.Left, syntaxNode, cfgNode);
            this.CaptureFieldAccesses(assignment.Right, syntaxNode, cfgNode);
            
            IdentifierNameSyntax lhs = null;
            ISymbol lhsFieldSymbol = null;
            if (assignment.Left is IdentifierNameSyntax)
            {
                lhs = assignment.Left as IdentifierNameSyntax;
                var lhsType = this.SemanticModel.GetTypeInfo(lhs).Type;
                if (this.AnalysisContext.IsTypePassedByValueOrImmutable(lhsType))
                {
                    return;
                }
            }
            else if (assignment.Left is MemberAccessExpressionSyntax)
            {
                var name = (assignment.Left as MemberAccessExpressionSyntax).Name;
                var lhsType = this.SemanticModel.GetTypeInfo(name).Type;
                if (this.AnalysisContext.IsTypePassedByValueOrImmutable(lhsType))
                {
                    return;
                }

                lhs = this.AnalysisContext.GetTopLevelIdentifier(assignment.Left, this.SemanticModel);
                lhsFieldSymbol = this.SemanticModel.GetSymbolInfo(name as IdentifierNameSyntax).Symbol;
            }
            else if (assignment.Left is ElementAccessExpressionSyntax)
            {
                var memberAccess = (assignment.Left as ElementAccessExpressionSyntax);
                if (memberAccess.Expression is IdentifierNameSyntax)
                {
                    lhs = memberAccess.Expression as IdentifierNameSyntax;
                    var lhsType = this.SemanticModel.GetTypeInfo(lhs).Type;
                    if (this.AnalysisContext.IsTypePassedByValueOrImmutable(lhsType))
                    {
                        return;
                    }
                }
                else if (memberAccess.Expression is MemberAccessExpressionSyntax)
                {
                    var name = (memberAccess.Expression as MemberAccessExpressionSyntax).Name;
                    var lhsType = this.SemanticModel.GetTypeInfo(name).Type;
                    if (this.AnalysisContext.IsTypePassedByValueOrImmutable(lhsType))
                    {
                        return;
                    }

                    lhs = this.AnalysisContext.GetTopLevelIdentifier(memberAccess.Expression, this.SemanticModel);
                    lhsFieldSymbol = this.SemanticModel.GetSymbolInfo(name as IdentifierNameSyntax).Symbol;
                }
            }

            var leftSymbol = this.SemanticModel.GetSymbolInfo(lhs).Symbol;

            if (assignment.Right is IdentifierNameSyntax ||
                assignment.Right is MemberAccessExpressionSyntax)
            {
                IdentifierNameSyntax rhs = null;
                if (assignment.Right is IdentifierNameSyntax)
                {
                    rhs = assignment.Right as IdentifierNameSyntax;
                }
                else if (assignment.Right is MemberAccessExpressionSyntax)
                {
                    rhs = this.AnalysisContext.GetTopLevelIdentifier(assignment.Right, this.SemanticModel);
                }

                var rightSymbol = this.SemanticModel.GetSymbolInfo(rhs).Symbol;
                this.MapReferenceToSymbol(rightSymbol, leftSymbol, syntaxNode, cfgNode);
                if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                {
                    this.MapReferenceToSymbol(rightSymbol, lhsFieldSymbol, syntaxNode, cfgNode);
                }

                HashSet<ITypeSymbol> referenceTypes = null;
                if (this.ResolveReferenceType(out referenceTypes, rightSymbol, syntaxNode, cfgNode))
                {
                    this.MapReferenceTypesToSymbol(referenceTypes, leftSymbol, syntaxNode, cfgNode);
                }
                else
                {
                    this.EraseReferenceTypes(leftSymbol, syntaxNode, cfgNode);
                }
            }
            else if (assignment.Right is LiteralExpressionSyntax &&
                assignment.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                this.ResetReferences(leftSymbol, syntaxNode, cfgNode);
                if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                {
                    this.ResetReferences(lhsFieldSymbol, syntaxNode, cfgNode);
                }
            }
            else if (assignment.Right is InvocationExpressionSyntax)
            {
                var invocation = assignment.Right as InvocationExpressionSyntax;
                var summary = this.AnalysisContext.TryGetSummary(invocation, this.SemanticModel);
                var reachableSymbols = this.ResolveSideEffectsInCall(invocation,
                    summary, syntaxNode, cfgNode);
                var returnSymbols = this.GetReturnSymbols(invocation, summary);
                this.MapSymbolsInInvocation(invocation, cfgNode);

                if (returnSymbols.Count == 0)
                {
                    this.ResetReferences(leftSymbol, syntaxNode, cfgNode);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.ResetReferences(lhsFieldSymbol, syntaxNode, cfgNode);
                    }
                }
                else if (returnSymbols.Contains(leftSymbol))
                {
                    this.MapReferencesToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode, false);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.MapReferencesToSymbol(returnSymbols, lhsFieldSymbol,
                            syntaxNode, cfgNode, false);
                    }
                }
                else
                {
                    this.MapReferencesToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.MapReferencesToSymbol(returnSymbols, lhsFieldSymbol,
                            syntaxNode, cfgNode);
                    }
                }

                if (lhsFieldSymbol != null && reachableSymbols.Count > 0)
                {
                    this.MapReachableFieldsToSymbol(reachableSymbols, lhsFieldSymbol,
                        syntaxNode, cfgNode);
                }

                if (summary != null && summary.ReturnTypeSet.Count > 0)
                {
                    this.MapReferenceTypesToSymbol(summary.ReturnTypeSet,
                        leftSymbol, syntaxNode, cfgNode);
                }
                else
                {
                    this.EraseReferenceTypes(leftSymbol, syntaxNode, cfgNode);
                }
            }
            else if (assignment.Right is ObjectCreationExpressionSyntax)
            {
                var objCreation = assignment.Right as ObjectCreationExpressionSyntax;
                var summary = this.AnalysisContext.TryGetSummary(objCreation, this.SemanticModel);
                var reachableSymbols = this.ResolveSideEffectsInCall(objCreation,
                    summary, syntaxNode, cfgNode);
                var returnSymbols = this.GetReturnSymbols(objCreation, summary);

                if (returnSymbols.Count == 0)
                {
                    this.ResetReferences(leftSymbol, syntaxNode, cfgNode);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.ResetReferences(lhsFieldSymbol, syntaxNode, cfgNode);
                    }
                }
                else if (returnSymbols.Contains(leftSymbol))
                {
                    this.MapReferencesToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode, false);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.MapReferencesToSymbol(returnSymbols, lhsFieldSymbol,
                            syntaxNode, cfgNode, false);
                    }
                }
                else
                {
                    this.MapReferencesToSymbol(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.MapReferencesToSymbol(returnSymbols, lhsFieldSymbol,
                            syntaxNode, cfgNode);
                    }
                }

                if (lhsFieldSymbol != null && reachableSymbols.Count > 0)
                {
                    this.MapReachableFieldsToSymbol(reachableSymbols, lhsFieldSymbol,
                        syntaxNode, cfgNode);
                }

                var typeSymbol = this.SemanticModel.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                if (typeSymbol != null)
                {
                    this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                        leftSymbol, syntaxNode, cfgNode);
                }
                else
                {
                    this.EraseReferenceTypes(leftSymbol, syntaxNode, cfgNode);
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the given invocation expression.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        private void AnalyzeInvocationExpression(InvocationExpressionSyntax invocation, SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            var summary = this.AnalysisContext.TryGetSummary(invocation, this.SemanticModel);
            this.ResolveSideEffectsInCall(invocation, summary, syntaxNode, cfgNode);
            this.GetReturnSymbols(invocation, summary);
            this.MapSymbolsInInvocation(invocation, cfgNode);
        }

        /// <summary>
        /// Analyzes the data-flow of the given return statement.
        /// </summary>
        /// <param name="retStmt">ReturnStatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        private void AnalyzeReturnStatement(ReturnStatementSyntax retStmt, SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            HashSet<ISymbol> returnSymbols = null;
            if (retStmt.Expression is IdentifierNameSyntax ||
                retStmt.Expression is MemberAccessExpressionSyntax)
            {
                IdentifierNameSyntax rhs = null;
                if (retStmt.Expression is IdentifierNameSyntax)
                {
                    rhs = retStmt.Expression as IdentifierNameSyntax;
                }
                else if (retStmt.Expression is MemberAccessExpressionSyntax)
                {
                    rhs = this.AnalysisContext.GetTopLevelIdentifier(retStmt.Expression, this.SemanticModel);
                }

                var rightSymbol = this.SemanticModel.GetSymbolInfo(rhs).Symbol;
                returnSymbols = new HashSet<ISymbol> { rightSymbol };

                HashSet<ITypeSymbol> referenceTypes = null;
                if (this.ResolveReferenceType(out referenceTypes, rightSymbol, syntaxNode, cfgNode))
                {
                    foreach (var referenceType in referenceTypes)
                    {
                        cfgNode.GetMethodSummary().ReturnTypeSet.Add(referenceType);
                    }
                }
            }
            else if (retStmt.Expression is InvocationExpressionSyntax)
            {
                var invocation = retStmt.Expression as InvocationExpressionSyntax;
                var summary = this.AnalysisContext.TryGetSummary(invocation, this.SemanticModel);
                this.ResolveSideEffectsInCall(invocation, summary, syntaxNode, cfgNode);
                returnSymbols = this.GetReturnSymbols(invocation, summary);

                if (summary != null)
                {
                    foreach (var referenceType in summary.ReturnTypeSet)
                    {
                        cfgNode.GetMethodSummary().ReturnTypeSet.Add(referenceType);
                    }
                }
            }
            else if (retStmt.Expression is ObjectCreationExpressionSyntax)
            {
                var objCreation = retStmt.Expression as ObjectCreationExpressionSyntax;
                var summary = this.AnalysisContext.TryGetSummary(objCreation, this.SemanticModel);
                this.ResolveSideEffectsInCall(objCreation, summary, syntaxNode, cfgNode);
                returnSymbols = this.GetReturnSymbols(objCreation, summary);

                var referenceType = this.SemanticModel.GetSymbolInfo(objCreation.Type).Symbol as ITypeSymbol;
                if (referenceType != null)
                {
                    cfgNode.GetMethodSummary().ReturnTypeSet.Add(referenceType);
                }
            }

            this.CaptureReturnSymbols(returnSymbols, syntaxNode, cfgNode);
        }

        #endregion

        #region data-flow transfer methods

        /// <summary>
        /// Transfers the data-flow map from the previous node to the new node.
        /// </summary>
        /// <param name="previousSyntaxNode">Previous SyntaxNode</param>
        /// <param name="previousCfgNode">Previous cfgNode</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void Transfer(SyntaxNode previousSyntaxNode, CFGNode previousCfgNode,
            SyntaxNode syntaxNode, CFGNode cfgNode)
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

            Dictionary<ISymbol, HashSet<ITypeSymbol>> previousReferenceTypeMap = null;
            if (this.TryGetReferenceTypeMapForSyntaxNode(previousSyntaxNode, previousCfgNode,
                out previousReferenceTypeMap))
            {
                foreach (var pair in previousReferenceTypeMap)
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

        #endregion

        #region access capturing methods

        /// <summary>
        /// Captures the parameter acccesses in the given expression.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        private void CaptureParameterAccesses(ExpressionSyntax expr, SyntaxNode syntaxNode,
            CFGNode cfgNode)
        {
            if (!(expr is MemberAccessExpressionSyntax))
            {
                return;
            }

            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = this.AnalysisContext.GetTopLevelIdentifier(expr, this.SemanticModel);
            if (identifier == null || name == null)
            {
                return;
            }

            var type = this.SemanticModel.GetTypeInfo(identifier).Type;
            if (this.AnalysisContext.IsTypePassedByValueOrImmutable(type) ||
                name.Equals(identifier))
            {
                return;
            }

            var symbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
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
            var parameterList = cfgNode.GetMethodSummary().Method.ParameterList.Parameters;
            for (int idx = 0; idx < parameterList.Count; idx++)
            {
                var paramSymbol = this.SemanticModel.GetDeclaredSymbol(parameterList[idx]);
                indexMap.Add(paramSymbol, idx);
            }

            if (map.ContainsKey(symbol))
            {
                foreach (var reference in map[symbol])
                {
                    if (reference.Kind == SymbolKind.Parameter)
                    {
                        if (reference.Equals(symbol) && this.DoesReferenceResetUntilCFGNode(reference,
                            cfgNode.GetMethodSummary().EntryNode.SyntaxNodes.First(),
                            cfgNode.GetMethodSummary().EntryNode, syntaxNode, cfgNode, true))
                        {
                            continue;
                        }

                        int index = indexMap[reference];
                        if (cfgNode.GetMethodSummary().AccessSet.ContainsKey(index))
                        {
                            cfgNode.GetMethodSummary().AccessSet[index].Add(syntaxNode);
                        }
                        else
                        {
                            cfgNode.GetMethodSummary().AccessSet.Add(index, new HashSet<SyntaxNode>());
                            cfgNode.GetMethodSummary().AccessSet[index].Add(syntaxNode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Captures the field acccesses in the given expression.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        private void CaptureFieldAccesses(ExpressionSyntax expr, SyntaxNode syntaxNode,
            CFGNode cfgNode)
        {
            if (!(expr is MemberAccessExpressionSyntax))
            {
                return;
            }

            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = this.AnalysisContext.GetTopLevelIdentifier(expr, this.SemanticModel);
            if (identifier == null || name == null)
            {
                return;
            }

            var type = this.SemanticModel.GetTypeInfo(identifier).Type;
            if (this.AnalysisContext.IsTypePassedByValueOrImmutable(type) ||
                name.Equals(identifier))
            {
                return;
            }

            var symbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
            var definition = SymbolFinder.FindSourceDefinitionAsync(symbol,
                this.AnalysisContext.Solution).Result;
            if (!(definition is IFieldSymbol))
            {
                return;
            }

            var fieldDecl = definition.DeclaringSyntaxReferences.First().GetSyntax().
                AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();
            if (this.DoesReferenceResetUntilCFGNode(symbol,
                cfgNode.GetMethodSummary().EntryNode.SyntaxNodes.First(),
                cfgNode.GetMethodSummary().EntryNode, syntaxNode, cfgNode, true))
            {
                return;
            }

            if (cfgNode.GetMethodSummary().FieldAccessSet.ContainsKey(symbol as IFieldSymbol))
            {
                cfgNode.GetMethodSummary().FieldAccessSet[symbol as IFieldSymbol].Add(syntaxNode);
            }
            else
            {
                cfgNode.GetMethodSummary().FieldAccessSet.Add(symbol as IFieldSymbol, new HashSet<SyntaxNode>());
                cfgNode.GetMethodSummary().FieldAccessSet[symbol as IFieldSymbol].Add(syntaxNode);
            }
        }

        /// <summary>
        /// Captures the return symbols.
        /// </summary>
        /// <param name="returnSymbols">Set of return symbols</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        private void CaptureReturnSymbols(HashSet<ISymbol> returnSymbols, SyntaxNode syntaxNode,
            CFGNode cfgNode)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> map = null;
            if (returnSymbols == null ||
                !this.TryGetDataFlowMapForSyntaxNode(syntaxNode, cfgNode, out map))
            {
                return;
            }

            Dictionary<ISymbol, int> indexMap = new Dictionary<ISymbol, int>();
            var parameterList = cfgNode.GetMethodSummary().Method.ParameterList.Parameters;
            for (int idx = 0; idx < parameterList.Count; idx++)
            {
                var paramSymbol = this.SemanticModel.GetDeclaredSymbol(parameterList[idx]);
                indexMap.Add(paramSymbol, idx);
            }

            foreach (var symbol in returnSymbols)
            {
                if (symbol.Kind == SymbolKind.Parameter)
                {
                    if (this.DoesReferenceResetUntilCFGNode(symbol,
                        cfgNode.GetMethodSummary().EntryNode.SyntaxNodes.First(),
                        cfgNode.GetMethodSummary().EntryNode, syntaxNode, cfgNode, true))
                    {
                        continue;
                    }

                    cfgNode.GetMethodSummary().ReturnSet.Item1.Add(indexMap[symbol]);
                }
                else if (symbol.Kind == SymbolKind.Field)
                {
                    cfgNode.GetMethodSummary().ReturnSet.Item2.Add(symbol as IFieldSymbol);
                }
                else if (map.ContainsKey(symbol))
                {
                    foreach (var reference in map[symbol].Where(v => v.Kind == SymbolKind.Field))
                    {
                        cfgNode.GetMethodSummary().ReturnSet.Item2.Add(reference as IFieldSymbol);
                    }
                }
            }
        }

        #endregion

        #region data-flow resolving methods

        /// <summary>
        /// Resolves side effects from the given object creation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <returns>Set of reachable field symbols</returns>
        private HashSet<ISymbol> ResolveSideEffectsInCall(ObjectCreationExpressionSyntax call, MethodSummary summary,
            SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            HashSet<ISymbol> reachableFields = new HashSet<ISymbol>();
            var sideEffects = summary.GetResolvedSideEffects(call.ArgumentList, this.SemanticModel);
            foreach (var sideEffect in sideEffects)
            {
                this.MapReferencesToSymbol(sideEffect.Value, sideEffect.Key, syntaxNode, cfgNode);
                reachableFields.Add(sideEffect.Key);
            }

            foreach (var fieldAccess in summary.FieldAccessSet)
            {
                foreach (var access in fieldAccess.Value)
                {
                    if (cfgNode.GetMethodSummary().FieldAccessSet.ContainsKey(fieldAccess.Key as IFieldSymbol))
                    {
                        cfgNode.GetMethodSummary().FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
                    }
                    else
                    {
                        cfgNode.GetMethodSummary().FieldAccessSet.Add(fieldAccess.Key as IFieldSymbol, new HashSet<SyntaxNode>());
                        cfgNode.GetMethodSummary().FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
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
        /// <param name="cfgNode">CFGNode</param>
        /// <returns>Set of reachable field symbols</returns>
        private HashSet<ISymbol> ResolveSideEffectsInCall(InvocationExpressionSyntax call, MethodSummary summary,
            SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            HashSet<ISymbol> reachableFields = new HashSet<ISymbol>();
            var sideEffects = summary.GetResolvedSideEffects(call.ArgumentList, this.SemanticModel);
            foreach (var sideEffect in sideEffects)
            {
                this.MapReferencesToSymbol(sideEffect.Value, sideEffect.Key, syntaxNode, cfgNode);
                reachableFields.Add(sideEffect.Key);
            }

            foreach (var fieldAccess in summary.FieldAccessSet)
            {
                foreach (var access in fieldAccess.Value)
                {
                    if (cfgNode.GetMethodSummary().FieldAccessSet.ContainsKey(fieldAccess.Key as IFieldSymbol))
                    {
                        cfgNode.GetMethodSummary().FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
                    }
                    else
                    {
                        cfgNode.GetMethodSummary().FieldAccessSet.Add(fieldAccess.Key as IFieldSymbol, new HashSet<SyntaxNode>());
                        cfgNode.GetMethodSummary().FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
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
        /// <param name="cfgNode">CFGNode</param>
        /// <returns>Boolean</returns>
        private bool ResolveReferenceType(out HashSet<ITypeSymbol> types, ISymbol symbol,
            SyntaxNode syntaxNode, CFGNode cfgNode)
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

        #endregion

        #region data-flow mapping methods

        /// <summary>
        /// Maps the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        protected void MapSymbol(ISymbol symbol, SyntaxNode syntaxNode, CFGNode cfgNode)
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
        private void MapReferenceToSymbol(ISymbol reference, ISymbol symbol, SyntaxNode syntaxNode,
            CFGNode cfgNode, bool markReset = true)
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
        private void MapReferencesToSymbol(HashSet<ISymbol> references, ISymbol symbol, SyntaxNode syntaxNode,
            CFGNode cfgNode, bool markReset = true)
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
            SyntaxNode syntaxNode, CFGNode cfgNode)
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
            SyntaxNode syntaxNode, CFGNode cfgNode)
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
        /// Maps symbols in the invocation.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="cfgNode">CFGNode</param>
        protected virtual void MapSymbolsInInvocation(InvocationExpressionSyntax call, CFGNode cfgNode)
        {

        }

        #endregion

        #region helper methods

        /// <summary>
        /// Returns the return symbols fromt he given object creation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Set of return symbols</returns>
        private HashSet<ISymbol> GetReturnSymbols(ObjectCreationExpressionSyntax call, MethodSummary summary)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            return summary.GetResolvedReturnSymbols(call.ArgumentList, this.SemanticModel);
        }

        /// <summary>
        /// Returns the return symbols fromt he given invocation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Set of return symbols</returns>
        private HashSet<ISymbol> GetReturnSymbols(InvocationExpressionSyntax call, MethodSummary summary)
        {
            if (summary == null)
            {
                return new HashSet<ISymbol>();
            }

            return summary.GetResolvedReturnSymbols(call.ArgumentList, this.SemanticModel);
        }

        /// <summary>
        /// Resets the references of the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void ResetReferences(ISymbol symbol, SyntaxNode syntaxNode, CFGNode cfgNode)
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
        /// Erases the set of reference types from the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void EraseReferenceTypes(ISymbol symbol, SyntaxNode syntaxNode, CFGNode cfgNode)
        {
            if (this.ReferenceTypeMap.ContainsKey(cfgNode) &&
                this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode) &&
                this.ReferenceTypeMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ReferenceTypeMap[cfgNode][syntaxNode].Remove(symbol);
            }
        }

        /// <summary>
        /// Marks that the symbol has been reassigned a reference.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void MarkSymbolReassignment(ISymbol symbol, SyntaxNode syntaxNode, CFGNode cfgNode)
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
        /// Returns true if the given reference resets until it
        /// reaches the target control-flow graph node.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="refSyntaxNode">Reference SyntaxNode</param>
        /// <param name="refCfgNode">Reference CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target CFGNode</param>
        /// <param name="track">Tracking</param>
        /// <returns>Boolean</returns>
        private bool DoesReferenceResetUntilCFGNode(ISymbol reference, SyntaxNode refSyntaxNode, CFGNode refCfgNode,
            SyntaxNode targetSyntaxNode, CFGNode targetCfgNode, bool track)
        {
            return this.DoesReferenceResetUntilCFGNode(reference, refSyntaxNode, refCfgNode, targetSyntaxNode,
                targetCfgNode, new HashSet<CFGNode>(), track);
        }

        /// <summary>
        /// Returns true if the given reference resets until it
        /// reaches the target control-flow graph node.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="refSyntaxNode">Reference SyntaxNode</param>
        /// <param name="refCfgNode">Reference CFGNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target CFGNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="track">Tracking</param>
        /// <returns>Boolean</returns>
        private bool DoesReferenceResetUntilCFGNode(ISymbol reference, SyntaxNode refSyntaxNode,CFGNode refCfgNode,
            SyntaxNode targetSyntaxNode, CFGNode targetCfgNode, HashSet<CFGNode> visited,
            bool track)
        {
            visited.Add(refCfgNode);

            bool result = false;
            if (refSyntaxNode.Equals(targetSyntaxNode) && refCfgNode.Equals(targetCfgNode) &&
                !track)
            {
                return result;
            }

            foreach (var node in refCfgNode.SyntaxNodes)
            {
                if (track && this.ReferenceResetMap.ContainsKey(refCfgNode) &&
                    this.ReferenceResetMap[refCfgNode].ContainsKey(node) &&
                    this.ReferenceResetMap[refCfgNode][node].Contains(reference))
                {
                    result = true;
                    break;
                }

                if (!track && node.Equals(refSyntaxNode))
                {
                    track = true;
                }
            }

            if (!result)
            {
                foreach (var successor in refCfgNode.GetImmediateSuccessors().Where(v => !visited.Contains(v)))
                {
                    if ((successor.Equals(targetCfgNode) ||
                        successor.IsPredecessorOf(targetCfgNode)) &&
                        this.DoesReferenceResetUntilCFGNode(reference, successor.SyntaxNodes.First(),
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
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="successorCfgNode">Successor CFGNode</param>
        /// <returns>Boolean</returns>
        private bool ReachedFixpoint(SyntaxNode syntaxNode, CFGNode cfgNode,
            CFGNode successorCfgNode)
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

        #region data-flow summary printing methods

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        internal void PrintDataFlowMap()
        {
            if (this.DataFlowMap.Count > 0)
            {
                IO.PrintLine("... |");
                IO.PrintLine("... | . Data-flow map");
                foreach (var cfgNode in this.DataFlowMap)
                {
                    IO.PrintLine("... | ... CFG node '{0}'", cfgNode.Key.Id);
                    foreach (var syntaxNode in cfgNode.Value)
                    {
                        IO.PrintLine("... | ..... '{0}'", syntaxNode.Key);
                        foreach (var pair in syntaxNode.Value)
                        {
                            foreach (var symbol in pair.Value)
                            {
                                IO.PrintLine("... | ....... '{0}' flows into '{1}'", symbol.Name, pair.Key.Name);
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
                    IO.PrintLine("... | ... CFG node '{0}'", cfgNode.Key.Id);
                    foreach (var syntaxNode in cfgNode.Value)
                    {
                        IO.PrintLine("... | ..... '{0}'", syntaxNode.Key);
                        foreach (var pair in syntaxNode.Value)
                        {
                            foreach (var symbol in pair.Value)
                            {
                                IO.PrintLine("... | ....... '{0}' has type '{1}'", pair.Key.Name, symbol.Name);
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
            if (this.ReferenceTypeMap.SelectMany(cfg => cfg.Value.SelectMany(node
                => node.Value.SelectMany(symbol => symbol.Value))).Count() > 0)
            {
                IO.PrintLine("... |");
                IO.PrintLine("... | . Types of references");
                foreach (var cfgNode in this.ReferenceTypeMap)
                {
                    IO.PrintLine("... | ... CFG node '{0}'", cfgNode.Key.Id);
                    foreach (var syntaxNode in cfgNode.Value)
                    {
                        IO.PrintLine("... | ..... '{0}'", syntaxNode.Key);
                        foreach (var pair in syntaxNode.Value)
                        {
                            foreach (var symbol in pair.Value)
                            {
                                IO.PrintLine("... | ....... '{0}' has type '{1}'", pair.Key.Name, symbol.Name);
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
                    IO.PrintLine("... | ... CFG node '{0}'", cfgNode.Key.Id);
                    foreach (var syntaxNode in cfgNode.Value)
                    {
                        IO.PrintLine("... | ..... '{0}'", syntaxNode.Key);
                        foreach (var symbol in syntaxNode.Value)
                        {
                            IO.PrintLine("... | ....... Resets '{0}'", symbol.Name);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
