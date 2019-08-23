﻿// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a method summary.
    /// </summary>
    public sealed class MethodSummary
    {
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
        /// The types of the input parameters.
        /// </summary>
        internal IDictionary<ParameterSyntax, ISet<ITypeSymbol>> ParameterTypes { get; private set; }

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
        /// Side-effects information.
        /// </summary>
        public MethodSideEffectsInfo SideEffectsInfo { get; private set; }

        /// <summary>
        /// A counter for creating unique IDs.
        /// </summary>
        private static int IdCounter = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodSummary"/> class.
        /// </summary>
        private MethodSummary(AnalysisContext context, BaseMethodDeclarationSyntax method,
            IDictionary<int, ISet<ITypeSymbol>> parameterTypes)
        {
            this.Id = IdCounter++;
            this.AnalysisContext = context;
            this.SemanticModel = context.Compilation.GetSemanticModel(method.SyntaxTree);
            this.Method = method;
            this.SideEffectsInfo = new MethodSideEffectsInfo(this);
            this.ResolveMethodParameterTypes(parameterTypes);
        }

        /// <summary>
        /// Creates the summary of the specified method.
        /// </summary>
        public static MethodSummary Create(AnalysisContext context, BaseMethodDeclarationSyntax method)
        {
            return Create(context, method, new Dictionary<int, ISet<ITypeSymbol>>());
        }

        /// <summary>
        /// Creates the summary of the specified method, using the
        /// specified parameter types.
        /// </summary>
        public static MethodSummary Create(AnalysisContext context, BaseMethodDeclarationSyntax method,
            IDictionary<int, ISet<ITypeSymbol>> parameterTypes)
        {
            if (method.Body is null)
            {
                return null;
            }

            var summary = new MethodSummary(context, method, parameterTypes);
            summary.BuildSummary();
            summary.AnalyzeSummary();

            return summary;
        }

        /// <summary>
        /// Returns all cached method summaries for the specified call symbol.
        /// </summary>
        public static ISet<MethodSummary> GetCachedSummaries(ISymbol callSymbol, Statement statement)
        {
            var calleeSummaries = new HashSet<MethodSummary>();

            if (callSymbol is null || statement is null ||
                !statement.Summary.DataFlowGraph.TryGetNodeContaining(statement, out IDataFlowNode node) ||
                !node.MethodSummaryCache.ContainsKey(callSymbol))
            {
                return calleeSummaries;
            }

            return node.MethodSummaryCache[callSymbol];
        }

        /// <summary>
        /// Resolves and returns all possible return symbols at
        /// the point of the specified invocation.
        /// </summary>
        public ISet<ISymbol> GetResolvedReturnSymbols(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            return this.SideEffectsInfo.GetResolvedReturnSymbols(invocation, model);
        }

        /// <summary>
        /// Returns symbols with given-up ownership.
        /// </summary>
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

        /// <summary>
        /// Builds the summary.
        /// </summary>
        private void BuildSummary()
        {
            this.ControlFlowGraph = new ControlFlowGraph(this);

            var dataFlowGraph = new DataFlowGraph(this);
            this.DataFlowGraph = dataFlowGraph;
            this.DataFlowAnalysis = dataFlowGraph;
        }

        /// <summary>
        /// Analyzes the summary.
        /// </summary>
        private void AnalyzeSummary()
        {
            new TaintTrackingAnalysis(this.DataFlowGraph).Run();
        }

        /// <summary>
        /// Resolves the parameter types of the method, using the specified parameter types.
        /// </summary>
        private void ResolveMethodParameterTypes(IDictionary<int, ISet<ITypeSymbol>> parameterTypes)
        {
            this.ParameterTypes = new Dictionary<ParameterSyntax, ISet<ITypeSymbol>>();

            for (int idx = 0; idx < this.Method.ParameterList.Parameters.Count; idx++)
            {
                var parameter = this.Method.ParameterList.Parameters[idx];

                if (parameterTypes.ContainsKey(idx))
                {
                    this.ParameterTypes.Add(parameter, parameterTypes[idx]);
                }
                else
                {
                    ITypeSymbol parameterType = this.SemanticModel.GetTypeInfo(parameter.Type).Type;
                    this.ParameterTypes.Add(parameter, new HashSet<ITypeSymbol> { parameterType });
                }
            }
        }

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
            Console.WriteLine("... | Method: '{0}'", AnalysisContext.GetFullMethodName(this.Method));

            this.ControlFlowGraph.PrettyPrint();

            Console.WriteLine("... |");
            Console.WriteLine("... ==================================================");
        }

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        public void PrintDataFlowInformation(bool showDetails = false)
        {
            this.PrintDataFlowInformation(showDetails, false);
        }

        /// <summary>
        /// Prints the data-flow information.
        /// </summary>
        private void PrintDataFlowInformation(bool showDetails, bool isChild)
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
            Console.WriteLine(indent + ". | Method: '{0}'", AnalysisContext.GetFullMethodName(this.Method));

            this.PrintGeneratedDefinitions(indent);
            this.PrintKilledDefinitions(indent);

            if (showDetails)
            {
                this.PrintInputDefinitions(indent);
                this.PrintOutputDefinitions(indent);
            }

            this.PrintTaintedDefinitions(indent);

            this.PrintFieldFlowParamIndexes(indent);
            this.PrintFieldAccesses(indent);
            this.PrintParameterAccesses(indent);
            this.PrintReturnedFields(indent);
            this.PrintReturnedParameters(indent);
            this.PrintReturnedTypes(indent);

            this.PrintGivesUpOwnershipMap(indent);
            this.PrintGivesUpOwnershipParameterIndexes(indent);

            this.PrintMethodSummaryCache(indent);

            Console.WriteLine(indent + ". |");
            Console.WriteLine(indent + ". ==================================================");

            this.PrintCachedMethodSummaries(showDetails);
        }

        /// <summary>
        /// Prints the generated definitions.
        /// </summary>
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
        /// Prints the parameters flowig into fields.
        /// </summary>
        private void PrintFieldFlowParamIndexes(string indent)
        {
            if (this.SideEffectsInfo.FieldFlowParamIndexes.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Parameters flowing into fields");
                foreach (var pair in this.SideEffectsInfo.FieldFlowParamIndexes)
                {
                    foreach (var index in pair.Value)
                    {
                        Console.WriteLine(indent + ". | ... from index '{0}' flows into '{1}'", index, pair.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Prints the field accesses.
        /// </summary>
        private void PrintFieldAccesses(string indent)
        {
            if (this.SideEffectsInfo.FieldAccesses.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Field accesses");
                foreach (var field in this.SideEffectsInfo.FieldAccesses)
                {
                    foreach (var statement in field.Value)
                    {
                        Console.WriteLine(indent + ". | ... '{0}' accessed in '{1}'", field.Key, statement.SyntaxNode);
                    }
                }
            }
        }

        /// <summary>
        /// Prints the parameter accesses.
        /// </summary>
        private void PrintParameterAccesses(string indent)
        {
            if (this.SideEffectsInfo.ParameterAccesses.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Parameter accesses");
                foreach (var index in this.SideEffectsInfo.ParameterAccesses)
                {
                    foreach (var statement in index.Value)
                    {
                        Console.WriteLine(indent + ". | ... at index '{0}' is accessed in '{1}'", index.Key, statement.SyntaxNode);
                    }
                }
            }
        }

        /// <summary>
        /// Prints the returned fields.
        /// </summary>
        private void PrintReturnedFields(string indent)
        {
            if (this.SideEffectsInfo.ReturnedFields.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Returned fields");
                foreach (var field in this.SideEffectsInfo.ReturnedFields)
                {
                    Console.WriteLine(indent + $". | ... field '{field}'");
                }
            }
        }

        /// <summary>
        /// Prints the returned parameters.
        /// </summary>
        private void PrintReturnedParameters(string indent)
        {
            if (this.SideEffectsInfo.ReturnedParameters.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Returned parameters");
                foreach (var parameter in this.SideEffectsInfo.ReturnedParameters)
                {
                    Console.WriteLine(indent + $". | ... parameter at index '{parameter}'");
                }
            }
        }

        /// <summary>
        /// Prints the returned types.
        /// </summary>
        private void PrintReturnedTypes(string indent)
        {
            if (this.SideEffectsInfo.ReturnTypes.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Return types");
                foreach (var type in this.SideEffectsInfo.ReturnTypes)
                {
                    Console.WriteLine(indent + $". | ... type '{type}'");
                }
            }
        }

        /// <summary>
        /// Prints the gives-up ownership parameter indexes.
        /// </summary>
        private void PrintGivesUpOwnershipParameterIndexes(string indent)
        {
            if (this.SideEffectsInfo.GivesUpOwnershipParamIndexes.Count > 0)
            {
                Console.WriteLine(indent + ". |");
                Console.WriteLine(indent + ". | . Gives-up ownership parameter indexes");
                Console.Write(indent + ". | ...");
                foreach (var index in this.SideEffectsInfo.GivesUpOwnershipParamIndexes)
                {
                    Console.Write(" '{0}'", index);
                }

                Console.WriteLine(string.Empty);
            }
        }

        /// <summary>
        /// Prints all cached method summaries.
        /// </summary>
        internal void PrintCachedMethodSummaries(bool showDetails)
        {
            foreach (var dfgNode in this.DataFlowGraph.Nodes)
            {
                foreach (var symbol in dfgNode.MethodSummaryCache)
                {
                    foreach (var summary in symbol.Value)
                    {
                        summary.PrintDataFlowInformation(showDetails, true);
                    }
                }
            }
        }
    }
}
