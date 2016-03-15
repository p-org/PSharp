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
    /// Class implementing a method summary. It contains information
    /// such as: the control flow graph; the gives up set; the access
    /// set; and any side-effects.
    /// </summary>
    internal class MethodSummary
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

        /// <summary>
        /// Method that this summary represents.
        /// </summary>
        internal BaseMethodDeclarationSyntax Method;

        /// <summary>
        /// Machine that the method of this summary belongs to.
        /// If the method does not belong to a machine, the
        /// object is null.
        /// </summary>
        internal ClassDeclarationSyntax Machine;

        /// <summary>
        /// The entry node of the control flow graph of the
        /// method of this summary.
        /// </summary>
        internal ControlFlowGraphNode Node;

        /// <summary>
        /// Set of all gives up nodes in the control flow graph of
        /// the method of this summary.
        /// </summary>
        internal HashSet<ControlFlowGraphNode> GivesUpNodes;

        /// <summary>
        /// Set of all exit nodes in the control flow graph of the
        /// method of this summary.
        /// </summary>
        internal HashSet<ControlFlowGraphNode> ExitNodes;

        /// <summary>
        /// Map containing all data flows in the control flow graph of
        /// the method of this summary.
        /// </summary>
        internal DataFlowMap DataFlowMap;

        /// <summary>
        /// Set of the indexes of parameters that the original method
        /// gives up during its execution.
        /// </summary>
        internal HashSet<int> GivesUpSet;

        /// <summary>
        /// Dictionary containing all read and write accesses in regards
        /// to the parameters of the original method.
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

        #region public API

        internal static class Factory
        {
            /// <summary>
            /// Returns the summary of the given method.
            /// </summary>
            /// <param name="context">AnalysisContext</param>
            /// <param name="method">Method</param>
            /// <returns>MethodSummary</returns>
            internal static MethodSummary Summarize(AnalysisContext context, BaseMethodDeclarationSyntax method)
            {
                if (context.Summaries.ContainsKey(method))
                {
                    return context.Summaries[method];
                }

                return new MethodSummary(context, method);
            }

            /// <summary>
            /// Returns the summary of the given method.
            /// </summary>
            /// <param name="context">AnalysisContext</param>
            /// <param name="method">Method</param>
            /// <param name="machine">Machine</param>
            /// <returns>MethodSummary</returns>
            internal static MethodSummary Summarize(AnalysisContext context, BaseMethodDeclarationSyntax method,
                ClassDeclarationSyntax machine)
            {
                if (context.Summaries.ContainsKey(method))
                {
                    return context.Summaries[method];
                }

                return new MethodSummary(context, method, machine);
            }
        }

        /// <summary>
        /// Tries to get the method summary of the given object creation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>MethodSummary</returns>
        internal static MethodSummary TryGetSummary(ObjectCreationExpressionSyntax call, SemanticModel model,
            AnalysisContext context)
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
            return MethodSummary.Factory.Summarize(context, constructorCall);
        }

        /// <summary>
        /// Tries to get the method summary of the given invocation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>MethodSummary</returns>
        internal static MethodSummary TryGetSummary(InvocationExpressionSyntax call, SemanticModel model,
            AnalysisContext context)
        {
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                return null;
            }

            if (callSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine") ||
                callSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.MachineState"))
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
            return MethodSummary.Factory.Summarize(context, invocationCall);
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
                        if (this.AnalysisContext.IsTypeAllowedToBeSend(argType) ||
                            this.AnalysisContext.IsMachineType(argType, model))
                        {
                            continue;
                        }

                        argSymbols.Add(model.GetSymbolInfo(arg).Symbol);
                    }
                    else if (argExpr is MemberAccessExpressionSyntax)
                    {
                        var name = (argExpr as MemberAccessExpressionSyntax).Name;
                        var argType = model.GetTypeInfo(name).Type;
                        if (this.AnalysisContext.IsTypeAllowedToBeSend(argType) ||
                            this.AnalysisContext.IsMachineType(argType, model))
                        {
                            continue;
                        }

                        arg = this.AnalysisContext.GetFirstNonMachineIdentifier(argExpr, model);
                        argSymbols.Add(model.GetSymbolInfo(arg).Symbol);
                    }
                    else if (argExpr is ObjectCreationExpressionSyntax)
                    {
                        var objCreation = argExpr as ObjectCreationExpressionSyntax;
                        var summary = MethodSummary.TryGetSummary(objCreation, model, this.AnalysisContext);
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
                        var summary = MethodSummary.TryGetSummary(invocation, model, this.AnalysisContext);
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
                    if (this.AnalysisContext.IsTypeAllowedToBeSend(argType) ||
                        this.AnalysisContext.IsMachineType(argType, model))
                    {
                        continue;
                    }
                }
                else if (argExpr is MemberAccessExpressionSyntax)
                {
                    var name = (argExpr as MemberAccessExpressionSyntax).Name;
                    var argType = model.GetTypeInfo(name).Type;
                    if (this.AnalysisContext.IsTypeAllowedToBeSend(argType) ||
                        this.AnalysisContext.IsMachineType(argType, model))
                    {
                        continue;
                    }

                    arg = this.AnalysisContext.GetFirstNonMachineIdentifier(argExpr, model);
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

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">Method</param>
        private MethodSummary(AnalysisContext context, BaseMethodDeclarationSyntax method)
        {
            this.AnalysisContext = context;
            this.Method = method;
            this.Machine = null;
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        private MethodSummary(AnalysisContext context, BaseMethodDeclarationSyntax method,
            ClassDeclarationSyntax machine)
        {
            this.AnalysisContext = context;
            this.Method = method;
            this.Machine = machine;
            this.Initialize();
        }

        /// <summary>
        /// Initializes the summary.
        /// </summary>
        private void Initialize()
        {
            this.Node = new ControlFlowGraphNode(this.AnalysisContext, this);
            this.GivesUpNodes = new HashSet<ControlFlowGraphNode>();
            this.ExitNodes = new HashSet<ControlFlowGraphNode>();
            this.GivesUpSet = new HashSet<int>();
            this.AccessSet = new Dictionary<int, HashSet<SyntaxNode>>();
            this.FieldAccessSet = new Dictionary<IFieldSymbol, HashSet<SyntaxNode>>();
            this.SideEffects = new Dictionary<IFieldSymbol, HashSet<int>>();
            this.ReturnSet = new Tuple<HashSet<int>, HashSet<IFieldSymbol>>(
                new HashSet<int>(), new HashSet<IFieldSymbol>());
            this.ReturnTypeSet = new HashSet<ITypeSymbol>();

            if (!this.TryConstruct())
            {
                return;
            }
            
            this.DataFlowMap = DataFlowAnalysis.AnalyseControlFlowGraph(this, this.AnalysisContext);
            this.ComputeAnySideEffects();
            AnalysisContext.Summaries.Add(this.Method, this);

            //this.DataFlowMap.Print();
            //this.DataFlowMap.PrintReachabilityMap();
            //this.DataFlowMap.PrintObjectTypeMap();
            //this.PrintAccesses();
            //this.PrintFieldAccesses();
            //this.PrintSideEffects();
            //this.PrintReturnSet();
            //this.PrintReturnTypeSet();
            //this.DataFlowMap.PrintResets();
        }

        /// <summary>
        /// Tries to construct the control flow graph for the given method.
        /// Returns false if it cannot construct the cfg.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool TryConstruct()
        {
            if (this.Method.Modifiers.Any(SyntaxKind.AbstractKeyword))
            {
                return false;
            }

            SemanticModel model = null;

            try
            {
                model = AnalysisContext.Compilation.GetSemanticModel(this.Method.SyntaxTree);
            }
            catch
            {
                return false;
            }

            //IO.Print("Printing method: {0}", this.Method);
            this.Node.Construct(this.Method.Body.Statements, 0, false, null);
            this.Node.CleanEmptySuccessors();
            this.ExitNodes = this.Node.GetExitNodes();
            //this.DebugPrint();

            return true;
        }

        /// <summary>
        /// Tries to compute any side effects in the control flow graph using
        /// information from the data flow analysis.
        /// </summary>
        private void ComputeAnySideEffects()
        {
            foreach (var exitNode in this.ExitNodes)
            {
                if (exitNode.SyntaxNodes.Count == 0)
                {
                    continue;
                }

                var exitSyntaxNode = exitNode.SyntaxNodes.Last();
                Dictionary<ISymbol, HashSet<ISymbol>> exitMap = null;
                if (this.DataFlowMap.TryGetMapForSyntaxNode(exitSyntaxNode, exitNode, out exitMap))
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

        #region debug methods

        /// <summary>
        /// Prints the accesses.
        /// </summary>
        internal void PrintAccesses()
        {
            IO.PrintLine("\nPrinting access set:");
            foreach (var index in this.AccessSet)
            {
                foreach (var syntaxNode in index.Value)
                {
                    IO.PrintLine("  > access: " + index.Key + " " + syntaxNode);
                }
            }
        }

        /// <summary>
        /// Prints the field accesses.
        /// </summary>
        internal void PrintFieldAccesses()
        {
            IO.PrintLine("\nPrinting field access set:");
            foreach (var field in this.FieldAccessSet)
            {
                foreach (var syntaxNode in field.Value)
                {
                    IO.PrintLine("  > access: " + field.Key.Name + " " + syntaxNode);
                }
            }
        }

        /// <summary>
        /// Prints the accesses.
        /// </summary>
        internal void PrintSideEffects()
        {
            IO.PrintLine("\nPrinting side effects:");
            foreach (var pair in this.SideEffects)
            {
                foreach (var index in pair.Value)
                {
                    IO.PrintLine("  " + pair.Key.Name + " " + index);
                }
            }
        }

        /// <summary>
        /// Prints the return set.
        /// </summary>
        internal void PrintReturnSet()
        {
            IO.PrintLine("\nPrinting return set:");
            foreach (var index in this.ReturnSet.Item1)
            {
                IO.PrintLine("  > return: " + index);
            }

            foreach (var field in this.ReturnSet.Item2)
            {
                IO.PrintLine("  > return: " + field.Name);
            }
        }

        /// <summary>
        /// Prints the return type set.
        /// </summary>
        internal void PrintReturnTypeSet()
        {
            IO.PrintLine("\nPrinting return type set:");
            foreach (var type in this.ReturnTypeSet)
            {
                IO.PrintLine("  " + type.Name);
            }
        }

        /// <summary>
        /// Print debug information.
        /// </summary>
        private void DebugPrint()
        {
            IO.PrintLine("DebugPrint");
            this.Node.DebugPrint();
            //this.Node.DebugPrintPredecessors();
            this.Node.DebugPrintSuccessors();
        }

        #endregion
    }
}
