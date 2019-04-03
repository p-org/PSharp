// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// An abstract P# state visitor.
    /// </summary>
    internal abstract class BaseStateVisitor : BaseVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStateVisitor"/> class.
        /// </summary>
        internal BaseStateVisitor(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog,
            List<Tuple<SyntaxToken, string>> warningLog)
            : base(project, errorLog, warningLog)
        {
        }

        /// <summary>
        /// Parses the syntax tree for errors.
        /// </summary>
        internal void Parse(SyntaxTree tree)
        {
            var project = this.Project.CompilationContext.GetProjectWithName(this.Project.Name);
            var compilation = project.GetCompilationAsync().Result;

            var states = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                ToList();

            foreach (var state in states)
            {
                this.CheckForFields(state);
                this.CheckForMethods(state);
                this.CheckForClasses(state);
                this.CheckForStructs(state);

                this.CheckForDuplicateOnEntry(state, compilation);
                this.CheckForDuplicateOnExit(state, compilation);
                this.CheckForMultipleSameEventHandlers(state, compilation);
                this.CheckForCorrectWildcardUse(state, compilation);

                this.CheckForSpecialProperties(state, compilation);
            }
        }

        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        protected abstract bool IsState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl);

        /// <summary>
        /// Returns the type of the state.
        /// </summary>
        protected abstract string GetTypeOfState();

        /// <summary>
        /// Checks for special properties.
        /// </summary>
        protected abstract void CheckForSpecialProperties(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation);

        /// <summary>
        /// Checks that no fields are declared inside the state.
        /// </summary>
        private void CheckForFields(ClassDeclarationSyntax state)
        {
            var fields = state.DescendantNodes().OfType<FieldDeclarationSyntax>().
                ToList();

            if (fields.Count > 0)
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" +
                    state.Identifier.ValueText + "' cannot declare fields."));
            }
        }

        /// <summary>
        /// Checks that no methods are declared inside the machine (beside the P# API ones).
        /// </summary>
        private void CheckForMethods(ClassDeclarationSyntax state)
        {
            var methods = state.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => !val.Modifiers.Any(SyntaxKind.OverrideKeyword)).
                ToList();

            if (methods.Count > 0)
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" +
                    state.Identifier.ValueText + "' cannot declare methods."));
            }
        }

        /// <summary>
        /// Checks that no classes are declared inside the state.
        /// </summary>
        private void CheckForClasses(ClassDeclarationSyntax state)
        {
            var classes = state.DescendantNodes().OfType<ClassDeclarationSyntax>().
                ToList();

            if (classes.Count > 0)
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" +
                    state.Identifier.ValueText + "' cannot declare classes."));
            }
        }

        /// <summary>
        /// Checks that no structs are declared inside the state.
        /// </summary>
        private void CheckForStructs(ClassDeclarationSyntax state)
        {
            var structs = state.DescendantNodes().OfType<StructDeclarationSyntax>().
                ToList();

            if (structs.Count > 0)
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" +
                    state.Identifier.ValueText + "' cannot declare structs."));
            }
        }

        /// <summary>
        /// Checks that a state does not have a duplicate entry action.
        /// </summary>
        private void CheckForDuplicateOnEntry(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(state.SyntaxTree);

            var onEntryAttribute = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEntry")).
                FirstOrDefault();

            var onEntryMethod = state.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.OverrideKeyword)).
                Where(val => val.Identifier.ValueText.Equals("OnEntry")).
                FirstOrDefault();

            if (onEntryAttribute != null && onEntryMethod != null)
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" +
                    state.Identifier.ValueText + "' cannot have two entry actions."));
            }
        }

        /// <summary>
        /// Checks that a state does not have a duplicate exit action.
        /// </summary>
        private void CheckForDuplicateOnExit(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(state.SyntaxTree);

            var onExitAttribute = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnExit")).
                FirstOrDefault();

            var onExitMethod = state.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.OverrideKeyword)).
                Where(val => val.Identifier.ValueText.Equals("OnExit")).
                FirstOrDefault();

            if (onExitAttribute != null && onExitMethod != null)
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" +
                    state.Identifier.ValueText + "' cannot have two exit actions."));
            }
        }

        /// <summary>
        /// Checks for multiple handlers for the same event.
        /// </summary>
        private void CheckForMultipleSameEventHandlers(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(state.SyntaxTree);

            var eventTypes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEventGotoState") ||
                  model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEventPushState") ||
                  model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEventDoAction")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count > 0).
                Where(val => val.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as TypeOfExpressionSyntax);

            // Ignore and Defer take a set of arguments.
            var setEventTypes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.IgnoreEvents") ||
                  model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.DeferEvents")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count > 0).
                SelectMany(val => val.ArgumentList.Arguments).
                Where(val => val.Expression is TypeOfExpressionSyntax).
                Select(val => val.Expression as TypeOfExpressionSyntax);

            eventTypes = eventTypes.Concat(setEventTypes);

            var eventHandlers = eventTypes.
                Where(val => val.Type is IdentifierNameSyntax).
                Select(val => val.Type as IdentifierNameSyntax).
                Select(val => val.ToFullString()).
                ToList();

            eventHandlers.AddRange(eventTypes.
                Where(val => val.Type is QualifiedNameSyntax).
                Select(val => val.Type as QualifiedNameSyntax).
                Select(val => val.ToFullString()).
                ToList());

            var eventOccurrences = eventHandlers.GroupBy(val => val);

            foreach (var e in eventOccurrences.Where(val => val.Count() > 1))
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                    "' cannot declare more than one handler for event '" + e.Key + "'."));
            }
        }

        /// <summary>
        /// Checks for correct wildcard usage.
        /// If "defer *" then:
        ///    no other event should be deferred.
        /// If "ignore *" or "on * do action" then:
        ///    no other action or ignore should be defined.
        /// If "On * goto" or "On * push" then:
        ///    no other transition, action or ignore should be defined.
        /// </summary>
        private void CheckForCorrectWildcardUse(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(state.SyntaxTree);

            var ignoreTypes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.IgnoreEvents")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count > 0).
                SelectMany(val => val.ArgumentList.Arguments).
                Where(val => val.Expression is TypeOfExpressionSyntax).
                Select(val => val.Expression as TypeOfExpressionSyntax);

            var deferTypes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.DeferEvents")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count > 0).
                SelectMany(val => val.ArgumentList.Arguments).
                Where(val => val.Expression is TypeOfExpressionSyntax).
                Select(val => val.Expression as TypeOfExpressionSyntax);

            var actionTypes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEventDoAction")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count > 0).
                Where(val => val.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as TypeOfExpressionSyntax);

            var transitionTypes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEventGotoState") ||
                  model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEventPushState")).
                Where(val => val.ArgumentList != null).
                Where(val => val.ArgumentList.Arguments.Count > 0).
                Where(val => val.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax).
                Select(val => val.ArgumentList.Arguments[0].Expression as TypeOfExpressionSyntax);

            var convertToStringSet = new Func<IEnumerable<TypeOfExpressionSyntax>, HashSet<string>>(ls =>
            {
                var eventHandlers = new HashSet<string>(ls.
                    Where(val => val.Type is IdentifierNameSyntax).
                    Select(val => val.Type as IdentifierNameSyntax).
                    Select(val => val.ToFullString()));

                eventHandlers.UnionWith(ls.
                    Where(val => val.Type is QualifiedNameSyntax).
                    Select(val => val.Type as QualifiedNameSyntax).
                    Select(val => val.ToFullString()));

                return eventHandlers;
            });

            var ignoredEvents = convertToStringSet(ignoreTypes);
            var deferredEvents = convertToStringSet(deferTypes);
            var actionEvents = convertToStringSet(actionTypes);
            var transitionEvents = convertToStringSet(transitionTypes);

            var isWildCard = new Func<string, bool>(s => s == "WildCardEvent" || s == "Microsoft.PSharp.WildCardEvent");
            var hasWildCard = new Func<HashSet<string>, bool>(set => set.Contains("WildCardEvent") ||
              set.Contains("Microsoft.PSharp.WildCardEvent"));

            if (hasWildCard(deferredEvents))
            {
                foreach (var e in deferredEvents.Where(s => !isWildCard(s)))
                {
                    this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                        "' cannot defer other event '" + e + "' when deferring the wildcard event."));
                }
            }

            if (hasWildCard(ignoredEvents))
            {
                foreach (var e in ignoredEvents.Where(s => !isWildCard(s)))
                {
                    this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                        "' cannot ignore other event '" + e + "' when ignoring the wildcard event."));
                }

                foreach (var e in actionEvents.Where(s => !isWildCard(s)))
                {
                    this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                        "' cannot define action on '" + e + "' when ignoring the wildcard event."));
                }
            }

            if (hasWildCard(actionEvents))
            {
                foreach (var e in ignoredEvents.Where(s => !isWildCard(s)))
                {
                    this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                        "' cannot ignore other event '" + e + "' when defining an action on the wildcard event."));
                }

                foreach (var e in actionEvents.Where(s => !isWildCard(s)))
                {
                    this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                        "' cannot define action on '" + e + "' when defining an action on the wildcard event."));
                }
            }

            if (hasWildCard(transitionEvents))
            {
                foreach (var e in ignoredEvents.Where(s => !isWildCard(s)))
                {
                    this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                        "' cannot ignore other event '" + e + "' when defining a transition on the wildcard event."));
                }

                foreach (var e in actionEvents.Where(s => !isWildCard(s)))
                {
                    this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                        "' cannot define action on '" + e + "' when defining a transition on the wildcard event."));
                }

                foreach (var e in transitionEvents.Where(s => !isWildCard(s)))
                {
                    this.ErrorLog.Add(Tuple.Create(state.Identifier, "State '" + state.Identifier.ValueText +
                        "' cannot define a transition on '" + e + "' when defining a transition on the wildcard event."));
                }
            }
        }
    }
}
