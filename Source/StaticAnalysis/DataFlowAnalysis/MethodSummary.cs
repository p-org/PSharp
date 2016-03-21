//-----------------------------------------------------------------------
// <copyright file="MethodSummary.cs">
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
    /// Class implementing a method summary.
    /// </summary>
    public class MethodSummary
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        protected AnalysisContext AnalysisContext;

        /// <summary>
        /// Method that this summary represents.
        /// </summary>
        internal BaseMethodDeclarationSyntax Method;

        /// <summary>
        /// The entry node of the control-flow graph of the
        /// method of this summary.
        /// </summary>
        internal CFGNode EntryNode;

        /// <summary>
        /// Set of all exit nodes in the control-flow graph of the
        /// method of this summary.
        /// </summary>
        internal HashSet<CFGNode> ExitNodes;

        /// <summary>
        /// The data-flow of the method of this summary.
        /// </summary>
        internal DataFlowAnalysis DataFlowAnalysis;

        /// <summary>
        /// Dictionary containing all read and write accesses
        /// of the parameters of the original method.
        /// </summary>
        internal Dictionary<int, HashSet<SyntaxNode>> AccessSet;

        /// <summary>
        /// Dictionary containing all field accesses.
        /// </summary>
        internal Dictionary<IFieldSymbol, HashSet<SyntaxNode>> FieldAccessSet;

        /// <summary>
        /// Dictionary containing all side effects in regards to the
        /// parameters of the original method.
        /// </summary>
        internal Dictionary<IFieldSymbol, HashSet<int>> SideEffects;

        /// <summary>
        /// Tuple containing all returns of the original method in regards
        /// to method parameters and fields.
        /// </summary>
        internal Tuple<HashSet<int>, HashSet<IFieldSymbol>> ReturnSet;

        /// <summary>
        /// Set of all return type symbols of the original method.
        /// </summary>
        internal HashSet<ITypeSymbol> ReturnTypeSet;

        #endregion

        #region constructors

        /// <summary>
        /// Creates the summary of the given method.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">Method</param>
        /// <returns>MethodSummary</returns>
        public static MethodSummary Create(AnalysisContext context, BaseMethodDeclarationSyntax method)
        {
            if (context.Summaries.ContainsKey(method))
            {
                return context.Summaries[method];
            }

            var summary = new MethodSummary(context, method);
            summary.BuildSummary();

            return summary;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">Method</param>
        protected MethodSummary(AnalysisContext context, BaseMethodDeclarationSyntax method)
        {
            this.AnalysisContext = context;
            this.Method = method;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Tries to get the method summary of the given object creation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>MethodSummary</returns>
        internal static MethodSummary TryGet(ObjectCreationExpressionSyntax call,
            SemanticModel model, AnalysisContext context)
        {
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                return null;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol, context.Solution).Result;
            if (definition == null)
            {
                return null;
            }

            if (definition.DeclaringSyntaxReferences.IsEmpty)
            {
                return null;
            }

            var constructorCall = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ConstructorDeclarationSyntax;
            return MethodSummary.Create(context, constructorCall);
        }

        /// <summary>
        /// Tries to get the method summary of the given invocation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>MethodSummary</returns>
        internal static MethodSummary TryGet(InvocationExpressionSyntax call,
            SemanticModel model, AnalysisContext context)
        {
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                return null;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol, context.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                return null;
            }

            var invocationCall = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as MethodDeclarationSyntax;
            return MethodSummary.Create(context, invocationCall);
        }

        /// <summary>
        /// Resolves and returns all possible side effects at the point of the
        /// given call argument list.
        /// </summary>
        /// <param name="argumentList">Argument list</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of side effects</returns>
        internal Dictionary<ISymbol, HashSet<ISymbol>> GetResolvedSideEffects(ArgumentListSyntax argumentList,
            SemanticModel model)
        {
            Dictionary<ISymbol, HashSet<ISymbol>> sideEffects = new Dictionary<ISymbol, HashSet<ISymbol>>();
            foreach (var sideEffect in this.SideEffects)
            {
                HashSet<ISymbol> argSymbols = new HashSet<ISymbol>();
                foreach (var index in sideEffect.Value)
                {
                    IdentifierNameSyntax arg = null;
                    var argExpr = argumentList.Arguments[index].Expression;
                    if (argExpr is IdentifierNameSyntax)
                    {
                        arg = argExpr as IdentifierNameSyntax;
                        var argType = model.GetTypeInfo(arg).Type;
                        if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                        {
                            continue;
                        }

                        argSymbols.Add(model.GetSymbolInfo(arg).Symbol);
                    }
                    else if (argExpr is MemberAccessExpressionSyntax)
                    {
                        var name = (argExpr as MemberAccessExpressionSyntax).Name;
                        var argType = model.GetTypeInfo(name).Type;
                        if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                        {
                            continue;
                        }

                        arg = this.AnalysisContext.GetTopLevelIdentifier(argExpr);
                        argSymbols.Add(model.GetSymbolInfo(arg).Symbol);
                    }
                    else if (argExpr is ObjectCreationExpressionSyntax)
                    {
                        var objCreation = argExpr as ObjectCreationExpressionSyntax;
                        var summary = MethodSummary.TryGet(objCreation, model, this.AnalysisContext);
                        if (summary == null)
                        {
                            continue;
                        }

                        var nestedSideEffects = summary.GetResolvedSideEffects(
                            objCreation.ArgumentList, model);
                        foreach (var nestedSideEffect in nestedSideEffects)
                        {
                            sideEffects.Add(nestedSideEffect.Key, nestedSideEffect.Value);
                        }
                    }
                    else if (argExpr is InvocationExpressionSyntax)
                    {
                        var invocation = argExpr as InvocationExpressionSyntax;
                        var summary = this.AnalysisContext.TryGetSummary(invocation, model);
                        if (summary == null)
                        {
                            continue;
                        }

                        var nestedSideEffects = summary.GetResolvedSideEffects(
                            invocation.ArgumentList, model);
                        foreach (var nestedSideEffect in nestedSideEffects)
                        {
                            sideEffects.Add(nestedSideEffect.Key, nestedSideEffect.Value);
                        }
                    }
                }

                sideEffects.Add(sideEffect.Key, argSymbols);
            }

            return sideEffects;
        }

        /// <summary>
        /// Resolves and returns all possible return symbols at the point of the
        /// given call argument list.
        /// </summary>
        /// <param name="argumentList">Argument list</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of return symbols</returns>
        internal HashSet<ISymbol> GetResolvedReturnSymbols(ArgumentListSyntax argumentList,
            SemanticModel model)
        {
            HashSet<ISymbol> returnSymbols = new HashSet<ISymbol>();

            foreach (var index in this.ReturnSet.Item1)
            {
                IdentifierNameSyntax arg = null;
                var argExpr = argumentList.Arguments[index].Expression;
                if (argExpr is IdentifierNameSyntax)
                {
                    arg = argExpr as IdentifierNameSyntax;
                    var argType = model.GetTypeInfo(arg).Type;
                    if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                    {
                        continue;
                    }
                }
                else if (argExpr is MemberAccessExpressionSyntax)
                {
                    var name = (argExpr as MemberAccessExpressionSyntax).Name;
                    var argType = model.GetTypeInfo(name).Type;
                    if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                    {
                        continue;
                    }

                    arg = this.AnalysisContext.GetTopLevelIdentifier(argExpr);
                }

                returnSymbols.Add(model.GetSymbolInfo(arg).Symbol);
            }

            foreach (var field in this.ReturnSet.Item2)
            {
                returnSymbols.Add(field as IFieldSymbol);
            }

            return returnSymbols;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Builds the summary.
        /// </summary>
        protected void BuildSummary()
        {
            if (!this.BuildControlFlowGraph())
            {
                return;
            }

            this.AccessSet = new Dictionary<int, HashSet<SyntaxNode>>();
            this.FieldAccessSet = new Dictionary<IFieldSymbol, HashSet<SyntaxNode>>();
            this.SideEffects = new Dictionary<IFieldSymbol, HashSet<int>>();
            this.ReturnSet = new Tuple<HashSet<int>, HashSet<IFieldSymbol>>(
                new HashSet<int>(), new HashSet<IFieldSymbol>());
            this.ReturnTypeSet = new HashSet<ITypeSymbol>();

            this.AnalyzeDataFlow();
            this.ComputeSideEffects();

            AnalysisContext.Summaries.Add(this.Method, this);
        }

        /// <summary>
        /// Creates a new control-flow graph node.
        /// </summary>
        /// <returns>CFGNode</returns>
        protected virtual CFGNode CreateNewControlFlowGraphNode()
        {
            return new CFGNode(this.AnalysisContext, this);
        }

        /// <summary>
        /// Analyzes the data-flow of the method.
        /// </summary>
        protected virtual void AnalyzeDataFlow()
        {
            var model = this.AnalysisContext.Compilation.GetSemanticModel(this.Method.SyntaxTree);
            this.DataFlowAnalysis = DataFlowAnalysis.Analyze(this, this.AnalysisContext, model);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Builds the control-flow graph of the method.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool BuildControlFlowGraph()
        {
            if (this.Method.Modifiers.Any(SyntaxKind.AbstractKeyword))
            {
                return false;
            }

            SemanticModel model = null;

            try
            {
                model = this.AnalysisContext.Compilation.GetSemanticModel(this.Method.SyntaxTree);
            }
            catch
            {
                return false;
            }

            this.EntryNode = this.CreateNewControlFlowGraphNode();
            this.ExitNodes = new HashSet<CFGNode>();
            this.EntryNode.Construct(this.Method);
            this.EntryNode.CleanEmptySuccessors();
            this.ExitNodes = this.EntryNode.GetExitNodes();

            return true;
        }

        /// <summary>
        /// Computes side effects in the control-flow graph using
        /// information from the data-flow analysis.
        /// </summary>
        private void ComputeSideEffects()
        {
            foreach (var exitNode in this.ExitNodes)
            {
                if (exitNode.SyntaxNodes.Count == 0)
                {
                    continue;
                }

                var exitSyntaxNode = exitNode.SyntaxNodes.Last();
                Dictionary<ISymbol, HashSet<ISymbol>> exitMap = null;
                if (this.DataFlowAnalysis.TryGetDataFlowMapForSyntaxNode(exitSyntaxNode, exitNode, out exitMap))
                {
                    foreach (var pair in exitMap)
                    {
                        var keyDefinition = SymbolFinder.FindSourceDefinitionAsync(pair.Key,
                            this.AnalysisContext.Solution).Result;
                        foreach (var value in pair.Value)
                        {
                            var valueDefinition = SymbolFinder.FindSourceDefinitionAsync(value,
                                this.AnalysisContext.Solution).Result;
                            if (keyDefinition == null || valueDefinition == null)
                            {
                                continue;
                            }

                            if (keyDefinition.Kind == SymbolKind.Field &&
                                valueDefinition.Kind == SymbolKind.Parameter)
                            {
                                if (!this.SideEffects.ContainsKey(pair.Key as IFieldSymbol))
                                {
                                    this.SideEffects.Add(pair.Key as IFieldSymbol, new HashSet<int>());
                                }

                                var parameter = valueDefinition.DeclaringSyntaxReferences.First().
                                    GetSyntax() as ParameterSyntax;
                                var parameterList = parameter.Parent as ParameterListSyntax;
                                for (int idx = 0; idx < parameterList.Parameters.Count; idx++)
                                {
                                    if (parameterList.Parameters[idx].Equals(parameter))
                                    {
                                        this.SideEffects[pair.Key as IFieldSymbol].Add(idx);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region summary printing methods

        /// <summary>
        /// Prints the control-flow information.
        /// </summary>
        public void PrintControlFlowInformation()
        {
            IO.PrintLine("..");
            IO.PrintLine("... ==================================================");
            IO.PrintLine("... =============== ControlFlow Summary ===============");
            IO.PrintLine("... ==================================================");
            IO.PrintLine("... |");
            IO.PrintLine("... | Method: '{0}'", this.AnalysisContext.GetFullMethodName(this.Method));

            this.EntryNode.PrintCFGNodes();
            this.EntryNode.PrintCFGSuccessors();
            this.EntryNode.PrintCFGPredecessors();

            IO.PrintLine("... |");
            IO.PrintLine("... ==================================================");
        }

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        public void PrintDataFlowInformation()
        {
            IO.PrintLine("..");
            IO.PrintLine("... ==================================================");
            IO.PrintLine("... ================ DataFlow Summary ================");
            IO.PrintLine("... ==================================================");
            IO.PrintLine("... |");
            IO.PrintLine("... | Method: '{0}'", this.AnalysisContext.GetFullMethodName(this.Method));

            this.DataFlowAnalysis.PrintDataFlowMap();
            this.DataFlowAnalysis.PrintFieldReachabilityMap();
            this.DataFlowAnalysis.PrintReferenceTypes();
            this.DataFlowAnalysis.PrintStatementsThatResetReferences();

            if (this.AccessSet.Count > 0)
            {
                IO.PrintLine("..... Access set");
                foreach (var index in this.AccessSet)
                {
                    foreach (var syntaxNode in index.Value)
                    {
                        IO.PrintLine("....... " + index.Key + " " + syntaxNode);
                    }
                }
            }

            if (this.FieldAccessSet.Count > 0)
            {
                IO.PrintLine("..... Field access set");
                foreach (var field in this.FieldAccessSet)
                {
                    foreach (var syntaxNode in field.Value)
                    {
                        IO.PrintLine("....... " + field.Key.Name + " " + syntaxNode);
                    }
                }
            }

            if (this.SideEffects.Count > 0)
            {
                IO.PrintLine("... |");
                IO.PrintLine("... | . Side effects");
                foreach (var pair in this.SideEffects)
                {
                    foreach (var index in pair.Value)
                    {
                        IO.PrintLine("... | ... parameter at index '{0}' flows into field '{1}'",
                            index, pair.Key.Name);
                    }
                }
            }

            if (this.ReturnSet.Item1.Count > 0 ||
                this.ReturnSet.Item2.Count > 0)
            {
                IO.PrintLine("..... Return set");
                foreach (var index in this.ReturnSet.Item1)
                {
                    IO.PrintLine("....... " + index);
                }

                foreach (var field in this.ReturnSet.Item2)
                {
                    IO.PrintLine("....... " + field.Name);
                }
            }

            if (this.ReturnTypeSet.Count > 0)
            {
                IO.PrintLine("..... Return type set");
                foreach (var type in this.ReturnTypeSet)
                {
                    IO.PrintLine("....... " + type.Name);
                }
            }

            IO.PrintLine("... |");
            IO.PrintLine("... ==================================================");
        }

        #endregion
    }
}
