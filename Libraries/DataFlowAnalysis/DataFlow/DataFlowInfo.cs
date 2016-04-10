//-----------------------------------------------------------------------
// <copyright file="DataFlowInfo.cs">
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

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Data-flow information.
    /// </summary>
    public class DataFlowInfo
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

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

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dfgNode">DataFlowNode</param>
        /// <param name="context">AnalysisContext</param>
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

        #endregion

        #region internal methods

        /// <summary>
        /// Generates a new definition for the specified symbol.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <param name="type">ITypeSymbol</param>
        internal void GenerateDefinition(ISymbol symbol, ITypeSymbol type)
        {
            var definition = this.GeneratedDefinitions.FirstOrDefault(def => def.Symbol.Equals(symbol));
            if (definition == null)
            {
                definition = new SymbolDefinition(symbol, type, this.DataFlowNode);
                this.GeneratedDefinitions.Add(definition);

                if (!this.AnalysisContext.IsTypePassedByValueOrImmutable(type))
                {
                    this.TaintDefinition(definition, definition);
                }
            }
        }

        /// <summary>
        /// Kills the definitions of the specified symbol.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        internal void KillDefinitions(ISymbol symbol)
        {
            var definitions = this.GetInputDefinitionsOfSymbol(symbol);
            this.KilledDefinitions.UnionWith(definitions);
        }

        /// <summary>
        /// Assigns the specified inputs definitions.
        /// </summary>
        /// <param name="definitions">SymbolDefinitions</param>
        internal void AssignInputDefinitions(ISet<SymbolDefinition> definitions)
        {
            this.InputDefinitions.UnionWith(definitions);
        }

        /// <summary>
        /// Assigns the output definitions.
        /// </summary>
        internal void AssignOutputDefinitions()
        {
            this.OutputDefinitions.UnionWith(this.GetAliveInputDefinitions());
        }

        /// <summary>
        /// Taints the specified symbol.
        /// </summary>
        /// <param name="taintSymbol">ISymbol</param>
        /// <param name="taintType">ITypeSymbol</param>
        /// <param name="symbol">ISymbol</param>
        internal void TaintSymbol(ISymbol taintSymbol, ISymbol symbol)
        {
            this.TaintSymbol(new HashSet<ISymbol> { taintSymbol }, symbol);
        }

        /// <summary>
        /// Taints the specified symbol.
        /// </summary>
        /// <param name="taintSymbols">ISymbols</param>
        /// <param name="symbol">ISymbol</param>
        internal void TaintSymbol(IEnumerable<ISymbol> taintSymbols, ISymbol symbol)
        {
            var taintDefinitions = new HashSet<SymbolDefinition>();
            foreach (var taintSymbol in taintSymbols)
            {
                if (taintSymbol.Kind == SymbolKind.Field &&
                    this.IsFreshSymbol(taintSymbol))
                {
                    var fieldSymbol = taintSymbol as IFieldSymbol;
                    this.GenerateDefinition(fieldSymbol, fieldSymbol.Type);
                    taintDefinitions.Add(this.GetGeneratedDefinitionOfSymbol(fieldSymbol));
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
        /// <param name="taintDefinition">SymbolDefinition</param>
        /// <param name="definition">SymbolDefinition</param>
        internal void TaintDefinition(SymbolDefinition taintDefinition,
            SymbolDefinition definition)
        {
            this.TaintDefinition(new HashSet<SymbolDefinition> { taintDefinition }, definition);
        }

        /// <summary>
        /// Taints the specified definition.
        /// </summary>
        /// <param name="taintDefinitions">SymbolDefinitions</param>
        /// <param name="definition">SymbolDefinition</param>
        internal void TaintDefinition(IEnumerable<SymbolDefinition> taintDefinitions,
            SymbolDefinition definition)
        {
            if (!this.TaintedDefinitions.ContainsKey(definition))
            {
                this.TaintedDefinitions.Add(definition, new HashSet<SymbolDefinition>());
            }

            this.TaintedDefinitions[definition].UnionWith(taintDefinitions);
        }

        /// <summary>
        /// Checks if the symbol is fresh.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <returns>Boolean</returns>
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
        /// Adds the specified type to the symbol.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <param name="type">ITypeSymbol</param>
        internal void AddTypeToSymbol(ISymbol symbol, ITypeSymbol type)
        {
            var generatedDefinition = this.GetGeneratedDefinitionOfSymbol(symbol);
            if (generatedDefinition != null && type != null)
            {
                generatedDefinition.Types.Add(type);
            }
        }

        /// <summary>
        /// Resets the type of the symbol with the specified type.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <param name="type">ITypeSymbol</param>
        internal void ResetTypeOfSymbol(ISymbol symbol, ITypeSymbol type)
        {
            var generatedDefinition = this.GetGeneratedDefinitionOfSymbol(symbol);
            if (generatedDefinition != null && type != null)
            {
                generatedDefinition.Types.Clear();
                generatedDefinition.Types.Add(type);
            }
        }

        /// <summary>
        /// Creates a new object that is a copy of
        /// the current instance.
        /// </summary>
        /// <returns>DataFlowInfo</returns>
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

        #endregion

        #region definition resolving methods

        /// <summary>
        /// Resolves the aliases of the specified symbol in
        /// the input definitions of the data-flow node.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <returns>SymbolDefinitions</returns>
        internal ISet<SymbolDefinition> ResolveInputAliases(ISymbol symbol)
        {
            var definitions = this.GetInputDefinitionsOfSymbol(symbol);
            return this.ResolveAliases(definitions);
        }

        /// <summary>
        /// Resolves the aliases of the specified symbol in
        /// the output definitions of the data-flow node.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <returns>SymbolDefinitions</returns>
        internal ISet<SymbolDefinition> ResolveOutputAliases(ISymbol symbol)
        {
            var definitions = this.GetOutputDefinitionsOfSymbol(symbol);
            return this.ResolveAliases(definitions);
        }

        /// <summary>
        /// Resolves the aliases of the specified set of definitions.
        /// </summary>
        /// <param name="definitions">SymbolDefinitions</param>
        /// <returns>SymbolDefinitions</returns>
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
        /// <param name="definition">SymbolDefinition</param>
        /// <returns>SymbolDefinitions</returns>
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

        #endregion

        #region helper methods

        /// <summary>
        /// Returns the generated definition for the specified symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>SymbolDefinition</returns>
        internal SymbolDefinition GetGeneratedDefinitionOfSymbol(ISymbol symbol)
        {
            return this.GetDefinitionsOfSymbol(symbol, this.GeneratedDefinitions).FirstOrDefault();
        }

        /// <summary>
        /// Returns the killed definitions for the specified symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="statement">Statement</param>
        /// <returns>SymbolDefinitions</returns>
        private ISet<SymbolDefinition> GetKilledDefinitionsOfSymbol(ISymbol symbol)
        {
            return this.GetDefinitionsOfSymbol(symbol, this.KilledDefinitions);
        }

        /// <summary>
        /// Returns the input definitions for the specified symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>SymbolDefinitions</returns>
        private ISet<SymbolDefinition> GetInputDefinitionsOfSymbol(ISymbol symbol)
        {
            return this.GetDefinitionsOfSymbol(symbol, this.InputDefinitions);
        }

        /// <summary>
        /// Returns the output definitions for the specified symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>SymbolDefinitions</returns>
        private ISet<SymbolDefinition> GetOutputDefinitionsOfSymbol(ISymbol symbol)
        {
            return this.GetDefinitionsOfSymbol(symbol, this.GetAliveInputDefinitions());
        }

        /// <summary>
        /// Returns the input definitions that have not be killed.
        /// </summary>
        /// <returns>SymbolDefinitions</returns>
        private ISet<SymbolDefinition> GetAliveInputDefinitions()
        {
            var definitions = this.InputDefinitions.Union(this.GeneratedDefinitions).
                Except(this.KilledDefinitions);
            return new HashSet<SymbolDefinition>(definitions);
        }

        /// <summary>
        /// Returns the definitions for the specified symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="definitions">SymbolDefinitions</param>
        /// <returns>SymbolDefinitions</returns>
        private ISet<SymbolDefinition> GetDefinitionsOfSymbol(ISymbol symbol,
            ISet<SymbolDefinition> definitions)
        {
            var resolvedDefinitions = new HashSet<SymbolDefinition>();
            resolvedDefinitions.UnionWith(definitions.Where(
                val => val.Symbol.Equals(symbol)));
            return resolvedDefinitions;
        }

        #endregion
    }
}
