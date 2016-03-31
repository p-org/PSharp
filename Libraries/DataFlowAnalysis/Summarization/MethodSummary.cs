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

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
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
        public BaseMethodDeclarationSyntax Method;

        /// <summary>
        /// The entry node of the control-flow graph of the
        /// method of this summary.
        /// </summary>
        public ControlFlowGraphNode EntryNode;

        /// <summary>
        /// Set of all exit nodes in the control-flow graph of the
        /// method of this summary.
        /// </summary>
        public HashSet<ControlFlowGraphNode> ExitNodes;

        /// <summary>
        /// The data-flow of the method of this summary.
        /// </summary>
        public DataFlowAnalysis DataFlowAnalysis;

        /// <summary>
        /// Dictionary containing all read and write accesses
        /// of the parameters of the original method.
        /// </summary>
        public Dictionary<int, HashSet<SyntaxNode>> ParameterAccessSet;

        /// <summary>
        /// Dictionary containing all field accesses.
        /// </summary>
        public Dictionary<IFieldSymbol, HashSet<SyntaxNode>> FieldAccessSet;

        /// <summary>
        /// Dictionary containing all side effects in regards to the
        /// parameters of the original method.
        /// </summary>
        public Dictionary<IFieldSymbol, HashSet<int>> SideEffects;

        /// <summary>
        /// Tuple containing all returns of the original method in regards
        /// to method parameters and fields.
        /// </summary>
        public Tuple<HashSet<int>, HashSet<IFieldSymbol>> ReturnSet;

        /// <summary>
        /// Set of all return type symbols of the original method.
        /// </summary>
        public HashSet<ITypeSymbol> ReturnTypeSet;

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
        public static MethodSummary TryGet(ObjectCreationExpressionSyntax call,
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
        public static MethodSummary TryGet(InvocationExpressionSyntax call,
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
        public Dictionary<ISymbol, HashSet<ISymbol>> GetResolvedSideEffects(ArgumentListSyntax argumentList,
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

                        arg = DataFlowQuerying.GetTopLevelIdentifier(argExpr);
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
        public HashSet<ISymbol> GetResolvedReturnSymbols(ArgumentListSyntax argumentList,
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

                    arg = DataFlowQuerying.GetTopLevelIdentifier(argExpr);
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
            
            this.ParameterAccessSet = new Dictionary<int, HashSet<SyntaxNode>>();
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
        /// <returns>ControlFlowGraphNode</returns>
        protected virtual ControlFlowGraphNode CreateNewControlFlowGraphNode()
        {
            return new ControlFlowGraphNode(this.AnalysisContext, this);
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
            this.ExitNodes = new HashSet<ControlFlowGraphNode>();
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
            Console.WriteLine("..");
            Console.WriteLine("... ==================================================");
            Console.WriteLine("... =============== ControlFlow Summary ===============");
            Console.WriteLine("... ==================================================");
            Console.WriteLine("... |");
            Console.WriteLine("... | Method: '{0}'", this.AnalysisContext.GetFullMethodName(this.Method));

            this.EntryNode.PrintControlFlowGraphNodes();
            this.EntryNode.PrintCFGSuccessors();
            this.EntryNode.PrintCFGPredecessors();

            Console.WriteLine("... |");
            Console.WriteLine("... ==================================================");
        }

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        public void PrintDataFlowInformation()
        {
            Console.WriteLine("..");
            Console.WriteLine("... ==================================================");
            Console.WriteLine("... ================ DataFlow Summary ================");
            Console.WriteLine("... ==================================================");
            Console.WriteLine("... |");
            Console.WriteLine("... | Method: '{0}'", this.AnalysisContext.GetFullMethodName(this.Method));

            this.DataFlowAnalysis.PrintDataFlowMap();
            this.DataFlowAnalysis.PrintFieldReachabilityMap();
            this.DataFlowAnalysis.PrintReferenceTypes();
            this.DataFlowAnalysis.PrintStatementsThatResetReferences();

            if (this.ParameterAccessSet.Count > 0)
            {
                Console.WriteLine("... |");
                Console.WriteLine("... | . Parameter access set");
                foreach (var index in this.ParameterAccessSet)
                {
                    foreach (var syntaxNode in index.Value)
                    {
                        Console.WriteLine("... | ... Parameter at index '{0}' is accessed in '{1}'",
                            index.Key, syntaxNode);
                    }
                }
            }

            if (this.FieldAccessSet.Count > 0)
            {
                Console.WriteLine("... |");
                Console.WriteLine("... | . Field access set");
                foreach (var field in this.FieldAccessSet)
                {
                    foreach (var syntaxNode in field.Value)
                    {
                        Console.WriteLine("... | ... Field '{0}' is accessed in '{1}'",
                           field.Key.Name, syntaxNode);
                    }
                }
            }

            if (this.SideEffects.Count > 0)
            {
                Console.WriteLine("... |");
                Console.WriteLine("... | . Side effects");
                foreach (var pair in this.SideEffects)
                {
                    foreach (var index in pair.Value)
                    {
                        Console.WriteLine("... | ... parameter at index '{0}' flows into field '{1}'",
                            index, pair.Key.Name);
                    }
                }
            }

            if (this.ReturnSet.Item1.Count > 0 ||
                this.ReturnSet.Item2.Count > 0)
            {
                Console.WriteLine("... |");
                Console.WriteLine("... | . Method return set");
                foreach (var index in this.ReturnSet.Item1)
                {
                    Console.WriteLine("... | ... Parameter at index '{0}' flows into return", index);
                }

                foreach (var field in this.ReturnSet.Item2)
                {
                    Console.WriteLine("... | ... Field '{0}' flows into return", field.Name);
                }
            }

            if (this.ReturnTypeSet.Count > 0)
            {
                Console.WriteLine("... |");
                Console.WriteLine("... | . Method return types");
                foreach (var type in this.ReturnTypeSet)
                {
                    Console.WriteLine("... | ... Type '{0}'", type.Name);
                }
            }

            Console.WriteLine("... |");
            Console.WriteLine("... ==================================================");
        }

        #endregion
    }
}
