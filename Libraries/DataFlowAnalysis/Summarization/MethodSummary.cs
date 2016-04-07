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

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a method summary.
    /// </summary>
    public sealed class MethodSummary
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

        /// <summary>
        /// The unique id of the summary.
        /// </summary>
        internal readonly int Id;

        /// <summary>
        /// Method that this summary represents.
        /// </summary>
        public BaseMethodDeclarationSyntax Method;

        /// <summary>
        /// Type declaration that contains the method
        /// that this summary represents.
        /// </summary>
        public TypeDeclarationSyntax TypeDeclaration;

        /// <summary>
        /// The control-flow graph of the method of this summary.
        /// </summary>
        public ControlFlowGraph ControlFlowGraph;

        /// <summary>
        /// The data-flow analysis engine that is analyzing
        /// this method summary.
        /// </summary>
        internal DataFlowAnalysisEngine DataFlowAnalysisEngine;

        /// <summary>
        /// Dictionary containing all read and write parameters
        /// accesses in the original method.
        /// </summary>
        public Dictionary<int, HashSet<Statement>> ParameterAccesses;

        /// <summary>
        /// Dictionary containing all read and write
        /// field accesses in the original method.
        /// </summary>
        public Dictionary<IFieldSymbol, HashSet<Statement>> FieldAccesses;

        /// <summary>
        /// Dictionary containing all side effects in regards to
        /// parameters flowing into fields in the original method.
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

        /// <summary>
        /// Set of the indexes of parameters that the original method
        /// gives up during its execution.
        /// </summary>
        public HashSet<int> GivesUpOwnershipParamIndexes;

        /// <summary>
        /// A counter for creating unique IDs.
        /// </summary>
        private static int IdCounter;

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static MethodSummary()
        {
            MethodSummary.IdCounter = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">BaseMethodDeclarationSyntax</param>
        /// <param name="typeDeclaration">TypeDeclarationSyntax</param>
        private MethodSummary(AnalysisContext context, BaseMethodDeclarationSyntax method,
            TypeDeclarationSyntax typeDeclaration)
        {
            this.AnalysisContext = context;
            this.Method = method;
            this.TypeDeclaration = typeDeclaration;
            this.Id = MethodSummary.IdCounter++;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Creates the summary of the given method.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">BaseMethodDeclarationSyntax</param>
        /// <param name="typeDeclaration">TypeDeclarationSyntax</param>
        /// <returns>MethodSummary</returns>
        public static MethodSummary Create(AnalysisContext context, BaseMethodDeclarationSyntax method,
            TypeDeclarationSyntax typeDeclaration = null)
        {
            var summary = new MethodSummary(context, method, typeDeclaration);
            return summary.BuildSummary();
        }

        /// <summary>
        /// Resolves and returns all possible method summaries
        /// for the given call symbol.
        /// </summary>
        /// <param name="callSymbol">ISymbol</param>
        /// <param name="statement">Statement</param>
        /// <returns>Set of method summaries</returns>
        public HashSet<MethodSummary> GetResolvedCalleeSummaries(ISymbol callSymbol,
            Statement statement)
        {
            var calleeSummaries = new HashSet<MethodSummary>();

            Dictionary<ISymbol, HashSet<MethodSummary>> calleeSummaryMap = null;
            if (!this.DataFlowAnalysisEngine.TryGetCalleeSummaryMapForStatement(
                statement, out calleeSummaryMap))
            {
                return calleeSummaries;
            }

            if (!calleeSummaryMap.ContainsKey(callSymbol))
            {
                return calleeSummaries;
            }

            return calleeSummaryMap[callSymbol];
        }

        /// <summary>
        /// Resolves and returns all possible return symbols at
        /// the point of the given invocation.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of return symbols</returns>
        public HashSet<ISymbol> GetResolvedReturnSymbols(InvocationExpressionSyntax invocation,
            SemanticModel model)
        {
            var returnSymbols = new HashSet<ISymbol>();

            foreach (var index in this.ReturnSet.Item1)
            {
                var argExpr = invocation.ArgumentList.Arguments[index].Expression;
                var arg = AnalysisContext.GetTopLevelIdentifier(argExpr);
                ITypeSymbol argType = model.GetTypeInfo(argExpr).Type;
                if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                {
                    continue;
                }

                var returnSymbol = model.GetSymbolInfo(arg).Symbol;
                returnSymbols.Add(returnSymbol);
            }

            foreach (var field in this.ReturnSet.Item2)
            {
                returnSymbols.Add(field);
            }

            return returnSymbols;
        }

        /// <summary>
        /// Returns symbols with given-up ownership.
        /// </summary>
        /// <returns>GivenUpOwnershipSymbols</returns>
        public IEnumerable<GivenUpOwnershipSymbol> GetSymbolsWithGivenUpOwnership()
        {
            return this.DataFlowAnalysisEngine.GetSymbolsWithGivenUpOwnership();
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Resolves and returns all possible side effects at the
        /// point of the given invocation.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="calleeSummary">Callee MethodSummary</param>
        /// <param name="statement">Statement</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of side effects</returns>
        internal Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> GetResolvedSideEffects(
            InvocationExpressionSyntax invocation, MethodSummary calleeSummary,
            Statement statement, SemanticModel model)
        {
            return this.GetResolvedSideEffects(invocation.ArgumentList,
                calleeSummary, statement, model);
        }

        /// <summary>
        /// Resolves and returns all possible side effects at the
        /// point of the given object creation.
        /// </summary>
        /// <param name="objCreation">ObjectCreationExpressionSyntax</param>
        /// <param name="calleeSummary">Callee MethodSummary</param>
        /// <param name="statement">Statement</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of side effects</returns>
        internal Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> GetResolvedSideEffects(
            ObjectCreationExpressionSyntax objCreation, MethodSummary calleeSummary,
            Statement statement, SemanticModel model)
        {
            return this.GetResolvedSideEffects(objCreation.ArgumentList,
                calleeSummary, statement, model);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Builds the summary.
        /// </summary>
        /// <returns>MethodSummary</returns>
        private MethodSummary BuildSummary()
        {
            if (!this.BuildControlFlowGraph())
            {
                return this;
            }
            
            this.ParameterAccesses = new Dictionary<int, HashSet<Statement>>();
            this.FieldAccesses = new Dictionary<IFieldSymbol, HashSet<Statement>>();
            this.SideEffects = new Dictionary<IFieldSymbol, HashSet<int>>();
            this.ReturnSet = new Tuple<HashSet<int>, HashSet<IFieldSymbol>>(
                new HashSet<int>(), new HashSet<IFieldSymbol>());
            this.ReturnTypeSet = new HashSet<ITypeSymbol>();
            this.GivesUpOwnershipParamIndexes = new HashSet<int>();

            this.AnalyzeDataFlow();
            this.ComputeSideEffects();

            return this;
        }

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

            this.ControlFlowGraph = ControlFlowGraph.Create(this.AnalysisContext, this);
            return true;
        }

        /// <summary>
        /// Analyzes the data-flow of the method.
        /// </summary>
        private void AnalyzeDataFlow()
        {
            var model = this.AnalysisContext.Compilation.GetSemanticModel(this.Method.SyntaxTree);
            this.DataFlowAnalysisEngine = DataFlowAnalysisEngine.Create(this, this.AnalysisContext, model);
            this.DataFlowAnalysisEngine.Run();
        }

        /// <summary>
        /// Computes field access side effects in the exit nodes of the
        /// control-flow graph using information from the data-flow analysis.
        /// </summary>
        private void ComputeSideEffects()
        {
            foreach (var exitNode in this.ControlFlowGraph.ExitNodes)
            {
                if (exitNode.Statements.Count == 0)
                {
                    continue;
                }

                var exitStatement = exitNode.Statements.Last();

                Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> exitDataFlowMap = null;
                if (!this.DataFlowAnalysisEngine.TryGetDataFlowMapForStatement(
                    exitStatement, out exitDataFlowMap))
                {
                    continue;
                }

                foreach (var pair in exitDataFlowMap)
                {
                    foreach (var value in pair.Value)
                    {
                        if (pair.Key.Kind != SymbolKind.Field ||
                            value.ContainingSymbol.Kind != SymbolKind.Parameter)
                        {
                            continue;
                        }

                        if (!this.SideEffects.ContainsKey(pair.Key.ContainingSymbol as IFieldSymbol))
                        {
                            this.SideEffects.Add(pair.Key.ContainingSymbol as IFieldSymbol, new HashSet<int>());
                        }

                        var parameter = value.ContainingSymbol.DeclaringSyntaxReferences.
                            First().GetSyntax() as ParameterSyntax;
                        var parameterList = parameter.Parent as ParameterListSyntax;
                        for (int idx = 0; idx < parameterList.Parameters.Count; idx++)
                        {
                            if (parameterList.Parameters[idx].Equals(parameter))
                            {
                                this.SideEffects[pair.Key.ContainingSymbol as IFieldSymbol].Add(idx);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves and returns all possible side effects at the
        /// point of the given call argument list.
        /// </summary>
        /// <param name="argumentList">Argument list</param>
        /// <param name="calleeSummary">Callee MethodSummary</param>
        /// <param name="statement">Statement</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of side effects</returns>
        private Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>> GetResolvedSideEffects(
            ArgumentListSyntax argumentList, MethodSummary calleeSummary, Statement statement,
            SemanticModel model)
        {
            var sideEffects = new Dictionary<DataFlowSymbol, HashSet<DataFlowSymbol>>();
            foreach (var sideEffect in calleeSummary.SideEffects)
            {
                var argSymbols = new HashSet<DataFlowSymbol>();
                foreach (var index in sideEffect.Value)
                {
                    var argExpr = argumentList.Arguments[index].Expression;
                    if (argExpr is IdentifierNameSyntax ||
                        argExpr is MemberAccessExpressionSyntax)
                    {
                        var argType = model.GetTypeInfo(argExpr).Type;
                        if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                        {
                            continue;
                        }

                        IdentifierNameSyntax argIdentifier = AnalysisContext.GetTopLevelIdentifier(argExpr);
                        ISymbol argSymbol = model.GetSymbolInfo(argIdentifier).Symbol;
                        var argDFSymbols = this.DataFlowAnalysisEngine.GetDataFlowSymbols(
                            argSymbol, statement);
                        argSymbols.UnionWith(argDFSymbols);

                        var sideEffectDFSymbols = this.DataFlowAnalysisEngine.GetDataFlowSymbols(
                            sideEffect.Key, statement);
                        foreach (var sideEffectDFSymbol in sideEffectDFSymbols)
                        {
                            sideEffects.Add(sideEffectDFSymbol, argSymbols);
                        }
                    }
                    else if (argExpr is InvocationExpressionSyntax ||
                        argExpr is ObjectCreationExpressionSyntax)
                    {
                        var invocation = argExpr as InvocationExpressionSyntax;
                        var objCreation = argExpr as ObjectCreationExpressionSyntax;

                        MethodSummary summary = null;
                        if (invocation != null)
                        {
                            summary = this.AnalysisContext.TryGetCachedSummary(invocation, model);
                            argumentList = invocation.ArgumentList;
                        }
                        else
                        {
                            summary = this.AnalysisContext.TryGetCachedSummary(objCreation, model);
                            argumentList = objCreation.ArgumentList;
                        }

                        if (summary == null)
                        {
                            continue;
                        }

                        var nestedSideEffects = summary.GetResolvedSideEffects(
                            argumentList, calleeSummary, statement, model);
                        foreach (var nestedSideEffect in nestedSideEffects)
                        {
                            sideEffects.Add(nestedSideEffect.Key, nestedSideEffect.Value);
                        }
                    }
                }
            }

            return sideEffects;
        }

        #endregion

        #region summary printing methods

        /// <summary>
        /// Prints the control-flow graph.
        /// </summary>
        public void PrintControlFlowGraph()
        {
            Console.WriteLine("..");
            Console.WriteLine("... ==================================================");
            Console.WriteLine("... ================ ControlFlowGraph ================");
            Console.WriteLine("... ==================================================");
            Console.WriteLine("... |");
            Console.WriteLine("... | Summary id: '{0}'", this.Id);
            Console.WriteLine("... | Method: '{0}'", this.AnalysisContext.
                GetFullMethodName(this.Method));

            this.ControlFlowGraph.EntryNode.PrintNodes();

            Console.WriteLine("... |");
            Console.WriteLine("... ==================================================");
        }

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        public void PrintDataFlowInformation()
        {
            this.PrintDataFlowInformation(false);
        }

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        /// <param name="isChild">Is child of summary</param>
        internal void PrintDataFlowInformation(bool isChild)
        {
            string indent = "..";
            if (isChild)
            {
                indent += "....";
            }

            Console.WriteLine(indent);
            Console.WriteLine(indent + ". ==================================================");
            Console.WriteLine(indent + ". ================ DataFlow Summary ================");
            Console.WriteLine(indent + ". ==================================================");
            Console.WriteLine(indent + ". |");
            Console.WriteLine(indent + ". | Summary id: '{0}'", this.Id);
            Console.WriteLine(indent + ". | Method: '{0}'", this.AnalysisContext.
                GetFullMethodName(this.Method));

            this.DataFlowAnalysisEngine.PrintDataFlowMap(indent);
            this.DataFlowAnalysisEngine.PrintReferenceTypes(indent);
            this.DataFlowAnalysisEngine.PrintCalleeSummaryMap(indent);
            this.DataFlowAnalysisEngine.PrintGivesUpOwnershipMap(indent);
            
            if (this.ParameterAccesses.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Parameter access set");
                foreach (var index in this.ParameterAccesses)
                {
                    foreach (var statement in index.Value)
                    {
                        Console.WriteLine(indent + ". | ... Parameter at index '{0}' " +
                            "is accessed in '{1}'", index.Key, statement.SyntaxNode);
                    }
                }
            }

            if (this.FieldAccesses.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Field access set");
                foreach (var field in this.FieldAccesses)
                {
                    foreach (var statement in field.Value)
                    {
                        Console.WriteLine(indent + ". | ... '{0}' accessed in '{1}'",
                           field.Key, statement.SyntaxNode);
                    }
                }
            }

            if (this.SideEffects.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Side effects");
                foreach (var pair in this.SideEffects)
                {
                    foreach (var index in pair.Value)
                    {
                        Console.WriteLine(indent + ". | ... parameter at index '{0}' " +
                            "flows into field '{1}'", index, pair.Key);
                    }
                }
            }

            if (this.ReturnSet.Item1.Count > 0 ||
                this.ReturnSet.Item2.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Method return set");
                foreach (var index in this.ReturnSet.Item1)
                {
                    Console.WriteLine(indent + ". | ... Parameter at index '{0}'", index);
                }

                foreach (var field in this.ReturnSet.Item2)
                {
                    Console.WriteLine(indent + ". | ... Field '{0}'", field);
                }
            }

            if (this.ReturnTypeSet.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Method return types");
                foreach (var type in this.ReturnTypeSet)
                {
                    Console.WriteLine(indent + ". | ... Type '{0}'", type);
                }
            }

            if (this.GivesUpOwnershipParamIndexes.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Gives-up ownership parameter indexes");
                Console.Write(indent + ". | ...");
                foreach (var index in this.GivesUpOwnershipParamIndexes)
                {
                    Console.Write(" '{0}'", index);
                }

                Console.WriteLine("");
            }

            Console.WriteLine(indent + ". |");
            Console.WriteLine(indent + ". ==================================================");
            
            this.DataFlowAnalysisEngine.PrintCalleeSummaries();
        }

        #endregion
    }
}
