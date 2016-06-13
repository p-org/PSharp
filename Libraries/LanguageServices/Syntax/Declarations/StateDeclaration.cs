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

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// State declaration syntax node.
    /// </summary>
    internal sealed class StateDeclaration : PSharpSyntaxNode
    {
        #region fields

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
        /// The machine parent node.
        /// </summary>
        internal readonly MachineDeclaration Machine;

        /// <summary>
        /// Parent state group (if any).
        /// </summary>
        internal readonly StateGroupDeclaration Group;

        /// <summary>
        /// The state keyword.
        /// </summary>
        internal Token StateKeyword;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal AccessModifier AccessModifier;

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
        internal Dictionary<Token, BlockSyntax> TransitionsOnExitActions;

        /// <summary>
        /// Dictionary containing actions handlers.
        /// </summary>
        internal Dictionary<Token, BlockSyntax> ActionHandlers;

        /// <summary>
        /// Set of deferred events.
        /// </summary>
        internal HashSet<Token> DeferredEvents;

        /// <summary>
        /// Set of ignored events.
        /// </summary>
        internal HashSet<Token> IgnoredEvents;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        /// <summary>
        /// Set of all generated method names.
        /// </summary>
        internal HashSet<string> GeneratedMethodNames;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="groupNode">StateGroupDeclaration</param>
        /// <param name="isStart">Is start state</param>
        /// <param name="isHot">Is hot state</param>
        /// <param name="isCold">Is cold state</param>
        internal StateDeclaration(IPSharpProgram program, MachineDeclaration machineNode,
            StateGroupDeclaration groupNode, bool isStart, bool isHot, bool isCold)
            : base(program)
        {
            this.IsStart = isStart;
            this.IsHot = isHot;
            this.IsCold = isCold;
            this.Machine = machineNode;
            this.Group = groupNode;
            this.GotoStateTransitions = new Dictionary<Token, List<Token>>();
            this.PushStateTransitions = new Dictionary<Token, List<Token>>();
            this.ActionBindings = new Dictionary<Token, Token>();
            this.TransitionsOnExitActions = new Dictionary<Token, BlockSyntax>();
            this.ActionHandlers = new Dictionary<Token, BlockSyntax>();
            this.DeferredEvents = new HashSet<Token>();
            this.IgnoredEvents = new HashSet<Token>();
            this.GeneratedMethodNames = new HashSet<string>();
        }

        /// <summary>
        /// Adds a goto state transition.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="stateIdentifiers">Token list</param>
        /// <param name="stmtBlock">Statement block</param>
        /// <returns>Boolean</returns>
        internal bool AddGotoStateTransition(Token eventIdentifier, List<Token> stateIdentifiers,
            BlockSyntax stmtBlock = null)
        {
            if (this.GotoStateTransitions.ContainsKey(eventIdentifier) ||
                this.PushStateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.GotoStateTransitions.Add(eventIdentifier, stateIdentifiers);
            if (stmtBlock != null)
            {
                this.TransitionsOnExitActions.Add(eventIdentifier, stmtBlock);
            }

            return true;
        }

        /// <summary>
        /// Adds a push state transition.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="stateIdentifiers">Token list</param>
        /// <returns>Boolean</returns>
        internal bool AddPushStateTransition(Token eventIdentifier, List<Token> stateIdentifiers)
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

            return true;
        }

        /// <summary>
        /// Adds an action binding.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="stmtBlock">BlockSyntax</param>
        /// <returns>Boolean</returns>
        internal bool AddActionBinding(Token eventIdentifier, BlockSyntax stmtBlock)
        {
            if (this.GotoStateTransitions.ContainsKey(eventIdentifier) ||
                this.PushStateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.ActionBindings.Add(eventIdentifier, null);
            this.ActionHandlers.Add(eventIdentifier, stmtBlock);

            return true;
        }

        /// <summary>
        /// Adds an action binding.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="actionIdentifier">Token</param>
        /// <returns>Boolean</returns>
        internal bool AddActionBinding(Token eventIdentifier, Token actionIdentifier)
        {
            if (this.GotoStateTransitions.ContainsKey(eventIdentifier) ||
                this.PushStateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.ActionBindings.Add(eventIdentifier, actionIdentifier);

            return true;
        }

        /// <summary>
        /// Adds a deferred event.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <returns>Boolean</returns>
        internal bool AddDeferredEvent(Token eventIdentifier)
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

            return true;
        }

        /// <summary>
        /// Adds an ignored event.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <returns>Boolean</returns>
        internal bool AddIgnoredEvent(Token eventIdentifier)
        {
            if (this.DeferredEvents.Contains(eventIdentifier) ||
                this.IgnoredEvents.Contains(eventIdentifier))
            {
                return false;
            }

            this.IgnoredEvents.Add(eventIdentifier);

            return true;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite()
        {
            string text = "";

            try
            {
                text = this.GetRewrittenStateDeclaration();
            }
            catch (Exception ex)
            {
                IO.Debug("Exception was thrown during rewriting:");
                IO.Debug(ex.Message);
                IO.Debug(ex.StackTrace);
                ErrorReporter.ReportAndExit("Failed to rewrite state '{0}' of machine '{1}'.",
                    this.Identifier.TextUnit.Text, this.Machine.Identifier.TextUnit.Text);
            }
            
            base.TextUnit = new TextUnit(text, this.StateKeyword.TextUnit.Line);
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

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten state declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenStateDeclaration()
        {
            string text = "";

            if (this.IsStart)
            {
                text += "[Microsoft.PSharp.Start]\n";
            }

            if (this.IsHot)
            {
                text += "[Microsoft.PSharp.Hot]\n";
            }
            else if (this.IsCold)
            {
                text += "[Microsoft.PSharp.Cold]\n";
            }

            text += this.InstrumentOnEntryAction();
            text += this.InstrumentOnExitAction();
            text += this.InstrumentGotoStateTransitions();
            text += this.InstrumentPushStateTransitions();
            text += this.InstrumentActionsBindings();

            text += this.InstrumentIgnoredEvents();
            text += this.InstrumentDeferredEvents();

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

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            return text;
        }

        /// <summary>
        /// Instruments the on entry action.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentOnEntryAction()
        {
            if (this.EntryDeclaration == null)
            {
                return "";
            }

            var generatedProcName = "psharp_" + this.GetFullyQualifiedName() + "_on_entry_action";
            this.GeneratedMethodNames.Add(generatedProcName);

            return "[OnEntry(nameof(" + generatedProcName + "))]\n";
        }

        /// <summary>
        /// Instruments the on exit action.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentOnExitAction()
        {
            if (this.ExitDeclaration == null)
            {
                return "";
            }

            var generatedProcName = "psharp_" + this.GetFullyQualifiedName() + "_on_exit_action";
            this.GeneratedMethodNames.Add(generatedProcName);

            return "[OnExit(nameof(" + generatedProcName +   "))]\n";
        }

        /// <summary>
        /// Instruments the goto state transitions.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentGotoStateTransitions()
        {
            if (this.GotoStateTransitions.Count == 0)
            {
                return "";
            }

            string text = "";

            foreach (var transition in this.GotoStateTransitions)
            {
                var onExitName = "";
                if (this.TransitionsOnExitActions.ContainsKey(transition.Key))
                {
                    onExitName = "psharp_" + this.GetFullyQualifiedName() + "_" +
                        transition.Key.TextUnit.Text + "_action";
                    this.GeneratedMethodNames.Add(onExitName);
                }

                text += "[OnEventGotoState(";

                if (transition.Key.Type == TokenType.HaltEvent)
                {
                    text += "typeof(Microsoft.PSharp.Halt)";
                }
                else if (transition.Key.Type == TokenType.DefaultEvent)
                {
                    text += "typeof(Microsoft.PSharp.Default)";
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
        private string InstrumentPushStateTransitions()
        {
            if (this.PushStateTransitions.Count == 0)
            {
                return "";
            }

            string text = "";

            foreach (var transition in this.PushStateTransitions)
            {
                text += "[OnEventPushState(";

                if (transition.Key.Type == TokenType.HaltEvent)
                {
                    text += "typeof(Microsoft.PSharp.Halt)";
                }
                else if (transition.Key.Type == TokenType.DefaultEvent)
                {
                    text += "typeof(Microsoft.PSharp.Default)";
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
        private string InstrumentActionsBindings()
        {
            if (this.ActionBindings.Count == 0)
            {
                return "";
            }

            string text = "";

            foreach (var binding in this.ActionBindings)
            {
                var actionName = "";
                if (this.ActionHandlers.ContainsKey(binding.Key))
                {
                    actionName = "psharp_" + this.GetFullyQualifiedName() + "_" +
                        binding.Key.TextUnit.Text + "_action";
                    this.GeneratedMethodNames.Add(actionName);
                }

                text += "[OnEventDoAction(";

                if (binding.Key.Type == TokenType.HaltEvent)
                {
                    text += "typeof(Microsoft.PSharp.Halt)";
                }
                else if (binding.Key.Type == TokenType.DefaultEvent)
                {
                    text += "typeof(Microsoft.PSharp.Default)";
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
        private string InstrumentIgnoredEvents()
        {
            if (this.IgnoredEvents.Count == 0)
            {
                return "";
            }

            string text = "[IgnoreEvents(";

            var eventIds = this.IgnoredEvents.ToList();
            for (int idx = 0; idx < eventIds.Count; idx++)
            {
                if (eventIds[idx].Type == TokenType.HaltEvent)
                {
                    text += "typeof(Microsoft.PSharp.Halt)";
                }
                else if (eventIds[idx].Type == TokenType.DefaultEvent)
                {
                    text += "typeof(Microsoft.PSharp.Default)";
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
        private string InstrumentDeferredEvents()
        {
            if (this.Machine.IsMonitor || this.DeferredEvents.Count == 0)
            {
                return "";
            }

            string text = "[DeferEvents(";

            var eventIds = this.DeferredEvents.ToList();
            for (int idx = 0; idx < eventIds.Count; idx++)
            {
                if (eventIds[idx].Type == TokenType.HaltEvent)
                {
                    text += "typeof(Microsoft.PSharp.Halt)";
                }
                else if (eventIds[idx].Type == TokenType.DefaultEvent)
                {
                    text += "typeof(Microsoft.PSharp.Default)";
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
