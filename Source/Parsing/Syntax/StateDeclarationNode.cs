//-----------------------------------------------------------------------
// <copyright file="StateDeclarationNode.cs">
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
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// State declaration node.
    /// </summary>
    public sealed class StateDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// True if the state is the initial state.
        /// </summary>
        public readonly bool IsInitial;

        /// <summary>
        /// The machine parent node.
        /// </summary>
        public readonly MachineDeclarationNode Machine;

        /// <summary>
        /// The state keyword.
        /// </summary>
        public Token StateKeyword;

        /// <summary>
        /// The modifier token.
        /// </summary>
        public Token Modifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        public Token LeftCurlyBracketToken;

        /// <summary>
        /// Entry declaration.
        /// </summary>
        public EntryDeclarationNode EntryDeclaration;

        /// <summary>
        /// Exit declaration.
        /// </summary>
        public ExitDeclarationNode ExitDeclaration;

        /// <summary>
        /// Dictionary containing state transitions.
        /// </summary>
        internal Dictionary<Token, Token> StateTransitions;

        /// <summary>
        /// Dictionary containing actions bindings.
        /// </summary>
        internal Dictionary<Token, Token> ActionBindings;

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
        public Token RightCurlyBracketToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="isInit">Is initial state</param>
        public StateDeclarationNode(MachineDeclarationNode machineNode, bool isInit)
        {
            this.IsInitial = isInit;
            this.Machine = machineNode;
            this.StateTransitions = new Dictionary<Token, Token>();
            this.ActionBindings = new Dictionary<Token, Token>();
            this.DeferredEvents = new HashSet<Token>();
            this.IgnoredEvents = new HashSet<Token>();
        }

        /// <summary>
        /// Adds a state transition.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="stateIdentifier">Token</param>
        /// <returns>Boolean value</returns>
        public bool AddStateTransition(Token eventIdentifier, Token stateIdentifier)
        {
            if (this.StateTransitions.ContainsKey(eventIdentifier) ||
                this.ActionBindings.ContainsKey(eventIdentifier))
            {
                return false;
            }

            this.StateTransitions.Add(eventIdentifier, stateIdentifier);

            return true;
        }

        /// <summary>
        /// Adds an action binding.
        /// </summary>
        /// <param name="eventIdentifier">Token</param>
        /// <param name="actionIdentifier">Token</param>
        /// <returns>Boolean value</returns>
        public bool AddActionBinding(Token eventIdentifier, Token actionIdentifier)
        {
            if (this.StateTransitions.ContainsKey(eventIdentifier) ||
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
        public bool AddDeferredEvent(Token eventIdentifier)
        {
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
        public bool AddIgnoredEvent(Token eventIdentifier)
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
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetFullText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetRewrittenText()
        {
            return base.RewrittenTextUnit.Text;
        }

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="position">Position</param>
        internal override void Rewrite(ref int position)
        {
            var start = position;

            if (this.EntryDeclaration != null)
            {
                this.EntryDeclaration.Rewrite(ref position);
            }

            if (this.ExitDeclaration != null)
            {
                this.ExitDeclaration.Rewrite(ref position);
            }

            var text = "";

            if (this.IsInitial)
            {
                text += "[Initial]\n";
            }

            if (this.Modifier != null)
            {
                base.RewrittenTokens.Add(this.Modifier);
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            var classKeyword = "class";
            var classTextUnit = new TextUnit(classKeyword, classKeyword.Length, text.Length);
            base.RewrittenTokens.Add(new Token(classTextUnit, this.StateKeyword.Line, TokenType.ClassDecl));
            text += classKeyword;
            text += " ";

            text += this.Identifier.TextUnit.Text;
            text += " ";

            base.RewrittenTokens.Add(new Token(new TextUnit(":", 1, text.Length), this.StateKeyword.Line, TokenType.Doublecolon));
            text += ":";
            text += " ";

            var stateClass = "State";
            var stateTextUnit = new TextUnit(stateClass, stateClass.Length, text.Length);
            base.RewrittenTokens.Add(new Token(stateTextUnit, this.StateKeyword.Line, TokenType.TypeIdentifier));
            text += stateClass;

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            base.RewrittenTokens.Add(this.LeftCurlyBracketToken);

            if (this.EntryDeclaration != null)
            {
                text += this.EntryDeclaration.GetRewrittenText();
                base.RewrittenTokens.AddRange(this.EntryDeclaration.RewrittenTokens);
            }

            if (this.ExitDeclaration != null)
            {
                text += this.ExitDeclaration.GetRewrittenText();
                base.RewrittenTokens.AddRange(this.ExitDeclaration.RewrittenTokens);
            }

            text += this.InstrumentDeferredEvents();
            text += this.InstrumentIgnoredEvents();

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";
            base.RewrittenTokens.Add(this.RightCurlyBracketToken);

            base.RewrittenTextUnit = new TextUnit(text, text.Length, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            if (this.EntryDeclaration != null)
            {
                this.EntryDeclaration.GenerateTextUnit();
            }

            if (this.ExitDeclaration != null)
            {
                this.ExitDeclaration.GenerateTextUnit();
            }

            var text = "";
            var initToken = this.StateKeyword;

            if (this.Modifier != null)
            {
                initToken = this.Modifier;
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            text += this.StateKeyword.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            if (this.EntryDeclaration != null)
            {
                text += this.EntryDeclaration.GetFullText();
            }

            if (this.ExitDeclaration != null)
            {
                text += this.ExitDeclaration.GetFullText();
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            int length = this.RightCurlyBracketToken.TextUnit.End - initToken.TextUnit.Start + 1;

            base.TextUnit = new TextUnit(text, length, initToken.TextUnit.Start);
        }

        #endregion

        #region private API

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
                text += "   typeof(" + eventIds[idx].TextUnit.Text + ")";
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
                text += "   typeof(" + eventIds[idx].TextUnit.Text + ")";
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
