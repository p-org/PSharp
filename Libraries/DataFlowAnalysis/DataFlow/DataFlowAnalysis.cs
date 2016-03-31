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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing data-flow analysis.
    /// </summary>
    public class DataFlowAnalysis
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

        /// <summary>
        /// The semantic model.
        /// </summary>
        private SemanticModel SemanticModel;

        /// <summary>
        /// DataFlowMap containing data-flow values.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ISymbol>>>> DataFlowMap;

        /// <summary>
        /// DataFlowMap containing field reachability values.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ISymbol>>>> FieldReachabilityMap;

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
        /// <param name="context">AnalysisContext</param>
        /// <param name="model">SemanticModel</param>
        protected DataFlowAnalysis(AnalysisContext context, SemanticModel model)
        {
            this.DataFlowMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ISymbol>>>>();
            this.FieldReachabilityMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ISymbol>>>>();
            this.ReferenceTypeMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ITypeSymbol>>>>();
            this.ReferenceResetMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
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
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        public bool TryGetDataFlowMapForSyntaxNode(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            out Dictionary<ISymbol, HashSet<ISymbol>> map)
        {
            if (cfgNode != null && syntaxNode != null &&
                this.DataFlowMap.ContainsKey(cfgNode) &&
                this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
            {
                map = this.DataFlowMap[cfgNode][syntaxNode];
                return true;
            }

            map = new Dictionary<ISymbol, HashSet<ISymbol>>();
            return false;
        }

        /// <summary>
        /// Tries to return the field reachability map for the given syntax node.
        /// Returns false and a null, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">FieldReachabilityMap</param>
        /// <returns>Boolean</returns>
        public bool TryGetFieldReachabilityMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, out Dictionary<ISymbol, HashSet<ISymbol>> map)
        {
            if (cfgNode != null && syntaxNode != null &&
                this.FieldReachabilityMap.ContainsKey(cfgNode) &&
                this.FieldReachabilityMap[cfgNode].ContainsKey(syntaxNode))
            {
                map = this.FieldReachabilityMap[cfgNode][syntaxNode];
                return true;
            }

            map = null;
            return false;
        }

        /// <summary>
        /// Tries to return the reference type map for the given syntax node.
        /// Returns false and a null, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">ReferenceTypeMap</param>
        /// <returns>Boolean</returns>
        public bool TryGetReferenceTypeMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, out Dictionary<ISymbol, HashSet<ITypeSymbol>> map)
        {
            if (cfgNode != null && syntaxNode != null &&
                this.ReferenceTypeMap.ContainsKey(cfgNode) &&
                this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode))
            {
                map = this.ReferenceTypeMap[cfgNode][syntaxNode];
                return true;
            }

            map = null;
            return false;
        }

        /// <summary>
        /// Returns true if the given reference resets in
        /// the control-flow graph node.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="refSyntaxNode">Reference SyntaxNode</param>
        /// <param name="refCfgNode">Reference ControlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        public bool DoesReferenceResetInControlFlowGraphNode(ISymbol reference, SyntaxNode refSyntaxNode,
            ControlFlowGraphNode refCfgNode)
        {
            if (this.ReferenceResetMap.ContainsKey(refCfgNode) &&
                this.ReferenceResetMap[refCfgNode].ContainsKey(refSyntaxNode) &&
                this.ReferenceResetMap[refCfgNode][refSyntaxNode].Contains(reference))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given reference resets until it
        /// reaches the target syntax node.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="refSyntaxNode">Reference SyntaxNode</param>
        /// <param name="refCfgNode">Reference ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target ControlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        public bool DoesReferenceResetUntilSyntaxNode(ISymbol reference, SyntaxNode refSyntaxNode,
            ControlFlowGraphNode refCfgNode, SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode)
        {
            if (!refCfgNode.IsInSameMethodAs(targetCfgNode))
            {
                return this.DoesReferenceResetUntilSyntaxNode(reference, targetSyntaxNode,
                    targetCfgNode, refSyntaxNode, refCfgNode, true);
            }

            return this.DoesReferenceResetUntilSyntaxNode(reference, refSyntaxNode,
                refCfgNode, targetSyntaxNode, targetCfgNode, false);
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
                this.MapDataFlowInReferences(new List<ISymbol> { paramSymbol }, paramSymbol,
                    methodSummary.Method.ParameterList, methodSummary.EntryNode);
            }

            this.AnalyzeControlFlowGraphNode(methodSummary.EntryNode, methodSummary.Method.ParameterList,
                methodSummary.EntryNode);
        }

        #endregion

        #region data-flow analysis methods

        /// <summary>
        /// Analyzes the data-flow of the given control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="previousSyntaxNode">Previous SyntaxNode</param>
        /// <param name="previousCfgNode">Previous ControlFlowGraphNode</param>
        private void AnalyzeControlFlowGraphNode(ControlFlowGraphNode cfgNode, SyntaxNode previousSyntaxNode,
            ControlFlowGraphNode previousCfgNode)
        {
            if (cfgNode.SyntaxNodes.Count > 0)
            {
                this.AnalyzeRegularControlFlowGraphNode(cfgNode, previousSyntaxNode,
                    previousCfgNode);
                previousSyntaxNode = cfgNode.SyntaxNodes.Last();
                previousCfgNode = cfgNode;
            }

            foreach (var successor in cfgNode.GetImmediateSuccessors())
            {
                if (successor.IsLoopHeadNode && cfgNode.IsSuccessorOf(cfgNode) &&
                    this.ReachedFixpoint(previousSyntaxNode, cfgNode, successor))
                {
                    continue;
                }

                this.AnalyzeControlFlowGraphNode(successor, previousSyntaxNode, cfgNode);
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the given regular control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="previousSyntaxNode">Previous SyntaxNode</param>
        /// <param name="previousCfgNode">Previous ControlFlowGraphNode</param>
        private void AnalyzeRegularControlFlowGraphNode(ControlFlowGraphNode cfgNode, SyntaxNode previousSyntaxNode,
            ControlFlowGraphNode previousCfgNode)
        {
            foreach (var syntaxNode in cfgNode.SyntaxNodes)
            {
                this.Transfer(previousSyntaxNode, previousCfgNode, syntaxNode, cfgNode);

                var stmt = syntaxNode as StatementSyntax;
                if (stmt == null)
                {
                    continue;
                }

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
                    else if (expr.Expression is ObjectCreationExpressionSyntax)
                    {
                        var invocation = expr.Expression as ObjectCreationExpressionSyntax;
                        this.AnalyzeObjectCreationExpression(invocation, syntaxNode, cfgNode);
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
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void AnalyzeVariableDeclaration(VariableDeclarationSyntax varDecl, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            foreach (var variable in varDecl.Variables)
            {
                if (variable.Initializer == null)
                {
                    continue;
                }

                this.ResolveMethodParameterAccesses(variable.Initializer.Value, syntaxNode, cfgNode);
                this.ResolveFieldAccesses(variable.Initializer.Value, syntaxNode, cfgNode);

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
                    ISymbol varSymbol = this.SemanticModel.GetSymbolInfo(variable.Initializer.Value).Symbol;
                    this.MapDataFlowInReferences(new List<ISymbol> { varSymbol }, declSymbol, syntaxNode, cfgNode);
                    this.MarkSymbolReassignment(declSymbol, syntaxNode, cfgNode);

                    HashSet<ITypeSymbol> referenceTypes = null;
                    if (varSymbol.Kind == SymbolKind.Field)
                    {
                        var typeSymbol = this.SemanticModel.GetTypeInfo(variable.Initializer.Value).Type;
                        if (typeSymbol != null)
                        {
                            this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                                declSymbol, syntaxNode, cfgNode, true);
                        }
                    }
                    else if (this.ResolveReferenceType(out referenceTypes, varSymbol, syntaxNode, cfgNode))
                    {
                        this.MapReferenceTypesToSymbol(referenceTypes, declSymbol,
                            syntaxNode, cfgNode, true);
                    }
                    else
                    {
                        this.EraseReferenceTypesForSymbol(declSymbol, syntaxNode, cfgNode);
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

                    MethodSummary calleeSummary = null;
                    HashSet<ISymbol> reachableSymbols = null;
                    HashSet<ISymbol> returnSymbols = null;
                    this.AnalyzeInvocationExpression(invocation, syntaxNode, cfgNode, out calleeSummary,
                        out reachableSymbols, out returnSymbols);

                    if (returnSymbols.Count == 0)
                    {
                        this.ResetReferences(declSymbol, syntaxNode, cfgNode);
                    }
                    else if (returnSymbols.Contains(declSymbol))
                    {
                        this.MapDataFlowInReferences(returnSymbols, declSymbol, syntaxNode, cfgNode);
                    }
                    else
                    {
                        this.MapDataFlowInReferences(returnSymbols, declSymbol, syntaxNode, cfgNode);
                        this.MarkSymbolReassignment(declSymbol, syntaxNode, cfgNode);
                    }

                    if (reachableSymbols.Count > 0)
                    {
                        this.MapReachableFieldsToSymbol(reachableSymbols, declSymbol,
                            syntaxNode, cfgNode, true);
                    }

                    if (calleeSummary != null && calleeSummary.ReturnTypeSet.Count > 0)
                    {
                        this.MapReferenceTypesToSymbol(calleeSummary.ReturnTypeSet,
                            declSymbol, syntaxNode, cfgNode, true);
                    }
                    else
                    {
                        this.EraseReferenceTypesForSymbol(declSymbol, syntaxNode, cfgNode);
                    }
                }
                else if (variable.Initializer.Value is ObjectCreationExpressionSyntax)
                {
                    var objCreation = variable.Initializer.Value as ObjectCreationExpressionSyntax;

                    MethodSummary calleeSummary = null;
                    HashSet<ISymbol> reachableSymbols = null;
                    HashSet<ISymbol> returnSymbols = null;
                    this.AnalyzeObjectCreationExpression(objCreation, syntaxNode, cfgNode, out calleeSummary,
                        out reachableSymbols, out returnSymbols);

                    if (returnSymbols.Count == 0)
                    {
                        this.ResetReferences(declSymbol, syntaxNode, cfgNode);
                    }
                    else if (returnSymbols.Contains(declSymbol))
                    {
                        this.MapDataFlowInReferences(returnSymbols, declSymbol, syntaxNode, cfgNode);
                    }
                    else
                    {
                        this.MapDataFlowInReferences(returnSymbols, declSymbol, syntaxNode, cfgNode);
                        this.MarkSymbolReassignment(declSymbol, syntaxNode, cfgNode);
                    }

                    if (reachableSymbols.Count > 0)
                    {
                        this.MapReachableFieldsToSymbol(reachableSymbols, declSymbol,
                            syntaxNode, cfgNode, true);
                    }

                    var typeSymbol = this.SemanticModel.GetTypeInfo(objCreation).Type;
                    if (typeSymbol != null)
                    {
                        this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                            declSymbol, syntaxNode, cfgNode, true);
                    }
                    else
                    {
                        this.EraseReferenceTypesForSymbol(declSymbol, syntaxNode, cfgNode);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the given assignment expression.
        /// </summary>
        /// <param name="binaryExpr">BinaryExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void AnalyzeAssignmentExpression(AssignmentExpressionSyntax assignment,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            this.ResolveMethodParameterAccesses(assignment.Left, syntaxNode, cfgNode);
            this.ResolveMethodParameterAccesses(assignment.Right, syntaxNode, cfgNode);
            this.ResolveFieldAccesses(assignment.Left, syntaxNode, cfgNode);
            this.ResolveFieldAccesses(assignment.Right, syntaxNode, cfgNode);

            IdentifierNameSyntax lhs = null;
            IFieldSymbol lhsFieldSymbol = null;
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

                lhs = DataFlowQuerying.GetTopLevelIdentifier(assignment.Left);
                lhsFieldSymbol = this.SemanticModel.GetSymbolInfo(name).Symbol as IFieldSymbol;
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

                    lhs = DataFlowQuerying.GetTopLevelIdentifier(memberAccess.Expression);
                    lhsFieldSymbol = this.SemanticModel.GetSymbolInfo(name).Symbol as IFieldSymbol;
                }
            }

            var leftSymbol = this.SemanticModel.GetSymbolInfo(lhs).Symbol;

            if (assignment.Right is IdentifierNameSyntax ||
                assignment.Right is MemberAccessExpressionSyntax)
            {
                IdentifierNameSyntax rhs = DataFlowQuerying.GetTopLevelIdentifier(assignment.Right);

                var rightSymbol = this.SemanticModel.GetSymbolInfo(rhs).Symbol;
                this.MapDataFlowInReferences(new List<ISymbol> { rightSymbol },
                    leftSymbol, syntaxNode, cfgNode);
                this.MarkSymbolReassignment(leftSymbol, syntaxNode, cfgNode);

                if (lhsFieldSymbol != null)
                {
                    if (lhsFieldSymbol.Equals(leftSymbol))
                    {
                        Dictionary<ISymbol, HashSet<ISymbol>> fieldReachabilityMap = null;
                        if (this.TryGetFieldReachabilityMapForSyntaxNode(syntaxNode, cfgNode,
                            out fieldReachabilityMap) &&
                            fieldReachabilityMap.ContainsKey(rightSymbol))
                        {
                            this.MapReachableFieldsToSymbol(fieldReachabilityMap[rightSymbol],
                                lhsFieldSymbol, syntaxNode, cfgNode, false);
                        }
                    }
                    else
                    {
                        this.MapDataFlowInReferences(new List<ISymbol> { rightSymbol },
                            lhsFieldSymbol, syntaxNode, cfgNode);
                        this.MarkSymbolReassignment(lhsFieldSymbol, syntaxNode, cfgNode);
                    }
                }

                HashSet<ITypeSymbol> referenceTypes = null;
                if (rightSymbol.Kind == SymbolKind.Field)
                {
                    var typeSymbol = this.SemanticModel.GetTypeInfo(rhs).Type;
                    if (typeSymbol != null)
                    {
                        this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                            leftSymbol, syntaxNode, cfgNode, true);
                    }
                }
                else if (this.ResolveReferenceType(out referenceTypes, rightSymbol, syntaxNode, cfgNode))
                {
                    this.MapReferenceTypesToSymbol(referenceTypes, leftSymbol,
                        syntaxNode, cfgNode, true);
                }
                else
                {
                    this.EraseReferenceTypesForSymbol(leftSymbol, syntaxNode, cfgNode);
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

                MethodSummary calleeSummary = null;
                HashSet<ISymbol> reachableSymbols = null;
                HashSet<ISymbol> returnSymbols = null;
                this.AnalyzeInvocationExpression(invocation, syntaxNode, cfgNode, out calleeSummary,
                    out reachableSymbols, out returnSymbols);

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
                    this.MapDataFlowInReferences(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.MapDataFlowInReferences(returnSymbols, lhsFieldSymbol,
                            syntaxNode, cfgNode);
                    }
                }
                else
                {
                    this.MapDataFlowInReferences(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                    this.MarkSymbolReassignment(leftSymbol, syntaxNode, cfgNode);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.MapDataFlowInReferences(returnSymbols, lhsFieldSymbol,
                            syntaxNode, cfgNode);
                        this.MarkSymbolReassignment(lhsFieldSymbol, syntaxNode, cfgNode);
                    }
                }
                
                if (lhsFieldSymbol != null && reachableSymbols.Count > 0)
                {
                    this.MapReachableFieldsToSymbol(reachableSymbols, lhsFieldSymbol,
                        syntaxNode, cfgNode, true);
                }

                if (calleeSummary != null && calleeSummary.ReturnTypeSet.Count > 0)
                {
                    this.MapReferenceTypesToSymbol(calleeSummary.ReturnTypeSet,
                        leftSymbol, syntaxNode, cfgNode, true);
                }
                else
                {
                    this.EraseReferenceTypesForSymbol(leftSymbol, syntaxNode, cfgNode);
                }
            }
            else if (assignment.Right is ObjectCreationExpressionSyntax)
            {
                var objCreation = assignment.Right as ObjectCreationExpressionSyntax;

                MethodSummary calleeSummary = null;
                HashSet<ISymbol> reachableSymbols = null;
                HashSet<ISymbol> returnSymbols = null;
                this.AnalyzeObjectCreationExpression(objCreation, syntaxNode, cfgNode, out calleeSummary,
                    out reachableSymbols, out returnSymbols);

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
                    this.MapDataFlowInReferences(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.MapDataFlowInReferences(returnSymbols, lhsFieldSymbol,
                            syntaxNode, cfgNode);
                    }
                }
                else
                {
                    this.MapDataFlowInReferences(returnSymbols, leftSymbol, syntaxNode, cfgNode);
                    this.MarkSymbolReassignment(leftSymbol, syntaxNode, cfgNode);
                    if (lhsFieldSymbol != null && !lhsFieldSymbol.Equals(leftSymbol))
                    {
                        this.MapDataFlowInReferences(returnSymbols, lhsFieldSymbol,
                            syntaxNode, cfgNode);
                        this.MarkSymbolReassignment(lhsFieldSymbol, syntaxNode, cfgNode);
                    }
                }

                if (lhsFieldSymbol != null && reachableSymbols.Count > 0)
                {
                    this.MapReachableFieldsToSymbol(reachableSymbols, lhsFieldSymbol,
                        syntaxNode, cfgNode, true);
                }

                var typeSymbol = this.SemanticModel.GetTypeInfo(objCreation).Type;
                if (typeSymbol != null)
                {
                    this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                        leftSymbol, syntaxNode, cfgNode, true);
                }
                else
                {
                    this.EraseReferenceTypesForSymbol(leftSymbol, syntaxNode, cfgNode);
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the given return statement.
        /// </summary>
        /// <param name="retStmt">ReturnStatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void AnalyzeReturnStatement(ReturnStatementSyntax retStmt, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            this.ResolveMethodParameterAccesses(retStmt.Expression, syntaxNode, cfgNode);
            this.ResolveFieldAccesses(retStmt.Expression, syntaxNode, cfgNode);

            HashSet<ISymbol> returnSymbols = null;
            if (retStmt.Expression is IdentifierNameSyntax ||
                retStmt.Expression is MemberAccessExpressionSyntax)
            {
                var rightSymbol = this.SemanticModel.GetSymbolInfo(retStmt.Expression).Symbol;
                returnSymbols = new HashSet<ISymbol> { rightSymbol };
                
                HashSet<ITypeSymbol> referenceTypes = null;
                if (rightSymbol.Kind == SymbolKind.Field)
                {
                    var typeSymbol = this.SemanticModel.GetTypeInfo(retStmt.Expression).Type;
                    if (typeSymbol != null)
                    {
                        this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                            rightSymbol, syntaxNode, cfgNode, true);
                        cfgNode.GetMethodSummary().ReturnTypeSet.Add(typeSymbol);
                    }
                }
                else if (this.ResolveReferenceType(out referenceTypes, rightSymbol, syntaxNode, cfgNode))
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

                MethodSummary calleeSummary = null;
                HashSet<ISymbol> reachableSymbols = null;
                this.AnalyzeInvocationExpression(invocation, syntaxNode, cfgNode, out calleeSummary,
                    out reachableSymbols, out returnSymbols);

                if (calleeSummary != null)
                {
                    foreach (var referenceType in calleeSummary.ReturnTypeSet)
                    {
                        cfgNode.GetMethodSummary().ReturnTypeSet.Add(referenceType);
                    }
                }
            }
            else if (retStmt.Expression is ObjectCreationExpressionSyntax)
            {
                var objCreation = retStmt.Expression as ObjectCreationExpressionSyntax;

                MethodSummary calleeSummary = null;
                HashSet<ISymbol> reachableSymbols = null;
                this.AnalyzeObjectCreationExpression(objCreation, syntaxNode, cfgNode, out calleeSummary,
                    out reachableSymbols, out returnSymbols);

                var referenceType = this.SemanticModel.GetTypeInfo(objCreation).Type;
                if (referenceType != null)
                {
                    cfgNode.GetMethodSummary().ReturnTypeSet.Add(referenceType);
                }
            }

            if (returnSymbols == null)
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

            foreach (var symbol in returnSymbols.Where(s => s.Kind == SymbolKind.Parameter))
            {
                if (this.DoesReferenceResetUntilSyntaxNode(symbol,
                        cfgNode.GetMethodSummary().EntryNode.SyntaxNodes.First(),
                        cfgNode.GetMethodSummary().EntryNode, syntaxNode, cfgNode, true))
                {
                    continue;
                }

                cfgNode.GetMethodSummary().ReturnSet.Item1.Add(indexMap[symbol]);
            }

            foreach (var symbol in returnSymbols.Where(s => s.Kind == SymbolKind.Field))
            {
                cfgNode.GetMethodSummary().ReturnSet.Item2.Add(symbol as IFieldSymbol);
            }

            Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
            if (this.TryGetDataFlowMapForSyntaxNode(syntaxNode, cfgNode, out dataFlowMap))
            {
                foreach (var symbol in returnSymbols.Where(s => dataFlowMap.ContainsKey(s)))
                {
                    foreach (var reference in dataFlowMap[symbol].Where(s => s.Kind == SymbolKind.Parameter))
                    {
                        if (this.DoesReferenceResetUntilSyntaxNode(reference,
                            cfgNode.GetMethodSummary().EntryNode.SyntaxNodes.First(),
                            cfgNode.GetMethodSummary().EntryNode, syntaxNode, cfgNode, true))
                        {
                            continue;
                        }

                        cfgNode.GetMethodSummary().ReturnSet.Item1.Add(indexMap[reference]);
                    }

                    foreach (var reference in dataFlowMap[symbol].Where(r => r.Kind == SymbolKind.Field))
                    {
                        cfgNode.GetMethodSummary().ReturnSet.Item2.Add(reference as IFieldSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the given object creation expression.
        /// </summary>
        /// <param name="objCreation">ObjectCreationExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void AnalyzeObjectCreationExpression(ObjectCreationExpressionSyntax objCreation, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            MethodSummary calleeSummary = null;
            HashSet<ISymbol> reachableSymbols = null;
            HashSet<ISymbol> returnSymbols = null;

            this.AnalyzeObjectCreationExpression(objCreation, syntaxNode, cfgNode, out calleeSummary,
                out reachableSymbols, out returnSymbols);
        }

        /// <summary>
        /// Analyzes the data-flow of the given invocation expression.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void AnalyzeInvocationExpression(InvocationExpressionSyntax invocation, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            MethodSummary calleeSummary = null;
            HashSet<ISymbol> reachableSymbols = null;
            HashSet<ISymbol> returnSymbols = null;

            this.AnalyzeInvocationExpression(invocation, syntaxNode, cfgNode, out calleeSummary,
                out reachableSymbols, out returnSymbols);
        }

        /// <summary>
        /// Analyzes the data-flow of the given object creation expression.
        /// </summary>
        /// <param name="objCreation">ObjectCreationExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="reachableSymbols">Reachable symbols</param>
        /// <param name="returnSymbols">Return symbols</param>
        private void AnalyzeObjectCreationExpression(ObjectCreationExpressionSyntax objCreation, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, out MethodSummary calleeSummary, out HashSet<ISymbol> reachableSymbols,
            out HashSet<ISymbol> returnSymbols)
        {
            this.MapSymbolsInCall(objCreation, cfgNode);

            calleeSummary = this.AnalysisContext.TryGetSummary(objCreation, this.SemanticModel);
            reachableSymbols = this.ResolveSideEffectsInObjectCreation(objCreation, calleeSummary, syntaxNode, cfgNode);
            returnSymbols = this.GetReturnSymbols(objCreation, calleeSummary);
        }

        /// <summary>
        /// Analyzes the data-flow of the given invocation expression.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="reachableSymbols">Reachable symbols</param>
        /// <param name="returnSymbols">Return symbols</param>
        private void AnalyzeInvocationExpression(InvocationExpressionSyntax invocation, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, out MethodSummary calleeSummary, out HashSet<ISymbol> reachableSymbols,
            out HashSet<ISymbol> returnSymbols)
        {
            this.MapSymbolsInCall(invocation, cfgNode);

            calleeSummary = this.AnalysisContext.TryGetSummary(invocation, this.SemanticModel);
            reachableSymbols = this.ResolveSideEffectsInInvocation(invocation, calleeSummary, syntaxNode, cfgNode);
            returnSymbols = this.GetReturnSymbols(invocation, calleeSummary);
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
        private void Transfer(SyntaxNode previousSyntaxNode, ControlFlowGraphNode previousCfgNode,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> previousDataFlowMap = null;
            if (this.TryGetDataFlowMapForSyntaxNode(previousSyntaxNode, previousCfgNode,
                out previousDataFlowMap))
            {
                foreach (var pair in previousDataFlowMap)
                {
                    this.MapDataFlowInSymbols(pair.Value, pair.Key, syntaxNode, cfgNode);
                }
            }

            Dictionary<ISymbol, HashSet<ISymbol>> previousFieldReachabilityMap = null;
            if (this.TryGetFieldReachabilityMapForSyntaxNode(previousSyntaxNode, previousCfgNode,
                out previousFieldReachabilityMap))
            {
                foreach (var pair in previousFieldReachabilityMap)
                {
                    this.MapReachableFieldsToSymbol(pair.Value, pair.Key, syntaxNode, cfgNode, false);
                }
            }

            Dictionary<ISymbol, HashSet<ITypeSymbol>> previousReferenceTypeMap = null;
            if (this.TryGetReferenceTypeMapForSyntaxNode(previousSyntaxNode, previousCfgNode,
                out previousReferenceTypeMap))
            {
                foreach (var pair in previousReferenceTypeMap)
                {
                    this.MapReferenceTypesToSymbol(pair.Value, pair.Key, syntaxNode, cfgNode, false);
                }
            }
        }

        #endregion

        #region resolution methods

        /// <summary>
        /// Resolves side effects from the given object creation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <returns>Set of reachable field symbols</returns>
        private HashSet<ISymbol> ResolveSideEffectsInObjectCreation(ObjectCreationExpressionSyntax objCreation,
            MethodSummary calleeSummary, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (calleeSummary == null)
            {
                return new HashSet<ISymbol>();
            }

            HashSet<ISymbol> reachableFields = new HashSet<ISymbol>();
            var sideEffects = calleeSummary.GetResolvedSideEffects(objCreation.ArgumentList, this.SemanticModel);
            foreach (var sideEffect in sideEffects)
            {
                this.MapDataFlowInReferences(sideEffect.Value, sideEffect.Key, syntaxNode, cfgNode);
                this.MarkSymbolReassignment(sideEffect.Key, syntaxNode, cfgNode);
                reachableFields.Add(sideEffect.Key);
            }

            this.ResolveMethodParameterAccessesInCallee(objCreation, calleeSummary, syntaxNode, cfgNode);

            foreach (var fieldAccess in calleeSummary.FieldAccessSet)
            {
                foreach (var access in fieldAccess.Value)
                {
                    if (!cfgNode.GetMethodSummary().FieldAccessSet.ContainsKey(fieldAccess.Key as IFieldSymbol))
                    {
                        cfgNode.GetMethodSummary().FieldAccessSet.Add(fieldAccess.Key
                            as IFieldSymbol, new HashSet<SyntaxNode>());
                    }

                    cfgNode.GetMethodSummary().FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
                }
            }

            return reachableFields;
        }

        /// <summary>
        /// Resolves the side effects from the given invocation summary.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <returns>Set of reachable field symbols</returns>
        private HashSet<ISymbol> ResolveSideEffectsInInvocation(InvocationExpressionSyntax invocation,
            MethodSummary calleeSummary, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (calleeSummary == null)
            {
                return new HashSet<ISymbol>();
            }

            HashSet<ISymbol> reachableFields = new HashSet<ISymbol>();
            var sideEffects = calleeSummary.GetResolvedSideEffects(invocation.ArgumentList, this.SemanticModel);
            foreach (var sideEffect in sideEffects)
            {
                this.MapDataFlowInReferences(sideEffect.Value, sideEffect.Key, syntaxNode, cfgNode);
                this.MarkSymbolReassignment(sideEffect.Key, syntaxNode, cfgNode);
                reachableFields.Add(sideEffect.Key);
            }

            this.ResolveMethodParameterAccessesInCallee(invocation, calleeSummary, syntaxNode, cfgNode);

            foreach (var fieldAccess in calleeSummary.FieldAccessSet)
            {
                foreach (var access in fieldAccess.Value)
                {
                    if (!cfgNode.GetMethodSummary().FieldAccessSet.ContainsKey(fieldAccess.Key as IFieldSymbol))
                    {
                        cfgNode.GetMethodSummary().FieldAccessSet.Add(fieldAccess.Key
                            as IFieldSymbol, new HashSet<SyntaxNode>());
                    }

                    cfgNode.GetMethodSummary().FieldAccessSet[fieldAccess.Key as IFieldSymbol].Add(access);
                }
            }

            IdentifierNameSyntax identifier = DataFlowQuerying.GetTopLevelIdentifier(invocation.Expression);
            if (identifier != null)
            {
                var idSymbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
                if (idSymbol != null)
                {
                    this.MapReachableFieldsToSymbol(reachableFields, idSymbol, syntaxNode, cfgNode, false);
                }
            }

            return reachableFields;
        }

        /// <summary>
        /// Resolves any method parameter acccesses in the given expression.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void ResolveMethodParameterAccesses(ExpressionSyntax expr, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            if (!(expr is MemberAccessExpressionSyntax))
            {
                return;
            }

            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = DataFlowQuerying.GetTopLevelIdentifier(expr);
            if (identifier == null || name == null)
            {
                return;
            }

            var symbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol == null)
            {
                return;
            }

            var type = this.SemanticModel.GetTypeInfo(identifier).Type;
            if (type == null)
            {
                return;
            }

            if (!this.AnalysisContext.IsTypePassedByValueOrImmutable(type) &&
                !name.Equals(identifier))
            {
                Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
                if (!this.TryGetDataFlowMapForSyntaxNode(syntaxNode, cfgNode, out dataFlowMap) ||
                    !dataFlowMap.ContainsKey(symbol))
                {
                    return;
                }

                var indexMap = new Dictionary<IParameterSymbol, int>();
                var parameterList = cfgNode.GetMethodSummary().Method.ParameterList.Parameters;
                for (int idx = 0; idx < parameterList.Count; idx++)
                {
                    var paramSymbol = this.SemanticModel.GetDeclaredSymbol(parameterList[idx]);
                    indexMap.Add(paramSymbol, idx);
                }

                foreach (var reference in dataFlowMap[symbol].Where(r => r.Kind == SymbolKind.Parameter))
                {
                    if (reference.Equals(symbol) && this.DoesReferenceResetUntilSyntaxNode(reference,
                        cfgNode.GetMethodSummary().EntryNode.SyntaxNodes.First(),
                        cfgNode.GetMethodSummary().EntryNode, syntaxNode, cfgNode, true))
                    {
                        continue;
                    }

                    int index = indexMap[reference as IParameterSymbol];
                    if (!cfgNode.GetMethodSummary().ParameterAccessSet.ContainsKey(index))
                    {
                        cfgNode.GetMethodSummary().ParameterAccessSet.Add(index, new HashSet<SyntaxNode>());
                    }

                    cfgNode.GetMethodSummary().ParameterAccessSet[index].Add(syntaxNode);
                }
            }
        }

        /// <summary>
        /// Resolves any method parameter acccesses in the given callee.
        /// </summary>
        /// <param name="invocation">ExpressionSyntax</param>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void ResolveMethodParameterAccessesInCallee(ExpressionSyntax invocation,
            MethodSummary calleeSummary, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!(invocation is InvocationExpressionSyntax) &&
                !(invocation is ObjectCreationExpressionSyntax))
            {
                return;
            }

            var calleeSymbol = this.SemanticModel.GetSymbolInfo(invocation).Symbol;
            if (calleeSymbol == null)
            {
                return;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(calleeSymbol,
                this.AnalysisContext.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                return;
            }

            ArgumentListSyntax argumentList;
            if (invocation is InvocationExpressionSyntax)
            {
                var invocationCallee = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as MethodDeclarationSyntax;
                if (invocationCallee.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                    invocationCallee.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
                    invocationCallee.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    return;
                }

                argumentList = (invocation as InvocationExpressionSyntax).ArgumentList;
            }
            else
            {
                argumentList = (invocation as ObjectCreationExpressionSyntax).ArgumentList;
            }

            for (int index = 0; index < argumentList.Arguments.Count; index++)
            {
                var arg = argumentList.Arguments[index];
                if (!calleeSummary.ParameterAccessSet.ContainsKey(index))
                {
                    continue;
                }

                var identifier = DataFlowQuerying.GetTopLevelIdentifier(arg.Expression);
                var symbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
                if (symbol == null)
                {
                    return;
                }

                var type = this.SemanticModel.GetTypeInfo(identifier).Type;
                if (type == null)
                {
                    return;
                }

                if (!this.AnalysisContext.IsTypePassedByValueOrImmutable(type))
                {
                    Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
                    if (!this.TryGetDataFlowMapForSyntaxNode(syntaxNode, cfgNode, out dataFlowMap) ||
                        !dataFlowMap.ContainsKey(symbol))
                    {
                        return;
                    }

                    var indexMap = new Dictionary<IParameterSymbol, int>();
                    var parameterList = cfgNode.GetMethodSummary().Method.ParameterList.Parameters;
                    for (int idx = 0; idx < parameterList.Count; idx++)
                    {
                        var paramSymbol = this.SemanticModel.GetDeclaredSymbol(parameterList[idx]);
                        indexMap.Add(paramSymbol, idx);
                    }

                    foreach (var reference in dataFlowMap[symbol].Where(r => r.Kind == SymbolKind.Parameter))
                    {
                        if (reference.Equals(symbol) && this.DoesReferenceResetUntilSyntaxNode(reference,
                            cfgNode.GetMethodSummary().EntryNode.SyntaxNodes.First(),
                            cfgNode.GetMethodSummary().EntryNode, syntaxNode, cfgNode, true))
                        {
                            continue;
                        }

                        int paramIndex = indexMap[reference as IParameterSymbol];
                        if (!cfgNode.GetMethodSummary().ParameterAccessSet.ContainsKey(paramIndex))
                        {
                            cfgNode.GetMethodSummary().ParameterAccessSet.Add(paramIndex, new HashSet<SyntaxNode>());
                        }

                        cfgNode.GetMethodSummary().ParameterAccessSet[paramIndex].UnionWith(
                            calleeSummary.ParameterAccessSet[index]);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves any field acccesses in the given expression.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void ResolveFieldAccesses(ExpressionSyntax expr, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            if (!(expr is MemberAccessExpressionSyntax))
            {
                return;
            }

            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = DataFlowQuerying.GetTopLevelIdentifier(expr);
            if (identifier == null || name == null)
            {
                return;
            }

            var symbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol == null)
            {
                return;
            }

            var type = this.SemanticModel.GetTypeInfo(identifier).Type;
            if (type == null)
            {
                return;
            }

            if (!this.AnalysisContext.IsTypePassedByValueOrImmutable(type) &&
                !name.Equals(identifier))
            {
                var definition = SymbolFinder.FindSourceDefinitionAsync(symbol,
                    this.AnalysisContext.Solution).Result;
                if (!(definition is IFieldSymbol))
                {
                    return;
                }

                var fieldDecl = definition.DeclaringSyntaxReferences.First().GetSyntax().
                    AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();
                if (this.DoesReferenceResetUntilSyntaxNode(symbol,
                    cfgNode.GetMethodSummary().EntryNode.SyntaxNodes.First(),
                    cfgNode.GetMethodSummary().EntryNode, syntaxNode, cfgNode, true))
                {
                    return;
                }

                if (!cfgNode.GetMethodSummary().FieldAccessSet.ContainsKey(symbol as IFieldSymbol))
                {
                    cfgNode.GetMethodSummary().FieldAccessSet.Add(symbol as IFieldSymbol, new HashSet<SyntaxNode>());
                }

                cfgNode.GetMethodSummary().FieldAccessSet[symbol as IFieldSymbol].Add(syntaxNode);
            }
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

        #endregion

        #region data-flow mapping methods

        /// <summary>
        /// Maps the data-flow from 'flowsFromSymbol' to 'flowsIntoSymbol'.
        /// </summary>
        /// <param name="flowsFromSymbol">Symbol</param>
        /// <param name="flowsIntoSymbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        protected void MapDataFlowInSymbols(ISymbol flowsFromSymbol, ISymbol flowsIntoSymbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            this.CheckAndInitializeDataFlowMapForSymbol(flowsIntoSymbol, syntaxNode, cfgNode);
            this.DataFlowMap[cfgNode][syntaxNode][flowsIntoSymbol].Add(flowsFromSymbol);
        }

        /// <summary>
        /// Maps the data-flow from 'flowsFromSymbols' to 'flowsIntoSymbol'.
        /// </summary>
        /// <param name="flowsFromSymbols">Symbols</param>
        /// <param name="flowsIntoSymbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        protected void MapDataFlowInSymbols(IEnumerable<ISymbol> flowsFromSymbols, ISymbol flowsIntoSymbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            this.CheckAndInitializeDataFlowMapForSymbol(flowsIntoSymbol, syntaxNode, cfgNode);
            this.DataFlowMap[cfgNode][syntaxNode][flowsIntoSymbol].UnionWith(flowsFromSymbols);
        }

        /// <summary>
        /// Maps the data-flow from the given set of references to the symbol.
        /// </summary>
        /// <param name="references">Set of references</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void MapDataFlowInReferences(IEnumerable<ISymbol> references, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            HashSet<ISymbol> additionalRefs = new HashSet<ISymbol>();
            foreach (var reference in references)
            {
                if (this.DataFlowMap.ContainsKey(cfgNode) &&
                    this.DataFlowMap[cfgNode].ContainsKey(syntaxNode) &&
                    this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(reference))
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

            this.CheckAndInitializeDataFlowMapForSymbol(symbol, syntaxNode, cfgNode, true);

            if (additionalRefs.Count > 0)
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol].UnionWith(additionalRefs);
            }
            else
            {
                this.DataFlowMap[cfgNode][syntaxNode][symbol].UnionWith(references);
            }
        }

        /// <summary>
        /// Maps the given set of reachable field symbols to the given symbol.
        /// </summary>
        /// <param name="fields">Set of field symbols</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        /// <param name="reset">Reset map</param>
        private void MapReachableFieldsToSymbol(IEnumerable<ISymbol> fields, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, bool reset)
        {
            this.CheckAndInitializeFieldReachabilityMapForSymbol(symbol, syntaxNode, cfgNode, true);
            this.FieldReachabilityMap[cfgNode][syntaxNode][symbol].UnionWith(fields);
        }

        /// <summary>
        /// Maps the given set of reference types to the given symbol.
        /// </summary>
        /// <param name="types">Set of reference types</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        /// <param name="reset">Reset map</param>
        private void MapReferenceTypesToSymbol(IEnumerable<ITypeSymbol> types, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, bool reset)
        {
            this.CheckAndInitializeReferenceTypeMapForSymbol(symbol, syntaxNode, cfgNode, reset);
            this.ReferenceTypeMap[cfgNode][syntaxNode][symbol].UnionWith(types);
        }

        /// <summary>
        /// Maps symbols in the call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void MapSymbolsInCall(ExpressionSyntax call, ControlFlowGraphNode cfgNode)
        {
            ArgumentListSyntax argumentList;
            if (call is InvocationExpressionSyntax)
            {
                argumentList = (call as InvocationExpressionSyntax).ArgumentList;
            }
            else
            {
                argumentList = (call as ObjectCreationExpressionSyntax).ArgumentList;
            }

            List<MemberAccessExpressionSyntax> accesses = argumentList.DescendantNodesAndSelf().
                OfType<MemberAccessExpressionSyntax>().ToList();

            foreach (var access in accesses)
            {
                IdentifierNameSyntax id = DataFlowQuerying.GetTopLevelIdentifier(access);
                if (id == null)
                {
                    continue;
                }

                var accessSymbol = this.SemanticModel.GetSymbolInfo(id).Symbol;
                this.MapDataFlowInSymbols(accessSymbol, accessSymbol, cfgNode.SyntaxNodes[0], cfgNode);
            }
        }

        /// <summary>
        /// Erases the set of reference types for the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void EraseReferenceTypesForSymbol(ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (this.ReferenceTypeMap.ContainsKey(cfgNode) &&
                this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode) &&
                this.ReferenceTypeMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ReferenceTypeMap[cfgNode][syntaxNode].Remove(symbol);
            }
        }

        /// <summary>
        /// Resets the references of the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void ResetReferences(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            this.CheckAndInitializeDataFlowMapForSymbol(symbol, syntaxNode, cfgNode, true);
            this.DataFlowMap[cfgNode][syntaxNode][symbol].Add(symbol);
            this.MarkSymbolReassignment(symbol, syntaxNode, cfgNode);
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
            }

            if (!this.ReferenceResetMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReferenceResetMap[cfgNode].Add(syntaxNode, new HashSet<ISymbol>());
            }

            this.ReferenceResetMap[cfgNode][syntaxNode].Add(symbol);
        }

        /// <summary>
        /// Returns true if the given reference resets until it
        /// reaches the target syntax node.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="refSyntaxNode">Reference SyntaxNode</param>
        /// <param name="refCfgNode">Reference ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target ControlFlowGraphNode</param>
        /// <param name="track">Tracking</param>
        /// <returns>Boolean</returns>
        private bool DoesReferenceResetUntilSyntaxNode(ISymbol reference, SyntaxNode refSyntaxNode,
            ControlFlowGraphNode refCfgNode, SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode,
            bool track)
        {
            if (refSyntaxNode.Equals(targetSyntaxNode) && refCfgNode.Equals(targetCfgNode) &&
                !track)
            {
                return false;
            }

            return this.DoesReferenceResetUntilSyntaxNode(reference, refSyntaxNode, refCfgNode,
                targetSyntaxNode, targetCfgNode, new HashSet<ControlFlowGraphNode>(), track);
        }

        /// <summary>
        /// Returns true if the given reference resets until it
        /// reaches the target syntax node.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="refSyntaxNode">Reference SyntaxNode</param>
        /// <param name="refCfgNode">Reference ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target ControlFlowGraphNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="track">Tracking</param>
        /// <returns>Boolean</returns>
        private bool DoesReferenceResetUntilSyntaxNode(ISymbol reference, SyntaxNode refSyntaxNode,
            ControlFlowGraphNode refCfgNode, SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode,
            HashSet<ControlFlowGraphNode> visited, bool track)
        {
            visited.Add(refCfgNode);

            bool result = false;
            foreach (var node in refCfgNode.SyntaxNodes)
            {
                if (track)
                {
                    if (this.ReferenceResetMap.ContainsKey(refCfgNode) &&
                        this.ReferenceResetMap[refCfgNode].ContainsKey(node) &&
                        this.ReferenceResetMap[refCfgNode][node].Contains(reference))
                    {
                        result = true;
                        break;
                    }
                    else if (refSyntaxNode.Equals(targetSyntaxNode) &&
                        refCfgNode.Equals(targetCfgNode))
                    {
                        return result;
                    }
                }
                else if (!track && node.Equals(refSyntaxNode))
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
                        this.DoesReferenceResetUntilSyntaxNode(reference, successor.SyntaxNodes.First(),
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
        /// <param name="loopHeadCfgNode">Loop head ControlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        private bool ReachedFixpoint(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            ControlFlowGraphNode loopHeadCfgNode)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> dataFlowMap = null;
            this.TryGetDataFlowMapForSyntaxNode(syntaxNode, cfgNode, out dataFlowMap);

            Dictionary<ISymbol, HashSet<ISymbol>> loopHeadDataFlowMap = null;
            this.TryGetDataFlowMapForSyntaxNode(loopHeadCfgNode.SyntaxNodes.First(),
                loopHeadCfgNode, out loopHeadDataFlowMap);

            if (dataFlowMap == null || loopHeadDataFlowMap == null)
            {
                return false;
            }

            foreach (var pair in dataFlowMap)
            {
                if (!loopHeadDataFlowMap.ContainsKey(pair.Key) ||
                    pair.Value.Any(v => !loopHeadDataFlowMap[pair.Key].Contains(v)))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region map updating methods

        /// <summary>
        /// Checks the data-flow map for the given symbol,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="flowsIntoSymbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="resetMap">Should reset the map</param>
        private void CheckAndInitializeDataFlowMapForSymbol(ISymbol flowsIntoSymbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, bool resetMap = false)
        {
            this.CheckAndInitializeDataFlowMapForSyntaxNode(syntaxNode, cfgNode);
            if (!this.DataFlowMap[cfgNode][syntaxNode].ContainsKey(flowsIntoSymbol))
            {
                this.DataFlowMap[cfgNode][syntaxNode].Add(flowsIntoSymbol, new HashSet<ISymbol>());
            }
            else if (resetMap)
            {
                this.DataFlowMap[cfgNode][syntaxNode][flowsIntoSymbol].Clear();
            }
        }

        /// <summary>
        /// Checks the data-flow map for the given syntax node,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void CheckAndInitializeDataFlowMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            if (!this.DataFlowMap.ContainsKey(cfgNode))
            {
                this.DataFlowMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ISymbol>>>());
            }

            if (!this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.DataFlowMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ISymbol>>());
            }
        }

        /// <summary>
        /// Checks the reference type map for the given symbol,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="resetMap">Should reset the map</param>
        private void CheckAndInitializeReferenceTypeMapForSymbol(ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, bool resetMap = false)
        {
            this.CheckAndInitializeReferenceTypeMapForSyntaxNode(syntaxNode, cfgNode);
            if (!this.ReferenceTypeMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ReferenceTypeMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ITypeSymbol>());
            }
            else if (resetMap)
            {
                this.ReferenceTypeMap[cfgNode][syntaxNode][symbol].Clear();
            }
        }

        /// <summary>
        /// Checks the reference type map for the given syntax node,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void CheckAndInitializeReferenceTypeMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            if (!this.ReferenceTypeMap.ContainsKey(cfgNode))
            {
                this.ReferenceTypeMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ITypeSymbol>>>());
            }

            if (!this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReferenceTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ITypeSymbol>>());
            }
        }

        /// <summary>
        /// Checks the field reachability map for the given symbol,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        /// <param name="reset">Reset map</param>
        private void CheckAndInitializeFieldReachabilityMapForSymbol(ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode, bool resetMap = false)
        {
            this.CheckAndInitializeFieldReachabilityMapForSyntaxNode(syntaxNode, cfgNode);
            if (!this.FieldReachabilityMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.FieldReachabilityMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
            }
            else if (resetMap)
            {
                this.FieldReachabilityMap[cfgNode][syntaxNode][symbol].Clear();
            }
        }

        /// <summary>
        /// Checks the field reachability map for the given syntax node,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void CheckAndInitializeFieldReachabilityMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            if (!this.FieldReachabilityMap.ContainsKey(cfgNode))
            {
                this.FieldReachabilityMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ISymbol>>>());
            }

            if (!this.FieldReachabilityMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.FieldReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ISymbol>>());
            }
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
                Console.WriteLine("... |");
                Console.WriteLine("... | . Data-flow map");
                foreach (var cfgNode in this.DataFlowMap)
                {
                    this.PrintDataFlowMap(cfgNode.Key);
                }
            }
        }

        /// <summary>
        /// Prints the field reachability map.
        /// </summary>
        internal void PrintFieldReachabilityMap()
        {
            if (this.FieldReachabilityMap.Count > 0)
            {
                Console.WriteLine("... |");
                Console.WriteLine("... | . Field reachability map");
                foreach (var cfgNode in this.FieldReachabilityMap)
                {
                    this.PrintFieldReachabilityMap(cfgNode.Key);
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
                Console.WriteLine("... |");
                Console.WriteLine("... | . Types of references");
                foreach (var cfgNode in this.ReferenceTypeMap)
                {
                    this.PrintReferenceTypes(cfgNode.Key);
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
                Console.WriteLine("... |");
                Console.WriteLine("... | . Statements that reset references");
                foreach (var cfgNode in this.ReferenceResetMap)
                {
                    this.PrintStatementsThatResetReferences(cfgNode.Key);
                }
            }
        }

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void PrintDataFlowMap(ControlFlowGraphNode cfgNode)
        {
            if (this.DataFlowMap.ContainsKey(cfgNode))
            {
                Console.WriteLine("... | ... CFG node '{0}'", cfgNode.Id);
                foreach (var syntaxNode in this.DataFlowMap[cfgNode])
                {
                    this.PrintDataFlowMap(syntaxNode.Key, cfgNode);
                }
            }
        }

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void PrintDataFlowMap(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (this.DataFlowMap.ContainsKey(cfgNode) &&
                this.DataFlowMap[cfgNode].ContainsKey(syntaxNode))
            {
                Console.WriteLine("... | ..... '{0}'", syntaxNode);
                foreach (var pair in this.DataFlowMap[cfgNode][syntaxNode])
                {
                    foreach (var symbol in pair.Value)
                    {
                        Console.WriteLine("... | ....... '{0}' flows into '{1}'",
                            symbol.Name, pair.Key.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Prints the field reachability map.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void PrintFieldReachabilityMap(ControlFlowGraphNode cfgNode)
        {
            if (this.FieldReachabilityMap.ContainsKey(cfgNode))
            {
                Console.WriteLine("... | ... CFG node '{0}'", cfgNode.Id);
                foreach (var syntaxNode in this.FieldReachabilityMap[cfgNode])
                {
                    this.PrintFieldReachabilityMap(syntaxNode.Key, cfgNode);
                }
            }
        }

        /// <summary>
        /// Prints the field reachability map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void PrintFieldReachabilityMap(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (this.FieldReachabilityMap.ContainsKey(cfgNode) &&
                this.FieldReachabilityMap[cfgNode].ContainsKey(syntaxNode))
            {
                Console.WriteLine("... | ..... '{0}'", syntaxNode);
                foreach (var pair in this.FieldReachabilityMap[cfgNode][syntaxNode])
                {
                    foreach (var symbol in pair.Value)
                    {
                        Console.WriteLine("... | ....... Field '{0}' is reachable from '{1}'",
                            symbol.Name, pair.Key.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Prints the type of references.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void PrintReferenceTypes(ControlFlowGraphNode cfgNode)
        {
            if (this.ReferenceTypeMap.ContainsKey(cfgNode))
            {
                Console.WriteLine("... | ... CFG node '{0}'", cfgNode.Id);
                foreach (var syntaxNode in this.ReferenceTypeMap[cfgNode])
                {
                    this.PrintReferenceTypes(syntaxNode.Key, cfgNode);
                }
            }
        }

        /// <summary>
        /// Prints the type of references.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void PrintReferenceTypes(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (this.ReferenceTypeMap.ContainsKey(cfgNode) &&
                this.ReferenceTypeMap[cfgNode].ContainsKey(syntaxNode))
            {
                Console.WriteLine("... | ..... '{0}'", syntaxNode);
                foreach (var pair in this.ReferenceTypeMap[cfgNode][syntaxNode])
                {
                    foreach (var type in pair.Value)
                    {
                        Console.WriteLine("... | ....... '{0}' has type '{1}'",
                            pair.Key.Name, type.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Prints statements that reset references.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void PrintStatementsThatResetReferences(ControlFlowGraphNode cfgNode)
        {
            if (this.ReferenceResetMap.ContainsKey(cfgNode))
            {
                Console.WriteLine("... | ... CFG node '{0}'", cfgNode.Id);
                foreach (var syntaxNode in this.ReferenceResetMap[cfgNode])
                {
                    this.PrintStatementsThatResetReferences(syntaxNode.Key, cfgNode);
                }
            }
        }

        /// <summary>
        /// Prints statements that reset references.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private void PrintStatementsThatResetReferences(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode)
        {
            if (this.ReferenceResetMap.ContainsKey(cfgNode) &&
                this.ReferenceResetMap[cfgNode].ContainsKey(syntaxNode))
            {
                Console.WriteLine("... | ..... '{0}'", syntaxNode);
                foreach (var symbol in this.ReferenceResetMap[cfgNode][syntaxNode])
                {
                    Console.WriteLine("... | ....... Resets '{0}'", symbol.Name);
                }
            }
        }

        #endregion
    }
}
