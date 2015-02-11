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
        /// Constructor
        /// </summary>
        /// <param name="text">Text</param>
        public TopLevelRewriter(string text)
            : base(text)
        {
            
        }

        #endregion

        #region protected API

        /// <summary>
        /// Parses the next available line.
        /// </summary>
        protected override void ParseNextLine()
        {
            if (base.LineIndex == base.Lines.Count)
            {
                return;
            }

            var split = Regex.Split(base.Lines[base.LineIndex], @"(//)|(/\*)|(\*/)|(;)|({)|(})|(:)|(\()|" +
                @"(\))|(\[)|(\])|(machine)|(state)|(event)|(private)|(protected)|(internal)|(\s+)");

            if (split.Length > 1)
            {
                this.ParseSplitLine(split);
            }

            base.LineIndex++;
            this.ParseNextLine();
        }

        #endregion

        #region private API

        /// <summary>
        /// Parses the split line.
        /// </summary>
        private void ParseSplitLine(string[] split)
        {
            var tokens = new List<Token>();

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].Equals("//"))
                {
                    while (i < split.Length)
                    {
                        tokens.Add(new Token(split[i]));
                        i++;
                    }
                }
                else if (split[i].Equals("/*"))
                {
                    tokens.Add(new Token(split[i]));
                    while (split[i].Equals("*/"))
                    {
                        i++;
                        tokens.Add(new Token(split[i]));
                    }
                }
                else if (split[i].Equals("machine"))
                {
                    tokens.Add(new Token(split[i], TokenType.Machine));
                }
                else if (split[i].Equals("state"))
                {
                    tokens.Add(new Token(split[i], TokenType.State));
                }
                else if (split[i].Equals("event"))
                {
                    tokens.Add(new Token(split[i], TokenType.Event));
                }
                else if (split[i].Equals("private"))
                {
                    tokens.Add(new Token(split[i], TokenType.Private));
                }
                else if (split[i].Equals("protected"))
                {
                    tokens.Add(new Token(split[i], TokenType.Protected));
                }
                else if (split[i].Equals("internal"))
                {
                    tokens.Add(new Token(split[i], TokenType.Internal));
                }
                else if (split[i].Equals(";"))
                {
                    tokens.Add(new Token(split[i], TokenType.Semicolon));
                }
                else if (split[i].Equals(":"))
                {
                    tokens.Add(new Token(split[i], TokenType.Doublecolon));
                }
                else if (split[i].Equals("{"))
                {
                    tokens.Add(new Token(split[i], TokenType.LeftCurlyBracket));
                }
                else if (split[i].Equals("}"))
                {
                    tokens.Add(new Token(split[i], TokenType.RightCurlyBracket));
                }
                else if (split[i].Equals("("))
                {
                    tokens.Add(new Token(split[i], TokenType.LeftParenthesis));
                }
                else if (split[i].Equals(")"))
                {
                    tokens.Add(new Token(split[i], TokenType.RightParenthesis));
                }
                else if (split[i].Equals("["))
                {
                    tokens.Add(new Token(split[i], TokenType.LeftSquareBracket));
                }
                else if (split[i].Equals("]"))
                {
                    tokens.Add(new Token(split[i], TokenType.RightSquareBracket));
                }
                else if (string.IsNullOrWhiteSpace(split[i]))
                {
                    tokens.Add(new Token(split[i], TokenType.WhiteSpace));
                }
                else
                {
                    tokens.Add(new Token(split[i]));
                }
            }

            for (int idx = 0; idx < tokens.Count; idx++)
            {
                if (tokens[idx].Type == TokenType.Machine)
                {
                    this.RewriteMachine(tokens, idx);
                }
                else if (tokens[idx].Type == TokenType.State)
                {

                }
                else if (tokens[idx].Type == TokenType.Event)
                {

                }
            }

            var parsedLine = "";
            foreach (var token in tokens)
            {
                parsedLine += token.String;
            }

            this.Lines[this.LineIndex] = parsedLine;
        }

        /// <summary>
        /// Rewrite the token in the given index to a machine
        /// declaration.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <param name="idx">Index of machine token</param>
        private void RewriteMachine(List<Token> tokens, int idx)
        {
            var nonEmptyTokens = tokens.FindAll(val => val.Type != TokenType.WhiteSpace);

            string error = "Incorrect machine declaration in line " + base.LineIndex + ":\n";
            error += base.Lines[base.LineIndex];

            if ((nonEmptyTokens.Count == 3 || nonEmptyTokens.Count == 5) &&
                nonEmptyTokens[1].Equals(tokens[idx]))
            {
                if (nonEmptyTokens[0].Type != TokenType.Private &&
                    nonEmptyTokens[0].Type != TokenType.Protected &&
                    nonEmptyTokens[0].Type != TokenType.Internal)
                {
                    ErrorReporter.ReportErrorAndExit(error);
                }

                tokens[idx] = new Token("class");

                if (nonEmptyTokens.Count == 3)
                {
                    tokens.Add(new Token(" : Machine"));
                }
            }
            else
            {
                ErrorReporter.ReportErrorAndExit(error);
            }
        }

        #endregion
    }
}
