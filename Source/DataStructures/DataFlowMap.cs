//-----------------------------------------------------------------------
// <copyright file="DataFlowMap.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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

namespace PSharp
{
    /// <summary>
    /// Implementation of a map tracking flow of data.
    /// </summary>
    internal class DataFlowMap
    {
        #region fields

        /// <summary>
        /// Map containing data flow values.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ISymbol>>>> Map;

        /// <summary>
        /// Map containing object reachability values.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ISymbol>>>> ReachabilityMap;

        /// <summary>
        /// Map containing object types.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            Dictionary<ISymbol, HashSet<ITypeSymbol>>>> ObjectTypeMap;

        /// <summary>
        /// Map containing resets.
        /// </summary>
        private Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
            HashSet<ISymbol>>> ResetMap;

        #endregion

        #region public API

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal DataFlowMap()
        {
            this.Map = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ISymbol>>>>();
            this.ReachabilityMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ISymbol>>>>();
            this.ObjectTypeMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                Dictionary<ISymbol, HashSet<ITypeSymbol>>>>();
            this.ResetMap = new Dictionary<ControlFlowGraphNode, Dictionary<SyntaxNode,
                HashSet<ISymbol>>>();
        }

        /// <summary>
        /// Transfers the data flow map from the previous node to the new node.
        /// </summary>
        /// <param name="previousSyntaxNode">Previous syntaxNode</param>
        /// <param name="previousCfgNode">Previous cfgNode</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        internal void Transfer(SyntaxNode previousSyntaxNode, ControlFlowGraphNode previousCfgNode,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.Map.ContainsKey(cfgNode))
            {
                this.Map.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.Map[cfgNode].ContainsKey(syntaxNode))
            {
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            Dictionary<ISymbol, HashSet<ISymbol>> previousMap = null;
            if (this.TryGetMapForSyntaxNode(previousSyntaxNode, previousCfgNode, out previousMap))
            {
                foreach (var pair in previousMap)
                {
                    if (!this.Map[cfgNode][syntaxNode].ContainsKey(pair.Key))
                    {
                        this.Map[cfgNode][syntaxNode].Add(pair.Key, new HashSet<ISymbol>());
                    }

                    foreach (var reference in pair.Value)
                    {
                        this.Map[cfgNode][syntaxNode][pair.Key].Add(reference);
                    }
                }
            }

            if (!this.ReachabilityMap.ContainsKey(cfgNode))
            {
                this.ReachabilityMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.ReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.ReachabilityMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            Dictionary<ISymbol, HashSet<ISymbol>> previousReachabilityMap = null;
            if (this.TryGetReachabilityMapForSyntaxNode(previousSyntaxNode, previousCfgNode,
                out previousReachabilityMap))
            {
                foreach (var pair in previousReachabilityMap)
                {
                    if (!this.ReachabilityMap[cfgNode][syntaxNode].ContainsKey(pair.Key))
                    {
                        this.ReachabilityMap[cfgNode][syntaxNode].Add(pair.Key, new HashSet<ISymbol>());
                    }

                    foreach (var reference in pair.Value)
                    {
                        this.ReachabilityMap[cfgNode][syntaxNode][pair.Key].Add(reference);
                    }
                }
            }

            if (!this.ObjectTypeMap.ContainsKey(cfgNode))
            {
                this.ObjectTypeMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ITypeSymbol>>>());
                this.ObjectTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ITypeSymbol>>());
            }
            else if (!this.ObjectTypeMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ObjectTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ITypeSymbol>>());
            }

