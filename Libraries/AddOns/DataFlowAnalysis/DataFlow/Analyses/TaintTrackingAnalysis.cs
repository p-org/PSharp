//-----------------------------------------------------------------------
// <copyright file="TaintTrackingAnalysis.cs">
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

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Taint tracking analysis.
    /// </summary>
    internal class TaintTrackingAnalysis : IAnalysisPass
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
        private MethodSummary Summary;

        /// <summary>
        /// The data-flow graph being analyzed.
        /// </summary>
        private IGraph<IDataFlowNode> DataFlowGraph;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dfg">DataFlowGraph</param>
        internal TaintTrackingAnalysis(IGraph<IDataFlowNode> dfg)
        {
            this.AnalysisContext = dfg.EntryNode.Summary.AnalysisContext;
            this.SemanticModel = dfg.EntryNode.Summary.SemanticModel;
            this.Summary = dfg.EntryNode.Summary;
            this.DataFlowGraph = dfg;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        public void Run()
        {
            var queue = new Queue<IDataFlowNode>();
            queue.Enqueue(this.DataFlowGraph.EntryNode);
            this.AnalyzeNode(this.DataFlowGraph.EntryNode);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                foreach (var successor in node.ISuccessors)
                {
                    var oldDataFlowInfo = successor.DataFlowInfo.Clone();
                    this.AnalyzeNode(successor);

                    if (!this.Compare(oldDataFlowInfo, successor.DataFlowInfo))
                    {
                        queue.Enqueue(successor);
                    }
                }
            }
        }

        #endregion

        #region reaching definitions analysis methods

        /// <summary>
        /// Computes the data-flow information in the specified node.
        /// </summary>
        /// <param name="node">IDataFlowNode</param>
        private void AnalyzeNode(IDataFlowNode node)
        {
            if (node.Statement != null && node.IPredecessors.Count > 0)
            {
                this.Transfer(node);
                this.AnalyzeStatement(node.Statement.SyntaxNode as StatementSyntax, node);
                node.DataFlowInfo.AssignOutputDefinitions();
            }
            else if (node.Statement != null)
            {
                this.InitializeParameters(node);
                this.InitializeFieldsAndProperties(node);
                node.DataFlowInfo.AssignOutputDefinitions();
            }

            if (node.ISuccessors.Count == 0)
            {
                this.ResolveParameterToFieldFlowSideEffects(node);
            }
        }

        /// <summary>
        /// Analyzes the data-flow information in the statement.
        /// </summary>
        /// <param name="statement">StatementSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void AnalyzeStatement(StatementSyntax statement, IDataFlowNode node)
        {
            var localDecl = statement?.DescendantNodesAndSelf().
                OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            var expr = statement?.DescendantNodesAndSelf().
                OfType<ExpressionStatementSyntax>().FirstOrDefault();
            var ret = statement?.DescendantNodesAndSelf().
                OfType<ReturnStatementSyntax>().FirstOrDefault();

            if (localDecl != null)
            {
                var varDecl = (statement as LocalDeclarationStatementSyntax).Declaration;
                this.AnalyzeVariableDeclaration(varDecl, node);
            }
            else if (expr != null)
            {
                if (expr.Expression is AssignmentExpressionSyntax)
                {
                    var assignment = expr.Expression as AssignmentExpressionSyntax;
                    this.AnalyzeAssignmentExpression(assignment, node);
                }
                else if (expr.Expression is IdentifierNameSyntax ||
                    expr.Expression is MemberAccessExpressionSyntax)
                {
                    this.AnalyzeBinaryExpression(expr.Expression, node);
                }
                else if (expr.Expression is InvocationExpressionSyntax ||
                    expr.Expression is ObjectCreationExpressionSyntax)
                {
                    this.AnalyzeMethodCall(expr.Expression, node);
                }
            }
            else if (ret != null)
            {
                this.AnalyzeReturnStatement(ret, node);
            }
        }

        /// <summary>
        /// Initializes the data-flow of the input parameters.
        /// </summary>
        /// <param name="node">IDataFlowNode</param>
        private void InitializeParameters(IDataFlowNode node)
        {
            for (int idx = 0; idx < node.Summary.Method.ParameterList.Parameters.Count; idx++)
            {
                var parameter = node.Summary.Method.ParameterList.Parameters[idx];
                IParameterSymbol paramSymbol = this.SemanticModel.GetDeclaredSymbol(parameter);

                SymbolDefinition definition = node.DataFlowInfo.GenerateDefinition(paramSymbol);
                node.DataFlowInfo.AssignTypesToDefinition(node.Summary.ParameterTypes[parameter], definition);
                node.DataFlowInfo.TaintDefinition(definition, definition);
            }
        }

        /// <summary>
        /// Initializes the data-flow of field and properties.
        /// </summary>
        /// <param name="node">IDataFlowNode</param>
        private void InitializeFieldsAndProperties(IDataFlowNode node)
        {
            var symbols = this.Summary.Method.DescendantNodes(n => true).
                OfType<IdentifierNameSyntax>().Select(id => this.SemanticModel.
                GetSymbolInfo(id).Symbol).Where(s => s != null).Distinct();

            var fieldSymbols = symbols.Where(val => val.Kind == SymbolKind.Field);
            foreach (var fieldSymbol in fieldSymbols.Select(s => s as IFieldSymbol))
            {
                SymbolDefinition definition = node.DataFlowInfo.GenerateDefinition(fieldSymbol);
                node.DataFlowInfo.AssignTypeToDefinition(fieldSymbol.Type, definition);
                node.DataFlowInfo.TaintDefinition(definition, definition);
            }

            var propertySymbols = symbols.Where(val => val.Kind == SymbolKind.Property);
            foreach (var propertySymbol in propertySymbols.Select(s => s as IPropertySymbol))
            {
                SymbolDefinition definition = node.DataFlowInfo.GenerateDefinition(propertySymbol);
                node.DataFlowInfo.AssignTypeToDefinition(propertySymbol.Type, definition);
                node.DataFlowInfo.TaintDefinition(definition, definition);
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the variable declaration.
        /// </summary>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void AnalyzeVariableDeclaration(VariableDeclarationSyntax varDecl, IDataFlowNode node)
        {
            foreach (var variable in varDecl.Variables)
            {
                if (variable.Initializer == null)
                {
                    continue;
                }

                var expr = variable.Initializer.Value;
                this.ResolveSideEffectsInExpression(expr, node);

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

                ISymbol leftSymbol = this.SemanticModel.GetDeclaredSymbol(variable);
                node.DataFlowInfo.GenerateDefinition(leftSymbol);

                this.AnalyzeAssignmentExpression(leftSymbol, expr, node);
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the assignment expression.
        /// </summary>
        /// <param name="assignment">AssignmentExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void AnalyzeAssignmentExpression(AssignmentExpressionSyntax assignment, IDataFlowNode node)
        {
            this.ResolveSideEffectsInExpression(assignment.Left, node);
            this.ResolveSideEffectsInExpression(assignment.Right, node);

            var leftIdentifier = this.AnalysisContext.GetIdentifier(assignment.Left);
            ISymbol leftSymbol = this.SemanticModel.GetSymbolInfo(leftIdentifier).Symbol;
            
            node.DataFlowInfo.KillDefinitions(leftSymbol);
            node.DataFlowInfo.GenerateDefinition(leftSymbol);
            
            ISet<Tuple<ISymbol, ITypeSymbol>> nestedLeftSymbolInfos;
            this.GetMemberExpressionSymbols(out nestedLeftSymbolInfos, assignment.Left);

            foreach (var nestedLeftSymbolInfo in nestedLeftSymbolInfos)
            {
                this.AnalyzeAssignmentExpression(nestedLeftSymbolInfo.Item1, assignment.Right, node);
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the binary expression.
        /// </summary>
        /// <param name="expr">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void AnalyzeBinaryExpression(ExpressionSyntax expr, IDataFlowNode node)
        {
            this.ResolveSideEffectsInExpression(expr, node);

            var identifier = this.AnalysisContext.GetIdentifier(expr);
            ISymbol symbol = this.SemanticModel.GetSymbolInfo(expr).Symbol;
            ITypeSymbol type = this.SemanticModel.GetTypeInfo(expr).Type;

            if (node.DataFlowInfo.IsFreshSymbol(symbol))
            {
                SymbolDefinition definition = node.DataFlowInfo.GenerateDefinition(symbol);
                node.DataFlowInfo.AssignTypeToDefinition(type, definition);
                node.DataFlowInfo.TaintDefinition(definition, definition);
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the assignment expression.
        /// </summary>
        /// <param name="leftSymbol">ISymbol</param>
        /// <param name="rightExpr">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void AnalyzeAssignmentExpression(ISymbol leftSymbol, ExpressionSyntax rightExpr,
            IDataFlowNode node)
        {
            ISet<ITypeSymbol> assignmentTypes = new HashSet<ITypeSymbol>();
            if (rightExpr is IdentifierNameSyntax ||
                rightExpr is MemberAccessExpressionSyntax)
            {
                ISymbol rightSymbol = this.SemanticModel.GetSymbolInfo(rightExpr).Symbol;
                ITypeSymbol rightType = this.SemanticModel.GetTypeInfo(rightExpr).Type;

                IdentifierNameSyntax rhs = this.AnalysisContext.GetRootIdentifier(rightExpr);
                ISymbol rightMemberSymbol = this.SemanticModel.GetSymbolInfo(rhs).Symbol;

                if (rightSymbol.Equals(rightMemberSymbol))
                {
                    assignmentTypes.UnionWith(node.DataFlowInfo.GetCandidateTypesOfSymbol(rightMemberSymbol));
                }
                else
                {
                    assignmentTypes.Add(rightType);
                }
                if (!this.AnalysisContext.IsTypePassedByValueOrImmutable(rightType))
                {
                    node.DataFlowInfo.TaintSymbol(rightMemberSymbol, rightMemberSymbol);
                    node.DataFlowInfo.TaintSymbol(rightMemberSymbol, leftSymbol);
                }
            }
            else if (rightExpr is InvocationExpressionSyntax ||
                rightExpr is ObjectCreationExpressionSyntax)
            {
                ISet<ISymbol> returnSymbols = null;
                this.AnalyzeMethodCall(rightExpr, node, out returnSymbols, out assignmentTypes);

                if (returnSymbols.Count > 0)
                {
                    foreach (var returnSymbol in returnSymbols)
                    {
                        node.DataFlowInfo.TaintSymbol(returnSymbol, returnSymbol);
                    }

                    if (assignmentTypes.Any(type => !this.AnalysisContext.IsTypePassedByValueOrImmutable(type)))
                    {
                        node.DataFlowInfo.TaintSymbol(returnSymbols, leftSymbol);
                    }
                }
            }

            node.DataFlowInfo.AssignTypesToSymbol(assignmentTypes, leftSymbol);
        }

        /// <summary>
        /// Analyzes the data-flow of the return statement.
        /// </summary>
        /// <param name="retStmt">ReturnStatementSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void AnalyzeReturnStatement(ReturnStatementSyntax retStmt, IDataFlowNode node)
        {
            this.ResolveSideEffectsInExpression(retStmt.Expression, node);

            ISet<ISymbol> returnSymbols = new HashSet<ISymbol>();
            ISet<ITypeSymbol> returnTypes = new HashSet<ITypeSymbol>();
            if (retStmt.Expression is IdentifierNameSyntax ||
                retStmt.Expression is MemberAccessExpressionSyntax)
            {
                ISymbol rightSymbol = this.SemanticModel.GetSymbolInfo(retStmt.Expression).Symbol;
                returnSymbols.Add(rightSymbol);
                returnTypes.UnionWith(node.DataFlowInfo.GetCandidateTypesOfSymbol(rightSymbol));
            }
            else if (retStmt.Expression is InvocationExpressionSyntax ||
                retStmt.Expression is ObjectCreationExpressionSyntax)
            {
                this.AnalyzeMethodCall(retStmt.Expression, node, out returnSymbols, out returnTypes);
                foreach (var returnSymbol in returnSymbols)
                {
                    node.DataFlowInfo.TaintSymbol(returnSymbol, returnSymbol);
                }
            }

            var indexMap = new Dictionary<IParameterSymbol, int>();
            var parameterList = this.Summary.Method.ParameterList.Parameters;
            for (int idx = 0; idx < parameterList.Count; idx++)
            {
                var paramSymbol = this.SemanticModel.GetDeclaredSymbol(parameterList[idx]);
                indexMap.Add(paramSymbol, idx);
            }

            foreach (var returnSymbol in returnSymbols)
            {
                var returnDefinitions = node.DataFlowInfo.ResolveOutputAliases(returnSymbol);
                foreach (var returnDefinition in returnDefinitions.Where(
                    def => node.DataFlowInfo.TaintedDefinitions.ContainsKey(def)))
                {
                    foreach (var definition in node.DataFlowInfo.TaintedDefinitions[returnDefinition].
                        Where(s => s.Kind == SymbolKind.Parameter))
                    {
                        var parameterSymbol = definition.Symbol as IParameterSymbol;
                        this.Summary.SideEffectsInfo.ReturnedParameters.Add(indexMap[parameterSymbol]);
                        this.Summary.SideEffectsInfo.ReturnTypes.Add(parameterSymbol.Type);
                    }

                    foreach (var definition in node.DataFlowInfo.TaintedDefinitions[returnDefinition].
                        Where(r => r.Kind == SymbolKind.Field))
                    {
                        var fieldSymbol = definition.Symbol as IFieldSymbol;
                        this.Summary.SideEffectsInfo.ReturnedFields.Add(fieldSymbol);
                        this.Summary.SideEffectsInfo.ReturnTypes.Add(fieldSymbol.Type);
                    }
                }
            }

            foreach (var returnType in returnTypes)
            {
                this.Summary.SideEffectsInfo.ReturnTypes.Add(returnType);
            }
        }

        /// <summary>
        /// Analyzes the data-flow of the method call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void AnalyzeMethodCall(ExpressionSyntax call, IDataFlowNode node)
        {
            ISet<ISymbol> returnSymbols = null;
            ISet<ITypeSymbol> returnTypes = null;
            this.AnalyzeMethodCall(call, node, out returnSymbols, out returnTypes);
        }

        /// <summary>
        /// Analyzes the data-flow in the method call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        /// <param name="returnSymbols">Return symbols</param>
        /// <param name="returnTypes">Return types</param>
        private void AnalyzeMethodCall(ExpressionSyntax call, IDataFlowNode node,
            out ISet<ISymbol> returnSymbols, out ISet<ITypeSymbol> returnTypes)
        {
            returnSymbols = new HashSet<ISymbol>();
            returnTypes = new HashSet<ITypeSymbol>();

            var callSymbol = this.SemanticModel.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                return;
            }

            var invocation = call as InvocationExpressionSyntax;
            var objCreation = call as ObjectCreationExpressionSyntax;

            ISet<MethodSummary> candidateCalleeSummaries;
            if (invocation != null)
            {
                candidateCalleeSummaries = MethodSummaryResolver.ResolveMethodSummaries(invocation, node);
            }
            else
            {
                candidateCalleeSummaries = MethodSummaryResolver.ResolveMethodSummaries(objCreation, node);
            }

            ArgumentListSyntax argumentList;
            if (invocation != null)
            {
                argumentList = invocation.ArgumentList;
            }
            else
            {
                argumentList = objCreation.ArgumentList;
            }
            
            this.ResolveGivesUpOwnershipInCall(callSymbol, argumentList, node);

            foreach (var candidateCalleeSummary in candidateCalleeSummaries)
            {
                this.MapCalleeSummaryToCallSymbol(candidateCalleeSummary, callSymbol, node);
                this.ResolveGivesUpOwnershipInCall(callSymbol, candidateCalleeSummary,
                    argumentList, node);
                this.ResolveSideEffectsInCall(call, candidateCalleeSummary, node);

                if (invocation != null)
                {
                    returnSymbols.UnionWith(candidateCalleeSummary.GetResolvedReturnSymbols(
                        invocation, this.SemanticModel));
                }

                returnTypes.UnionWith(candidateCalleeSummary.SideEffectsInfo.ReturnTypes);
            }
            
            if (objCreation != null)
            {
                returnTypes.Add(this.SemanticModel.GetTypeInfo(call).Type);
            }

            if (invocation != null)
            {
                this.ResolveSideEffectsInExpression(invocation.Expression, node);
            }
        }

        #endregion

        #region transfer methods

        /// <summary>
        /// Transfers the data-flow information from the
        /// previous data-flow node.
        /// </summary>
        /// <param name="node">IDataFlowNode</param>
        private void Transfer(IDataFlowNode node)
        {
            foreach (var predecessor in node.IPredecessors)
            {
                node.DataFlowInfo.AssignInputDefinitions(predecessor.DataFlowInfo.OutputDefinitions);
                
                foreach (var pair in predecessor.DataFlowInfo.TaintedDefinitions)
                {
                    if (!predecessor.DataFlowInfo.KilledDefinitions.Contains(pair.Key))
                    {
                        node.DataFlowInfo.TaintDefinition(pair.Value, pair.Key);
                    }
                }
            }
        }

        #endregion

        #region resolution methods

        /// <summary>
        /// Resolves side-effects in the specified expression.
        /// </summary>
        /// <param name="expr">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveSideEffectsInExpression(ExpressionSyntax expr, IDataFlowNode node)
        {
            if (expr is MemberAccessExpressionSyntax)
            {
                var memberAccess = expr as MemberAccessExpressionSyntax;
                this.ResolveMethodParameterAccesses(memberAccess, node);
                this.ResolveFieldAccesses(memberAccess, node);
            }
        }

        /// <summary>
        /// Resolves any method parameter acccesses in the member access expression.
        /// </summary>
        /// <param name="expr">MemberAccessExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveMethodParameterAccesses(MemberAccessExpressionSyntax expr, IDataFlowNode node)
        {
            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = this.AnalysisContext.GetRootIdentifier(expr);
            if (identifier == null || name == null || name.Equals(identifier))
            {
                return;
            }

            this.ResolveMethodParameterAccesses(identifier,
                new HashSet<Statement> { node.Statement }, node);
        }

        /// <summary>
        /// Resolves the method parameter acccesses in the identifier.
        /// </summary>
        /// <param name="identifier">IdentifierNameSyntax</param>
        /// <param name="parameterAccesses">Parameter accesses</param>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveMethodParameterAccesses(IdentifierNameSyntax identifier,
            ISet<Statement> parameterAccesses, IDataFlowNode node)
        {
            var symbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol == null)
            {
                return;
            }

            var indexMap = new Dictionary<IParameterSymbol, int>();
            var parameterList = this.Summary.Method.ParameterList.Parameters;
            for (int idx = 0; idx < parameterList.Count; idx++)
            {
                indexMap.Add(this.SemanticModel.GetDeclaredSymbol(parameterList[idx]), idx);
            }

            foreach (var pair in indexMap)
            {
                if (this.Summary.DataFlowAnalysis.FlowsFromParameter(pair.Key, symbol, node.Statement))
                {
                    if (!this.Summary.SideEffectsInfo.ParameterAccesses.ContainsKey(pair.Value))
                    {
                        this.Summary.SideEffectsInfo.ParameterAccesses.Add(pair.Value,
                            new HashSet<Statement>());
                    }

                    foreach (var access in parameterAccesses)
                    {
                        this.Summary.SideEffectsInfo.ParameterAccesses[pair.Value].Add(access);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves any field acccesses in the member access expression.
        /// </summary>
        /// <param name="expr">MemberAccessExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveFieldAccesses(MemberAccessExpressionSyntax expr, IDataFlowNode node)
        {
            var name = (expr as MemberAccessExpressionSyntax).Name;
            var identifier = this.AnalysisContext.GetRootIdentifier(expr);
            if (identifier == null || name == null || name.Equals(identifier))
            {
                return;
            }

            var fieldSymbol = this.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (fieldSymbol == null)
            {
                return;
            }

            var aliasDefinitions = node.DataFlowInfo.ResolveInputAliases(fieldSymbol);
            foreach (var aliasDefinition in aliasDefinitions)
            {
                if (aliasDefinition.Kind == SymbolKind.Field &&
                    this.Summary.DataFlowAnalysis.FlowsFromMethodEntry(aliasDefinition.Symbol, node.Statement))
                {
                    this.MapFieldAccessInStatement(aliasDefinition.Symbol as IFieldSymbol,
                        node.Statement);
                }
            }
        }

        #endregion

        #region side-effect resolution methods

        /// <summary>
        /// Resolves parameters flowing into fields side-effects.
        /// </summary>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveParameterToFieldFlowSideEffects(IDataFlowNode node)
        {
            var fieldFlowSideEffects = node.Summary.SideEffectsInfo.FieldFlowParamIndexes;
            foreach (var pair in node.DataFlowInfo.TaintedDefinitions)
            {
                foreach (var value in pair.Value)
                {
                    if (pair.Key.Kind != SymbolKind.Field ||
                        value.Kind != SymbolKind.Parameter)
                    {
                        continue;
                    }

                    if (!fieldFlowSideEffects.ContainsKey(pair.Key.Symbol as IFieldSymbol))
                    {
                        fieldFlowSideEffects.Add(pair.Key.Symbol as IFieldSymbol, new HashSet<int>());
                    }

                    var parameter = value.Symbol.DeclaringSyntaxReferences.
                        First().GetSyntax() as ParameterSyntax;
                    var parameterList = parameter.Parent as ParameterListSyntax;
                    for (int idx = 0; idx < parameterList.Parameters.Count; idx++)
                    {
                        if (parameterList.Parameters[idx].Equals(parameter))
                        {
                            fieldFlowSideEffects[pair.Key.Symbol as IFieldSymbol].Add(idx);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves the side-effects in the call.
        /// </summary>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveSideEffectsInCall(ExpressionSyntax call,
            MethodSummary calleeSummary, IDataFlowNode node)
        {
            var invocation = call as InvocationExpressionSyntax;
            var objCreation = call as ObjectCreationExpressionSyntax;
            if (calleeSummary == null ||
                (invocation == null && objCreation == null))
            {
                return;
            }

            ArgumentListSyntax argumentList;
            if (invocation != null)
            {
                argumentList = invocation.ArgumentList;
            }
            else
            {
                argumentList = objCreation.ArgumentList;
            }

            var sideEffects = this.ResolveSideEffectsInCall(argumentList, calleeSummary, node);
            foreach (var sideEffect in sideEffects)
            {
                node.DataFlowInfo.KillDefinitions(sideEffect.Key);

                SymbolDefinition definition = node.DataFlowInfo.GenerateDefinition(sideEffect.Key);
                node.DataFlowInfo.AssignTypeToDefinition(sideEffect.Key.Type, definition);
                node.DataFlowInfo.TaintDefinition(definition, definition);
                
                foreach (var symbol in sideEffect.Value)
                {
                    node.DataFlowInfo.TaintSymbol(symbol, sideEffect.Key);
                }
            }

            for (int index = 0; index < argumentList.Arguments.Count; index++)
            {
                if (!calleeSummary.SideEffectsInfo.ParameterAccesses.ContainsKey(index))
                {
                    continue;
                }

                var argIdentifier = this.AnalysisContext.GetRootIdentifier(
                    argumentList.Arguments[index].Expression);
                this.ResolveMethodParameterAccesses(argIdentifier,
                    calleeSummary.SideEffectsInfo.ParameterAccesses[index], node);
            }

            foreach (var fieldAccess in calleeSummary.SideEffectsInfo.FieldAccesses)
            {
                foreach (var access in fieldAccess.Value)
                {
                    this.MapFieldAccessInStatement(fieldAccess.Key as IFieldSymbol, access);
                }
            }
        }

        /// <summary>
        /// Resolves the side-effects in the call.
        /// </summary>
        /// <param name="argumentList">Argument list</param>
        /// <param name="calleeSummary">Callee MethodSummary</param>
        /// <param name="node">IDataFlowNode</param>
        /// <returns>Set of side-effects</returns>
        private IDictionary<IFieldSymbol, ISet<ISymbol>> ResolveSideEffectsInCall(ArgumentListSyntax argumentList,
            MethodSummary calleeSummary, IDataFlowNode node)
        {
            var sideEffects = new Dictionary<IFieldSymbol, ISet<ISymbol>>();
            foreach (var sideEffect in calleeSummary.SideEffectsInfo.FieldFlowParamIndexes)
            {
                foreach (var index in sideEffect.Value)
                {
                    var argExpr = argumentList.Arguments[index].Expression;
                    if (argExpr is IdentifierNameSyntax ||
                        argExpr is MemberAccessExpressionSyntax)
                    {
                        var argType = node.Summary.SemanticModel.GetTypeInfo(argExpr).Type;
                        if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                        {
                            continue;
                        }

                        IdentifierNameSyntax argIdentifier = this.AnalysisContext.GetRootIdentifier(argExpr);
                        if (!sideEffects.ContainsKey(sideEffect.Key))
                        {
                            sideEffects.Add(sideEffect.Key, new HashSet<ISymbol>());
                        }

                        sideEffects[sideEffect.Key].Add(node.Summary.SemanticModel.GetSymbolInfo(argIdentifier).Symbol);
                    }
                }
            }

            return sideEffects;
        }

        #endregion

        #region gives-up ownership methods

        /// <summary>
        /// Resolves the gives-up ownership information in the call.
        /// </summary>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="argumentList">ArgumentListSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveGivesUpOwnershipInCall(ISymbol callSymbol,
            ArgumentListSyntax argumentList, IDataFlowNode node)
        {
            string methodName = callSymbol.ContainingNamespace.ToString() + "." + callSymbol.Name;
            if (this.AnalysisContext.GivesUpOwnershipMethods.ContainsKey(methodName) &&
                (this.AnalysisContext.GivesUpOwnershipMethods[methodName].Max() <
                argumentList.Arguments.Count))
            {
                foreach (var paramIndex in this.AnalysisContext.GivesUpOwnershipMethods[methodName])
                {
                    var argExpr = argumentList.Arguments[paramIndex].Expression;
                    this.ResolveGivesUpOwnershipInArgument(callSymbol, argExpr, node);
                }
            }
        }

        /// <summary>
        /// Resolves the gives-up ownership information in the call.
        /// </summary>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="methodSummary">MethodSummary</param>
        /// <param name="argumentList">ArgumentListSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveGivesUpOwnershipInCall(ISymbol callSymbol, MethodSummary methodSummary,
            ArgumentListSyntax argumentList, IDataFlowNode node)
        {
            foreach (var paramIndex in methodSummary.SideEffectsInfo.GivesUpOwnershipParamIndexes)
            {
                var argExpr = argumentList.Arguments[paramIndex].Expression;
                this.ResolveGivesUpOwnershipInArgument(callSymbol, argExpr, node);
            }
        }

        /// <summary>
        /// Resolves the gives-up ownership information in the argument.
        /// </summary>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="argExpr">ExpressionSyntax</param>
        /// <param name="node">IDataFlowNode</param>
        private void ResolveGivesUpOwnershipInArgument(ISymbol callSymbol,
            ExpressionSyntax argExpr, IDataFlowNode node)
        {
            if (argExpr is IdentifierNameSyntax ||
                argExpr is MemberAccessExpressionSyntax)
            {
                IdentifierNameSyntax argIdentifier = this.AnalysisContext.GetRootIdentifier(argExpr);
                ISymbol argSymbol = this.SemanticModel.GetSymbolInfo(argIdentifier).Symbol;

                for (int idx = 0; idx < this.Summary.Method.ParameterList.Parameters.Count; idx++)
                {
                    ParameterSyntax param = this.Summary.Method.ParameterList.Parameters[idx];
                    TypeInfo typeInfo = this.SemanticModel.GetTypeInfo(param.Type);
                    if (this.AnalysisContext.IsTypePassedByValueOrImmutable(typeInfo.Type))
                    {
                        continue;
                    }

                    IParameterSymbol paramSymbol = this.SemanticModel.GetDeclaredSymbol(param);
                    if (this.Summary.DataFlowAnalysis.FlowsFromParameter(paramSymbol, argSymbol, node.Statement))
                    {
                        this.Summary.SideEffectsInfo.GivesUpOwnershipParamIndexes.Add(idx);
                    }
                }
                
                var argTypes = node.DataFlowInfo.GetCandidateTypesOfSymbol(argSymbol);
                if (argTypes.Any(type => !this.AnalysisContext.IsTypePassedByValueOrImmutable(type)))
                {
                    node.GivesUpOwnershipMap.Add(argSymbol);
                }
            }
            else if (argExpr is BinaryExpressionSyntax &&
                argExpr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = argExpr as BinaryExpressionSyntax;
                this.ResolveGivesUpOwnershipInArgument(callSymbol, binExpr.Left, node);
            }
            else if (argExpr is InvocationExpressionSyntax ||
                argExpr is ObjectCreationExpressionSyntax)
            {
                ArgumentListSyntax argumentList = this.AnalysisContext.GetArgumentList(argExpr);
                foreach (var arg in argumentList.Arguments)
                {
                    this.ResolveGivesUpOwnershipInArgument(callSymbol, arg.Expression, node);
                }
            }
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Returns the symbol infos from the specified member expression.
        /// </summary>
        /// <param name="symbolInfos">ISymbols</param>
        /// <param name="expr">ExpressionSyntax</param>
        private void GetMemberExpressionSymbols(out ISet<Tuple<ISymbol, ITypeSymbol>> symbolInfos,
            ExpressionSyntax expr)
        {
            symbolInfos = new HashSet<Tuple<ISymbol, ITypeSymbol>>();
            if (expr is IdentifierNameSyntax ||
                expr is MemberAccessExpressionSyntax)
            {
                var memberExprs = this.AnalysisContext.GetIdentifiers(expr);
                foreach (var memberExpr in memberExprs)
                {
                    symbolInfos.Add(Tuple.Create(this.SemanticModel.GetSymbolInfo(memberExpr).Symbol,
                        this.SemanticModel.GetTypeInfo(memberExpr).Type));
                }
            }
            else if (expr is ElementAccessExpressionSyntax)
            {
                var memberAccess = (expr as ElementAccessExpressionSyntax);
                if (memberAccess.Expression is IdentifierNameSyntax ||
                    memberAccess.Expression is MemberAccessExpressionSyntax)
                {
                    var memberExprs = this.AnalysisContext.GetIdentifiers(expr);
                    foreach (var memberExpr in memberExprs)
                    {
                        symbolInfos.Add(Tuple.Create(this.SemanticModel.GetSymbolInfo(memberExpr).Symbol,
                            this.SemanticModel.GetTypeInfo(memberExpr).Type));
                    }
                }
            }
        }

        /// <summary>
        /// Maps the access of the field symbol.
        /// </summary>
        /// <param name="fieldSymbol">IFieldSymbol</param>
        /// <param name="statement">Statement</param>
        private void MapFieldAccessInStatement(IFieldSymbol fieldSymbol, Statement statement)
        {
            if (!this.Summary.SideEffectsInfo.FieldAccesses.ContainsKey(fieldSymbol))
            {
                this.Summary.SideEffectsInfo.FieldAccesses.Add(fieldSymbol,
                    new HashSet<Statement>());
            }

            this.Summary.SideEffectsInfo.FieldAccesses[fieldSymbol].Add(statement);
        }

        /// <summary>
        /// Maps the callee summary to the call symbol.
        /// </summary>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="callSymbol">Symbol</param>
        /// <param name="node">IDataFlowNode</param>
        private void MapCalleeSummaryToCallSymbol(MethodSummary calleeSummary,
            ISymbol callSymbol, IDataFlowNode node)
        {
            if (!node.MethodSummaryCache.ContainsKey(callSymbol))
            {
                node.MethodSummaryCache.Add(callSymbol, new HashSet<MethodSummary>());
            }

            node.MethodSummaryCache[callSymbol].Add(calleeSummary);
        }

        /// <summary>
        /// Compare with the specified data-flow information.
        /// </summary>
        /// <param name="oldInfo">DataFlowInfo</param>
        /// <param name="newInfo">DataFlowInfo</param>
        /// <returns>Boolean</returns>
        private bool Compare(DataFlowInfo oldInfo, DataFlowInfo newInfo)
        {
            if (!oldInfo.GeneratedDefinitions.SetEquals(newInfo.GeneratedDefinitions) ||
                !oldInfo.KilledDefinitions.SetEquals(newInfo.KilledDefinitions) ||
                !oldInfo.InputDefinitions.SetEquals(newInfo.InputDefinitions) ||
                !oldInfo.OutputDefinitions.SetEquals(newInfo.OutputDefinitions) ||
                !oldInfo.TaintedDefinitions.Keys.SequenceEqual(newInfo.TaintedDefinitions.Keys))
            {
                return false;
            }

            foreach (var symbol in oldInfo.TaintedDefinitions)
            {
                if (!symbol.Value.SetEquals(newInfo.TaintedDefinitions[symbol.Key]))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
