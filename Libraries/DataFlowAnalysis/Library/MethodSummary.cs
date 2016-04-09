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
    public sealed class MethodSummary
    {
        #region fields

        /// <summary>
        /// The unique id of the summary.
        /// </summary>
        internal readonly int Id;

        /// <summary>
        /// The analysis context.
        /// </summary>
        internal AnalysisContext AnalysisContext { get; private set; }

        /// <summary>
        /// The semantic model of this summary.
        /// </summary>
        internal SemanticModel SemanticModel { get; private set; }

        /// <summary>
        /// Method that this summary represents.
        /// </summary>
        public BaseMethodDeclarationSyntax Method { get; private set; }

        /// <summary>
        /// Type declaration that contains the method
        /// that this summary represents.
        /// </summary>
        public TypeDeclarationSyntax TypeDeclaration { get; private set; }

        /// <summary>
        /// The control-flow graph of this summary.
        /// </summary>
        public IGraph<IControlFlowNode> ControlFlowGraph { get; private set; }

        /// <summary>
        /// The data-flow graph of this summary.
        /// </summary>
        internal IGraph<IDataFlowNode> DataFlowGraph { get; private set; }

        /// <summary>
        /// The data-flow analysis of this summary.
        /// </summary>
        public IDataFlowAnalysis DataFlowAnalysis { get; private set; }

        /// <summary>
        /// Dictionary containing all read and write parameters
        /// accesses in the method.
        /// </summary>
        public IDictionary<int, ISet<Statement>> ParameterAccesses;

        /// <summary>
        /// Dictionary containing all read and write
        /// field accesses in the method.
        /// </summary>
        public IDictionary<IFieldSymbol, ISet<Statement>> FieldAccesses;

        /// <summary>
        /// Dictionary containing all side effects in regards to
        /// parameters flowing into fields in the method.
        /// </summary>
        public IDictionary<IFieldSymbol, ISet<int>> SideEffects;

        /// <summary>
        /// Set of fields and associated types that are
        /// returned to the caller of the method.
        /// </summary>
        public ISet<Tuple<IFieldSymbol, ITypeSymbol>> ReturnedFields;

        /// <summary>
        /// Set of parameter indexes and associated types that are
        /// returned to the caller of the method.
        /// </summary>
        public ISet<Tuple<int, ITypeSymbol>> ReturnedParameters;

        /// <summary>
        /// Set of the indexes of parameters that the method
        /// gives up during its execution.
        /// </summary>
        public ISet<int> GivesUpOwnershipParamIndexes;

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
            this.SemanticModel = context.Compilation.GetSemanticModel(method.SyntaxTree);
            this.Method = method;
            this.TypeDeclaration = typeDeclaration;
            this.Id = MethodSummary.IdCounter++;
        }

        /// <summary>
        /// Creates the summary of the specified method.
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
        /// Returns all cached method summaries for the specified call symbol.
        /// </summary>
        /// <param name="callSymbol">ISymbol</param>
        /// <param name="statement">Statement</param>
        /// <returns>MethodSummarys</returns>
        public static ISet<MethodSummary> GetCachedSummaries(ISymbol callSymbol, Statement statement)
        {
            var calleeSummaries = new HashSet<MethodSummary>();

            IDataFlowNode node;
            if (callSymbol == null || statement == null ||
                !statement.Summary.DataFlowGraph.TryGetNodeContaining(statement, out node) ||
                !node.MethodSummaryCache.ContainsKey(callSymbol))
            {
                return calleeSummaries;
            }

            return node.MethodSummaryCache[callSymbol];
        }

        #endregion

        #region public methods

        /// <summary>
        /// Resolves and returns all possible return symbols at
        /// the point of the specified invocation.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of return symbols</returns>
        public ISet<Tuple<ISymbol, ITypeSymbol>> GetResolvedReturnSymbols(
            InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var returnSymbols = new HashSet<Tuple<ISymbol, ITypeSymbol>>();

            foreach (var parameter in this.ReturnedParameters)
            {
                var argExpr = invocation.ArgumentList.Arguments[parameter.Item1].Expression;
                var returnSymbol = model.GetSymbolInfo(argExpr).Symbol;
                returnSymbols.Add(Tuple.Create(returnSymbol, parameter.Item2));
            }

            foreach (var field in this.ReturnedFields)
            {
                returnSymbols.Add(Tuple.Create(field.Item1 as ISymbol, field.Item2));
            }

            return returnSymbols;
        }

        /// <summary>
        /// Returns symbols with given-up ownership.
        /// </summary>
        /// <returns>GivenUpOwnershipSymbols</returns>
        public IEnumerable<GivenUpOwnershipSymbol> GetSymbolsWithGivenUpOwnership()
        {
            var symbols = new List<GivenUpOwnershipSymbol>();
            foreach (var dfgNode in this.DataFlowGraph.Nodes)
            {
                foreach (var symbol in dfgNode.GivesUpOwnershipMap)
                {
                    symbols.Add(new GivenUpOwnershipSymbol(symbol, dfgNode.Statement));
                }
            }

            return symbols;
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
            
            this.ParameterAccesses = new Dictionary<int, ISet<Statement>>();
            this.FieldAccesses = new Dictionary<IFieldSymbol, ISet<Statement>>();
            this.SideEffects = new Dictionary<IFieldSymbol, ISet<int>>();
            this.ReturnedFields = new HashSet<Tuple<IFieldSymbol, ITypeSymbol>>();
            this.ReturnedParameters = new HashSet<Tuple<int, ITypeSymbol>>();
            this.GivesUpOwnershipParamIndexes = new HashSet<int>();

            this.AnalyzeDataFlow();

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

            this.ControlFlowGraph = new ControlFlowGraph(this);
            return true;
        }

        /// <summary>
        /// Analyzes the data-flow of the method.
        /// </summary>
        private void AnalyzeDataFlow()
        {
            var dataFlowGraph = new DataFlowGraph(this);
            this.DataFlowGraph = dataFlowGraph;
            this.DataFlowAnalysis = dataFlowGraph;

            new TaintTrackingAnalysis(dataFlowGraph).Run();
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

            this.ControlFlowGraph.PrettyPrint();

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
        private void PrintDataFlowInformation(bool isChild)
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

            this.PrintGeneratedDefinitions(indent);
            this.PrintKilledDefinitions(indent);
            this.PrintInputDefinitions(indent);
            this.PrintOutputDefinitions(indent);
            this.PrintTaintedDefinitions(indent);
            this.PrintMethodSummaryCache(indent);
            this.PrintGivesUpOwnershipMap(indent);

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

            if (this.ReturnedFields.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Returned fields");
                foreach (var field in this.ReturnedFields)
                {
                    Console.WriteLine(indent + ". | ... Field " +
                        $"'{field.Item1}' of type '{field.Item2}'");
                }
            }

            if (this.ReturnedParameters.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Returned parameters");
                foreach (var parameter in this.ReturnedParameters)
                {
                    Console.WriteLine(indent + ". | ... Parameter at index " +
                        $"'{parameter.Item1}' of type '{parameter.Item2}'");
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

            this.PrintCachedMethodSummaries();
        }

        /// <summary>
        /// Prints the generated definitions.
        /// </summary>
        /// <param name="indent">Indent</param>
        private void PrintGeneratedDefinitions(string indent)
        {
            if (this.DataFlowGraph.Nodes.Any(val => val.DataFlowInfo.GeneratedDefinitions.Count > 0))
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Generated definitions");
                foreach (var cfgNode in this.DataFlowGraph.Nodes.Select(
                    val => val.ControlFlowNode).Distinct())
                {
                    var dfgNodes = this.DataFlowGraph.Nodes.Where(
                        val => val.ControlFlowNode.Equals(cfgNode));
                    if (dfgNodes.All(val => val.DataFlowInfo.GeneratedDefinitions.Count == 0))
                    {
                        continue;
                    }

                    Console.WriteLine(indent + $". | ... CFG '{cfgNode}'");
                    foreach (var dfgNode in dfgNodes)
                    {
                        if (dfgNode.DataFlowInfo.GeneratedDefinitions.Count == 0)
                        {
                            continue;
                        }

                        Console.WriteLine(indent + $". | ..... {dfgNode.Statement.SyntaxNode}");
                        foreach (var definition in dfgNode.DataFlowInfo.GeneratedDefinitions)
                        {
                            Console.WriteLine(indent + $". | ....... generates '{definition}'");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the killed definitions.
        /// </summary>
        /// <param name="indent">Indent</param>
        private void PrintKilledDefinitions(string indent)
        {
            if (this.DataFlowGraph.Nodes.Any(val => val.DataFlowInfo.KilledDefinitions.Count > 0))
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Killed definitions");
                foreach (var cfgNode in this.DataFlowGraph.Nodes.Select(
                    val => val.ControlFlowNode).Distinct())
                {
                    var dfgNodes = this.DataFlowGraph.Nodes.Where(
                        val => val.ControlFlowNode.Equals(cfgNode));
                    if (dfgNodes.All(val => val.DataFlowInfo.KilledDefinitions.Count == 0))
                    {
                        continue;
                    }

                    Console.WriteLine(indent + $". | ... CFG '{cfgNode}'");
                    foreach (var dfgNode in dfgNodes)
                    {
                        if (dfgNode.DataFlowInfo.KilledDefinitions.Count == 0)
                        {
                            continue;
                        }

                        Console.WriteLine(indent + $". | ..... {dfgNode.Statement.SyntaxNode}");
                        foreach (var definition in dfgNode.DataFlowInfo.KilledDefinitions)
                        {
                            Console.WriteLine(indent + $". | ....... kills '{definition}'");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the input definitions.
        /// </summary>
        /// <param name="indent">Indent</param>
        private void PrintInputDefinitions(string indent)
        {
            if (this.DataFlowGraph.Nodes.Any(val => val.DataFlowInfo.InputDefinitions.Count > 0))
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Input definitions");
                foreach (var cfgNode in this.DataFlowGraph.Nodes.Select(
                    val => val.ControlFlowNode).Distinct())
                {
                    var dfgNodes = this.DataFlowGraph.Nodes.Where(
                        val => val.ControlFlowNode.Equals(cfgNode));
                    if (dfgNodes.All(val => val.DataFlowInfo.InputDefinitions.Count == 0))
                    {
                        continue;
                    }

                    Console.WriteLine(indent + $". | ... CFG '{cfgNode}'");
                    foreach (var dfgNode in dfgNodes)
                    {
                        if (dfgNode.DataFlowInfo.InputDefinitions.Count == 0)
                        {
                            continue;
                        }

                        Console.WriteLine(indent + $". | ..... {dfgNode.Statement.SyntaxNode}");
                        foreach (var definition in dfgNode.DataFlowInfo.InputDefinitions)
                        {
                            Console.WriteLine(indent + $". | ....... in '{definition}'");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the output definitions.
        /// </summary>
        /// <param name="indent">Indent</param>
        private void PrintOutputDefinitions(string indent)
        {
            if (this.DataFlowGraph.Nodes.Any(val => val.DataFlowInfo.OutputDefinitions.Count > 0))
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Output definitions");
                foreach (var cfgNode in this.DataFlowGraph.Nodes.Select(
                    val => val.ControlFlowNode).Distinct())
                {
                    var dfgNodes = this.DataFlowGraph.Nodes.Where(
                        val => val.ControlFlowNode.Equals(cfgNode));
                    if (dfgNodes.All(val => val.DataFlowInfo.OutputDefinitions.Count == 0))
                    {
                        continue;
                    }

                    Console.WriteLine(indent + $". | ... CFG '{cfgNode}'");
                    foreach (var dfgNode in dfgNodes)
                    {
                        if (dfgNode.DataFlowInfo.OutputDefinitions.Count == 0)
                        {
                            continue;
                        }

                        Console.WriteLine(indent + $". | ..... {dfgNode.Statement.SyntaxNode}");
                        foreach (var definition in dfgNode.DataFlowInfo.OutputDefinitions)
                        {
                            Console.WriteLine(indent + $". | ....... out '{definition}'");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the tainted definitions.
        /// </summary>
        /// <param name="indent">Indent</param>
        private void PrintTaintedDefinitions(string indent)
        {
            if (this.DataFlowGraph.Nodes.Any(val => val.DataFlowInfo.TaintedDefinitions.Count > 0))
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Taint tracking information");
                foreach (var cfgNode in this.DataFlowGraph.Nodes.Select(
                    val => val.ControlFlowNode).Distinct())
                {
                    var dfgNodes = this.DataFlowGraph.Nodes.Where(
                        val => val.ControlFlowNode.Equals(cfgNode));
                    if (dfgNodes.All(val => val.DataFlowInfo.TaintedDefinitions.Count == 0))
                    {
                        continue;
                    }

                    Console.WriteLine(indent + $". | ... CFG '{cfgNode}'");
                    foreach (var dfgNode in dfgNodes)
                    {
                        if (dfgNode.DataFlowInfo.TaintedDefinitions.Count == 0)
                        {
                            continue;
                        }

                        Console.WriteLine(indent + $". | ..... {dfgNode.Statement.SyntaxNode}");
                        foreach (var pair in dfgNode.DataFlowInfo.TaintedDefinitions)
                        {
                            foreach (var symbol in pair.Value)
                            {
                                Console.WriteLine(indent + ". | ....... " +
                                    $"'{pair.Key.Name}' <=== '{symbol.Name}'");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the method summary cache.
        /// </summary>
        /// <param name="indent">Indent</param>
        private void PrintMethodSummaryCache(string indent)
        {
            if (this.DataFlowGraph.Nodes.Any(val => val.MethodSummaryCache.Count > 0))
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Method summary cache");
                foreach (var cfgNode in this.DataFlowGraph.Nodes.Select(
                    val => val.ControlFlowNode).Distinct())
                {
                    var dfgNodes = this.DataFlowGraph.Nodes.Where(
                        val => val.ControlFlowNode.Equals(cfgNode));
                    if (dfgNodes.All(val => val.MethodSummaryCache.Count == 0))
                    {
                        continue;
                    }

                    Console.WriteLine(indent + $". | ... CFG '{cfgNode}'");
                    foreach (var dfgNode in dfgNodes)
                    {
                        if (dfgNode.MethodSummaryCache.Count == 0)
                        {
                            continue;
                        }

                        Console.WriteLine(indent + $". | ..... {dfgNode.Statement.SyntaxNode}");
                        foreach (var pair in dfgNode.MethodSummaryCache)
                        {
                            foreach (var summary in pair.Value)
                            {
                                Console.WriteLine(indent + ". | ....... " +
                                    $"callee summary with id '{summary.Id}'");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the gives-up ownership map.
        /// </summary>
        /// <param name="indent">Indent</param>
        private void PrintGivesUpOwnershipMap(string indent)
        {
            if (this.DataFlowGraph.Nodes.Any(val => val.GivesUpOwnershipMap.Count > 0))
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Operations giving up ownership");
                foreach (var cfgNode in this.DataFlowGraph.Nodes.Select(
                    val => val.ControlFlowNode).Distinct())
                {
                    var dfgNodes = this.DataFlowGraph.Nodes.Where(
                        val => val.ControlFlowNode.Equals(cfgNode));
                    if (dfgNodes.All(val => val.GivesUpOwnershipMap.Count == 0))
                    {
                        continue;
                    }

                    Console.WriteLine(indent + $". | ... CFG '{cfgNode}'");
                    foreach (var dfgNode in dfgNodes)
                    {
                        if (dfgNode.GivesUpOwnershipMap.Count == 0)
                        {
                            continue;
                        }

                        Console.WriteLine(indent + $". | ..... {dfgNode.Statement.SyntaxNode}");
                        Console.Write(indent + ". | ....... gives up ownership of");
                        foreach (var symbol in dfgNode.GivesUpOwnershipMap)
                        {
                            Console.Write($" '{symbol.Name}'");
                        }

                        Console.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// Prints all cached method summaries.
        /// </summary>
        internal void PrintCachedMethodSummaries()
        {
            foreach (var dfgNode in this.DataFlowGraph.Nodes)
            {
                foreach (var symbol in dfgNode.MethodSummaryCache)
                {
                    foreach (var summary in symbol.Value)
                    {
                        summary.PrintDataFlowInformation(true);
                    }
                }
            }
        }

        #endregion
    }
}
