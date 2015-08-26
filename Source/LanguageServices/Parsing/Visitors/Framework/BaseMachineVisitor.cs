//-----------------------------------------------------------------------
// <copyright file="BaseMachineVisitor.cs">
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// An abstract P# machine visitor.
    /// </summary>
    internal abstract class BaseMachineVisitor : BaseVisitor
    {
        #region fields

        /// <summary>
        /// Map from machines to a list of actions.
        /// </summary>
        protected Dictionary<ClassDeclarationSyntax, List<MethodDeclarationSyntax>> Actions;

        /// <summary>
        /// Map from machines to actions that contain a raise statement.
        /// </summary>
        protected Dictionary<ClassDeclarationSyntax, List<MethodDeclarationSyntax>> ActionsThatRaise;

        /// <summary>
        /// Map from machines to actions that contain a pop statement.
        /// </summary>
        protected Dictionary<ClassDeclarationSyntax, List<MethodDeclarationSyntax>> ActionsThaPop;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        internal BaseMachineVisitor(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog)
            : base(project, errorLog)
        {
            this.Actions = new Dictionary<ClassDeclarationSyntax, List<MethodDeclarationSyntax>>();
            this.ActionsThatRaise = new Dictionary<ClassDeclarationSyntax, List<MethodDeclarationSyntax>>();
            this.ActionsThaPop = new Dictionary<ClassDeclarationSyntax, List<MethodDeclarationSyntax>>();
        }

        /// <summary>
        /// Parses the syntax tree for errors.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        internal void Parse(SyntaxTree tree)
        {
            var project = ProgramInfo.GetProjectWithName(base.Project.Name);
            var compilation = project.GetCompilationAsync().Result;

            var machines = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsMachine(compilation, val)).
                ToList();

            foreach (var machine in machines)
            {
                this.DiscoverMachineActions(machine, compilation);
                this.DiscoverMachineActionsThatRaise(machine, compilation);
                
                if (!this.GetTypeOfMachine().Equals("Monitor"))
                {
                    this.DiscoverMachineActionsThatPop(machine, compilation);
                }
            }

            foreach (var machine in machines)
            {
                this.CheckForPublicFields(machine);
                this.CheckForInternalFields(machine);
                this.CheckForPublicMethods(machine);
                this.CheckForInternalMethods(machine);

                this.CheckForAtLeastOneState(machine, compilation);
                this.CheckForNonStateClasses(machine, compilation);
                this.CheckForStructs(machine);
                this.CheckForStartState(machine, compilation);

                this.CheckForNestedRaiseStatementsInActions(machine);
                this.CheckForRaiseStatementsInMethods(machine);

                if (!this.GetTypeOfMachine().Equals("Monitor"))
                {
                    this.CheckForNestedPopStatementsInActions(machine);
                    this.CheckForPopStatementsInMethods(machine);
                }
            }
        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns true if the given class declaration is a machine.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean value</returns>
        protected abstract bool IsMachine(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl);

        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean value</returns>
        protected abstract bool IsState(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl);

        /// <summary>
        /// Returns the type of the machine.
        /// </summary>
        /// <returns>Text</returns>
        protected abstract string GetTypeOfMachine();

        #endregion

        #region private methods

        /// <summary>
        /// Discovers the available actions of the given machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void DiscoverMachineActions(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(machine.SyntaxTree);

            var onEntryActionNames = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                SelectMany(val => val.AttributeLists).
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEntry")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 1).
                Where(val => val.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax).
                Select(val => val.Token.ValueText).
                ToList();

            var onEntryNameOfActionNames = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                SelectMany(val => val.AttributeLists).
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEntry")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 1).
                Where(val => val.ArgumentList.Arguments[0].Expression is InvocationExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as InvocationExpressionSyntax).
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("nameof")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 1).
                Where(val => val.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as IdentifierNameSyntax).
                Select(val => val.Identifier.ValueText).
                ToList();

            var onExitActionNames = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                SelectMany(val => val.AttributeLists).
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnExit")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 1).
                Where(val => val.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax).
                Select(val => val.Token.ValueText).
                ToList();

            var onExitNameOfActionNames = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                SelectMany(val => val.AttributeLists).
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnExit")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 1).
                Where(val => val.ArgumentList.Arguments[0].Expression is InvocationExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as InvocationExpressionSyntax).
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("nameof")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 1).
                Where(val => val.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as IdentifierNameSyntax).
                Select(val => val.Identifier.ValueText).
                ToList();
            
            var onEventDoActionNames = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                SelectMany(val => val.AttributeLists).
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEventDoAction")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 2).
                Where(val => val.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[1].Expression as LiteralExpressionSyntax).
                Select(val => val.Token.ValueText).
                ToList();

            var onEventDoNameOfActionNames = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                SelectMany(val => val.AttributeLists).
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEventDoAction")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 2).
                Where(val => val.ArgumentList.Arguments[1].Expression is InvocationExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[1].Expression as InvocationExpressionSyntax).
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("nameof")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count == 1).
                Where(val => val.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as IdentifierNameSyntax).
                Select(val => val.Identifier.ValueText).
                ToList();

            var actionNames = new HashSet<string>();
            actionNames.UnionWith(onEntryActionNames);
            actionNames.UnionWith(onEntryNameOfActionNames);
            actionNames.UnionWith(onExitActionNames);
            actionNames.UnionWith(onExitNameOfActionNames);
            actionNames.UnionWith(onEventDoActionNames);
            actionNames.UnionWith(onEventDoNameOfActionNames);

            this.Actions.Add(machine, new List<MethodDeclarationSyntax>());

            foreach (var actionName in actionNames)
            {
                var action = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                    Where(val => val.ParameterList != null).
                    Where(val => val.ParameterList.Parameters.Count == 0).
                    Where(val => val.Identifier.ValueText.Equals(actionName)).
                    FirstOrDefault();

                if (action != null)
                {
                    this.Actions[machine].Add(action);
                }
            }

            var states = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                ToList();

            var stateMethods = states.SelectMany(val => val.DescendantNodes().OfType<MethodDeclarationSyntax>()).
                Where(val => val.Modifiers.Any(SyntaxKind.OverrideKeyword)).
                ToList();

            foreach (var method in stateMethods)
            {
                this.Actions[machine].Add(method);
            }
        }

        /// <summary>
        /// Discovers the actions of the given machine that raise.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void DiscoverMachineActionsThatRaise(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            this.ActionsThatRaise.Add(machine, new List<MethodDeclarationSyntax>());

            var actions = this.Actions[machine];
            foreach (var action in actions)
            {
                var hasRaise = action.DescendantNodes().OfType<InvocationExpressionSyntax>().
                    Where(val => val.Expression is IdentifierNameSyntax).
                    Select(val => val.Expression as IdentifierNameSyntax).
                    Any(val => val.Identifier.ValueText.Equals("Raise"));

                if (!hasRaise)
                {
                    hasRaise = action.DescendantNodes().OfType<InvocationExpressionSyntax>().
                        Where(val => val.Expression is MemberAccessExpressionSyntax).
                        Select(val => val.Expression as MemberAccessExpressionSyntax).
                        Any(val => val.Name.Identifier.ValueText.Equals("Raise"));
                }

                if (hasRaise)
                {
                    this.ActionsThatRaise[machine].Add(action);
                }
            }
        }

        /// <summary>
        /// Discovers the actions of the given machine that pop.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void DiscoverMachineActionsThatPop(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            this.ActionsThaPop.Add(machine, new List<MethodDeclarationSyntax>());

            var actions = this.Actions[machine];
            foreach (var action in actions)
            {
                var hasPop = action.DescendantNodes().OfType<InvocationExpressionSyntax>().
                    Where(val => val.Expression is IdentifierNameSyntax).
                    Select(val => val.Expression as IdentifierNameSyntax).
                    Any(val => val.Identifier.ValueText.Equals("Pop"));

                if (!hasPop)
                {
                    hasPop = action.DescendantNodes().OfType<InvocationExpressionSyntax>().
                        Where(val => val.Expression is MemberAccessExpressionSyntax).
                        Select(val => val.Expression as MemberAccessExpressionSyntax).
                        Any(val => val.Name.Identifier.ValueText.Equals("Pop"));
                }

                if (hasPop)
                {
                    this.ActionsThaPop[machine].Add(action);
                }
            }
        }

        /// <summary>
        /// Checks that machine fields are non-public.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForPublicFields(ClassDeclarationSyntax machine)
        {
            var fieldIdentifiers = machine.DescendantNodes().OfType<FieldDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.PublicKeyword)).
                SelectMany(val => val.Declaration.Variables).
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in fieldIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier, "Not allowed to declare field '" +
                    identifier.ValueText + "' of " + this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "' as public."));
            }
        }

        /// <summary>
        /// Checks that machine fields are non-internal.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForInternalFields(ClassDeclarationSyntax machine)
        {
            var fieldIdentifiers = machine.DescendantNodes().OfType<FieldDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.InternalKeyword)).
                SelectMany(val => val.Declaration.Variables).
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in fieldIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier, "Not allowed to declare field '" +
                    identifier.ValueText + "' of " + this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "' as internal."));
            }
        }

        /// <summary>
        /// Checks that machine methods are non-public.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForPublicMethods(ClassDeclarationSyntax machine)
        {
            var methodIdentifiers = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.PublicKeyword)).
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in methodIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier, "Not allowed to declare method '" +
                    identifier.ValueText + "' of " + this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "' as public."));
            }
        }

        /// <summary>
        /// Checks that machine methods are non-internal.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForInternalMethods(ClassDeclarationSyntax machine)
        {
            var methodIdentifiers = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.InternalKeyword)).
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in methodIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier, "Not allowed to declare method '" +
                    identifier.ValueText + "' of " + this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "' as internal."));
            }
        }

        /// <summary>
        /// Checks that at least one state is declared inside the machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForAtLeastOneState(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            var states = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                ToList();

            if (states.Count == 0)
            {
                base.ErrorLog.Add(Tuple.Create(machine.Identifier, this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "' must declare at least one state."));
            }
        }

        /// <summary>
        /// Checks that no non-state classes are declared inside the machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForNonStateClasses(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            var classIdentifiers = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => !this.IsState(compilation, val)).
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in classIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier, "Not allowed to declare non-state class '" +
                    identifier.ValueText + "' inside " + this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "'."));
            }
        }

        /// <summary>
        /// Checks that no structs are declared inside the machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForStructs(ClassDeclarationSyntax machine)
        {
            var structIdentifiers = machine.DescendantNodes().OfType<StructDeclarationSyntax>().
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in structIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier, "Not allowed to declare struct '" +
                    identifier.ValueText + "' inside " + this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "'."));
            }
        }

        /// <summary>
        /// Checks that a machine has an start state.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForStartState(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(machine.SyntaxTree);
            
            var stateAttributes = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                SelectMany(val => val.AttributeLists).
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.Start")).
                ToList();

            if (stateAttributes.Count == 0)
            {
                base.ErrorLog.Add(Tuple.Create(machine.Identifier, this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "' must declare a start state."));
            }
            else if (stateAttributes.Count > 1)
            {
                base.ErrorLog.Add(Tuple.Create(machine.Identifier, this.GetTypeOfMachine().ToLower() + " '" +
                    machine.Identifier.ValueText + "' must declare only one start state."));
            }
        }

        /// <summary>
        /// Checks that a nested raise statement is not used in a machine action.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForNestedRaiseStatementsInActions(ClassDeclarationSyntax machine)
        {
            var actions = this.Actions[machine];
            foreach (var action in actions)
            {
                var hasNestedRaise = action.DescendantNodes().OfType<InvocationExpressionSyntax>().
                    Where(val => val.Expression is IdentifierNameSyntax).
                    Select(val => val.Expression as IdentifierNameSyntax).
                    Select(val => val.Identifier.ValueText).
                    Any(val => this.ActionsThatRaise[machine].Any(m => m.Identifier.ValueText.Equals(val)));

                if (!hasNestedRaise)
                {
                    hasNestedRaise = action.DescendantNodes().OfType<InvocationExpressionSyntax>().
                        Where(val => val.Expression is MemberAccessExpressionSyntax).
                        Select(val => val.Expression as MemberAccessExpressionSyntax).
                        Select(val => val.Name.Identifier.ValueText).
                        Any(val => this.ActionsThatRaise[machine].Any(m => m.Identifier.ValueText.Equals(val)));
                }

                if (hasNestedRaise)
                {
                    base.ErrorLog.Add(Tuple.Create(action.Identifier, "Method '" + action.Identifier.ValueText +
                        "' of " + this.GetTypeOfMachine().ToLower() + " `" + machine.Identifier.ValueText +
                        "` must not call a method that raises an event."));
                }
            }
        }

        /// <summary>
        /// Checks that a nested pop statement is not used in a machine action.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForNestedPopStatementsInActions(ClassDeclarationSyntax machine)
        {
            var actions = this.Actions[machine];
            foreach (var action in actions)
            {
                var hasNestedPop = action.DescendantNodes().OfType<InvocationExpressionSyntax>().
                    Where(val => val.Expression is IdentifierNameSyntax).
                    Select(val => val.Expression as IdentifierNameSyntax).
                    Select(val => val.Identifier.ValueText).
                    Any(val => this.ActionsThaPop[machine].Any(m => m.Identifier.ValueText.Equals(val)));

                if (!hasNestedPop)
                {
                    hasNestedPop = action.DescendantNodes().OfType<InvocationExpressionSyntax>().
                        Where(val => val.Expression is MemberAccessExpressionSyntax).
                        Select(val => val.Expression as MemberAccessExpressionSyntax).
                        Select(val => val.Name.Identifier.ValueText).
                        Any(val => this.ActionsThaPop[machine].Any(m => m.Identifier.ValueText.Equals(val)));
                }

                if (hasNestedPop)
                {
                    base.ErrorLog.Add(Tuple.Create(action.Identifier, "Method '" + action.Identifier.ValueText +
                        "' of " + this.GetTypeOfMachine().ToLower() + " `" + machine.Identifier.ValueText +
                        "` must not call a method that pops a state."));
                }
            }
        }

        /// <summary>
        /// Checks that a raise statement is not used in a machine method.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForRaiseStatementsInMethods(ClassDeclarationSyntax machine)
        {
            var methods = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => !this.Actions[machine].Contains(val)).
                ToList();
            
            foreach (var method in methods)
            {
                var hasRaise = method.DescendantNodes().OfType<InvocationExpressionSyntax>().
                    Where(val => val.Expression is IdentifierNameSyntax).
                    Select(val => val.Expression as IdentifierNameSyntax).
                    Any(val => val.Identifier.ValueText.Equals("Raise"));

                if (!hasRaise)
                {
                    hasRaise = method.DescendantNodes().OfType<InvocationExpressionSyntax>().
                        Where(val => val.Expression is MemberAccessExpressionSyntax).
                        Select(val => val.Expression as MemberAccessExpressionSyntax).
                        Any(val => val.Name.Identifier.ValueText.Equals("Raise"));
                }

                if (hasRaise)
                {
                    base.ErrorLog.Add(Tuple.Create(method.Identifier, "Method '" + method.Identifier.ValueText +
                        "' of " + this.GetTypeOfMachine().ToLower() + " `" + machine.Identifier.ValueText +
                        "` must not raise an event."));
                }

                var hasNestedRaise = method.DescendantNodes().OfType<InvocationExpressionSyntax>().
                    Where(val => val.Expression is IdentifierNameSyntax).
                    Select(val => val.Expression as IdentifierNameSyntax).
                    Select(val => val.Identifier.ValueText).
                    Any(val => this.ActionsThatRaise[machine].Any(a => a.Identifier.ValueText.Equals(val)));

                if (!hasNestedRaise)
                {
                    hasNestedRaise = method.DescendantNodes().OfType<InvocationExpressionSyntax>().
                        Where(val => val.Expression is MemberAccessExpressionSyntax).
                        Select(val => val.Expression as MemberAccessExpressionSyntax).
                        Select(val => val.Name.Identifier.ValueText).
                        Any(val => this.ActionsThatRaise[machine].Any(a => a.Identifier.ValueText.Equals(val)));
                }

                if (hasNestedRaise)
                {
                    base.ErrorLog.Add(Tuple.Create(method.Identifier, "Method '" + method.Identifier.ValueText +
                        "' of " + this.GetTypeOfMachine().ToLower() + " `" + machine.Identifier.ValueText +
                        "` must not call a method that raises an event."));
                }
            }
        }

        /// <summary>
        /// Checks that a pop statement is not used in a machine method.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForPopStatementsInMethods(ClassDeclarationSyntax machine)
        {
            var methods = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => !this.Actions[machine].Contains(val)).
                ToList();

            foreach (var method in methods)
            {
                var hasPop = method.DescendantNodes().OfType<InvocationExpressionSyntax>().
                    Where(val => val.Expression is IdentifierNameSyntax).
                    Select(val => val.Expression as IdentifierNameSyntax).
                    Any(val => val.Identifier.ValueText.Equals("Pop"));

                if (!hasPop)
                {
                    hasPop = method.DescendantNodes().OfType<InvocationExpressionSyntax>().
                        Where(val => val.Expression is MemberAccessExpressionSyntax).
                        Select(val => val.Expression as MemberAccessExpressionSyntax).
                        Any(val => val.Name.Identifier.ValueText.Equals("Pop"));
                }

                if (hasPop)
                {
                    base.ErrorLog.Add(Tuple.Create(method.Identifier, "Method '" + method.Identifier.ValueText +
                        "' of " + this.GetTypeOfMachine().ToLower() + " `" + machine.Identifier.ValueText +
                        "` must not pop a state."));
                }

                var hasNestedPop = method.DescendantNodes().OfType<InvocationExpressionSyntax>().
                    Where(val => val.Expression is IdentifierNameSyntax).
                    Select(val => val.Expression as IdentifierNameSyntax).
                    Select(val => val.Identifier.ValueText).
                    Any(val => this.ActionsThaPop[machine].Any(a => a.Identifier.ValueText.Equals(val)));

                if (!hasNestedPop)
                {
                    hasNestedPop = method.DescendantNodes().OfType<InvocationExpressionSyntax>().
                        Where(val => val.Expression is MemberAccessExpressionSyntax).
                        Select(val => val.Expression as MemberAccessExpressionSyntax).
                        Select(val => val.Name.Identifier.ValueText).
                        Any(val => this.ActionsThaPop[machine].Any(a => a.Identifier.ValueText.Equals(val)));
                }

                if (hasNestedPop)
                {
                    base.ErrorLog.Add(Tuple.Create(method.Identifier, "Method '" + method.Identifier.ValueText +
                        "' of " + this.GetTypeOfMachine().ToLower() + " `" + machine.Identifier.ValueText +
                        "` must not call a method that pops a state."));
                }
            }
        }

        #endregion
    }
}
