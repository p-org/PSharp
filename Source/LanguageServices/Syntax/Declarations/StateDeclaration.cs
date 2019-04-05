// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// State declaration syntax node.
    /// </summary>
    internal sealed class StateDeclaration : PSharpSyntaxNode
    {
        /// <summary>
        /// The machine parent node.
        /// </summary>
        internal readonly MachineDeclaration Machine;

        /// <summary>
        /// Parent state group (if any).
        /// </summary>
        internal readonly StateGroupDeclaration Group;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal readonly AccessModifier AccessModifier;

        /// <summary>
        /// True if the state is the start state.
        /// </summary>
        internal readonly bool IsStart;

        /// <summary>
        /// True if the state is a hot state.
        /// </summary>
        internal readonly bool IsHot;

        /// <summary>
        /// True if the state is a cold state.
        /// </summary>
        internal readonly bool IsCold;

        /// <summary>
        /// The state keyword.
        /// </summary>
        internal Token StateKeyword;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        internal Token LeftCurlyBracketToken;

        /// <summary>
        /// True if the machine is abstract.
        /// </summary>
        internal bool IsAbstract;

        /// <summary>
        /// The token identifying the base state this state inherits from, if any.
        /// </summary>
        internal Token BaseStateToken;

        /// <summary>
        /// Entry declaration.
        /// </summary>
        internal EntryDeclaration EntryDeclaration;

        /// <summary>
        /// Exit declaration.
        /// </summary>
        internal ExitDeclaration ExitDeclaration;

        /// <summary>
        /// Dictionary containing goto state transitions.
        /// </summary>
        internal Dictionary<Token, List<Token>> GotoStateTransitions;

        /// <summary>
        /// Dictionary containing push state transitions.
        /// </summary>
        internal Dictionary<Token, List<Token>> PushStateTransitions;

        /// <summary>
        /// Dictionary containing actions bindings.
        /// </summary>
        internal Dictionary<Token, Token> ActionBindings;

        /// <summary>
        /// Dictionary containing transitions on exit actions.
        /// </summary>
        internal Dictionary<Token, AnonymousActionHandler> TransitionsOnExitActions;

        /// <summary>
        /// Dictionary containing actions handlers.
        /// </summary>
        internal Dictionary<Token, AnonymousActionHandler> ActionHandlers;

        /// <summary>
        /// Set of deferred events.
        /// </summary>
        internal HashSet<Token> DeferredEvents;

        /// <summary>
        /// Set of ignored events.
        /// </summary>
        internal HashSet<Token> IgnoredEvents;

        /// <summary>
        /// Map from resolved event identifier tokens to their
        /// list of tokens and type id.
        /// </summary>
        internal Dictionary<Token, Tuple<List<Token>, int>> ResolvedEventIdentifierTokens;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        /// <summary>
        /// Set of all rewritten method.
        /// </summary>
        internal HashSet<QualifiedMethod> RewrittenMethods;

        /// <summary>
        /// Whether to use nameof or a quoted string (based on C# version).
        /// </summary>
        private readonly bool isNameofSupported;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateDeclaration"/> class.
        /// </summary>
        internal StateDeclaration(IPSharpProgram program, MachineDeclaration machineNode,
            StateGroupDeclaration groupNode, ModifierSet modSet)
            : base(program)
        {
            this.Machine = machineNode;
            this.Group = groupNode;
            this.AccessModifier = modSet.AccessModifier;
            this.IsStart = modSet.IsStart;
            this.IsHot = modSet.IsHot;
            this.IsCold = modSet.IsCold;
            this.IsAbstract = modSet.InheritanceModifier == InheritanceModifier.Abstract;
            this.GotoStateTransitions = new Dictionary<Token, List<Token>>();
            this.PushStateTransitions = new Dictionary<Token, List<Token>>();
            this.ActionBindings = new Dictionary<Token, Token>();
            this.TransitionsOnExitActions = new Dictionary<Token, AnonymousActionHandler>();
            this.ActionHandlers = new Dictionary<Token, AnonymousActionHandler>();
            this.DeferredEvents = new HashSet<Token>();
            this.IgnoredEvents = new HashSet<Token>();
            this.ResolvedEventIdentifierTokens = new Dictionary<Token, Tuple<List<Token>, int>>();
            this.RewrittenMethods = new HashSet<QualifiedMethod>();
            this.isNameofSupported = this.Program.GetProject().CompilationContext.Configuration.IsRewriteCSharpVersion(6, 0);
        }

        /// <summary>
        /// Adds a goto state transition.
        /// </summary>
        internal bool AddGotoStateTransition(Token eventIdentifier, List<Token> eventIdentifierTokens,
            List<Token> stateIdentifiers, AnonymousActionHandler actionHandler = null)
        {
            if (this.GotoStateTransitions.ContainsKey(eventIdentifier) ||
                this.PushStateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.GotoStateTransitions.Add(eventIdentifier, stateIdentifiers);
            if (actionHandler != null)
            {
                this.TransitionsOnExitActions.Add(eventIdentifier, actionHandler);
            }

            this.ResolvedEventIdentifierTokens[eventIdentifier] = Tuple.Create(
                eventIdentifierTokens, this.ResolvedEventIdentifierTokens.Count);

            return true;
        }

        /// <summary>
        /// Adds a push state transition.
        /// </summary>
        internal bool AddPushStateTransition(Token eventIdentifier, List<Token> eventIdentifierTokens,
            List<Token> stateIdentifiers)
        {
            if (this.Machine.IsMonitor)
            {
                return false;
            }

            if (this.GotoStateTransitions.ContainsKey(eventIdentifier) ||
                this.PushStateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.PushStateTransitions.Add(eventIdentifier, stateIdentifiers);
            this.ResolvedEventIdentifierTokens[eventIdentifier] = Tuple.Create(
                eventIdentifierTokens, this.ResolvedEventIdentifierTokens.Count);

            return true;
        }

        /// <summary>
        /// Adds an action binding.
        /// </summary>
        internal bool AddActionBinding(Token eventIdentifier, List<Token> eventIdentifierTokens,
            AnonymousActionHandler actionHandler)
        {
            if (this.GotoStateTransitions.ContainsKey(eventIdentifier) ||
                this.PushStateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.ActionBindings.Add(eventIdentifier, null);
            this.ActionHandlers.Add(eventIdentifier, actionHandler);
            this.ResolvedEventIdentifierTokens[eventIdentifier] = Tuple.Create(
                eventIdentifierTokens, this.ResolvedEventIdentifierTokens.Count);

            return true;
        }

        /// <summary>
        /// Adds an action binding.
        /// </summary>
        internal bool AddActionBinding(Token eventIdentifier, List<Token> eventIdentifierTokens,
            Token actionIdentifier)
        {
            if (this.GotoStateTransitions.ContainsKey(eventIdentifier) ||
                this.PushStateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.ActionBindings.Add(eventIdentifier, actionIdentifier);
            this.ResolvedEventIdentifierTokens[eventIdentifier] = Tuple.Create(
                eventIdentifierTokens, this.ResolvedEventIdentifierTokens.Count);

            return true;
        }

        /// <summary>
        /// Adds a deferred event.
        /// </summary>
        internal bool AddDeferredEvent(Token eventIdentifier, List<Token> eventIdentifierTokens)
        {
            if (this.Machine.IsMonitor)
            {
                return false;
            }

            if (this.DeferredEvents.Contains(eventIdentifier) ||
                this.IgnoredEvents.Contains(eventIdentifier))
            {
                return false;
            }

            this.DeferredEvents.Add(eventIdentifier);
            this.ResolvedEventIdentifierTokens[eventIdentifier] = Tuple.Create(
                eventIdentifierTokens, this.ResolvedEventIdentifierTokens.Count);

            return true;
        }

        /// <summary>
        /// Adds an ignored event.
        /// </summary>
        internal bool AddIgnoredEvent(Token eventIdentifier, List<Token> eventIdentifierTokens)
        {
            if (this.DeferredEvents.Contains(eventIdentifier) ||
                this.IgnoredEvents.Contains(eventIdentifier))
            {
                return false;
            }

            this.IgnoredEvents.Add(eventIdentifier);
            this.ResolvedEventIdentifierTokens[eventIdentifier] = Tuple.Create(
                eventIdentifierTokens, this.ResolvedEventIdentifierTokens.Count);

            return true;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            string text = string.Empty;

            try
            {
                text = this.GetRewrittenStateDeclaration(indentLevel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception was thrown during rewriting:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit(
                    "Failed to rewrite state '{0}' of machine '{1}'.",
                    this.Identifier.TextUnit.Text,
                    this.Machine.Identifier.TextUnit.Text);
            }

            this.TextUnit = new TextUnit(text, this.StateKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Returns the fully qualified state name.
        /// </summary>
        internal string GetFullyQualifiedName(char delimiter = '_')
        {
            var qualifiedName = this.Identifier.TextUnit.Text;
            var containingGroup = this.Group;
            while (containingGroup != null)
            {
                qualifiedName = containingGroup.Identifier.TextUnit.Text + delimiter + qualifiedName;
                containingGroup = containingGroup.Group;
            }

            return qualifiedName;
        }

        /// <summary>
        /// Returns the resolved event handler name that corresponds to the
        /// specified event identifier.
        /// </summary>
        internal string GetResolvedEventHandlerName(Token eventIdentifier)
        {
            var resolvedEvent = this.ResolvedEventIdentifierTokens[eventIdentifier];
            var eventIdentifierTokens = resolvedEvent.Item1.TakeWhile(
                tok => tok.Type != TokenType.LeftAngleBracket);
            string qualifiedEventIdentifier = string.Empty;
            foreach (var tok in eventIdentifierTokens.Where(tok => tok.Type != TokenType.Dot))
            {
                qualifiedEventIdentifier += $"_{tok.TextUnit.Text}";
            }

            string typeId = string.Empty;
            if (eventIdentifierTokens.Count() != resolvedEvent.Item1.Count)
            {
                typeId += "_type_" + this.ResolvedEventIdentifierTokens[eventIdentifier].Item2;
            }

            return qualifiedEventIdentifier + typeId;
        }

        /// <summary>
        /// Returns the rewritten state declaration.
        /// </summary>
        private string GetRewrittenStateDeclaration(int indentLevel)
        {
            var indent = GetIndent(indentLevel);
            string text = string.Empty;

            if (this.IsStart)
            {
                text += indent + "[Microsoft.PSharp.Start]\n";
            }

            if (this.IsHot)
            {
                text += indent + "[Microsoft.PSharp.Hot]\n";
            }
            else if (this.IsCold)
            {
                text += indent + "[Microsoft.PSharp.Cold]\n";
            }

            text += this.InstrumentOnEntryAction(indent);
            text += this.InstrumentOnExitAction(indent);
            text += this.InstrumentGotoStateTransitions(indent);
            text += this.InstrumentPushStateTransitions(indent);
            text += this.InstrumentActionsBindings(indent);

            text += this.InstrumentIgnoredEvents(indent);
            text += this.InstrumentDeferredEvents(indent);

            text += indent;
            if (this.Group != null)
            {
                // When inside a group, the state should be made public.
                text += "public ";
            }
            else
            {
                // Otherwise, we look at the access modifier provided by the user.
                if (this.AccessModifier == AccessModifier.Protected)
                {
                    text += "protected ";
                }
                else if (this.AccessModifier == AccessModifier.Private)
                {
                    text += "private ";
                }
            }

            string getBaseTokenName(string requiredBaseType)
            {
                if (this.BaseStateToken is null)
                {
                    return requiredBaseType;
                }

                // TODO: Ensure base derives (transitively) from MachineState? What if base is a state in a machine in another dll?
                return this.BaseStateToken.Text;
            }

            var baseTokenName = getBaseTokenName(this.Machine.IsMonitor ? "MonitorState" : "MachineState");
            if (this.IsAbstract)
            {
                text += "abstract ";
            }

            text += "class " + this.Identifier.TextUnit.Text + " : " + baseTokenName;

            text += "\n" + indent + this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            text += indent + this.RightCurlyBracketToken.TextUnit.Text + "\n";

            return text;
        }

        private string Nameof(string item) => this.isNameofSupported ? $"nameof({item})" : $"\"{item}\"";

        /// <summary>
        /// Instruments the on entry action.
        /// </summary>
        private string InstrumentOnEntryAction(string indent)
        {
            if (this.EntryDeclaration is null)
            {
                return string.Empty;
            }

            var suffix = this.EntryDeclaration.IsAsync ? "_async" : string.Empty;
            var generatedProcName = "psharp_" + this.GetFullyQualifiedName() + $"_on_entry_action{suffix}";
            this.RewrittenMethods.Add(new QualifiedMethod(
                generatedProcName,
                this.Machine.Identifier.TextUnit.Text,
                this.Machine.Namespace.QualifiedName));

            return indent + $"[OnEntry({this.Nameof(generatedProcName)})]\n";
        }

        /// <summary>
        /// Instruments the on exit action.
        /// </summary>
        private string InstrumentOnExitAction(string indent)
        {
            if (this.ExitDeclaration is null)
            {
                return string.Empty;
            }

            var suffix = this.ExitDeclaration.IsAsync ? "_async" : string.Empty;
            var generatedProcName = "psharp_" + this.GetFullyQualifiedName() + $"_on_exit_action{suffix}";
            this.RewrittenMethods.Add(new QualifiedMethod(
                generatedProcName,
                this.Machine.Identifier.TextUnit.Text,
                this.Machine.Namespace.QualifiedName));

            return indent + $"[OnExit({this.Nameof(generatedProcName)})]\n";
        }

        /// <summary>
        /// Instruments the goto state transitions.
        /// </summary>
        private string InstrumentGotoStateTransitions(string indent)
        {
            if (this.GotoStateTransitions.Count == 0)
            {
                return string.Empty;
            }

            string text = string.Empty;
            foreach (var transition in this.GotoStateTransitions)
            {
                var onExitName = string.Empty;
                if (this.TransitionsOnExitActions.TryGetValue(transition.Key, out AnonymousActionHandler handler))
                {
                    var suffix = handler.IsAsync ? "_async" : string.Empty;
                    onExitName = "psharp_" + this.GetFullyQualifiedName() +
                        this.GetResolvedEventHandlerName(transition.Key) + $"_action{suffix}";
                    this.RewrittenMethods.Add(new QualifiedMethod(
                        onExitName,
                        this.Machine.Identifier.TextUnit.Text,
                        this.Machine.Namespace.QualifiedName));
                }

                text += indent + "[OnEventGotoState(";

                if (transition.Key.Type == TokenType.HaltEvent)
                {
                    text += "typeof(" + typeof(Halt).FullName + ")";
                }
                else if (transition.Key.Type == TokenType.DefaultEvent)
                {
                    text += "typeof(" + typeof(Default).FullName + ")";
                }
                else if (transition.Key.Type == TokenType.MulOp)
                {
                    text += "typeof(" + typeof(WildCardEvent).FullName + ")";
                }
                else
                {
                    text += "typeof(" + transition.Key.TextUnit.Text + ")";
                }

                var stateIdentifier = transition.Value.
                    Select(token => token.TextUnit.Text).
                    Aggregate(string.Empty, (acc, id) => string.IsNullOrEmpty(acc) ? id : acc + "." + id);

                text += ", typeof(" + stateIdentifier + ")";

                if (onExitName.Length > 0)
                {
                    text += $", {this.Nameof(onExitName)}";
                }

                text += ")]\n";
            }

            return text;
        }

        /// <summary>
        /// Instruments the push state transitions.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentPushStateTransitions(string indent)
        {
            if (this.PushStateTransitions.Count == 0)
            {
                return string.Empty;
            }

            string text = string.Empty;
            foreach (var transition in this.PushStateTransitions)
            {
                text += indent + "[OnEventPushState(";

                if (transition.Key.Type == TokenType.HaltEvent)
                {
                    text += "typeof(" + typeof(Halt).FullName + ")";
                }
                else if (transition.Key.Type == TokenType.DefaultEvent)
                {
                    text += "typeof(" + typeof(Default).FullName + ")";
                }
                else if (transition.Key.Type == TokenType.MulOp)
                {
                    text += "typeof(" + typeof(WildCardEvent).FullName + ")";
                }
                else
                {
                    text += "typeof(" + transition.Key.TextUnit.Text + ")";
                }

                var stateIdentifier = transition.Value.
                    Select(token => token.TextUnit.Text).
                    Aggregate(string.Empty, (acc, id) => string.IsNullOrEmpty(acc) ? id : acc + "." + id);

                text += ", typeof(" + stateIdentifier + ")";

                text += ")]\n";
            }

            return text;
        }

        /// <summary>
        /// Instruments the action bindings.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentActionsBindings(string indent)
        {
            if (this.ActionBindings.Count == 0)
            {
                return string.Empty;
            }

            string text = string.Empty;
            foreach (var binding in this.ActionBindings)
            {
                var actionName = string.Empty;
                if (this.ActionHandlers.TryGetValue(binding.Key, out AnonymousActionHandler handler))
                {
                    var suffix = handler.IsAsync ? "_async" : string.Empty;
                    actionName = "psharp_" + this.GetFullyQualifiedName() +
                        this.GetResolvedEventHandlerName(binding.Key) + $"_action{suffix}";
                    this.RewrittenMethods.Add(new QualifiedMethod(
                        actionName,
                        this.Machine.Identifier.TextUnit.Text,
                        this.Machine.Namespace.QualifiedName));
                }

                text += indent + "[OnEventDoAction(";

                if (binding.Key.Type == TokenType.HaltEvent)
                {
                    text += "typeof(" + typeof(Halt).FullName + ")";
                }
                else if (binding.Key.Type == TokenType.DefaultEvent)
                {
                    text += "typeof(" + typeof(Default).FullName + ")";
                }
                else if (binding.Key.Type == TokenType.MulOp)
                {
                    text += "typeof(" + typeof(WildCardEvent).FullName + ")";
                }
                else
                {
                    text += "typeof(" + binding.Key.TextUnit.Text + ")";
                }

                if (actionName.Length > 0)
                {
                    text += $", {this.Nameof(actionName)}";
                }
                else
                {
                    text += $", {this.Nameof(binding.Value.TextUnit.Text)}";
                }

                text += ")]\n";
            }

            return text;
        }

        /// <summary>
        /// Instruments the ignored events.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentIgnoredEvents(string indent)
        {
            if (this.IgnoredEvents.Count == 0)
            {
                return string.Empty;
            }

            string text = indent + "[IgnoreEvents(";

            var eventIds = this.IgnoredEvents.ToList();
            for (int idx = 0; idx < eventIds.Count; idx++)
            {
                if (eventIds[idx].Type == TokenType.HaltEvent)
                {
                    text += "typeof(" + typeof(Halt).FullName + ")";
                }
                else if (eventIds[idx].Type == TokenType.DefaultEvent)
                {
                    text += "typeof(" + typeof(Default).FullName + ")";
                }
                else if (eventIds[idx].Type == TokenType.MulOp)
                {
                    text += "typeof(" + typeof(WildCardEvent).FullName + ")";
                }
                else
                {
                    text += "typeof(" + eventIds[idx].TextUnit.Text + ")";
                }

                if (idx < eventIds.Count - 1)
                {
                    text += ", ";
                }
            }

            text += ")]\n";

            return text;
        }

        /// <summary>
        /// Instruments the deferred events.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentDeferredEvents(string indent)
        {
            if (this.Machine.IsMonitor || this.DeferredEvents.Count == 0)
            {
                return string.Empty;
            }

            string text = indent + "[DeferEvents(";

            var eventIds = this.DeferredEvents.ToList();
            for (int idx = 0; idx < eventIds.Count; idx++)
            {
                if (eventIds[idx].Type == TokenType.HaltEvent)
                {
                    text += "typeof(" + typeof(Halt).FullName + ")";
                }
                else if (eventIds[idx].Type == TokenType.DefaultEvent)
                {
                    text += "typeof(" + typeof(Default).FullName + ")";
                }
                else if (eventIds[idx].Type == TokenType.MulOp)
                {
                    text += "typeof(" + typeof(WildCardEvent).FullName + ")";
                }
                else
                {
                    text += "typeof(" + eventIds[idx].TextUnit.Text + ")";
                }

                if (idx < eventIds.Count - 1)
                {
                    text += ", ";
                }
            }

            text += ")]\n";

            return text;
        }
    }
}
