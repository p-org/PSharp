//-----------------------------------------------------------------------
// <copyright file="StateTransitionAnalysis.cs">
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

using Microsoft.PSharp.Tooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.PSharp.StaticAnalysis
{
    public static class StateTransitionAnalysis
    {
        #region fields

        /// <summary>
        /// Number of machines in the program.
        /// </summary>
        internal static int NumOfMachines = 0;

        /// <summary>
        /// Number of transitions in the program.
        /// </summary>
        internal static int NumOfTransitions = 0;

        /// <summary>
        /// Number of action bindings in the program.
        /// </summary>
        internal static int NumOfActionBindings = 0;

        #endregion

        #region public API

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        public static void Run()
        {
            foreach (var machine in AnalysisContext.Machines)
            {
                StateTransitionAnalysis.ConstructGraphForMachine(machine);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Tries to construct the state transition graph for the given machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        private static void ConstructGraphForMachine(ClassDeclarationSyntax machine)
        {
            var model = AnalysisContext.Compilation.GetSemanticModel(machine.SyntaxTree);

            Dictionary<ClassDeclarationSyntax, HashSet<ClassDeclarationSyntax>> stateTransitions = null;
            StateTransitionAnalysis.TryParseStateTransitions(out stateTransitions, machine, model);

            Dictionary<ClassDeclarationSyntax, HashSet<MethodDeclarationSyntax>> actionBindings = null;
            StateTransitionAnalysis.TryParseActionBindings(out actionBindings, machine, model);

            StateTransitionAnalysis.ComputeStatistics(stateTransitions, actionBindings);

            ClassDeclarationSyntax initState = null;
            foreach (var state in stateTransitions)
            {
                foreach (var attributeList in state.Key.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (attribute.Name.ToString().Equals("Initial"))
                        {
                            initState = state.Key;
                        }
                    }
                }
            }

            if (initState == null)
            {
                return;
            }

            var initNode = new StateTransitionGraphNode(initState, machine);
            initNode.IsInitialNode = true;
            initNode.Construct(stateTransitions, actionBindings);

            AnalysisContext.StateTransitionGraphs.Add(machine, initNode);
        }

        /// <summary>
        /// Tries to parse and return the state transitions for the given machine.
        /// </summary>
        /// <param name="stateTransitions">State transitions</param>
        /// <param name="machine">Machine</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        private static bool TryParseStateTransitions(out Dictionary<ClassDeclarationSyntax,
            HashSet<ClassDeclarationSyntax>> stateTransitions, ClassDeclarationSyntax machine,
            SemanticModel model)
        {
            stateTransitions = new Dictionary<ClassDeclarationSyntax, HashSet<ClassDeclarationSyntax>>();

            var defineGotoStateTransitionsMethod = machine.ChildNodes().
                OfType<MethodDeclarationSyntax>().FirstOrDefault(v
                => v.Identifier.ValueText.Equals("DefineGotoStateTransitions") &&
                v.Modifiers.Any(SyntaxKind.OverrideKeyword) && v.ReturnType.ToString().
                Equals("System.Collections.Generic.Dictionary<Type, GotoStateTransitions>"));
            if (defineGotoStateTransitionsMethod == null)
            {
                return false;
            }

            var returnStmt = defineGotoStateTransitionsMethod.DescendantNodes().
                OfType<ReturnStatementSyntax>().First();
            var returnSymbol = model.GetSymbolInfo(returnStmt.Expression).Symbol;
            Dictionary<ClassDeclarationSyntax, IdentifierNameSyntax> stateMap = null;
            if (!StateTransitionAnalysis.TryParseStateMap(out stateMap, returnSymbol,
                defineGotoStateTransitionsMethod, model))
            {
                return false;
            }

            foreach (var state in stateMap)
            {
                if (state.Value == null)
                {
                    continue;
                }

                var dictionarySymbol = model.GetSymbolInfo(state.Value).Symbol;
                var dictionaryInvocations = StateTransitionAnalysis.GetInvocationsFromSymbol(
                    dictionarySymbol, defineGotoStateTransitionsMethod);

                var transitions = ParseTransitions(dictionarySymbol,
                    defineGotoStateTransitionsMethod, model);
                if (transitions.Count == 0)
                {
                    continue;
                }

                stateTransitions.Add(state.Key, transitions);
            }

            return true;
        }

        /// <summary>
        /// Tries to parse and return the action bindings for the given machine.
        /// </summary>
        /// <param name="actionBindings">Action bindings</param>
        /// <param name="machine">Machine</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        private static bool TryParseActionBindings(out Dictionary<ClassDeclarationSyntax,
            HashSet<MethodDeclarationSyntax>> actionBindings, ClassDeclarationSyntax machine,
            SemanticModel model)
        {
            actionBindings = new Dictionary<ClassDeclarationSyntax, HashSet<MethodDeclarationSyntax>>();

            var defineActionBindingsMethod = machine.ChildNodes().
                OfType<MethodDeclarationSyntax>().FirstOrDefault(v
                => v.Identifier.ValueText.Equals("DefineActionBindings") &&
                v.Modifiers.Any(SyntaxKind.OverrideKeyword) && v.ReturnType.ToString().
                Equals("System.Collections.Generic.Dictionary<Type, ActionBindings>"));
            if (defineActionBindingsMethod == null)
            {
                return false;
            }

            var returnStmt = defineActionBindingsMethod.DescendantNodes().
                OfType<ReturnStatementSyntax>().First();
            var returnSymbol = model.GetSymbolInfo(returnStmt.Expression).Symbol;
            Dictionary<ClassDeclarationSyntax, IdentifierNameSyntax> stateMap = null;
            if (!StateTransitionAnalysis.TryParseStateMap(out stateMap, returnSymbol,
                defineActionBindingsMethod, model))
            {
                return false;
            }

            foreach (var state in stateMap)
            {
                var dictionarySymbol = model.GetSymbolInfo(state.Value).Symbol;
                var dictionaryInvocations = StateTransitionAnalysis.GetInvocationsFromSymbol(
                    dictionarySymbol, defineActionBindingsMethod);

                var actions = ParseActions(dictionarySymbol, defineActionBindingsMethod, model);
                if (actions.Count == 0)
                {
                    continue;
                }

                actionBindings.Add(state.Key, actions);
            }

            return true;
        }

        /// <summary>
        /// Parses and returns the state map from the given symbol.
        /// </summary>
        /// <param name="stateMap">State map</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="method">Method</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        private static bool TryParseStateMap(out Dictionary<ClassDeclarationSyntax, IdentifierNameSyntax> stateMap,
            ISymbol symbol, MethodDeclarationSyntax method, SemanticModel model)
        {
            stateMap = new Dictionary<ClassDeclarationSyntax, IdentifierNameSyntax>();

            var invocations = StateTransitionAnalysis.GetInvocationsFromSymbol(symbol, method);
            foreach (var inv in invocations)
            {
                if (!(inv.Expression is MemberAccessExpressionSyntax))
                {
                    continue;
                }

                var expr = inv.Expression as MemberAccessExpressionSyntax;
                if (!expr.Name.ToString().Equals("Add"))
                {
                    continue;
                }

                var stateType = (inv.ArgumentList.Arguments[0].Expression
                    as TypeOfExpressionSyntax).Type as IdentifierNameSyntax;
                var dictionary = inv.ArgumentList.Arguments[1].Expression
                    as IdentifierNameSyntax;
                var stateSymbol = model.GetTypeInfo(stateType).Type;
                var stateDefinition = SymbolFinder.FindSourceDefinitionAsync(stateSymbol,
                    ProgramInfo.Solution).Result;
                if (stateDefinition == null)
                {
                    return false;
                }

                var state = stateDefinition.DeclaringSyntaxReferences.First().GetSyntax()
                    as ClassDeclarationSyntax;
                stateMap.Add(state, dictionary);
            }

            return true;
        }

        /// <summary>
        /// Parses and returns the list of transitions from the given symbol.
        /// </summary>
        /// <param name="transitions">List of transitions</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="method">Method</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of transitions</returns>
        private static HashSet<ClassDeclarationSyntax> ParseTransitions(ISymbol symbol,
            MethodDeclarationSyntax method, SemanticModel model)
        {
            var transitions = new HashSet<ClassDeclarationSyntax>();

            var invocations = StateTransitionAnalysis.GetInvocationsFromSymbol(symbol, method);
            foreach (var inv in invocations)
            {
                if (!(inv.Expression is MemberAccessExpressionSyntax))
                {
                    continue;
                }

                var expr = inv.Expression as MemberAccessExpressionSyntax;
                if (!expr.Name.ToString().Equals("Add"))
                {
                    continue;
                }

                var stateType = (inv.ArgumentList.Arguments[1].Expression
                    as TypeOfExpressionSyntax).Type as IdentifierNameSyntax;
                var stateSymbol = model.GetTypeInfo(stateType).Type;
                var stateDefinition = SymbolFinder.FindSourceDefinitionAsync(stateSymbol,
                    ProgramInfo.Solution).Result;
                if (stateDefinition == null)
                {
                    continue;
                }

                var state = stateDefinition.DeclaringSyntaxReferences.First().GetSyntax()
                    as ClassDeclarationSyntax;
                transitions.Add(state);
            }

            return transitions;
        }

        /// <summary>
        /// Parses and returns the list of actions from the given symbol.
        /// </summary>
        /// <param name="actions">List of actions</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="method">Method</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of actions</returns>
        private static HashSet<MethodDeclarationSyntax> ParseActions(ISymbol symbol,
            MethodDeclarationSyntax method, SemanticModel model)
        {
            var actions = new HashSet<MethodDeclarationSyntax>();

            var invocations = StateTransitionAnalysis.GetInvocationsFromSymbol(symbol, method);
            foreach (var inv in invocations)
            {
                if (!(inv.Expression is MemberAccessExpressionSyntax))
                {
                    continue;
                }

                var expr = inv.Expression as MemberAccessExpressionSyntax;
                if (!expr.Name.ToString().Equals("Add"))
                {
                    continue;
                }

                var actionType = (inv.ArgumentList.Arguments[1].Expression
                    as ObjectCreationExpressionSyntax).ArgumentList.Arguments[0].
                    Expression as IdentifierNameSyntax;
                var actionSymbol = model.GetSymbolInfo(actionType).Symbol;
                var actionDefinition = SymbolFinder.FindSourceDefinitionAsync(actionSymbol,
                    ProgramInfo.Solution).Result;
                if (actionDefinition == null)
                {
                    continue;
                }

                var action = actionDefinition.DeclaringSyntaxReferences.First().GetSyntax()
                    as MethodDeclarationSyntax;
                actions.Add(action);
            }

            return actions;
        }

        /// <summary>
        /// Returns the list of invocations from the given symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="method">Method</param>
        /// <returns>List of invocations</returns>
        private static List<InvocationExpressionSyntax> GetInvocationsFromSymbol(ISymbol symbol,
            MethodDeclarationSyntax method)
        {
            var invocations = new List<InvocationExpressionSyntax>();
            var locations = SymbolFinder.FindReferencesAsync(symbol, ProgramInfo.Solution).
                Result.First().Locations;
            foreach (var loc in locations)
            {
                SyntaxNode node = null;
                try
                {
                    node = method.FindNode(loc.Location.SourceSpan);
                }
                catch
                {
                    continue;
                }
                
                var invocation = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                if (invocation != null && invocation.DescendantNodes().OfType<IdentifierNameSyntax>().
                    First().Identifier.ValueText.Equals(symbol.Name))
                {
                    invocations.Add(invocation);
                }
            }

            return invocations;
        }

        /// <summary>
        /// Computes statistics from the given state transitions and action
        /// bindings information.
        /// </summary>
        /// <param name="stateTransitions">State transitions</param>
        /// <param name="actionBindings">Action bindings</param>
        private static void ComputeStatistics(Dictionary<ClassDeclarationSyntax, HashSet<ClassDeclarationSyntax>> stateTransitions,
            Dictionary<ClassDeclarationSyntax, HashSet<MethodDeclarationSyntax>> actionBindings)
        {
            StateTransitionAnalysis.NumOfMachines++;

            if (stateTransitions != null)
            {
                foreach (var state in stateTransitions)
                {
                    StateTransitionAnalysis.NumOfTransitions += state.Value.Count;
                }
            }

            if (actionBindings != null)
            {
                foreach (var state in actionBindings)
                {
                    StateTransitionAnalysis.NumOfActionBindings += state.Value.Count;
                }
            }
        }

        #endregion
    }
}