            Dictionary<ISymbol, HashSet<ITypeSymbol>> previousObjectTypeMap = null;
            if (this.TryGetObjectTypeMapForSyntaxNode(previousSyntaxNode, previousCfgNode,
                out previousObjectTypeMap))
            {
                foreach (var pair in previousObjectTypeMap)
                {
                    if (!this.ObjectTypeMap[cfgNode][syntaxNode].ContainsKey(pair.Key))
                    {
                        this.ObjectTypeMap[cfgNode][syntaxNode].Add(pair.Key, new HashSet<ITypeSymbol>());
                    }

                    foreach (var type in pair.Value)
                    {
                        this.ObjectTypeMap[cfgNode][syntaxNode][pair.Key].Add(type);
                    }
                }
            }
        }

        /// <summary>
        /// Resets the references of the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        internal void ResetSymbol(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.Map.ContainsKey(cfgNode))
            {
                this.Map.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.Map[cfgNode].ContainsKey(syntaxNode))
            {
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            if (this.Map[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.Map[cfgNode][syntaxNode][symbol] = new HashSet<ISymbol> { symbol };
            }
            else
            {
                this.Map[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol> { symbol });
            }

            this.MarkSymbolReassignment(symbol, syntaxNode, cfgNode);
        }

        /// <summary>
        /// Maps the given reference to the given symbol.
        /// </summary>
        /// <param name="reference">Reference</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        /// <param name="markReset">Should mark symbol as reset</param>
        internal void MapRefToSymbol(ISymbol reference, ISymbol symbol, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, bool markReset = true)
        {
            if (!this.Map.ContainsKey(cfgNode))
            {
                this.Map.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.Map[cfgNode].ContainsKey(syntaxNode))
            {
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            HashSet<ISymbol> additionalRefs = new HashSet<ISymbol>();
            if (this.Map[cfgNode][syntaxNode].ContainsKey(reference))
            {
                foreach (var r in this.Map[cfgNode][syntaxNode][reference])
                {
                    if (!reference.Equals(r))
                    {
                        additionalRefs.Add(r);
                    }
                }
            }

            if (this.Map[cfgNode][syntaxNode].ContainsKey(symbol) && additionalRefs.Count > 0)
            {
                this.Map[cfgNode][syntaxNode][symbol] = new HashSet<ISymbol>();
            }
            else if (additionalRefs.Count > 0)
            {
                this.Map[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
            }
            else if (this.Map[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.Map[cfgNode][syntaxNode][symbol] = new HashSet<ISymbol> { reference };
            }
            else
            {
                this.Map[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol> { reference });
            }

            foreach (var r in additionalRefs)
            {
                this.Map[cfgNode][syntaxNode][symbol].Add(r);
            }

            if (markReset)
            {
                this.MarkSymbolReassignment(symbol, syntaxNode, cfgNode);
            }
        }

        /// <summary>
        /// Maps the given symbol if its not already mapped.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        internal void MapSymbolIfNotExists(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.Map.ContainsKey(cfgNode))
            {
                this.Map.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.Map[cfgNode].ContainsKey(syntaxNode))
            {
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            if (!this.Map[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.Map[cfgNode][syntaxNode][symbol] = new HashSet<ISymbol> { symbol };
            }
        }

        /// <summary>
        /// Maps the given set of references to the given symbol.
        /// </summary>
        /// <param name="references">Set of references</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        /// <param name="markReset">Should mark symbol as reset</param>
        internal void MapRefsToSymbol(HashSet<ISymbol> references, ISymbol symbol, SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, bool markReset = true)
        {
            if (!this.Map.ContainsKey(cfgNode))
            {
                this.Map.Add(cfgNode, new Dictionary<SyntaxNode, Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }
            else if (!this.Map[cfgNode].ContainsKey(syntaxNode))
            {
                this.Map[cfgNode].Add(syntaxNode, new Dictionary<ISymbol, HashSet<ISymbol>>());
            }

            HashSet<ISymbol> additionalRefs = new HashSet<ISymbol>();
            foreach (var reference in references)
            {
                if (this.Map[cfgNode][syntaxNode].ContainsKey(reference))
                {
                    foreach (var r in this.Map[cfgNode][syntaxNode][reference])
                    {
                        if (!reference.Equals(r))
                        {
                            additionalRefs.Add(r);
                        }
                    }
                }
            }

            if (this.Map[cfgNode][syntaxNode].ContainsKey(symbol) && additionalRefs.Count > 0)
            {
                this.Map[cfgNode][syntaxNode][symbol].Clear();
            }
            else if (additionalRefs.Count > 0)
            {
                this.Map[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
            }
            else if (this.Map[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.Map[cfgNode][syntaxNode][symbol].Clear();
                foreach (var reference in references)
                {
                    this.Map[cfgNode][syntaxNode][symbol].Add(reference);
                }
            }
            else
            {
                this.Map[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
                foreach (var reference in references)
                {
                    this.Map[cfgNode][syntaxNode][symbol].Add(reference);
                }
            }

            foreach (var r in additionalRefs)
            {
                this.Map[cfgNode][syntaxNode][symbol].Add(r);
            }

            if (markReset)
            {
                this.MarkSymbolReassignment(symbol, syntaxNode, cfgNode);
            }
        }

        /// <summary>
        /// Maps the given set of reachable field symbols to the given symbol.
        /// </summary>
        /// <param name="fields">Set of field symbols</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        internal void MapReachableFieldsToSymbol(HashSet<ISymbol> fields, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.ReachabilityMap.ContainsKey(cfgNode))
            {
                this.ReachabilityMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ISymbol>>>());
                this.ReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ISymbol>>());
            }
            else if (!this.ReachabilityMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ReachabilityMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ISymbol>>());
            }

            if (this.ReachabilityMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ReachabilityMap[cfgNode][syntaxNode][symbol].Clear();
                foreach (var reference in fields)
                {
                    this.ReachabilityMap[cfgNode][syntaxNode][symbol].Add(reference);
                }
            }
            else
            {
                this.ReachabilityMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ISymbol>());
                foreach (var reference in fields)
                {
                    this.ReachabilityMap[cfgNode][syntaxNode][symbol].Add(reference);
                }
            }
        }

        /// <summary>
        /// Maps the given set of object types to the given symbol.
        /// </summary>
        /// <param name="types">Set of object types</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        internal void MapObjectTypesToSymbol(HashSet<ITypeSymbol> types, ISymbol symbol,
            SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.ObjectTypeMap.ContainsKey(cfgNode))
            {
                this.ObjectTypeMap.Add(cfgNode, new Dictionary<SyntaxNode,
                    Dictionary<ISymbol, HashSet<ITypeSymbol>>>());
                this.ObjectTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ITypeSymbol>>());
            }
            else if (!this.ObjectTypeMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ObjectTypeMap[cfgNode].Add(syntaxNode, new Dictionary<ISymbol,
                    HashSet<ITypeSymbol>>());
            }

            if (this.ObjectTypeMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ObjectTypeMap[cfgNode][syntaxNode][symbol].Clear();
                foreach (var type in types)
                {
                    this.ObjectTypeMap[cfgNode][syntaxNode][symbol].Add(type);
                }
            }
            else
            {
                this.ObjectTypeMap[cfgNode][syntaxNode].Add(symbol, new HashSet<ITypeSymbol>());
                foreach (var type in types)
                {
                    this.ObjectTypeMap[cfgNode][syntaxNode][symbol].Add(type);
                }
            }
        }

        /// <summary>
        /// Erases the set of object types from the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        internal void EraseObjectTypesFromSymbol(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (this.ObjectTypeMap.ContainsKey(cfgNode) &&
                this.ObjectTypeMap[cfgNode].ContainsKey(syntaxNode) &&
                this.ObjectTypeMap[cfgNode][syntaxNode].ContainsKey(symbol))
            {
                this.ObjectTypeMap[cfgNode][syntaxNode].Remove(symbol);
            }
        }

        /// <summary>
        /// Returns true if the given symbol resets until it reaches the
        /// target control flow graph node.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="track">Tracking</param>
        /// <returns>Boolean value</returns>
        internal bool DoesSymbolReset(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode, bool track = false)
        {
            return this.DoesSymbolReset(symbol, syntaxNode, cfgNode, targetSyntaxNode,
                targetCfgNode, new HashSet<ControlFlowGraphNode>(), track);
        }

        /// <summary>
        /// Returns true if there is a map for the given syntax node.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <returns>Boolean value</returns>
        internal bool ExistsMapForSyntaxNode(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (this.Map.ContainsKey(cfgNode))
            {
                if (this.Map[cfgNode].ContainsKey(syntaxNode))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to return the map for the given syntax node. Returns false and
        /// a null map, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">Map</param>
        /// <returns>Boolean value</returns>
        internal bool TryGetMapForSyntaxNode(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            out Dictionary<ISymbol, HashSet<ISymbol>> map)
        {
            if (cfgNode == null || syntaxNode == null)
            {
                map = null;
                return false;
            }

            if (this.Map.ContainsKey(cfgNode))
            {
                if (this.Map[cfgNode].ContainsKey(syntaxNode))
                {
                    map = this.Map[cfgNode][syntaxNode];
                    return true;
                }
                else
                {
                    map = null;
                    return false;
                }
            }
            else
            {
                map = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to return the reachability map for the given syntax node. Returns false
        /// and a null map, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">Map</param>
        /// <returns>Boolean value</returns>
        internal bool TryGetReachabilityMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, out Dictionary<ISymbol, HashSet<ISymbol>> map)
        {
            if (cfgNode == null || syntaxNode == null)
            {
                map = null;
                return false;
            }

            if (this.ReachabilityMap.ContainsKey(cfgNode))
            {
                if (this.ReachabilityMap[cfgNode].ContainsKey(syntaxNode))
                {
                    map = this.ReachabilityMap[cfgNode][syntaxNode];
                    return true;
                }
                else
                {
                    map = null;
                    return false;
                }
            }
            else
            {
                map = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to return the object type map for the given syntax node. Returns false
        /// and a null map, if it cannot find such map.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="map">Map</param>
        /// <returns>Boolean value</returns>
        internal bool TryGetObjectTypeMapForSyntaxNode(SyntaxNode syntaxNode,
            ControlFlowGraphNode cfgNode, out Dictionary<ISymbol, HashSet<ITypeSymbol>> map)
        {
            if (cfgNode == null || syntaxNode == null)
            {
                map = null;
                return false;
            }

            if (this.ObjectTypeMap.ContainsKey(cfgNode))
            {
                if (this.ObjectTypeMap[cfgNode].ContainsKey(syntaxNode))
                {
                    map = this.ObjectTypeMap[cfgNode][syntaxNode];
                    return true;
                }
                else
                {
                    map = null;
                    return false;
                }
            }
            else
            {
                map = null;
                return false;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Marks that the symbol has been reassigned a reference.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">CfgNode</param>
        private void MarkSymbolReassignment(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            if (!this.ResetMap.ContainsKey(cfgNode))
            {
                this.ResetMap.Add(cfgNode, new Dictionary<SyntaxNode, HashSet<ISymbol>>());
                this.ResetMap[cfgNode].Add(syntaxNode, new HashSet<ISymbol>());
            }
            else if (!this.ResetMap[cfgNode].ContainsKey(syntaxNode))
            {
                this.ResetMap[cfgNode].Add(syntaxNode, new HashSet<ISymbol>());
            }

            this.ResetMap[cfgNode][syntaxNode].Add(symbol);
        }

        /// <summary>
        /// Returns true if the given symbol resets until it reaches the
        /// target control flow graph node.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="targetSyntaxNode">Target syntaxNode</param>
        /// <param name="targetCfgNode">Target controlFlowGraphNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="track">Tracking</param>
        /// <returns>Boolean value</returns>
        internal bool DoesSymbolReset(ISymbol symbol, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            SyntaxNode targetSyntaxNode, ControlFlowGraphNode targetCfgNode, HashSet<ControlFlowGraphNode> visited,
            bool track)
        {
            visited.Add(cfgNode);

            bool result = false;
            if (syntaxNode.Equals(targetSyntaxNode) && cfgNode.Equals(targetCfgNode) &&
                !track)
            {
                return result;
            }

            foreach (var node in cfgNode.SyntaxNodes)
            {
                if (track && this.ResetMap.ContainsKey(cfgNode) &&
                    this.ResetMap[cfgNode].ContainsKey(node) &&
                    this.ResetMap[cfgNode][node].Contains(symbol))
                {
                    result = true;
                    break;
                }

                if (!track && node.Equals(syntaxNode))
                {
                    track = true;
                }
            }

            if (!result)
            {
                foreach (var successor in cfgNode.ISuccessors.Where(v => !visited.Contains(v)))
                {
                    if ((successor.Equals(targetCfgNode) ||
                        successor.IsPredecessorOf(targetCfgNode)) &&
                        this.DoesSymbolReset(symbol, successor.SyntaxNodes.First(),
                        successor, targetSyntaxNode, targetCfgNode, visited, true))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        #endregion

        #region debug methods

        /// <summary>
        /// Prints the data flow map.
        /// </summary>
        internal void Print()
        {
            Console.WriteLine("\nPrinting data flow map:");
            foreach (var cfgNode in this.Map)
            {
                Console.WriteLine("  > cfgNode: " + cfgNode.Key.Id);
                foreach (var syntaxNode in cfgNode.Value)
                {
                    Console.WriteLine("    > syntaxNode: " + syntaxNode.Key);
                    foreach (var pair in syntaxNode.Value)
                    {
                        foreach (var symbol in pair.Value)
                        {
                            Console.WriteLine("        " + pair.Key.Name + " ::= " + symbol.Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the reachability map.
        /// </summary>
        internal void PrintReachabilityMap()
        {
            Console.WriteLine("\nPrinting reachability map:");
            foreach (var cfgNode in this.ReachabilityMap)
            {
                Console.WriteLine("  > cfgNode: " + cfgNode.Key.Id);
                foreach (var syntaxNode in cfgNode.Value)
                {
                    Console.WriteLine("    > syntaxNode: " + syntaxNode.Key);
                    foreach (var pair in syntaxNode.Value)
                    {
                        foreach (var symbol in pair.Value)
                        {
                            Console.WriteLine("        " + pair.Key.Name + " ::= " + symbol.Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the object type map.
        /// </summary>
        internal void PrintObjectTypeMap()
        {
            Console.WriteLine("\nPrinting object type map:");
            foreach (var cfgNode in this.ObjectTypeMap)
            {
                Console.WriteLine("  > cfgNode: " + cfgNode.Key.Id);
                foreach (var syntaxNode in cfgNode.Value)
                {
                    Console.WriteLine("    > syntaxNode: " + syntaxNode.Key);
                    foreach (var pair in syntaxNode.Value)
                    {
                        foreach (var symbol in pair.Value)
                        {
                            Console.WriteLine("        " + pair.Key.Name + " ::= " + symbol.Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the data flow map of the given syntax node.
        /// </summary>
        internal void Print(ControlFlowGraphNode cfgNode)
        {
            Console.WriteLine("\nPrinting data flow map of cfgNode {0}: ", cfgNode.Id);
            if (this.Map.ContainsKey(cfgNode))
            {
                foreach (var syntaxNode in this.Map[cfgNode])
                {
                    Console.WriteLine("    > syntaxNode: " + syntaxNode.Key);
                    foreach (var pair in syntaxNode.Value)
                    {
                        foreach (var symbol in pair.Value)
                        {
                            Console.WriteLine("        " + pair.Key.Name + " ::= " + symbol.Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prints the resets.
        /// </summary>
        internal void PrintResets()
        {
            Console.WriteLine("\nPrinting resets:");
            foreach (var cfgNode in this.ResetMap)
            {
                Console.WriteLine("  > cfgNode: " + cfgNode.Key.Id);
                foreach (var syntaxNode in cfgNode.Value)
                {
                    Console.WriteLine("    > syntaxNode: " + syntaxNode.Key);
                    foreach (var symbol in syntaxNode.Value)
                    {
                        Console.WriteLine("      " + symbol.Name);
                    }
                }
            }
        }

        #endregion
    }
}
