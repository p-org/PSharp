//-----------------------------------------------------------------------
// <copyright file="PSharpParser.cs">
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

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# parser.
    /// </summary>
    internal class PSharpParser : BaseParser
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public PSharpParser(List<Token> tokens)
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
                this.ExtractStateActionDeclaration();
            }
            else if (token.Type == TokenType.DeferEvent)
            {
                this.ExtractDeferEventDeclaration();
            }
            else if (token.Type == TokenType.IgnoreEvent)
            {
                this.ExtractIgnoreEventDeclaration();
            }

            base.Index++;
            this.ParseNextToken();
        }

        #endregion

        #region private API

        /// <summary>
        /// Extracts the machine declaration.
        /// </summary>
        private void ExtractMachineDeclaration()
        {
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.MachineIdentifier)
            {
                base.CurrentMachine = base.Tokens[base.Index].Text;
            }
            else
            {
                throw new ParsingException("machine identifier expected.");
            }
        }

        /// <summary>
        /// Extracts the state declaration.
        /// </summary>
        private void ExtractStateDeclaration()
        {
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.StateIdentifier)
            {
                base.CurrentState = base.Tokens[base.Index].Text;
            }
            else
            {
                throw new ParsingException("state identifier expected.");
            }
        }

        /// <summary>
        /// Extracts the state action declaration.
        /// </summary>
        private void ExtractStateActionDeclaration()
        {
            var type = GetActionType();
            if (type == ActionType.Do || type == ActionType.Goto)
            {
                this.EraseStatement();
            }
            else if (type == ActionType.None)
            {
                throw new ParsingException("no action type identified.");
            }
        }

        /// <summary>
        /// Extracts the defer event declaration.
        /// </summary>
        private void ExtractDeferEventDeclaration()
        {
            var startIdx = base.Index;
            base.Index++;
            base.SkipWhiteSpaceTokens();

            if (!ParsingEngine.DeferredEvents.ContainsKey(base.CurrentMachine))
            {
                ParsingEngine.DeferredEvents.Add(base.CurrentMachine,
                    new Dictionary<string, HashSet<string>>());
            }

            if (!ParsingEngine.DeferredEvents[base.CurrentMachine].ContainsKey(base.CurrentState))
            {
                ParsingEngine.DeferredEvents[base.CurrentMachine].Add(base.CurrentState,
                    new HashSet<string>());
            }

            var eventIds = new List<string>();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type == TokenType.Comma)
                {
                    eventIds.Add("");
                }
                else if (eventIds.Count == 0)
                {
                    eventIds.Add(base.Tokens[base.Index].Text);
                }
                else
                {
                    eventIds[eventIds.Count - 1] += base.Tokens[base.Index].Text;
                }

                base.Index++;
            }

            base.Index = startIdx;

            foreach (var eventId in eventIds)
            {
                if (ParsingEngine.DeferredEvents[base.CurrentMachine][base.CurrentState].Contains(eventId))
                {
                    ErrorReporter.ReportErrorAndExit("State '{0}' in machine '{1}' already defers " +
                        "event '{2}'", base.CurrentState, base.CurrentMachine, eventId);
                }
                else
                {
                    ParsingEngine.DeferredEvents[base.CurrentMachine][base.CurrentState].Add(eventId);
                }
            }

            this.EraseStatement();
        }

        /// <summary>
        /// Extracts the ignore event declaration.
        /// </summary>
        private void ExtractIgnoreEventDeclaration()
        {
            var startIdx = base.Index;
            base.Index++;
            base.SkipWhiteSpaceTokens();

            if (!ParsingEngine.IgnoredEvents.ContainsKey(base.CurrentMachine))
            {
                ParsingEngine.IgnoredEvents.Add(base.CurrentMachine,
                    new Dictionary<string, HashSet<string>>());
            }

            if (!ParsingEngine.IgnoredEvents[base.CurrentMachine].ContainsKey(base.CurrentState))
            {
                ParsingEngine.IgnoredEvents[base.CurrentMachine].Add(base.CurrentState,
                    new HashSet<string>());
            }

            var eventIds = new List<string>();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type == TokenType.Comma)
                {
                    eventIds.Add("");
                }
                else if (eventIds.Count == 0)
                {
                    eventIds.Add(base.Tokens[base.Index].Text);
                }
                else
                {
                    eventIds[eventIds.Count - 1] += base.Tokens[base.Index].Text;
                }

                base.Index++;
            }

            base.Index = startIdx;

            foreach (var eventId in eventIds)
            {
                if (ParsingEngine.IgnoredEvents[base.CurrentMachine][base.CurrentState].Contains(eventId))
                {
                    ErrorReporter.ReportErrorAndExit("State '{0}' in machine '{1}' already ignores " +
                        "event '{2}'", base.CurrentState, base.CurrentMachine, eventId);
                }
                else
                {
                    ParsingEngine.IgnoredEvents[base.CurrentMachine][base.CurrentState].Add(eventId);
                }
            }

            this.EraseStatement();
        }

        /// <summary>
        /// Erases a statement.
        /// </summary>
        private void EraseStatement()
        {
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                base.Tokens.RemoveAt(base.Index);
            }

            base.Tokens.RemoveAt(base.Index);
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

            if (base.Tokens[base.Index].Type == TokenType.EventIdentifier)
            {
                eventId = base.Tokens[base.Index].Text;
            }
            else
            {
                throw new ParsingException("event identifier expected.");
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

            if (base.Tokens[base.Index].Type == TokenType.ActionIdentifier && type == ActionType.Do ||
                base.Tokens[base.Index].Type == TokenType.StateIdentifier && type == ActionType.Goto)
            {
                ParsingEngine.StateActions[base.CurrentMachine][base.CurrentState].Add(
                    eventId, new Tuple<string, ActionType>(base.Tokens[base.Index].Text, type));
            }
            else
            {
                throw new ParsingException("action or state identifier expected.");
            }

            base.Index =  startIdx;
            return type;
        }

        #endregion
    }
}
