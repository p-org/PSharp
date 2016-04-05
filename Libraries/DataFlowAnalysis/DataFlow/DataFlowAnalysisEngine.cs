//-----------------------------------------------------------------------
// <copyright file="DataFlowAnalysisEngine.cs">
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

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a data-flow analysis engine.
    /// </summary>
    public class DataFlowAnalysisEngine
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
        /// The method summary being analyzed.
        /// </summary>
        private MethodSummary MethodSummary;

        /// <summary>
        /// Map containing data-flow values.
        /// </summary>
        private Dictionary<Statement, Dictionary<DataFlowSymbol,
            HashSet<DataFlowSymbol>>> DataFlowMap;

        /// <summary>
        /// Map containing reference types.
        /// </summary>
        private Dictionary<Statement, Dictionary<DataFlowSymbol,
            HashSet<ITypeSymbol>>> ReferenceTypeMap;

        /// <summary>
        /// Map containing method summaries at each call site.
        /// </summary>
        private Dictionary<Statement, Dictionary<ISymbol,
            HashSet<MethodSummary>>> CalleeSummaryMap;

        /// <summary>
        /// Map containing gives-up ownership syntax statements.
        /// </summary>
        private Dictionary<Statement, HashSet<ISymbol>> GivesUpOwnershipMap;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="methodSummary">MethodSummary</param>
        /// <param name="context">AnalysisContext</param>
        /// <param name="model">SemanticModel</param>
        private DataFlowAnalysisEngine(MethodSummary methodSummary,
            AnalysisContext context, SemanticModel model)
        {
            this.MethodSummary = methodSummary;
            this.AnalysisContext = context;
            this.SemanticModel = model;

            this.DataFlowMap = new Dictionary<Statement,
                Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>>>();
            this.ReferenceTypeMap = new Dictionary<Statement,
                Dictionary<DataFlowSymbol, HashSet<ITypeSymbol>>>();
            this.CalleeSummaryMap = new Dictionary<Statement,
                Dictionary<ISymbol, HashSet<MethodSummary>>>();
            this.GivesUpOwnershipMap = new Dictionary<Statement,
                HashSet<ISymbol>>();
        }

        #endregion

        #region public analysis API

        /// <summary>
        /// Returns true if the target symbol flows from the parameter list.
        /// </summary>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        public static bool FlowsFromParameterList(ISymbol targetSymbol,
            Statement targetStatement)
        {
            foreach (var param in targetStatement.GetMethodSummary().Method.ParameterList.Parameters)
            {
                IParameterSymbol paramSymbol = targetStatement.GetMethodSummary().
                    DataFlowAnalysisEngine.SemanticModel.GetDeclaredSymbol(param);
                if (DataFlowAnalysisEngine.FlowsIntoSymbol(paramSymbol, targetSymbol,
                    targetStatement.GetMethodSummary().EntryNode.Statements[0],
                    targetStatement))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the target symbol flows from the parameter symbol.
        /// </summary>
        /// <param name="paramSymbol">Parameter Symbol</param>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        public static bool FlowsFromParameter(IParameterSymbol paramSymbol, ISymbol targetSymbol,
            Statement targetStatement)
        {
            return DataFlowAnalysisEngine.FlowsIntoSymbol(paramSymbol, targetSymbol,
                targetStatement.GetMethodSummary().EntryNode.Statements[0],
                targetStatement);
        }

        /// <summary>
        /// Returns true if the symbol flows into the target symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="statement">Statement</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        public static bool FlowsIntoSymbol(ISymbol symbol, ISymbol targetSymbol,
            Statement statement, Statement targetStatement)
        {
            if (!statement.IsInSameMethodAs(targetStatement))
            {
                return false;
            }

            var dfaEngine = targetStatement.GetMethodSummary().DataFlowAnalysisEngine;
            var fromSymbols = dfaEngine.GetDataFlowSymbols(symbol, statement);
            var toSymbols = dfaEngine.GetDataFlowSymbols(targetSymbol, targetStatement);

            return dfaEngine.FlowsIntoSymbol(fromSymbols, toSymbols, statement, targetStatement);
        }

        #endregion

        #region internal analysis API

        /// <summary>
        /// Analyzes the data-flow of the method.
        /// </summary>
        /// <param name="methodSummary">MethodSummary</param>
        /// <param name="context">AnalysisContext</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>DataFlowAnalysisEngine</returns>
        internal static DataFlowAnalysisEngine Create(MethodSummary methodSummary,
            AnalysisContext context, SemanticModel model)
        {
            return new DataFlowAnalysisEngine(methodSummary, context, model);
        }

        /// <summary>
        /// Runs the data-flow analysis.
        /// </summary>
        internal void Run()
        {
            foreach (var param in this.MethodSummary.Method.ParameterList.Parameters)
            {
                ITypeSymbol paramType = this.SemanticModel.GetTypeInfo(param.Type).Type;
                if (this.AnalysisContext.IsTypePassedByValueOrImmutable(paramType))
                {
                    continue;
                }

                IParameterSymbol paramSymbol = this.SemanticModel.GetDeclaredSymbol(param);
                DataFlowSymbol paramDFSymbol = new DataFlowSymbol(paramSymbol);

                this.MapDataFlowInSymbols(new List<DataFlowSymbol> { paramDFSymbol },
                    paramDFSymbol, this.MethodSummary.EntryNode.Statements[0]);
                this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { paramType },
                    paramDFSymbol, this.MethodSummary.EntryNode.Statements[0], false);
            }

            this.AnalyzeControlFlowGraphNode(this.MethodSummary.EntryNode,
                this.MethodSummary.EntryNode.Statements[0]);
        }

        /// <summary>
        /// Tries to return the data-flow map for the statement.
        /// Returns false and a null, if it cannot find such map.
        /// </summary>
        /// <param name="statement">Statement</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        internal bool TryGetDataFlowMapForStatement(Statement statement,
            out Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> map)
        {
            map = new Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>>();
            if (statement != null &&
                this.DataFlowMap.ContainsKey(statement))
            {
                foreach (var pair in this.DataFlowMap[statement])
                {
                    map.Add(pair.Key, new HashSet<DataFlowSymbol>(pair.Value));
                }

                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Tries to return the reference type map for the statement.
        /// Returns false and a null, if it cannot find such map.
        /// </summary>
        /// <param name="statement">Statement</param>
        /// <param name="map">ReferenceTypeMap</param>
        /// <returns>Boolean</returns>
        internal bool TryGetReferenceTypeMapForStatement(Statement statement,
            out Dictionary<DataFlowSymbol, HashSet<ITypeSymbol>> map)
        {
            map = new Dictionary<DataFlowSymbol, HashSet<ITypeSymbol>>();
            if (statement != null &&
                this.ReferenceTypeMap.ContainsKey(statement))
            {
                foreach (var pair in this.ReferenceTypeMap[statement])
                {
                    map.Add(pair.Key, new HashSet<ITypeSymbol>(pair.Value));
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to return the callee summary map for the statement.
        /// Returns false and a null, if it cannot find such map.
        /// </summary>
        /// <param name="statement">Statement</param>
        /// <param name="map">DataFlowMap</param>
        /// <returns>Boolean</returns>
        internal bool TryGetCalleeSummaryMapForStatement(Statement statement,
            out Dictionary<ISymbol, HashSet<MethodSummary>> map)
        {
            map = new Dictionary<ISymbol, HashSet<MethodSummary>>();
            if (statement != null &&
                this.CalleeSummaryMap.ContainsKey(statement))
            {
                foreach (var pair in this.CalleeSummaryMap[statement])
                {
                    map.Add(pair.Key, new HashSet<MethodSummary>(pair.Value));
                }

                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Returns true if the symbols flows into the target symbols.
        /// </summary>
        /// <param name="fromSymbols">From DataFlowSymbols</param>
        /// <param name="toSymbols">To DataFlowSymbols</param>
        /// <param name="fromStatement">From Statement</param>
        /// <param name="toStatement">To Statement</param>
        /// <returns>Boolean</returns>
        internal bool FlowsIntoSymbol(ISet<DataFlowSymbol> fromSymbols, ISet<DataFlowSymbol> toSymbols,
            Statement fromStatement, Statement toStatement)
        {
            var aliasSymbols = this.ResolveAliasSymbols(fromSymbols, toStatement);
            if (aliasSymbols.Overlaps(toSymbols))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the data-flow symbols for the symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="statement">Statement</param>
        /// <returns>Set of data-flow symbols</returns>
        internal ISet<DataFlowSymbol> GetDataFlowSymbols(ISymbol symbol,
            Statement statement)
        {
            var resolvedSymbols = new HashSet<DataFlowSymbol>();

            Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> dataFlowMap = null;
            if (this.TryGetDataFlowMapForStatement(statement, out dataFlowMap))
            {
                resolvedSymbols.UnionWith(dataFlowMap.Keys.Where(val
                    => val.ContainingSymbol.Equals(symbol)));
            }
            
            if (resolvedSymbols.Count == 0)
            {
                resolvedSymbols.Add(new DataFlowSymbol(symbol));
            }

            return resolvedSymbols;
        }

        /// <summary>
        /// Returns the alias symbols of the symbol.
        /// </summary>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        /// <returns>DataFlowSymbols</returns>
        internal ISet<DataFlowSymbol> GetAliasSymbols(DataFlowSymbol symbol,
            Statement statement)
        {
            return this.ResolveAliasSymbols(new HashSet<DataFlowSymbol> { symbol },
                statement);
        }

        /// <summary>
        /// Returns symbols with given-up ownership.
        /// </summary>
        /// <returns>GivenUpOwnershipSymbols</returns>
        internal IEnumerable<GivenUpOwnershipSymbol> GetSymbolsWithGivenUpOwnership()
        {
            var symbols = new List<GivenUpOwnershipSymbol>();
            foreach (var pair in this.GivesUpOwnershipMap)
            {
                foreach (var symbol in pair.Value)
                {
                    symbols.Add(new GivenUpOwnershipSymbol(symbol, pair.Key));
                }
            }

            return symbols;
        }

        #endregion

        #region data-flow analysis methods

        /// <summary>
        /// Analyzes the data-flow of the control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="statement">Previous Statement</param>
        private void AnalyzeControlFlowGraphNode(ControlFlowGraphNode cfgNode,
            Statement previousStatement)
        {
            if (cfgNode.Statements.Count > 0)
            {
                this.AnalyzeRegularControlFlowGraphNode(cfgNode, previousStatement);
                previousStatement = cfgNode.Statements.Last();
            }

            foreach (var successor in cfgNode.GetImmediateSuccessors())
            {
                if (successor.IsLoopHeadNode && cfgNode.IsSuccessorOf(cfgNode) &&
                    this.ReachedFixpoint(previousStatement, successor))
                {
                    continue;
                }

                this.AnalyzeControlFlowGraphNode(successor, previousStatement);
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the regular control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="previousStatement">Previous Statement</param>
        private void AnalyzeRegularControlFlowGraphNode(ControlFlowGraphNode cfgNode,
            Statement previousStatement)
        {
            foreach (var statement in cfgNode.Statements)
            {
                this.Transfer(previousStatement, statement);

                var stmt = statement.SyntaxNode as StatementSyntax;
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
                    this.AnalyzeVariableDeclaration(varDecl, statement);
                }
                else if (expr != null)
                {
                    if (expr.Expression is AssignmentExpressionSyntax)
                    {
                        var assignment = expr.Expression as AssignmentExpressionSyntax;
                        this.AnalyzeAssignmentExpression(assignment, statement);
                    }
                    else if (expr.Expression is InvocationExpressionSyntax ||
                        expr.Expression is ObjectCreationExpressionSyntax)
                    {
                        this.AnalyzeMethodCall(expr.Expression, statement);
                    }
                }
                else if (ret != null)
                {
                    this.AnalyzeReturnStatement(ret, statement);
                }

                previousStatement = statement;
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the variable declaration.
        /// </summary>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="statement">Statement</param>
        private void AnalyzeVariableDeclaration(VariableDeclarationSyntax varDecl,
            Statement statement)
        {
            foreach (var variable in varDecl.Variables)
            {
                if (variable.Initializer == null)
                {
                    continue;
                }

                var expr = variable.Initializer.Value;
                if (expr is MemberAccessExpressionSyntax)
                {
                    var memberAccess = expr as MemberAccessExpressionSyntax;
                    this.ResolveMethodParameterAccesses(memberAccess, statement);
                    this.ResolveFieldAccesses(memberAccess, statement);
                }

                ITypeSymbol declType = null;
                if (expr is LiteralExpressionSyntax &&
                    expr.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    declType = this.SemanticModel.GetTypeInfo(varDecl.Type).Type;
                }
                else
                {
                    declType = this.SemanticModel.GetTypeInfo(expr).Type;
                }

                if (this.AnalysisContext.IsTypePassedByValueOrImmutable(declType))
                {
                    if (expr is InvocationExpressionSyntax ||
                        expr is ObjectCreationExpressionSyntax)
                    {
                        this.AnalyzeMethodCall(expr, statement);
                    }
                }
                else
                {
                    var leftSymbol = this.SemanticModel.GetDeclaredSymbol(variable);
                    DataFlowSymbol leftDFSymbol = new DataFlowSymbol(leftSymbol);

                    this.AnalyzeAssignmentExpression(leftDFSymbol, expr, statement);
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the assignment expression.
        /// </summary>
        /// <param name="binaryExpr">BinaryExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        private void AnalyzeAssignmentExpression(AssignmentExpressionSyntax assignment,
            Statement statement)
        {
            if (assignment.Left is MemberAccessExpressionSyntax)
            {
                var memberAccess = assignment.Left as MemberAccessExpressionSyntax;
                this.ResolveMethodParameterAccesses(memberAccess, statement);
                this.ResolveFieldAccesses(memberAccess, statement);
            }

            if (assignment.Right is MemberAccessExpressionSyntax)
            {
                var memberAccess = assignment.Right as MemberAccessExpressionSyntax;
                this.ResolveMethodParameterAccesses(memberAccess, statement);
                this.ResolveFieldAccesses(memberAccess, statement);
            }

            HashSet<IdentifierNameSyntax> leftExprs = null;
            ITypeSymbol lhsType = null;
            if (assignment.Left is IdentifierNameSyntax ||
                assignment.Left is MemberAccessExpressionSyntax)
            {
                lhsType = this.SemanticModel.GetTypeInfo(assignment.Left).Type;
                leftExprs = AnalysisContext.GetIdentifiers(assignment.Left);
            }
            else if (assignment.Left is ElementAccessExpressionSyntax)
            {
                var memberAccess = (assignment.Left as ElementAccessExpressionSyntax);
                if (memberAccess.Expression is IdentifierNameSyntax ||
                    memberAccess.Expression is MemberAccessExpressionSyntax)
                {
                    lhsType = this.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
                    leftExprs = AnalysisContext.GetIdentifiers(assignment.Left);
                }
            }

            if (this.AnalysisContext.IsTypePassedByValueOrImmutable(lhsType))
            {
                if (assignment.Right is InvocationExpressionSyntax ||
                    assignment.Right is ObjectCreationExpressionSyntax)
                {
                    this.AnalyzeMethodCall(assignment.Right, statement);
                }
            }
            else
            {
                foreach (var leftExpr in leftExprs)
                {
                    var leftSymbol = this.SemanticModel.GetSymbolInfo(leftExpr).Symbol;
                    DataFlowSymbol leftDFSymbol = new DataFlowSymbol(leftSymbol);

                    this.AnalyzeAssignmentExpression(leftDFSymbol, assignment.Right, statement);
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the assignment expression.
        /// </summary>
        /// <param name="leftSymbol">DataFlowSymbol</param>
        /// <param name="rightExpr">ExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        private void AnalyzeAssignmentExpression(DataFlowSymbol leftSymbol,
            ExpressionSyntax rightExpr, Statement statement)
        {
            if (rightExpr is IdentifierNameSyntax ||
                rightExpr is MemberAccessExpressionSyntax)
            {
                this.ResetSymbol(leftSymbol, statement);

                IdentifierNameSyntax rhs = AnalysisContext.GetTopLevelIdentifier(rightExpr);
                var rightSymbol = this.SemanticModel.GetSymbolInfo(rhs).Symbol;
                var rightDFSymbols = this.GetDataFlowSymbols(rightSymbol, statement);
                foreach (var rightDFSymbol in rightDFSymbols)
                {
                    this.MapDataFlowInSymbols(new HashSet<DataFlowSymbol> { rightDFSymbol },
                        rightDFSymbol, statement);
                }

                this.MapDataFlowInSymbols(rightDFSymbols, leftSymbol, statement);
                
                var rightType = this.SemanticModel.GetTypeInfo(rhs).Type;
                foreach (var rightDFSymbol in rightDFSymbols)
                {
                    HashSet<ITypeSymbol> referenceTypes = null;
                    if (rightDFSymbol.Kind == SymbolKind.Field)
                    {
                        this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { rightType },
                                leftSymbol, statement, true);
                    }
                    else if (this.ResolveReferenceType(out referenceTypes, rightDFSymbol, statement))
                    {
                        this.MapReferenceTypesToSymbol(referenceTypes, leftSymbol,
                            statement, true);
                    }
                    else
                    {
                        this.EraseReferenceTypesForSymbol(leftSymbol, statement);
                    }
                }
            }
            else if (rightExpr is LiteralExpressionSyntax &&
                rightExpr.IsKind(SyntaxKind.NullLiteralExpression))
            {
                this.ResetSymbol(leftSymbol, statement);
            }
            else if (rightExpr is InvocationExpressionSyntax ||
                rightExpr is ObjectCreationExpressionSyntax)
            {
                HashSet<ISymbol> returnSymbols = null;
                HashSet<ITypeSymbol> returnTypes = null;

                this.AnalyzeMethodCall(rightExpr, statement, out returnSymbols, out returnTypes);

                var returnDFSymbols = new HashSet<DataFlowSymbol>();
                foreach (var returnSymbol in returnSymbols)
                {
                    returnDFSymbols.UnionWith(this.GetDataFlowSymbols(returnSymbol, statement));
                }

                if (!returnDFSymbols.Contains(leftSymbol) &&
                    returnDFSymbols.Count != 1)
                {
                    this.ResetSymbol(leftSymbol, statement);
                }

                foreach (var returnDFSymbol in returnDFSymbols)
                {
                    this.MapDataFlowInSymbols(new HashSet<DataFlowSymbol> { returnDFSymbol },
                        returnDFSymbol, statement);
                }

                this.MapDataFlowInSymbols(returnDFSymbols, leftSymbol, statement);

                if (returnTypes.Count > 0)
                {
                    this.MapReferenceTypesToSymbol(returnTypes, leftSymbol, statement, true);
                }
                else
                {
                    this.EraseReferenceTypesForSymbol(leftSymbol, statement);
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the return statement.
        /// </summary>
        /// <param name="retStmt">ReturnStatementSyntax</param>
        /// <param name="statement">Statement</param>
        private void AnalyzeReturnStatement(ReturnStatementSyntax retStmt,
            Statement statement)
        {
            var returnDFSymbols = new HashSet<DataFlowSymbol>();
            if (retStmt.Expression is IdentifierNameSyntax ||
                retStmt.Expression is MemberAccessExpressionSyntax)
            {
                var rightSymbol = this.SemanticModel.GetSymbolInfo(retStmt.Expression).Symbol;
                var rightDFSymbols = this.GetDataFlowSymbols(rightSymbol, statement);
                foreach (var rightDFSymbol in rightDFSymbols)
                {
                    this.MapDataFlowInSymbols(new HashSet<DataFlowSymbol> { rightDFSymbol },
                        rightDFSymbol, statement);
                    returnDFSymbols.Add(rightDFSymbol);
                }

                if (retStmt.Expression is MemberAccessExpressionSyntax)
                {
                    var memberAccess = retStmt.Expression as MemberAccessExpressionSyntax;
                    this.ResolveMethodParameterAccesses(memberAccess, statement);
                    this.ResolveFieldAccesses(memberAccess, statement);
                }

                foreach (var rightDFSymbol in rightDFSymbols)
                {
                    HashSet<ITypeSymbol> referenceTypes = null;
                    if (rightDFSymbol.Kind == SymbolKind.Field)
                    {
                        var typeSymbol = this.SemanticModel.GetTypeInfo(retStmt.Expression).Type;
                        if (typeSymbol != null)
                        {
                            this.MapReferenceTypesToSymbol(new HashSet<ITypeSymbol> { typeSymbol },
                                rightDFSymbol, statement, true);
                            this.MethodSummary.ReturnTypeSet.Add(typeSymbol);
                        }
                    }
                    else if (this.ResolveReferenceType(out referenceTypes, rightDFSymbol, statement))
                    {
                        this.MethodSummary.ReturnTypeSet.UnionWith(referenceTypes);
                    }
                }
            }
            else if (retStmt.Expression is InvocationExpressionSyntax ||
                retStmt.Expression is ObjectCreationExpressionSyntax)
            {
                HashSet<ISymbol> returnSymbols = null;
                HashSet<ITypeSymbol> returnTypes = null;

                this.AnalyzeMethodCall(retStmt.Expression, statement,
                    out returnSymbols, out returnTypes);

                foreach (var returnSymbol in returnSymbols)
                {
                    var dfSymbols = this.GetDataFlowSymbols(returnSymbol, statement);
                    foreach (var dfSymbol in dfSymbols)
                    {
                        this.MapDataFlowInSymbols(new HashSet<DataFlowSymbol> { dfSymbol },
                            dfSymbol, statement);
                        returnDFSymbols.Add(dfSymbol);
                    }
                }

                this.MethodSummary.ReturnTypeSet.UnionWith(returnTypes);
            }

            if (returnDFSymbols.Count == 0)
            {
                return;
            }

            var indexMap = new Dictionary<IParameterSymbol, int>();
            var parameterList = this.MethodSummary.Method.ParameterList.Parameters;
            for (int idx = 0; idx < parameterList.Count; idx++)
            {
                var paramSymbol = this.SemanticModel.GetDeclaredSymbol(parameterList[idx]);
                indexMap.Add(paramSymbol, idx);
            }

            Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> dataFlowMap = null;
            if (this.TryGetDataFlowMapForStatement(statement, out dataFlowMap))
            {
                foreach (var symbol in returnDFSymbols.Where(s => dataFlowMap.ContainsKey(s)))
                {
                    foreach (var reference in dataFlowMap[symbol].Where(s => s.Kind == SymbolKind.Parameter))
                    {
                        if (!reference.HasResetAfterMethodEntry)
                        {
                            this.MethodSummary.ReturnSet.Item1.Add(
                                indexMap[reference.ContainingSymbol as IParameterSymbol]);
                        }
                    }

                    foreach (var reference in dataFlowMap[symbol].Where(r => r.Kind == SymbolKind.Field))
                    {
                        this.MethodSummary.ReturnSet.Item2.Add(
                            reference.ContainingSymbol as IFieldSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the method call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        private void AnalyzeMethodCall(ExpressionSyntax call, Statement statement)
        {
            HashSet<ISymbol> returnSymbols = null;
            HashSet<ITypeSymbol> returnTypes = null;

            this.AnalyzeMethodCall(call, statement, out returnSymbols, out returnTypes);
        }

        /// <summary>
        /// Analyzes the data-flow in the method call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        /// <param name="returnSymbols">Return symbols</param>
        /// <param name="returnTypes">Return types</param>
        private void AnalyzeMethodCall(ExpressionSyntax call, Statement statement,
            out HashSet<ISymbol> returnSymbols, out HashSet<ITypeSymbol> returnTypes)
        {
            returnSymbols = new HashSet<ISymbol>();
            returnTypes = new HashSet<ITypeSymbol>();

            this.MapSymbolsInCall(call, statement);

            var invocation = call as InvocationExpressionSyntax;
            var objCreation = call as ObjectCreationExpressionSyntax;

            var callSymbol = this.SemanticModel.GetSymbolInfo(call).Symbol;
            ArgumentListSyntax argumentList;

            var candidateCalleeSummaries = new HashSet<MethodSummary>();
            if (invocation != null)
            {
                var candidateCallees = this.AnalysisContext.ResolveCandidateMethodsAtCallSite(
                    invocation, statement, this.SemanticModel);
                foreach (var candidateCallee in candidateCallees)
                {
                    var calleeSummary = MethodSummary.Create(this.AnalysisContext, candidateCallee);
                    if (calleeSummary != null)
                    {
                        candidateCalleeSummaries.Add(calleeSummary);
                    }
                }

                argumentList = invocation.ArgumentList;
            }
            else
            {
                var constructor = this.AnalysisContext.ResolveConstructor(objCreation, this.SemanticModel);
                if (constructor != null)
                {
                    var calleeSummary = MethodSummary.Create(this.AnalysisContext, constructor);
                    if (calleeSummary != null)
                    {
                        candidateCalleeSummaries.Add(calleeSummary);
                    }
                }

                argumentList = objCreation.ArgumentList;
            }

            if (callSymbol != null)
            {
                this.ResolveGivesUpOwnershipInCall(callSymbol, argumentList, statement);
            }

            foreach (var candidateCalleeSummary in candidateCalleeSummaries)
            {
                if (callSymbol != null)
                {
                    this.MapCalleeSummaryToCallSymbol(candidateCalleeSummary, callSymbol, statement);
                    this.ResolveGivesUpOwnershipInCall(callSymbol, candidateCalleeSummary,
                        argumentList, statement);
                }

                this.ResolveSideEffectsInCall(call, candidateCalleeSummary, statement);

                if (invocation != null)
                {
                    returnSymbols.UnionWith(candidateCalleeSummary.GetResolvedReturnSymbols(
                        invocation, this.SemanticModel));
                }

                returnTypes.UnionWith(candidateCalleeSummary.ReturnTypeSet);
            }

            returnTypes.Add(this.SemanticModel.GetTypeInfo(call).Type);
        }

        #endregion

        #region data-flow transfer methods

        /// <summary>
        /// Transfers the data-flow map from the previous statement
        /// to the next statement.
        /// </summary>
        /// <param name="previousStatement">Previous Statement</param>
        /// <param name="nextStatement">Next Statement</param>
        private void Transfer(Statement previousStatement,
            Statement nextStatement)
        {
            Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> previousDataFlowMap = null;
            if (this.TryGetDataFlowMapForStatement(previousStatement,
                out previousDataFlowMap))
            {
                foreach (var pair in previousDataFlowMap)
                {
                    this.MapDataFlowInSymbols(pair.Value, pair.Key, nextStatement);
                }
            }

            Dictionary<DataFlowSymbol, HashSet<ITypeSymbol>> previousReferenceTypeMap = null;
            if (this.TryGetReferenceTypeMapForStatement(previousStatement,
                out previousReferenceTypeMap))
            {
                foreach (var pair in previousReferenceTypeMap)
                {
                    this.MapReferenceTypesToSymbol(pair.Value, pair.Key, nextStatement, false);
                }
            }
        }

        #endregion

        #region resolution methods

        /// <summary>
        /// Resolves the alias symbols of the symbol.
        /// </summary>
        /// <param name="symbols">DataFlowSymbols</param>
        /// <param name="statement">Statement</param>
        /// <returns>DataFlowSymbols</returns>
        private ISet<DataFlowSymbol> ResolveAliasSymbols(ISet<DataFlowSymbol> symbols,
            Statement statement)
        {
            Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> dataFlowMap = null;
            this.TryGetDataFlowMapForStatement(statement, out dataFlowMap);

            var resolvedAliases = new HashSet<DataFlowSymbol>(symbols);
            var aliasesToResolve = new List<DataFlowSymbol>(symbols);

            while (aliasesToResolve.Count > 0)
            {
                var aliases = aliasesToResolve.SelectMany(val
                    => this.ResolveDirectAliasSymbols(val, dataFlowMap)).
                    Where(val => !resolvedAliases.Contains(val)).ToList();
                resolvedAliases.UnionWith(aliases);

                aliasesToResolve = aliases;
            }

            return resolvedAliases;
        }

        /// <summary>
        /// Resolves the direct alias symbols of the symbol.
        /// </summary>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="dataFlowMap">DataFlowMap</param>
        /// <returns>DataFlowSymbols</returns>
        private ISet<DataFlowSymbol> ResolveDirectAliasSymbols(DataFlowSymbol symbol,
            Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> dataFlowMap)
        {
            var aliasSymbols = new HashSet<DataFlowSymbol>();

            foreach (var pair in dataFlowMap)
            {
                if (pair.Key.Equals(symbol))
                {
                    aliasSymbols.UnionWith(pair.Value);
                }

                if (pair.Value.Contains(symbol))
                {
                    aliasSymbols.Add(pair.Key);
                }
            }

            return aliasSymbols;
        }

        /// <summary>
        /// Resolves the side effects in the call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="statement">Statement</param>
        private void ResolveSideEffectsInCall(ExpressionSyntax call,
            MethodSummary calleeSummary, Statement statement)
        {
            var invocation = call as InvocationExpressionSyntax;
            var objCreation = call as ObjectCreationExpressionSyntax;
            if (calleeSummary == null ||
                (invocation == null && objCreation == null))
            {
                return;
            }

            Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> sideEffects;
            ArgumentListSyntax argumentList;

            if (invocation != null)
            {
                sideEffects = this.MethodSummary.GetResolvedSideEffects(invocation,
                    calleeSummary, statement, this.SemanticModel);
                argumentList = invocation.ArgumentList;
            }
            else
            {
                sideEffects = this.MethodSummary.GetResolvedSideEffects(objCreation,
                    calleeSummary, statement, this.SemanticModel);
                argumentList = objCreation.ArgumentList;
            }

            foreach (var sideEffect in sideEffects)
            {
                this.ResetSymbol(sideEffect.Key, statement);
                this.MapDataFlowInSymbols(sideEffect.Value, sideEffect.Key, statement);
            }

            for (int index = 0; index < argumentList.Arguments.Count; index++)
            {
                if (!calleeSummary.ParameterAccesses.ContainsKey(index))
                {
                    continue;
                }

                var argIdentifier = AnalysisContext.GetTopLevelIdentifier(
                    argumentList.Arguments[index].Expression);

                this.ResolveMethodParameterAccesses(argIdentifier,
                    calleeSummary.ParameterAccesses[index], statement);
            }

            foreach (var fieldAccess in calleeSummary.FieldAccesses)
            {
                foreach (var access in fieldAccess.Value)
                {
                    this.MapFieldAccessInStatement(fieldAccess.Key as IFieldSymbol, access);
                }
            }
        }

        /// <summary>
        /// Resolves any method parameter acccesses in the member access expression.
        /// </summary>
        /// <param name="expr">MemberAccessExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        private void ResolveMethodParameterAccesses(MemberAccessExpressionSyntax expr,
            Statement statement)
        {
            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = AnalysisContext.GetTopLevelIdentifier(expr);
            if (identifier == null || name == null || name.Equals(identifier))
            {
                return;
            }

            this.ResolveMethodParameterAccesses(identifier,
                new HashSet<Statement> { statement }, statement);
        }

        /// <summary>
        /// Resolves the method parameter acccesses in the identifier.
        /// </summary>
        /// <param name="identifier">IdentifierNameSyntax</param>
        /// <param name="parameterAccesses">Parameter accesses</param>
        /// <param name="statement">Statement</param>
        private void ResolveMethodParameterAccesses(IdentifierNameSyntax identifier,
            HashSet<Statement> parameterAccesses, Statement statement)
        {
            var symbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol == null)
            {
                return;
            }

            var indexMap = new Dictionary<IParameterSymbol, int>();
            var parameterList = this.MethodSummary.Method.ParameterList.Parameters;
            for (int idx = 0; idx < parameterList.Count; idx++)
            {
                indexMap.Add(this.SemanticModel.GetDeclaredSymbol(parameterList[idx]), idx);
            }

            foreach (var pair in indexMap)
            {
                if (DataFlowAnalysisEngine.FlowsFromParameter(pair.Key, symbol, statement))
                {
                    if (!this.MethodSummary.ParameterAccesses.ContainsKey(pair.Value))
                    {
                        this.MethodSummary.ParameterAccesses.Add(pair.Value,
                            new HashSet<Statement>());
                    }

                    foreach (var access in parameterAccesses)
                    {
                        this.MethodSummary.ParameterAccesses[pair.Value].Add(access);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves any field acccesses in the member access expression.
        /// </summary>
        /// <param name="expr">MemberAccessExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        private void ResolveFieldAccesses(MemberAccessExpressionSyntax expr,
            Statement statement)
        {
            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = AnalysisContext.GetTopLevelIdentifier(expr);
            if (identifier == null || name == null || name.Equals(identifier))
            {
                return;
            }

            var fieldSymbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (fieldSymbol == null)
            {
                return;
            }

            var fieldDFSymbols = this.GetDataFlowSymbols(fieldSymbol, statement);
            var aliasSymbols = this.ResolveAliasSymbols(fieldDFSymbols, statement);

            foreach (var aliasSymbol in aliasSymbols)
            {
                if (aliasSymbol.Kind == SymbolKind.Field &&
                    !aliasSymbol.HasResetAfterMethodEntry)
                {
                    this.MapFieldAccessInStatement(aliasSymbol.ContainingSymbol as IFieldSymbol,
                        statement);
                }
            }
        }

        /// <summary>
        /// Resolves the reference type of the symbol on the statement.
        /// </summary>
        /// <param name="types">Set of reference type symbols</param>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        /// <returns>Boolean</returns>
        private bool ResolveReferenceType(out HashSet<ITypeSymbol> types, DataFlowSymbol symbol,
            Statement statement)
        {
            types = new HashSet<ITypeSymbol>();

            Dictionary<DataFlowSymbol, HashSet<ITypeSymbol>> referenceTypeMap = null;
            if (!this.TryGetReferenceTypeMapForStatement(statement,
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
        /// Resolves the gives-up ownership information in the call.
        /// </summary>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="argumentList">ArgumentListSyntax</param>
        /// <param name="statement">Statement</param>
        private void ResolveGivesUpOwnershipInCall(ISymbol callSymbol, ArgumentListSyntax argumentList,
            Statement statement)
        {
            string methodName = callSymbol.ContainingNamespace.ToString() + "." + callSymbol.Name;
            if (this.AnalysisContext.GivesUpOwnershipMethods.ContainsKey(methodName) &&
                (this.AnalysisContext.GivesUpOwnershipMethods[methodName].Max() <
                argumentList.Arguments.Count))
            {
                foreach (var paramIndex in this.AnalysisContext.GivesUpOwnershipMethods[methodName])
                {
                    var argExpr = argumentList.Arguments[paramIndex].Expression;
                    this.ResolveGivesUpOwnershipInArgument(callSymbol, argExpr, statement);
                }
            }
        }

        /// <summary>
        /// Resolves the gives-up ownership information in the call.
        /// </summary>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="methodSummary">MethodSummary</param>
        /// <param name="argumentList">ArgumentListSyntax</param>
        /// <param name="statement">Statement</param>
        private void ResolveGivesUpOwnershipInCall(ISymbol callSymbol, MethodSummary methodSummary,
            ArgumentListSyntax argumentList, Statement statement)
        {
            foreach (var paramIndex in methodSummary.GivesUpOwnershipParamIndexes)
            {
                var argExpr = argumentList.Arguments[paramIndex].Expression;
                this.ResolveGivesUpOwnershipInArgument(callSymbol, argExpr, statement);
            }
        }

        /// <summary>
        /// Resolves the gives-up ownership information in the argument.
        /// </summary>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="argExpr">ExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        private void ResolveGivesUpOwnershipInArgument(ISymbol callSymbol, ExpressionSyntax argExpr,
            Statement statement)
        {
            if (argExpr is IdentifierNameSyntax ||
                argExpr is MemberAccessExpressionSyntax)
            {
                IdentifierNameSyntax argIdentifier = AnalysisContext.GetTopLevelIdentifier(argExpr);
                ISymbol argSymbol = this.SemanticModel.GetSymbolInfo(argIdentifier).Symbol;

                for (int idx = 0; idx < this.MethodSummary.Method.ParameterList.Parameters.Count; idx++)
                {
                    ParameterSyntax param = this.MethodSummary.Method.ParameterList.Parameters[idx];
                    TypeInfo typeInfo = this.SemanticModel.GetTypeInfo(param.Type);
                    if (this.AnalysisContext.IsTypePassedByValueOrImmutable(typeInfo.Type))
                    {
                        continue;
                    }
                    
                    IParameterSymbol paramSymbol = this.SemanticModel.GetDeclaredSymbol(param);

                    if (DataFlowAnalysisEngine.FlowsFromParameter(paramSymbol, argSymbol, statement))
                    {
                        this.MethodSummary.GivesUpOwnershipParamIndexes.Add(idx);
                    }
                }

                Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> dataFlowMap = null;
                this.TryGetDataFlowMapForStatement(statement, out dataFlowMap);
                if (!dataFlowMap.Any(val => val.Key.ContainingSymbol.Equals(argSymbol)))
                {
                    var argDFSymbols = this.GetDataFlowSymbols(argSymbol, statement);
                    foreach (var argDFSymbol in argDFSymbols)
                    {
                        this.MapDataFlowInSymbols(new HashSet<DataFlowSymbol> { argDFSymbol },
                            argDFSymbol, statement);
                    }
                }

                this.MapSymbolWithGivenUpOwnership(argSymbol, statement);
            }
            else if (argExpr is BinaryExpressionSyntax &&
                argExpr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = argExpr as BinaryExpressionSyntax;
                this.ResolveGivesUpOwnershipInArgument(callSymbol, binExpr.Left, statement);
            }
            else if (argExpr is InvocationExpressionSyntax ||
                argExpr is ObjectCreationExpressionSyntax)
            {
                ArgumentListSyntax argumentList = this.AnalysisContext.GetArgumentList(argExpr);
                foreach (var arg in argumentList.Arguments)
                {
                    this.ResolveGivesUpOwnershipInArgument(callSymbol, arg.Expression, statement);
                }
            }
        }

        #endregion

        #region data-flow mapping methods

        /// <summary>
        /// Maps the data-flow from 'flowsFromSymbols' to 'flowsIntoSymbol'.
        /// </summary>
        /// <param name="flowsFromSymbols">DataFlowSymbols</param>
        /// <param name="flowsIntoSymbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        private void MapDataFlowInSymbols(IEnumerable<DataFlowSymbol> flowsFromSymbols,
            DataFlowSymbol flowsIntoSymbol, Statement statement)
        {
            var resolvedSymbols = new HashSet<DataFlowSymbol>(flowsFromSymbols);
            foreach (var flowsFromSymbol in flowsFromSymbols)
            {
                if (this.DataFlowMap.ContainsKey(statement) &&
                    this.DataFlowMap[statement].ContainsKey(flowsFromSymbol))
                {
                    resolvedSymbols.UnionWith(this.DataFlowMap[statement][flowsFromSymbol]);
                }
            }

            this.CheckAndInitializeDataFlowMapForSymbol(flowsIntoSymbol, statement);
            this.DataFlowMap[statement][flowsIntoSymbol].UnionWith(resolvedSymbols);
        }

        /// <summary>
        /// Maps the set of reference types to the symbol.
        /// </summary>
        /// <param name="types">Set of reference types</param>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        /// <param name="reset">Reset map</param>
        private void MapReferenceTypesToSymbol(IEnumerable<ITypeSymbol> types, DataFlowSymbol symbol,
            Statement statement, bool reset)
        {
            this.CheckAndInitializeReferenceTypeMapForSymbol(symbol, statement, reset);
            this.ReferenceTypeMap[statement][symbol].UnionWith(types);
        }

        /// <summary>
        /// Maps the callee summary to the call symbol.
        /// </summary>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="statement">Statement</param>
        private void MapCalleeSummaryToCallSymbol(MethodSummary calleeSummary, ISymbol callSymbol,
            Statement statement)
        {
            this.CheckAndInitializeCalleeSummaryMapForSymbol(callSymbol, statement);
            this.CalleeSummaryMap[statement][callSymbol].Add(calleeSummary);
        }

        /// <summary>
        /// Maps the symbol with given-up ownership.
        /// </summary>
        /// <param name="givenUpSymbol">Symbol</param>
        /// <param name="statement">Statement</param>
        private void MapSymbolWithGivenUpOwnership(ISymbol givenUpSymbol,
            Statement statement)
        {
            this.CheckAndInitializeGivesUpOwnershipMapForStatement(statement);
            this.GivesUpOwnershipMap[statement].Add(givenUpSymbol);
        }

        /// <summary>
        /// Maps symbols in the call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        private void MapSymbolsInCall(ExpressionSyntax call, Statement statement)
        {
            ArgumentListSyntax argumentList = this.AnalysisContext.GetArgumentList(call);

            List<MemberAccessExpressionSyntax> accesses = argumentList.DescendantNodesAndSelf().
                OfType<MemberAccessExpressionSyntax>().ToList();

            //foreach (var access in accesses)
            //{
            //    IdentifierNameSyntax id = AnalysisContext.GetTopLevelIdentifier(access);
            //    if (id == null)
            //    {
            //        continue;
            //    }

            //    var accessSymbol = this.SemanticModel.GetSymbolInfo(id).Symbol;
            //    this.MapDataFlowInSymbols(accessSymbol, accessSymbol, cfgNode.SyntaxNodes[0], cfgNode);
            //}
        }

        /// <summary>
        /// Maps the access of the field symbol.
        /// </summary>
        /// <param name="fieldSymbol">IFieldSymbol</param>
        /// <param name="statement">Statement</param>
        private void MapFieldAccessInStatement(IFieldSymbol fieldSymbol,
            Statement statement)
        {
            if (!this.MethodSummary.FieldAccesses.ContainsKey(fieldSymbol))
            {
                this.MethodSummary.FieldAccesses.Add(fieldSymbol,
                    new HashSet<Statement>());
            }

            this.MethodSummary.FieldAccesses[fieldSymbol].Add(statement);
        }

        /// <summary>
        /// Resets the symbol.
        /// </summary>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        private void ResetSymbol(DataFlowSymbol symbol, Statement statement)
        {
            this.EraseDataFlowForSymbol(symbol, statement);
            symbol.HasResetAfterMethodEntry = true;

            this.CheckAndInitializeDataFlowMapForSymbol(symbol, statement);
            this.DataFlowMap[statement][symbol].Add(symbol);
        }

        #endregion

        #region reset checking methods

        /// <summary>
        /// Checks if the data-flow analysis reached a fixpoint
        /// regarding the successor.
        /// </summary>
        /// <param name="statement">Statement</param>
        /// <param name="loopHeadCfgNode">ControlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        private bool ReachedFixpoint(Statement statement, ControlFlowGraphNode loopHeadCfgNode)
        {
            Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> dataFlowMap = null;
            this.TryGetDataFlowMapForStatement(statement, out dataFlowMap);

            Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> loopHeadDataFlowMap = null;
            this.TryGetDataFlowMapForStatement(loopHeadCfgNode.Statements.First(),
                out loopHeadDataFlowMap);

            if (dataFlowMap == null || loopHeadDataFlowMap == null)
            {
                return false;
            }

            foreach (var pair in dataFlowMap)
            {
                if (!loopHeadDataFlowMap.Keys.Any(v => v.ContainingSymbol.Equals(pair.Key.ContainingSymbol)))
                {
                    return false;
                }

                var loopPair = loopHeadDataFlowMap.First(s => s.Key.ContainingSymbol.Equals(pair.Key.ContainingSymbol));
                if (pair.Value.Any(v => !loopPair.Value.Any(p => p.ContainingSymbol.Equals(v.ContainingSymbol))))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region map updating methods

        /// <summary>
        /// Checks the data-flow map for the symbol,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        private void CheckAndInitializeDataFlowMapForSymbol(DataFlowSymbol symbol,
            Statement statement)
        {
            if (!this.DataFlowMap.ContainsKey(statement))
            {
                this.DataFlowMap.Add(statement, new Dictionary<DataFlowSymbol,
                    HashSet<DataFlowSymbol>>());
            }

            if (!this.DataFlowMap[statement].ContainsKey(symbol))
            {
                this.DataFlowMap[statement].Add(symbol, new HashSet<DataFlowSymbol>());
            }
        }

        /// <summary>
        /// Checks the reference type map for the symbol,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        /// <param name="resetMap">Should reset the map</param>
        private void CheckAndInitializeReferenceTypeMapForSymbol(DataFlowSymbol symbol,
            Statement statement, bool resetMap = false)
        {
            if (!this.ReferenceTypeMap.ContainsKey(statement))
            {
                this.ReferenceTypeMap.Add(statement,
                    new Dictionary<DataFlowSymbol, HashSet<ITypeSymbol>>());
            }

            if (!this.ReferenceTypeMap[statement].ContainsKey(symbol))
            {
                this.ReferenceTypeMap[statement].Add(symbol, new HashSet<ITypeSymbol>());
            }
            else if (resetMap)
            {
                this.ReferenceTypeMap[statement][symbol].Clear();
            }
        }

        /// <summary>
        /// Checks the callee summary map for the symbol,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="statement">Statement</param>
        private void CheckAndInitializeCalleeSummaryMapForSymbol(ISymbol callSymbol,
            Statement statement)
        {
            if (!this.CalleeSummaryMap.ContainsKey(statement))
            {
                this.CalleeSummaryMap.Add(statement,
                    new Dictionary<ISymbol, HashSet<MethodSummary>>());
            }

            if (!this.CalleeSummaryMap[statement].ContainsKey(callSymbol))
            {
                this.CalleeSummaryMap[statement].Add(callSymbol,
                    new HashSet<MethodSummary>());
            }
        }

        /// <summary>
        /// Checks the gives-up ownership map for the statement,
        /// and initializes it, if needed.
        /// </summary>
        /// <param name="statement">Statement</param>
        private void CheckAndInitializeGivesUpOwnershipMapForStatement(Statement statement)
        {
            if (!this.GivesUpOwnershipMap.ContainsKey(statement))
            {
                this.GivesUpOwnershipMap.Add(statement, new HashSet<ISymbol>());
            }
        }

        /// <summary>
        /// Erases the data-flow information for the symbol.
        /// </summary>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        private void EraseDataFlowForSymbol(DataFlowSymbol symbol,
            Statement statement)
        {
            if (this.DataFlowMap.ContainsKey(statement))
            {
                var dfs = this.DataFlowMap[statement].Keys.FirstOrDefault(
                    val => val.ContainingSymbol.Equals(symbol.ContainingSymbol));
                if (dfs != null)
                {
                    this.DataFlowMap[statement].Remove(dfs);
                }
            }
        }

        /// <summary>
        /// Erases the set of reference types for the symbol.
        /// </summary>
        /// <param name="symbol">DataFlowSymbol</param>
        /// <param name="statement">Statement</param>
        private void EraseReferenceTypesForSymbol(DataFlowSymbol symbol,
            Statement statement)
        {
            if (this.ReferenceTypeMap.ContainsKey(statement) &&
                this.ReferenceTypeMap[statement].ContainsKey(symbol))
            {
                this.ReferenceTypeMap[statement].Remove(symbol);
            }
        }

        #endregion

        #region data-flow summary printing methods

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        /// <param name="indent">Indent</param>
        internal void PrintDataFlowMap(string indent)
        {
            if (this.DataFlowMap.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Data-flow map");
                foreach (var cfgNode in this.DataFlowMap.Keys.Select(
                    val => val.ControlFlowGraphNode).Distinct())
                {
                    Console.WriteLine(indent + ". | ... in CFG node '{0}'", cfgNode.Id);
                    foreach (var statement in this.DataFlowMap.Keys.Where(val
                        => val.ControlFlowGraphNode.Equals(cfgNode)))
                    {
                        Console.WriteLine(indent + ". | ..... '{0}'", statement.SyntaxNode);
                        foreach (var pair in this.DataFlowMap[statement])
                        {
                            foreach (var symbol in pair.Value)
                            {
                                Console.WriteLine(indent + ". | ....... '{0}' <=== '{1}'",
                                    pair.Key.Name, symbol.Name);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the type of references.
        /// </summary>
        /// <param name="indent">Indent</param>
        internal void PrintReferenceTypes(string indent)
        {
            if (this.ReferenceTypeMap.SelectMany(stmt => stmt.Value.SelectMany(
                symbol => symbol.Value)).Count() > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Types of references");
                foreach (var cfgNode in this.ReferenceTypeMap.Keys.Select(
                    val => val.ControlFlowGraphNode).Distinct())
                {
                    Console.WriteLine(indent + ". | ... in CFG node '{0}'", cfgNode.Id);
                    foreach (var statement in this.ReferenceTypeMap.Keys.Where(val
                        => val.ControlFlowGraphNode.Equals(cfgNode)))
                    {
                        Console.WriteLine(indent + ". | ..... '{0}'", statement.SyntaxNode);
                        foreach (var pair in this.ReferenceTypeMap[statement])
                        {
                            foreach (var type in pair.Value)
                            {
                                Console.WriteLine(indent + ". | ....... '{0}' has type '{1}'",
                                    pair.Key.Name, type);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the callee summary information.
        /// </summary>
        /// <param name="indent">Indent</param>
        internal void PrintCalleeSummaryMap(string indent)
        {
            if (this.CalleeSummaryMap.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Available callee summaries");
                foreach (var cfgNode in this.CalleeSummaryMap.Keys.Select(
                    val => val.ControlFlowGraphNode).Distinct())
                {
                    Console.WriteLine(indent + ". | ... CFG node '{0}'", cfgNode.Id);
                    foreach (var statement in this.CalleeSummaryMap.Keys.Where(val
                        => val.ControlFlowGraphNode.Equals(cfgNode)))
                    {
                        Console.WriteLine(indent + ". | ..... '{0}'", statement.SyntaxNode);
                        foreach (var pair in this.CalleeSummaryMap[statement])
                        {
                            foreach (var summary in pair.Value)
                            {
                                Console.WriteLine(indent + ". | ....... callee summary with id '{0}'",
                                    summary.Id);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the gives-up ownership information.
        /// </summary>
        /// <param name="indent">Indent</param>
        internal void PrintGivesUpOwnershipMap(string indent)
        {
            if (this.GivesUpOwnershipMap.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Operations giving up ownership");
                foreach (var cfgNode in this.GivesUpOwnershipMap.Keys.Select(
                    val => val.ControlFlowGraphNode).Distinct())
                {
                    Console.WriteLine(indent + ". | ... CFG node '{0}'", cfgNode.Id);
                    foreach (var statement in this.GivesUpOwnershipMap.Keys.Where(val
                        => val.ControlFlowGraphNode.Equals(cfgNode)))
                    {
                        Console.WriteLine(indent + ". | ..... '{0}'", statement.SyntaxNode);
                        Console.Write(indent + ". | ....... gives up ownership of");
                        foreach (var symbol in this.GivesUpOwnershipMap[statement])
                        {
                            Console.Write(" '{0}'", symbol);
                        }

                        Console.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// Prints all callee summaries.
        /// </summary>
        internal void PrintCalleeSummaries()
        {
            var summaries = this.CalleeSummaryMap.SelectMany(val => val.Value).
                SelectMany(val => val.Value);
            foreach (var summary in summaries)
            {
                summary.PrintDataFlowInformation(true);
            }
        }

        #endregion
    }
}
