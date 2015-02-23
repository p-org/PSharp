//-----------------------------------------------------------------------
// <copyright file="PSharpLexer.cs">
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
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# lexer.
    /// </summary>
    public class PSharpLexer : ILexer
    {
        #region fields

        /// <summary>
        /// List of tokens.
        /// </summary>
        protected List<Token> Tokens;

        /// <summary>
        /// List of text units to be tokenized.
        /// </summary>
        protected List<TextUnit> TextUnits;

        /// <summary>
        /// The current line index.
        /// </summary>
        protected int LineIndex;

        /// <summary>
        /// The current index.
        /// </summary>
        protected int Index;

        #endregion

        #region public API

        /// <summary>
        /// Tokenizes the given text.
        /// </summary>
        /// <param name="text">Text to tokenize</param>
        /// <returns>List of tokens</returns>
        public List<Token> Tokenize(string text)
        {
            if (text.Length == 0)
            {
                return new List<Token>();
            }

            this.Tokens = new List<Token>();
            this.TextUnits = new List<TextUnit>();
            this.LineIndex = 1;
            this.Index = 0;

            using (StringReader sr = new StringReader(text))
            {
                string lineText;
                while ((lineText = sr.ReadLine()) != null)
                {
                    var split = this.SplitText(lineText);
                    foreach (var tok in split)
                    {
                        if (tok.Equals(""))
                        {
                            continue;
                        }

                        this.TextUnits.Add(new TextUnit(tok));
                    }

                    this.TextUnits.Add(new TextUnit("\n"));
                }
            }

            this.TokenizeNext();

            return this.Tokens;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Splits the given text using a regex pattern and returns the split text.
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Tokenized text</returns>
        protected string[] SplitText(string text)
        {
            return Regex.Split(text, this.GetPattern());
        }

        /// <summary>
        /// Tokenizes the next text units.
        /// </summary>
        /// <param name="textUnits">Text units</param>
        protected void TokenizeNext()
        {
            if (this.Index == this.TextUnits.Count)
            {
                return;
            }

            var token = this.TextUnits[this.Index];
            switch (token.Text)
            {
                case "\n":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.NewLine));
                    this.LineIndex++;
                    break;

                case " ":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.WhiteSpace));
                    break;

                case "//":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.CommentLine));
                    break;

                case "/*":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.CommentStart));
                    break;

                case "*/":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.CommentEnd));
                    break;

                case "#":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Region));
                    break;

                case "{":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.LeftCurlyBracket));
                    break;

                case "}":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.RightCurlyBracket));
                    break;

                case "(":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.LeftParenthesis));
                    break;

                case ")":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.RightParenthesis));
                    break;

                case "[":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.LeftSquareBracket));
                    break;

                case "]":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.RightSquareBracket));
                    break;

                case ";":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Semicolon));
                    break;

                case ":":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Doublecolon));
                    break;

                case ",":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Comma));
                    break;

                case ".":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Dot));
                    break;

                case "&":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.AndOperator));
                    break;

                case "|":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.OrOperator));
                    break;

                case "!":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.NotOperator));
                    break;

                case "=":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.EqualOperator));
                    break;

                case "<":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.LessThanOperator));
                    break;

                case ">":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.GreaterThanOperator));
                    break;

                case "+":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.PlusOperator));
                    break;

                case "-":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.MinusOperator));
                    break;

                case "*":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.MultiplyOperator));
                    break;

                case "/":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.DivideOperator));
                    break;

                case "%":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.ModOperator));
                    break;

                case "private":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Private));
                    break;

                case "protected":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Protected));
                    break;

                case "internal":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Internal));
                    break;

                case "public":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Public));
                    break;

                case "abstract":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Abstract));
                    break;

                case "virtual":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Virtual));
                    break;

                case "override":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Override));
                    break;

                case "namespace":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.NamespaceDecl));
                    break;

                case "class":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.ClassDecl));
                    break;

                case "struct":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.StructDecl));
                    break;

                case "using":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Using));
                    break;

                case "machine":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.MachineDecl));
                    break;

                case "state":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.StateDecl));
                    break;

                case "event":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.EventDecl));
                    break;

                case "action":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.ActionDecl));
                    break;

                case "on":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.OnAction));
                    break;

                case "do":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.DoAction));
                    break;

                case "goto":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.GotoState));
                    break;

                case "defer":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.DeferEvent));
                    break;

                case "ignore":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.IgnoreEvent));
                    break;

                case "to":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.ToMachine));
                    break;

                case "entry":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Entry));
                    break;

                case "exit":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Exit));
                    break;

                case "this":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.This));
                    break;

                case "base":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Base));
                    break;

                case "new":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.New));
                    break;

                case "as":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.As));
                    break;

                case "for":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.ForLoop));
                    break;

                case "while":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.WhileLoop));
                    break;

                case "if":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.IfCondition));
                    break;

                case "else":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.ElseCondition));
                    break;

                case "break":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Break));
                    break;

                case "continue":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Continue));
                    break;

                case "return":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Return));
                    break;

                case "create":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.CreateMachine));
                    break;

                case "send":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.SendEvent));
                    break;

                case "raise":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.RaiseEvent));
                    break;

                case "delete":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.DeleteMachine));
                    break;

                case "assert":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Assert));
                    break;

                case "payload":
                    this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Payload));
                    break;

                default:
                    if (String.IsNullOrWhiteSpace(token.Text))
                    {
                        this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.WhiteSpace));
                    }
                    else
                    {
                        this.Tokens.Add(new Token(token.Text, this.LineIndex, this.Index, TokenType.Identifier));
                    }

                    break;
            }

            this.Index++;
            this.TokenizeNext();
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Returns the regex pattern.
        /// </summary>
        /// <returns></returns>
        private string GetPattern()
        {
            var pattern = @"(//|/\*|\*/|;|{|}|:|,|\.|\(|\)|\[|\]|#|\s+|" +
                @"&|\||!|=|<|>|\+|-|\*|/|%|" +
                @"\busing\b|\bnamespace\b|\bclass\b|\bstruct\b|" +
                @"\bmachine\b|\bstate\b|\bevent\b|" +
                @"\bon\b|\bdo\b|\bgoto\b|\bdefer\b|\bignore\b|\bto\b|\bentry\b|\bexit\b|" +
                @"\bcreate\b|\braise\b|\bsend\b|" +
                @"\bprivate\b|\bprotected\b|\binternal\b|\bpublic\b|\babstract\b|\bvirtual\b|\boverride\b|" +
                @"\bnew\b|\bas\b)";
            return pattern;
        }

        #endregion
    }
}
