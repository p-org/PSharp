//-----------------------------------------------------------------------
// <copyright file="StateDeclaration.cs">
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
        /// True if the state is the start state.
        /// </summary>
        internal readonly bool IsStart;

        /// <summary>
        /// The machine parent node.
        /// </summary>
        internal readonly MachineDeclaration Machine;

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
        internal Dictionary<Token, Token> GotoStateTransitions;

        /// <summary>
        /// Dictionary containing push state transitions.
        /// </summary>
        internal Dictionary<Token, Token> PushStateTransitions;

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

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="machineNode">PMachineDeclarationNode</param>
        /// <param name="isStart">Is start state</param>
        /// <param name="isModel">Is a model</param>
        internal StateDeclaration(IPSharpProgram program, MachineDeclaration machineNode,
            bool isStart, bool isModel)
            : base(program, isModel)
        {
            this.IsStart = isStart;
            this.Machine = machineNode;
            this.GotoStateTransitions = new Dictionary<Token, Token>();
            this.PushStateTransitions = new Dictionary<Token, Token>();
            this.ActionBindings = new Dictionary<Token, Token>();
            this.TransitionsOnExitActions = new Dictionary<Token, BlockSyntax>();
            this.ActionHandlers = new Dictionary<Token, BlockSyntax>();
            this.DeferredEvents = new HashSet<Token>();
            this.IgnoredEvents = new HashSet<Token>();
        }

        /// <summary>
        /// Adds a goto state transition.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="stateIdentifier">Token</param>
        /// <param name="stmtBlock">Statement block</param>
        /// <returns>Boolean value</returns>
        internal bool AddGotoStateTransition(Token eventIdentifier, Token stateIdentifier,
            BlockSyntax stmtBlock = null)
        {
            if (this.GotoStateTransitions.ContainsKey(eventIdentifier) ||
                this.PushStateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.GotoStateTransitions.Add(eventIdentifier, stateIdentifier);
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
        /// <param name="stateIdentifier">Token</param>
        /// <returns>Boolean value</returns>
        internal bool AddPushStateTransition(Token eventIdentifier, Token stateIdentifier)
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

            this.PushStateTransitions.Add(eventIdentifier, stateIdentifier);

            return true;
        }

        /// <summary>
        /// Adds an action binding.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="stateIdentifier">Token</param>
        /// <returns>Boolean value</returns>
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
        /// <returns>Boolean value</returns>
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
        /// <returns>Boolean value</returns>
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
        /// <returns>Boolean value</returns>
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
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            if (this.EntryDeclaration != null)
            {
                this.EntryDeclaration.Rewrite();
            }

            if (this.ExitDeclaration != null)
            {
                this.ExitDeclaration.Rewrite();
            }

            var text = this.GetRewrittenStateDeclaration();

            base.TextUnit = new TextUnit(text, this.StateKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            if (this.EntryDeclaration != null)
            {
                this.EntryDeclaration.Model();
            }

            if (this.ExitDeclaration != null)
            {
                this.ExitDeclaration.Model();
            }

            var text = this.GetRewrittenStateDeclaration();

            base.TextUnit = new TextUnit(text, this.StateKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten state declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenStateDeclaration()
        {
            var text = "";

            if (this.IsStart)
            {
                text += "[Microsoft.PSharp.Start]\n";
            }

            text += this.InstrumentGotoStateTransitions();
            text += this.InstrumentPushStateTransitions();
            text += this.InstrumentActionsBindings();

            text += this.InstrumentIgnoredEvents();
            text += this.InstrumentDeferredEvents();

            if (this.AccessModifier == AccessModifier.Protected)
            {
                text += "protected ";
            }
            else if (this.AccessModifier == AccessModifier.Private)
            {
                text += "private ";
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

            if (this.EntryDeclaration != null)
            {
                text += this.EntryDeclaration.TextUnit.Text;
            }

            if (this.ExitDeclaration != null)
            {
                text += this.ExitDeclaration.TextUnit.Text;
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            return text;
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

            var text = "";

            foreach (var transition in this.GotoStateTransitions)
            {
                var onExitName = "";
                if (this.TransitionsOnExitActions.ContainsKey(transition.Key))
                {
                    onExitName = "psharp_" + this.Identifier.TextUnit.Text + "_" +
                        transition.Key.TextUnit.Text + "_action";
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

                text += ", typeof(" + transition.Value.TextUnit.Text + ")";

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

            var text = "";

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

                text += ", typeof(" + transition.Value.TextUnit.Text + ")";

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

            var text = "";

            foreach (var binding in this.ActionBindings)
            {
                var actionName = "";
                if (this.ActionHandlers.ContainsKey(binding.Key))
                {
                    actionName = "psharp_" + this.Identifier.TextUnit.Text + "_" +
                        binding.Key.TextUnit.Text + "_action";
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

            var text = "[IgnoreEvents(";

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

            var text = "[DeferEvents(";

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
