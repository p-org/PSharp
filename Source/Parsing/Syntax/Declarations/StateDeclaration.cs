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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// State declaration syntax node.
    /// </summary>
    internal sealed class StateDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// True if the state is the initial state.
        /// </summary>
        internal readonly bool IsInitial;

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
        /// <param name="isInit">Is initial state</param>
        /// <param name="isModel">Is a model</param>
        internal StateDeclaration(IPSharpProgram program, MachineDeclaration machineNode,
            bool isInit, bool isModel)
            : base(program, isModel)
        {
            this.IsInitial = isInit;
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

            if (this.IsInitial)
            {
                text += "[Initial]\n";
            }

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

            if (!this.Machine.IsMonitor)
            {
                text += this.InstrumentDeferredEvents();
            }

            text += this.InstrumentIgnoredEvents();

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            return text;
        }

        /// <summary>
        /// Instruments the deferred events.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentDeferredEvents()
        {
            if (this.DeferredEvents.Count == 0)
            {
                return "";
            }

            var text = "\n";
            text += " protected override System.Collections.Generic.HashSet<Type> DefineDeferredEvents()\n";
            text += " {\n";
            text += "  return new System.Collections.Generic.HashSet<Type>\n";
            text += "  {\n";

            var eventIds = this.DeferredEvents.ToList();
            for (int idx = 0; idx < eventIds.Count; idx++)
            {
                if (eventIds[idx].Type == TokenType.HaltEvent)
                {
                    text += "   typeof(Microsoft.PSharp.Halt)";
                }
                else if (eventIds[idx].Type == TokenType.DefaultEvent)
                {
                    text += "   typeof(Microsoft.PSharp.Default)";
                }
                else
                {
                    text += "   typeof(" + eventIds[idx].TextUnit.Text + ")";
                }

                if (idx < eventIds.Count - 1)
                {
                    text += ",\n";
                }
                else
                {
                    text += "\n";
                }
            }

            text += "  };\n";
            text += " }\n";

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

            var text = "\n";
            text += " protected override System.Collections.Generic.HashSet<Type> DefineIgnoredEvents()\n";
            text += " {\n";
            text += "  return new System.Collections.Generic.HashSet<Type>\n";
            text += "  {\n";

            var eventIds = this.IgnoredEvents.ToList();
            for (int idx = 0; idx < eventIds.Count; idx++)
            {
                if (eventIds[idx].Type == TokenType.HaltEvent)
                {
                    text += "   typeof(Microsoft.PSharp.Halt)";
                }
                else if (eventIds[idx].Type == TokenType.DefaultEvent)
                {
                    text += "   typeof(Microsoft.PSharp.Default)";
                }
                else
                {
                    text += "   typeof(" + eventIds[idx].TextUnit.Text + ")";
                }

                if (idx < eventIds.Count - 1)
                {
                    text += ",\n";
                }
                else
                {
                    text += "\n";
                }
            }

            text += "  };\n";
            text += " }\n";

            return text;
        }

        #endregion
    }
}
