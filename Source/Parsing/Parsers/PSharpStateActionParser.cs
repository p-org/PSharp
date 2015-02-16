//-----------------------------------------------------------------------
// <copyright file="PSharpStateActionParser.cs">
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

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# state action parser.
    /// </summary>
    internal class PSharpStateActionParser : BaseParser
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public PSharpStateActionParser(List<Token> tokens)
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
            if (token.Type == TokenType.MachineDecl)
            {
                this.ExtractMachineDeclaration();
            }
            else if (token.Type == TokenType.StateDecl)
            {
                this.ExtractStateDeclaration();
            }
            else if (token.Type == TokenType.OnAction)
            {
                this.RewriteStateActionDeclaration();
            }
            else if (token.Type == TokenType.MachineRightCurlyBracket)
            {
                this.InstrumentStateActions();
            }

            base.Index++;
            this.ParseNextToken();
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the machine declaration.
        /// </summary>
        private void ExtractMachineDeclaration()
        {
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.None)
            {
                base.CurrentMachine = base.Tokens[base.Index].String;
            }
            else
            {
                throw new ParsingException("parser: machine identifier expected.");
            }

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.MachineLeftCurlyBracket)
            {
                base.Index++;
            }

            base.Index++;
        }

        /// <summary>
        /// Rewrites the state declaration.
        /// </summary>
        private void ExtractStateDeclaration()
        {
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.None)
            {
                base.CurrentState = base.Tokens[base.Index].String;
            }
            else
            {
                throw new ParsingException("parser: state identifier expected.");
            }
        }

        /// <summary>
        /// Rewrites the state action declaration.
        /// </summary>
        private void RewriteStateActionDeclaration()
        {
            var type = GetActionType();
            if (type == ActionType.OnEntry || type == ActionType.OnExit)
            {
                base.Tokens[base.Index] = new Token("protected", TokenType.Private);
                base.Index++;

                base.SkipWhiteSpaceTokens();
                this.RewriteOnActionDeclaration(base.Tokens[base.Index].Type);
            }
            else if (type == ActionType.Do || type == ActionType.Goto)
            {
                this.EraseOnActionDeclaration();
            }
            if (type == ActionType.None)
            {
                throw new ParsingException("parser: no action type identified.");
            }
        }

        /// <summary>
        /// Rewrites the on action declaration.
        /// </summary>
        /// <param name="type">TokenType</param>
        private void RewriteOnActionDeclaration(TokenType type)
        {
            if (type != TokenType.Entry && type != TokenType.Exit)
            {
                throw new ParsingException("parser: expected entry or exit on action type.");
            }

            var replaceIdx = base.Index;

            base.Tokens[replaceIdx] = new Token("override", TokenType.Override);
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token("void"));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
            replaceIdx++;

            if (type == TokenType.Entry)
            {
                base.Tokens.Insert(replaceIdx, new Token("OnEntry"));
                replaceIdx++;
            }
            else if (type == TokenType.Exit)
            {
                base.Tokens.Insert(replaceIdx, new Token("OnExit"));
                replaceIdx++;
            }

            base.Tokens.Insert(replaceIdx, new Token("(", TokenType.LeftParenthesis));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(")", TokenType.RightParenthesis));
            replaceIdx++;

            base.Index = replaceIdx;
            base.Index++;

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.DoAction)
            {
                base.Tokens.RemoveAt(base.Index);
            }

            base.Tokens.RemoveAt(base.Index);
        }

        /// <summary>
        /// Erases the on action declaration.
        /// </summary>
        private void EraseOnActionDeclaration()
        {
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                base.Tokens.RemoveAt(base.Index);
            }

            base.Tokens.RemoveAt(base.Index);
        }

        /// <summary>
        /// Instruments the state actions.
        /// </summary>
        private void InstrumentStateActions()
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

        #endregion

        #region helper methods

        /// <summary>
        /// Returns the action type of the current state action declaration.
        /// </summary>
        /// <returns>ActionType</returns>
        private ActionType GetActionType()
        {
            var type = ActionType.None;
            var startIdx = base.Index;
            var eventId = "";

            base.Index++;
            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type == TokenType.Entry)
            {
                base.Index = startIdx;
                return ActionType.OnEntry;
            }
            else if (base.Tokens[base.Index].Type == TokenType.Exit)
            {
                base.Index = startIdx;
                return ActionType.OnExit;
            }
            else if (base.Tokens[base.Index].Type == TokenType.None)
            {
                eventId = base.Tokens[base.Index].String;
            }
            else
            {
                throw new ParsingException("parser: no action type identified.");
            }

            base.Index++;
            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type == TokenType.DoAction)
            {
                type = ActionType.Do;
            }
            else if (base.Tokens[base.Index].Type == TokenType.GotoState)
            {
                type = ActionType.Goto;
            }

            base.Index++;
            base.SkipWhiteSpaceTokens();

            if (!ParsingEngine.StateActions.ContainsKey(base.CurrentMachine))
            {
                ParsingEngine.StateActions.Add(base.CurrentMachine,
                    new Dictionary<string, Dictionary<string, Tuple<string, ActionType>>>());
            }

            if (!ParsingEngine.StateActions[base.CurrentMachine].ContainsKey(base.CurrentState))
            {
                ParsingEngine.StateActions[base.CurrentMachine].Add(base.CurrentState,
                    new Dictionary<string, Tuple<string, ActionType>>());
            }

            if (ParsingEngine.StateActions[base.CurrentMachine][base.CurrentState].ContainsKey(eventId))
            {
                ErrorReporter.ReportErrorAndExit("State '{0}' in machine '{1}' already contains an action " +
                    "on event '{2}'", base.CurrentState, base.CurrentMachine, eventId);
            }

            if (base.Tokens[base.Index].Type == TokenType.None && type == ActionType.Do ||
                base.Tokens[base.Index].Type == TokenType.None && type == ActionType.Goto)
            {
                ParsingEngine.StateActions[base.CurrentMachine][base.CurrentState].Add(
                    eventId, new Tuple<string, ActionType>(base.Tokens[base.Index].String, type));
            }
            else
            {
                throw new ParsingException("parser: identifier expected.");
            }

            base.Index =  startIdx;
            return type;
        }

        #endregion
    }
}
