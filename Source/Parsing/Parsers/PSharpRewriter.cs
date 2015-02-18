//-----------------------------------------------------------------------
// <copyright file="PSharpRewriter.cs">
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

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# rewriter.
    /// </summary>
    internal class PSharpRewriter : BaseParser
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public PSharpRewriter(List<Token> tokens)
            : base(tokens)
        {
            
        }

        #endregion

        #region protected API

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected override void ParseNextToken()
        {
            if (base.Index == base.Tokens.Count)
            {
                return;
            }

            var token = base.Tokens[base.Index];
            if (token.Type == TokenType.EventDecl)
            {
                this.RewriteEventDeclaration();
            }
            else if (token.Type == TokenType.MachineDecl)
            {
                this.RewriteMachineDeclaration();
            }
            else if (token.Type == TokenType.StateDecl)
            {
                this.RewriteStateDeclaration();
            }
            else if ((token.Type == TokenType.Entry) || (token.Type == TokenType.Exit))
            {
                this.RewriteStateActionDeclaration();
            }
            else if (token.Type == TokenType.ActionDecl)
            {
                this.RewriteActionDeclaration();
            }
            else if (token.Type == TokenType.CreateMachine)
            {
                this.RewriteCreateStatement();
            }
            else if (token.Type == TokenType.RaiseEvent)
            {
                this.RewriteRaiseStatement();
            }
            else if (token.Type == TokenType.SendEvent)
            {
                this.RewriteSendStatement();
            }
            else if (token.Type == TokenType.DeleteMachine)
            {
                this.RewriteDeleteStatement();
            }
            else if (token.Type == TokenType.Assert)
            {
                this.RewriteAssertStatement();
            }
            else if (token.Type == TokenType.Payload)
            {
                this.RewritePayload();
            }
            else if (token.Type == TokenType.This)
            {
                this.RewriteThis();
            }
            else if (token.Type == TokenType.Identifier)
            {
                this.RewriteIdentifier();
            }
            else if (token.Type == TokenType.MachineRightCurlyBracket)
            {
                this.InstrumentTransitionsAndActionsBindings();
            }
            else if (token.Type == TokenType.StateRightCurlyBracket)
            {
                this.InstrumentDeferredEvents();
                this.InstrumentIgnoredEvents();
                base.CurrentState = "";
            }

            base.Index++;
            this.ParseNextToken();
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the event declaration.
        /// </summary>
        private void RewriteEventDeclaration()
        {
            base.Tokens[base.Index] = new Token("class", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type != TokenType.EventIdentifier)
            {
                throw new ParsingException("parser: event identifier expected.");
            }

            var identifier = base.Tokens[base.Index].Text;

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type == TokenType.Semicolon)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token("Event"));
            }
            else
            {
                throw new ParsingException("parser: semicolon expected.");
            }

            base.Index = replaceIdx;
            base.Index++;

            var eventBody = "\n";
            eventBody += "\t{\n";
            eventBody += "\t\tpublic " + identifier + "(params Object[] payload)\n";
            eventBody += "\t\t\t: base(payload)\n";
            eventBody += "\t\t{ }\n";
            eventBody += "\t}";

            base.Tokens.Insert(base.Index, new Token(eventBody));
        }

        /// <summary>
        /// Rewrites the machine declaration.
        /// </summary>
        private void RewriteMachineDeclaration()
        {
            base.Tokens[base.Index] = new Token("class", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.MachineIdentifier)
            {
                base.CurrentMachine = base.Tokens[base.Index].Text;
            }
            else
            {
                throw new ParsingException("parser: machine identifier expected.");
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.MachineLeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                this.Tokens.Insert(replaceIdx, new Token("Machine"));

                base.Index = replaceIdx;
            }
            else if (base.Tokens[base.Index].Type != TokenType.Doublecolon)
            {
                throw new ParsingException("parser: doublecolon expected.");
            }
        }

        /// <summary>
        /// Rewrites the state declaration.
        /// </summary>
        private void RewriteStateDeclaration()
        {
            base.Tokens[base.Index] = new Token("class", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.StateIdentifier)
            {
                base.CurrentState = base.Tokens[base.Index].Text;
            }
            else
            {
                throw new ParsingException("parser: state identifier expected.");
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.StateLeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                this.Tokens.Insert(replaceIdx, new Token("State"));

                base.Index = replaceIdx;
            }
            else
            {
                throw new ParsingException("parser: left curly bracket expected.");
            }
        }

        /// <summary>
        /// Rewrites the state action declaration.
        /// </summary>
        private void RewriteStateActionDeclaration()
        {
            base.Tokens.Insert(base.Index, new Token("protected", TokenType.Protected));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(" ", TokenType.WhiteSpace));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("override", TokenType.Override));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(" ", TokenType.WhiteSpace));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("void"));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(" ", TokenType.WhiteSpace));
            base.Index++;

            if (base.Tokens[base.Index].Type == TokenType.Entry)
            {
                base.Tokens[base.Index] = new Token("OnEntry");
            }
            else if (base.Tokens[base.Index].Type == TokenType.Exit)
            {
                base.Tokens[base.Index] = new Token("OnExit");
            }

            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(")", TokenType.RightParenthesis));
            base.Index++;
        }

        /// <summary>
        /// Rewrites the action declaration.
        /// </summary>
        private void RewriteActionDeclaration()
        {
            base.Tokens[base.Index] = new Token("void", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type != TokenType.ActionIdentifier)
            {
                throw new ParsingException("parser: action identifier expected.");
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token("(", TokenType.LeftParenthesis));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(")", TokenType.RightParenthesis));
                replaceIdx++;

                base.Index = replaceIdx;
            }
            else
            {
                throw new ParsingException("parser: left curly bracket expected.");
            }
        }

        /// <summary>
        /// Rewrites the create statement.
        /// </summary>
        private void RewriteCreateStatement()
        {
            base.Tokens[base.Index] = new Token("Machine");
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(".", TokenType.Dot));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("Factory"));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(".", TokenType.Dot));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("CreateMachine"));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("<", TokenType.LessThanOperator));
            base.Index++;

            var startIdx = base.Index;
            var machineId = "";
            var payload = new List<List<Token>>();

            base.SkipWhiteSpaceTokens();
            machineId = base.Tokens[base.Index].Text;
            base.Index++;

            while (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                base.Index++;
            }

            base.Index++;
            base.SkipWhiteSpaceTokens();
            while (base.Tokens[base.Index].Type != TokenType.RightCurlyBracket)
            {
                if (payload.Count == 0)
                {
                    payload.Add(new List<Token>());
                }

                if (base.Tokens[base.Index].Type == TokenType.Comma)
                {
                    payload.Add(new List<Token>());
                }

                payload[payload.Count - 1].Add(base.Tokens[base.Index]);
                base.Index++;
            }

            while (base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                base.Index++;
            }

            base.Index--;
            while (base.Index != startIdx)
            {
                base.Tokens.RemoveAt(base.Index);
                base.Index--;
            }

            base.Tokens.Insert(base.Index, new Token(machineId, TokenType.EventIdentifier));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(">", TokenType.GreaterThanOperator));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            for (int idx = 0; idx < payload.Count; idx++)
            {
                foreach (var tok in payload[idx])
                {
                    base.Tokens.Insert(base.Index, tok);
                    base.Index++;
                }

                if (idx < payload.Count - 1)
                {
                    base.Tokens.Insert(base.Index, new Token(",", TokenType.Comma));
                    base.Index++;
                }
            }

            base.Tokens[base.Index] = new Token(")", TokenType.RightParenthesis);
            base.Index = startIdx - 1;
        }

        /// <summary>
        /// Rewrites the raise statement.
        /// </summary>
        private void RewriteRaiseStatement()
        {
            base.Tokens[base.Index] = new Token("this", TokenType.This);
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(".", TokenType.Dot));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("Raise"));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("new", TokenType.New));
            base.Index++;

            base.SkipWhiteSpaceTokens();
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(")", TokenType.RightParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(")", TokenType.RightParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(";", TokenType.Semicolon));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("return", TokenType.Return));
        }

        /// <summary>
        /// Rewrites the send statement.
        /// </summary>
        private void RewriteSendStatement()
        {
            base.Tokens[base.Index] = new Token("this", TokenType.This);
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(".", TokenType.Dot));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("Send"));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            var startIdx = base.Index;
            var eventId = "";
            var machineIds = new List<Token>();
            var payload = new List<Token>();

            base.SkipWhiteSpaceTokens();

            eventId = base.Tokens[base.Index].Text;

            base.Index++;
            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                base.Index++;
                while (base.Tokens[base.Index].Type != TokenType.RightCurlyBracket)
                {
                    payload.Add(base.Tokens[base.Index]);
                    base.Index++;
                }

                base.Index++;
                base.SkipWhiteSpaceTokens();
            }
           
            base.Index++;
            base.SkipWhiteSpaceTokens();

            while (base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                machineIds.Add(base.Tokens[base.Index]);
                base.Index++;
            }

            base.Index--;
            while (base.Index != startIdx)
            {
                base.Tokens.RemoveAt(base.Index);
                base.Index--;
            }

            foreach (var id in machineIds)
            {
                base.Tokens.Insert(base.Index, id);
                base.Index++;
            }

            base.Tokens.Insert(base.Index, new Token(",", TokenType.Comma));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(" ", TokenType.WhiteSpace));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("new", TokenType.New));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(" ", TokenType.WhiteSpace));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(eventId, TokenType.EventIdentifier));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            foreach (var item in payload)
            {
                base.Tokens.Insert(base.Index, item);
                base.Index++;
            }

            base.Tokens.Insert(base.Index, new Token(")", TokenType.RightParenthesis));
            base.Index++;

            base.Tokens[base.Index] = new Token(")", TokenType.RightParenthesis);
            base.Index = startIdx - 1;
        }

        /// <summary>
        /// Rewrites the delete statement.
        /// </summary>
        private void RewriteDeleteStatement()
        {
            base.Tokens[base.Index] = new Token("this", TokenType.This);
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(".", TokenType.Dot));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("Delete"));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(")", TokenType.RightParenthesis));
            base.Index++;

            base.SkipWhiteSpaceTokens();
        }

        /// <summary>
        /// Rewrites the assert statement.
        /// </summary>
        private void RewriteAssertStatement()
        {
            base.Tokens[base.Index] = new Token("this", TokenType.This);
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(".", TokenType.Dot));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("Assert"));
            base.Index++;
        }

        /// <summary>
        /// Rewrites the payload.
        /// </summary>
        private void RewritePayload()
        {
            base.Tokens[base.Index] = new Token("this", TokenType.This);
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(".", TokenType.Dot));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("Payload"));
        }

        /// <summary>
        /// Rewrites the this.
        /// </summary>
        private void RewriteThis()
        {
            if (base.CurrentState.Equals(""))
            {
                return;
            }

            var replaceIdx = base.Index;
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.Dot)
            {
                base.Tokens.RemoveAt(base.Index);
                base.Tokens.RemoveAt(replaceIdx);
                base.Index = replaceIdx - 1;
            }
            else
            {
                base.Tokens.Insert(base.Index, new Token(".", TokenType.Dot));
                base.Index++;

                base.Tokens.Insert(base.Index, new Token("Machine", TokenType.Identifier));
            }
        }

        /// <summary>
        /// Rewrites the identifier.
        /// </summary>
        private void RewriteIdentifier()
        {
            if (base.CurrentState.Equals("") ||
                !ParsingEngine.MachineFieldsAndMethods.ContainsKey(base.CurrentMachine) ||
                !ParsingEngine.MachineFieldsAndMethods[base.CurrentMachine].Contains(base.Tokens[base.Index].Text))
            {
                return;
            }

            var replaceIdx = base.Index;
            base.Tokens.Insert(replaceIdx, new Token("(", TokenType.LeftParenthesis));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token("this", TokenType.This));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(".", TokenType.Dot));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token("Machine", TokenType.Identifier));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token("as", TokenType.As));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(base.CurrentMachine, TokenType.Identifier));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(")", TokenType.RightParenthesis));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(".", TokenType.Dot));
            replaceIdx++;

            base.Index = replaceIdx;
        }

        /// <summary>
        /// Instruments the state transitions and action bindings.
        /// </summary>
        private void InstrumentTransitionsAndActionsBindings()
        {
            if (!ParsingEngine.StateActions.ContainsKey(base.CurrentMachine))
            {
                return;
            }

            var stateFunc = "\n";
            stateFunc += "\tprotected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()\n";
            stateFunc += "\t{\n";
            stateFunc += "\t\tvar dict = new Dictionary<Type, StepStateTransitions>();\n";
            stateFunc += "\n";

            bool isEmpty = true;
            foreach (var state in ParsingEngine.StateActions[base.CurrentMachine])
            {
                stateFunc += "\t\tvar " + state.Key.ToLower() + "Dict = new StepStateTransitions();\n";

                foreach (var pair in state.Value.Where(val => val.Value.Item2 == ActionType.Goto))
                {
                    stateFunc += "\t\t" + state.Key.ToLower() + "Dict.Add(typeof(" + pair.Key +
                        "), typeof(" + pair.Value.Item1 + "));\n";
                    isEmpty = false;
                }

                stateFunc += "\t\tdict.Add(typeof(" + state.Key + "), " + state.Key.ToLower() + "Dict);\n";
                stateFunc += "\n";
            }

            stateFunc += "\t\treturn dict;\n";
            stateFunc += "\t}\n";

            if (!isEmpty)
            {
                base.Tokens.Insert(base.Index, new Token(stateFunc));
                base.Index++;
            }

            var actionFunc = "\n";
            actionFunc += "\tprotected override Dictionary<Type, ActionBindings> DefineActionBindings()\n";
            actionFunc += "\t{\n";
            actionFunc += "\t\tvar dict = new Dictionary<Type, ActionBindings>();\n";
            actionFunc += "\n";

            isEmpty = true;
            foreach (var state in ParsingEngine.StateActions[base.CurrentMachine])
            {
                actionFunc += "\t\tvar " + state.Key.ToLower() + "Dict = new ActionBindings();\n";

                foreach (var pair in state.Value.Where(val => val.Value.Item2 == ActionType.Do))
                {
                    actionFunc += "\t\t" + state.Key.ToLower() + "Dict.Add(typeof(" + pair.Key +
                        "), new Action(" + pair.Value.Item1 + "));\n";
                    isEmpty = false;
                }

                actionFunc += "\t\tdict.Add(typeof(" + state.Key + "), " + state.Key.ToLower() + "Dict);\n";
                actionFunc += "\n";
            }

            actionFunc += "\t\treturn dict;\n";
            actionFunc += "\t}\n";

            if (!isEmpty)
            {
                base.Tokens.Insert(base.Index, new Token(actionFunc));
                base.Index++;
            }
        }

        /// <summary>
        /// Instruments the deferred events.
        /// </summary>
        private void InstrumentDeferredEvents()
        {
            if (!ParsingEngine.DeferredEvents.ContainsKey(base.CurrentMachine) ||
                !ParsingEngine.DeferredEvents[base.CurrentMachine].ContainsKey(base.CurrentState))
            {
                return;
            }

            var func = "\n";
            func += "\tprotected override HashSet<Type> DefineDeferredEvents()\n";
            func += "\t{\n";
            func += "\t\treturn new HashSet<Type>\n";
            func += "\t\t{\n";

            var eventIds = ParsingEngine.DeferredEvents[base.CurrentMachine][base.CurrentState].ToList();
            for (int idx = 0; idx < eventIds.Count; idx++)
            {
                func += "\t\t\ttypeof(" + eventIds[idx] + ")";
                if (idx < eventIds.Count - 1)
                {
                    func += ",\n";
                }
                else
                {
                    func += "\n";
                }
            }

            func += "\t\t};\n";
            func += "\t}\n";

            if (eventIds.Count > 0)
            {
                base.Tokens.Insert(base.Index, new Token(func));
                base.Index++;
            }
        }

        /// <summary>
        /// Instruments the ignored events.
        /// </summary>
        private void InstrumentIgnoredEvents()
        {
            if (!ParsingEngine.IgnoredEvents.ContainsKey(base.CurrentMachine) ||
                !ParsingEngine.IgnoredEvents[base.CurrentMachine].ContainsKey(base.CurrentState))
            {
                return;
            }

            var func = "\n";
            func += "\tprotected override HashSet<Type> DefineIgnoredEvents()\n";
            func += "\t{\n";
            func += "\t\treturn new HashSet<Type>\n";
            func += "\t\t{\n";

            var eventIds = ParsingEngine.IgnoredEvents[base.CurrentMachine][base.CurrentState].ToList();
            for (int idx = 0; idx < eventIds.Count; idx++)
            {
                func += "\t\t\ttypeof(" + eventIds[idx] + ")";
                if (idx < eventIds.Count - 1)
                {
                    func += ",\n";
                }
                else
                {
                    func += "\n";
                }
            }

            func += "\t\t};\n";
            func += "\t}\n";

            if (eventIds.Count > 0)
            {
                base.Tokens.Insert(base.Index, new Token(func));
                base.Index++;
            }
        }

        #endregion
    }
}
