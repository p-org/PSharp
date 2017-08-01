//-----------------------------------------------------------------------
// <copyright file="StateDeclaration.cs">
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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// State declaration syntax node.
    /// </summary>
    internal sealed class StateDeclaration : PSharpSyntaxNode
    {
        #region fields

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

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="groupNode">StateGroupDeclaration</param>
        /// <param name="modSet">Modifier set</param>
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
            this.GotoStateTransitions = new Dictionary<Token, List<Token>>();
            this.PushStateTransitions = new Dictionary<Token, List<Token>>();
            this.ActionBindings = new Dictionary<Token, Token>();
            this.TransitionsOnExitActions = new Dictionary<Token, AnonymousActionHandler>();
            this.ActionHandlers = new Dictionary<Token, AnonymousActionHandler>();
            this.DeferredEvents = new HashSet<Token>();
            this.IgnoredEvents = new HashSet<Token>();
            this.ResolvedEventIdentifierTokens = new Dictionary<Token, Tuple<List<Token>, int>>();
            this.RewrittenMethods = new HashSet<QualifiedMethod>();
        }

        /// <summary>
        /// Adds a goto state transition.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="eventIdentifierTokens">Tokens</param>
        /// <param name="stateIdentifiers">Token list</param>
        /// <param name="actionHandler">AnonymousActionHandler</param>
        /// <returns>Boolean</returns>
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
        /// <param name="eventIdentifier">Token</param>
        /// <param name="eventIdentifierTokens">Tokens</param>
        /// <param name="stateIdentifiers">Token list</param>
        /// <returns>Boolean</returns>
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
        /// <param name="eventIdentifier">Token</param>
        /// <param name="eventIdentifierTokens">Tokens</param>
        /// <param name="actionHandler">AnonymousActionHandler</param>
        /// <returns>Boolean</returns>
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
        /// <param name="eventIdentifier">Token</param>
        /// <param name="eventIdentifierTokens">Tokens</param>
        /// <param name="actionIdentifier">Token</param>
        /// <returns>Boolean</returns>
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
        /// <param name="eventIdentifier">Token</param>
        /// <param name="eventIdentifierTokens">Tokens</param>
        /// <returns>Boolean</returns>
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
        /// <param name="eventIdentifier">Token</param>
        /// <param name="eventIdentifierTokens">Tokens</param>
        /// <returns>Boolean</returns>
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
            string text = "";
            try
            {
                text = this.GetRewrittenStateDeclaration(indentLevel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception was thrown during rewriting:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit("Failed to rewrite state '{0}' of machine '{1}'.",
                    this.Identifier.TextUnit.Text, this.Machine.Identifier.TextUnit.Text);
            }
            
            base.TextUnit = this.StateKeyword.TextUnit.WithText(text);
        }

        /// <summary>
        /// Returns the fully qualified state name.
        /// <param name="delimiter">Delimiter</param>
        /// </summary>
        /// <returns>Text</returns>
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
        /// <param name="eventIdentifier">Token</param>
        /// <returns>Name</returns>
        internal string GetResolvedEventHandlerName(Token eventIdentifier)
        {
            var resolvedEvent = this.ResolvedEventIdentifierTokens[eventIdentifier];
            var eventIdentifierTokens = resolvedEvent.Item1.TakeWhile(
                tok => tok.Type != TokenType.LeftAngleBracket);
            string qualifiedEventIdentifier = "";
            foreach (var tok in eventIdentifierTokens.Where(tok => tok.Type != TokenType.Dot))
            {
                qualifiedEventIdentifier += $"_{tok.TextUnit.Text}";
            }

            string typeId = "";
            if (eventIdentifierTokens.Count() != resolvedEvent.Item1.Count)
            {
                typeId += "_type_" + this.ResolvedEventIdentifierTokens[eventIdentifier].Item2;
            }

            return qualifiedEventIdentifier + typeId;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten state declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenStateDeclaration(int indentLevel)
        {
            var indent = GetIndent(indentLevel);
            string text = "";

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

            if (!this.Machine.IsMonitor)
            {
                text += "class " + this.Identifier.TextUnit.Text + " : MachineState";
            }
            else
            {
                text += "class " + this.Identifier.TextUnit.Text + " : MonitorState";
            }

            text += "\n" + indent + this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            text += indent + this.RightCurlyBracketToken.TextUnit.Text + "\n";

            return text;
        }

        /// <summary>
        /// Instruments the on entry action.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentOnEntryAction(string indent)
        {
            if (this.EntryDeclaration == null)
            {
                return "";
            }

            var suffix = this.EntryDeclaration.IsAsync ? "_async" : string.Empty;
            var generatedProcName = "psharp_" + this.GetFullyQualifiedName() + $"_on_entry_action{suffix}";
            this.RewrittenMethods.Add(new QualifiedMethod(generatedProcName,
                this.Machine.Identifier.TextUnit.Text,
                this.Machine.Namespace.QualifiedName));

            return indent + "[OnEntry(nameof(" + generatedProcName + "))]\n";
        }

        /// <summary>
        /// Instruments the on exit action.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentOnExitAction(string indent)
        {
            if (this.ExitDeclaration == null)
            {
                return "";
            }

            var suffix = this.ExitDeclaration.IsAsync ? "_async" : string.Empty;
            var generatedProcName = "psharp_" + this.GetFullyQualifiedName() + $"_on_exit_action{suffix}";
            this.RewrittenMethods.Add(new QualifiedMethod(generatedProcName,
                this.Machine.Identifier.TextUnit.Text,
                this.Machine.Namespace.QualifiedName));

            return indent + "[OnExit(nameof(" + generatedProcName +   "))]\n";
        }

        /// <summary>
        /// Instruments the goto state transitions.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentGotoStateTransitions(string indent)
        {
            if (this.GotoStateTransitions.Count == 0)
            {
                return "";
            }

            string text = "";

            foreach (var transition in this.GotoStateTransitions)
            {
                var onExitName = "";
                AnonymousActionHandler handler;
                if (this.TransitionsOnExitActions.TryGetValue(transition.Key, out handler))
                {
                    var suffix = handler.IsAsync ? "_async" : string.Empty;
                    onExitName = "psharp_" + this.GetFullyQualifiedName() +
                        this.GetResolvedEventHandlerName(transition.Key) + $"_action{suffix}";
                    this.RewrittenMethods.Add(new QualifiedMethod(onExitName,
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
                    Aggregate("", (acc, id) => (acc == "") ? id : acc + "." + id);
                    
                text += ", typeof(" + stateIdentifier + ")";

                if (onExitName.Length > 0)
                {
                    text += ", nameof(" + onExitName + ")";
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
                return "";
            }

            string text = "";

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
                    Aggregate("", (acc, id) => (acc == "") ? id : acc + "." + id);

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
                return "";
            }

            string text = "";

            foreach (var binding in this.ActionBindings)
            {
                var actionName = "";
                AnonymousActionHandler handler;
                if (this.ActionHandlers.TryGetValue(binding.Key, out handler))
                {
                    var suffix = handler.IsAsync ? "_async" : string.Empty;
                    actionName = "psharp_" + this.GetFullyQualifiedName() +
                        this.GetResolvedEventHandlerName(binding.Key) + $"_action{suffix}";
                    this.RewrittenMethods.Add(new QualifiedMethod(actionName,
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
                    text += ", nameof(" + actionName + ")";
                }
                else
                {
                    text += ", nameof(" + binding.Value.TextUnit.Text + ")";
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
                return "";
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
                return "";
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


        #endregion
    }
}
