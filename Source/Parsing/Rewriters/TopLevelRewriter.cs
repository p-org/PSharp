//-----------------------------------------------------------------------
// <copyright file="TopLevelRewriter.cs">
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
using System.Text.RegularExpressions;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# top level declaration rewriter.
    /// </summary>
    internal class TopLevelRewriter : BaseRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public TopLevelRewriter(List<Token> tokens)
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
            else if (token.Type == TokenType.ActionDecl)
            {
                this.RewriteActionDeclaration();
            }
            else if (token.Type == TokenType.OnAction)
            {
                this.RewriteOnActionDeclaration();
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

            if (base.Tokens[base.Index].Type != TokenType.None)
            {
                base.ReportParsingFailure();
            }

            var identifier = base.Tokens[base.Index].String;

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
                base.ReportParsingFailure();
            }

            base.Index = replaceIdx;
            base.Index++;

            var eventBody = "\n";
            eventBody += "\t{\n";
            eventBody += "\t\tpublic " + identifier + "()\n";
            eventBody += "\t\t\t: base()\n";
            eventBody += "\t\t{ }\n";
            eventBody += "\n";
            eventBody += "\t\tpublic " + identifier + "(Object payload)\n";
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
            if (base.Tokens[base.Index].Type != TokenType.None)
            {
                base.ReportParsingFailure();
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                this.Tokens.Insert(replaceIdx, new Token("Machine"));

                base.Index = replaceIdx;
                base.Index++;
            }
            else if (base.Tokens[base.Index].Type != TokenType.Doublecolon)
            {
                base.ReportParsingFailure();
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
            if (base.Tokens[base.Index].Type != TokenType.None)
            {
                base.ReportParsingFailure();
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                this.Tokens.Insert(replaceIdx, new Token("State"));

                base.Index = replaceIdx;
                base.Index++;
            }
            else
            {
                base.ReportParsingFailure();
            }
        }

        /// <summary>
        /// Rewrites the action declaration.
        /// </summary>
        private void RewriteActionDeclaration()
        {
            base.Tokens[base.Index] = new Token("void", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type != TokenType.None)
            {
                base.ReportParsingFailure();
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
                base.Index++;
            }
            else
            {
                base.ReportParsingFailure();
            }
        }

        /// <summary>
        /// Rewrites the on action declaration.
        /// </summary>
        private void RewriteOnActionDeclaration()
        {
            var startIdx = base.Index;
            base.Tokens[base.Index] = new Token("protected", TokenType.Private);
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.Entry ||
                base.Tokens[base.Index].Type == TokenType.Exit)
            {
                this.RewriteStateActionDeclaration(base.Tokens[base.Index].Type);
            }
            else
            {
                throw new NotImplementedException("on action");
            }
        }

        /// <summary>
        /// Rewrites the on action declaration.
        /// </summary>
        /// <param name="type">TokenType</param>
        private void RewriteStateActionDeclaration(TokenType type)
        {
            if (type != TokenType.Entry && type != TokenType.Exit)
            {
                base.ReportParsingFailure();
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

            while (base.Tokens[base.Index].Type != TokenType.DoAction)
            {
                base.Tokens.RemoveAt(base.Index);
            }

            base.Tokens.RemoveAt(base.Index);
        }

        #endregion
    }
}
