// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Data-flow information.
    /// </summary>
    public class DataFlowInfo
    {
        /// <summary>
        /// The analysis context.
        /// </summary>
        private readonly AnalysisContext AnalysisContext;

        /// <summary>
        /// Node that contains the data-flow information.
        /// </summary>
        private readonly DataFlowNode DataFlowNode;

        /// <summary>
        /// Set of generated definitions.
        /// </summary>
        public ISet<SymbolDefinition> GeneratedDefinitions { get; private set; }

        /// <summary>
        /// Set of killed definitions.
        /// </summary>
        public ISet<SymbolDefinition> KilledDefinitions { get; private set; }

        /// <summary>
        /// Set of input definitions.
        /// </summary>
        public ISet<SymbolDefinition> InputDefinitions { get; private set; }

        /// <summary>
        /// Set of output definitions.
        /// </summary>
        public ISet<SymbolDefinition> OutputDefinitions { get; private set; }

        /// <summary>
        /// Map containing tainted definitions.
        /// </summary>
        public IDictionary<SymbolDefinition, ISet<SymbolDefinition>>
            TaintedDefinitions { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowInfo"/> class.
        /// </summary>
        internal DataFlowInfo(DataFlowNode dfgNode, AnalysisContext context)
        {
            this.AnalysisContext = context;
            this.DataFlowNode = dfgNode;
            this.GeneratedDefinitions = new HashSet<SymbolDefinition>();
            this.KilledDefinitions = new HashSet<SymbolDefinition>();
            this.InputDefinitions = new HashSet<SymbolDefinition>();
            this.OutputDefinitions = new HashSet<SymbolDefinition>();
            this.TaintedDefinitions = new Dictionary<SymbolDefinition, ISet<SymbolDefinition>>();
        }

        /// <summary>
        /// Generates a new definition for the specified symbol.
        /// </summary>
        internal SymbolDefinition GenerateDefinition(ISymbol symbol)
        {
            var definition = this.GeneratedDefinitions.FirstOrDefault(def => def.Symbol.Equals(symbol));
            if (definition is null)
            {
                definition = new SymbolDefinition(symbol, this.DataFlowNode);
                this.GeneratedDefinitions.Add(definition);
            }

            return definition;
        }

        /// <summary>
        /// Kills the definitions of the specified symbol.
        /// </summary>
        internal void KillDefinitions(ISymbol symbol)
        {
            var definitions = this.GetInputDefinitionsOfSymbol(symbol);
            this.KilledDefinitions.UnionWith(definitions);
        }

        /// <summary>
        /// Assigns the specified inputs definitions.
        /// </summary>
        internal void AssignInputDefinitions(ISet<SymbolDefinition> definitions)
        {
            this.InputDefinitions.UnionWith(definitions);
        }

        /// <summary>
        /// Assigns the output definitions.
        /// </summary>
        internal void AssignOutputDefinitions()
        {
            this.OutputDefinitions.UnionWith(this.GetOutputDefinitions());
        }

        /// <summary>
        /// Assigns the specified types to the symbol.
        /// </summary>
        internal void AssignTypesToSymbol(ISet<ITypeSymbol> types, ISymbol symbol)
        {
            var generatedDefinition = this.GetGeneratedDefinitionOfSymbol(symbol);
            if (generatedDefinition != null && types.Count > 0)
            {
                generatedDefinition.CandidateTypes.Clear();
                generatedDefinition.CandidateTypes.UnionWith(types);
            }
        }

        /// <summary>
        /// Assigns the specified type to the definition.
        /// </summary>
        internal static void AssignTypeToDefinition(ITypeSymbol type, SymbolDefinition definition)
        {
            if (type != null)
            {
                definition.CandidateTypes.Clear();
                definition.CandidateTypes.Add(type);
            }
        }

        /// <summary>
        /// Assigns the specified types to the definition.
        /// </summary>
        internal static void AssignTypesToDefinition(ISet<ITypeSymbol> types, SymbolDefinition definition)
        {
            if (types.Count > 0 && !types.Any(type => type is null))
            {
                definition.CandidateTypes.Clear();
                definition.CandidateTypes.UnionWith(types);
            }
        }

        /// <summary>
        /// Returns the candidate types of the specified symbol.
        /// </summary>
        internal ISet<ITypeSymbol> GetCandidateTypesOfSymbol(ISymbol symbol)
        {
            var candidateTypes = new HashSet<ITypeSymbol>();
            var definitions = this.GetInputOrGeneratedDefinitionsOfSymbol(symbol);
            foreach (var definition in definitions)
            {
                candidateTypes.UnionWith(definition.CandidateTypes);
            }

            return candidateTypes;
        }

        /// <summary>
        /// Taints the specified symbol.
        /// </summary>
        internal void TaintSymbol(ISymbol taintSymbol, ISymbol symbol)
        {
            this.TaintSymbol(new HashSet<ISymbol> { taintSymbol }, symbol);
        }

        /// <summary>
        /// Taints the specified symbol.
        /// </summary>
        internal void TaintSymbol(IEnumerable<ISymbol> taintSymbols, ISymbol symbol)
        {
            var taintDefinitions = new HashSet<SymbolDefinition>();
            foreach (var taintSymbol in taintSymbols)
            {
                if (taintSymbol.Kind == SymbolKind.Field &&
                    this.IsFreshSymbol(taintSymbol))
                {
                    this.GenerateDefinition(taintSymbol);
                    taintDefinitions.Add(this.GetGeneratedDefinitionOfSymbol(taintSymbol));
                }
                else
                {
                    taintDefinitions.UnionWith(this.GetInputDefinitionsOfSymbol(taintSymbol));
                }
            }

            var resolvedDefinitions = new HashSet<SymbolDefinition>(taintDefinitions);
            foreach (var taintDefinition in taintDefinitions.Where(
                def => this.TaintedDefinitions.ContainsKey(def)))
            {
                resolvedDefinitions.UnionWith(this.TaintedDefinitions[taintDefinition]);
            }

            var definitions = this.GetOutputDefinitionsOfSymbol(symbol);
            foreach (var definition in definitions)
            {
                this.TaintDefinition(resolvedDefinitions, definition);
            }
        }

        /// <summary>
        /// Taints the specified definition.
        /// </summary>
        internal void TaintDefinition(SymbolDefinition taintDefinition, SymbolDefinition definition)
        {
            this.TaintDefinition(new HashSet<SymbolDefinition> { taintDefinition }, definition);
        }

        /// <summary>
        /// Taints the specified definition.
        /// </summary>
        internal void TaintDefinition(IEnumerable<SymbolDefinition> taintDefinitions, SymbolDefinition definition)
        {
            if (!this.TaintedDefinitions.ContainsKey(definition))
            {
                this.TaintedDefinitions.Add(definition, new HashSet<SymbolDefinition>());
            }

            this.TaintedDefinitions[definition].UnionWith(taintDefinitions);
        }

        /// <summary>
        /// Resolves the aliases of the specified symbol in the input definitions of the data-flow node.
        /// </summary>
        internal ISet<SymbolDefinition> ResolveInputAliases(ISymbol symbol)
        {
            var definitions = this.GetInputDefinitionsOfSymbol(symbol);
            return this.ResolveAliases(definitions);
        }

        /// <summary>
        /// Resolves the aliases of the specified symbol in
        /// the output definitions of the data-flow node.
        /// </summary>
        internal ISet<SymbolDefinition> ResolveOutputAliases(ISymbol symbol)
        {
            var definitions = this.GetOutputDefinitionsOfSymbol(symbol);
            return this.ResolveAliases(definitions);
        }

        /// <summary>
        /// Resolves the aliases of the specified set of definitions.
        /// </summary>
        private ISet<SymbolDefinition> ResolveAliases(ISet<SymbolDefinition> definitions)
        {
            var resolvedAliases = new HashSet<SymbolDefinition>(definitions);
            var aliasesToResolve = new List<SymbolDefinition>(definitions);

            while (aliasesToResolve.Count > 0)
            {
                var aliases = aliasesToResolve.SelectMany(
                    val => this.ResolveDirectAliases(val)).
                    Where(val => !resolvedAliases.Contains(val)).ToList();
                resolvedAliases.UnionWith(aliases);
                aliasesToResolve = aliases;
            }

            return resolvedAliases;
        }

        /// <summary>
        /// Resolves the direct aliases of the specified definition.
        /// </summary>
        private ISet<SymbolDefinition> ResolveDirectAliases(SymbolDefinition definition)
        {
            var aliasDefinitions = new HashSet<SymbolDefinition>();
            foreach (var pair in this.TaintedDefinitions)
            {
                if (pair.Key.Equals(definition))
                {
                    aliasDefinitions.UnionWith(pair.Value);
                }

                if (pair.Value.Contains(definition))
                {
                    aliasDefinitions.Add(pair.Key);
                }
            }

            return aliasDefinitions;
        }

        /// <summary>
        /// Returns the generated definition for the specified symbol.
        /// </summary>
        internal SymbolDefinition GetGeneratedDefinitionOfSymbol(ISymbol symbol) =>
            GetDefinitionsOfSymbol(symbol, this.GeneratedDefinitions).FirstOrDefault();

        /// <summary>
        /// Checks if the symbol is fresh.
        /// </summary>
        internal bool IsFreshSymbol(ISymbol symbol)
        {
            var inputDefinitions = this.GetInputDefinitionsOfSymbol(symbol);
            if (inputDefinitions.Count == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the input or generated definitions.
        /// </summary>
        private ISet<SymbolDefinition> GetInputOrGeneratedDefinitions()
        {
            var definitions = this.InputDefinitions.Union(this.GeneratedDefinitions);
            return new HashSet<SymbolDefinition>(definitions);
        }

        /// <summary>
        /// Returns the output definitions, which are the generated definitions,
        /// and the input definitions that have not be killed.
        /// </summary>
        private ISet<SymbolDefinition> GetOutputDefinitions()
        {
            var definitions = this.InputDefinitions.Except(this.KilledDefinitions).
                Union(this.GeneratedDefinitions);
            return new HashSet<SymbolDefinition>(definitions);
        }

        /// <summary>
        /// Returns the killed definitions for the specified symbol.
        /// </summary>
        private ISet<SymbolDefinition> GetKilledDefinitionsOfSymbol(ISymbol symbol) =>
            GetDefinitionsOfSymbol(symbol, this.KilledDefinitions);

        /// <summary>
        /// Returns the input definitions for the specified symbol.
        /// </summary>
        private ISet<SymbolDefinition> GetInputDefinitionsOfSymbol(ISymbol symbol) =>
            GetDefinitionsOfSymbol(symbol, this.InputDefinitions);

        /// <summary>
        /// Returns the input or generated definitions for the specified symbol.
        /// </summary>
        private ISet<SymbolDefinition> GetInputOrGeneratedDefinitionsOfSymbol(ISymbol symbol) =>
            GetDefinitionsOfSymbol(symbol, this.GetInputOrGeneratedDefinitions());

        /// <summary>
        /// Returns the output definitions for the specified symbol.
        /// </summary>
        private ISet<SymbolDefinition> GetOutputDefinitionsOfSymbol(ISymbol symbol) =>
            GetDefinitionsOfSymbol(symbol, this.GetOutputDefinitions());

        /// <summary>
        /// Returns the definitions for the specified symbol.
        /// </summary>
        private static ISet<SymbolDefinition> GetDefinitionsOfSymbol(ISymbol symbol, ISet<SymbolDefinition> definitions)
        {
            var resolvedDefinitions = new HashSet<SymbolDefinition>();
            resolvedDefinitions.UnionWith(definitions.Where(
                val => val.Symbol.Equals(symbol)));
            return resolvedDefinitions;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        internal DataFlowInfo Clone()
        {
            var newInfo = new DataFlowInfo(this.DataFlowNode, this.AnalysisContext);

            newInfo.GeneratedDefinitions.UnionWith(this.GeneratedDefinitions);
            newInfo.KilledDefinitions.UnionWith(this.KilledDefinitions);
            newInfo.InputDefinitions.UnionWith(this.InputDefinitions);
            newInfo.OutputDefinitions.UnionWith(this.OutputDefinitions);

            foreach (var pair in this.TaintedDefinitions)
            {
                newInfo.TaintedDefinitions.Add(pair.Key, new HashSet<SymbolDefinition>(pair.Value));
            }

            return newInfo;
        }
    }
}
