//-----------------------------------------------------------------------
// <copyright file="PLexer.cs">
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

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P lexer.
    /// </summary>
    public class PLexer : BaseLexer
    {
        #region protected API

        /// <summary>
        /// Tokenizes the next text units.
        /// </summary>
        /// <param name="textUnits">Text units</param>
        protected override void TokenizeNext()
        {
            if (this.Index == this.TextUnits.Count)
            {
                return;
            }

            var unit = this.TextUnits[this.Index];
            switch (unit.Text)
            {
                case "\n":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.NewLine));
                    this.LineIndex++;
                    break;

                case " ":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.WhiteSpace));
                    break;

                case "//":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.CommentLine));
                    break;

                case "/*":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.CommentStart));
                    break;

                case "*/":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.CommentEnd));
                    break;

                case "#":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Region));
                    break;

                case "{":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.LeftCurlyBracket));
                    break;

                case "}":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.RightCurlyBracket));
                    break;

                case "(":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.LeftParenthesis));
                    break;

                case ")":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.RightParenthesis));
                    break;

                case "[":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.LeftSquareBracket));
                    break;

                case "]":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.RightSquareBracket));
                    break;

                case "<":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.LeftAngleBracket));
                    break;

                case ">":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.RightAngleBracket));
                    break;

                case ";":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Semicolon));
                    break;

                case ":":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Colon));
                    break;

                case ",":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Comma));
                    break;

                case ".":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Dot));
                    break;

                case "==":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.EqualOp));
                    break;

                case "=":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.AssignOp));
                    break;

                case "+=":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.InsertOp));
                    break;

                case "-=":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.RemoveOp));
                    break;

                case "!=":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.NotEqualOp));
                    break;

                case "<=":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.LessOrEqualOp));
                    break;

                case ">=":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.GreaterOrEqualOp));
                    break;

                case "+":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.PlusOp));
                    break;

                case "-":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.MinusOp));
                    break;

                case "*":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.MulOp));
                    break;

                case "/":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.DivOp));
                    break;

                case "%":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.ModOp));
                    break;

                case "!":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.LogNotOp));
                    break;

                case "&&":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.LogAndOp));
                    break;

                case "||":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.LogOrOp));
                    break;

                case "$":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.NonDeterministic));
                    break;

                case "machine":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.MachineDecl));
                    break;

                case "model":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.ModelDecl));
                    break;

                case "monitor":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.MonitorDecl));
                    break;

                case "state":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.StateDecl));
                    break;

                case "event":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.EventDecl));
                    break;

                case "action":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.ActionDecl));
                    break;

                case "main":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.MainMachine));
                    break;

                case "start":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.StartState));
                    break;

                case "on":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.OnAction));
                    break;

                case "do":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.DoAction));
                    break;

                case "goto":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.GotoState));
                    break;

                case "defer":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.DeferEvent));
                    break;

                case "ignore":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.IgnoreEvent));
                    break;

                case "to":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.ToMachine));
                    break;

                case "entry":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Entry));
                    break;

                case "exit":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Exit));
                    break;

                case "this":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.This));
                    break;

                case "base":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Base));
                    break;

                case "new":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.New));
                    break;

                case "null":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Null));
                    break;

                case "true":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.True));
                    break;

                case "false":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.False));
                    break;

                case "sizeof":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.SizeOf));
                    break;

                case "in":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.In));
                    break;

                case "as":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.As));
                    break;

                case "keys":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Keys));
                    break;

                case "values":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Values));
                    break;

                case "for":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.ForLoop));
                    break;

                case "while":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.WhileLoop));
                    break;

                case "if":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.IfCondition));
                    break;

                case "else":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.ElseCondition));
                    break;

                case "break":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Break));
                    break;

                case "continue":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Continue));
                    break;

                case "return":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Return));
                    break;

                case "create":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.CreateMachine));
                    break;

                case "send":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.SendEvent));
                    break;

                case "raise":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.RaiseEvent));
                    break;

                case "delete":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.DeleteMachine));
                    break;

                case "assert":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Assert));
                    break;

                case "payload":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Payload));
                    break;

                case "var":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Var));
                    break;

                case "int":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Int));
                    break;

                case "bool":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Bool));
                    break;

                case "foreign":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Foreign));
                    break;

                case "any":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Any));
                    break;

                case "seq":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Seq));
                    break;

                case "map":
                    this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Map));
                    break;

                default:
                    if (String.IsNullOrWhiteSpace(unit.Text))
                    {
                        this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.WhiteSpace));
                    }
                    else
                    {
                        this.Tokens.Add(new Token(unit, this.LineIndex, TokenType.Identifier));
                    }

                    break;
            }

            this.Index++;
            this.TokenizeNext();
        }

        /// <summary>
        /// Returns the regex pattern.
        /// </summary>
        /// <returns></returns>
        protected override string GetPattern()
        {
            var pattern = @"(//|/\*|\*/|#|" +
                @"\.|:|,|;|" +
                @"==|=|\+=|-=|!=|<=|>=|<|>|" +
                @"\+|-|\*|/|" +
                @"!|&&|\|\||" +
                @"%|\$|" +
                @"{|}|\(|\)|\[|\]|" +
                @"\bmachine\b|\bmodel\b|\bmonitor\b|\bstate\b|\bevent\b|" +
                @"\bmain\b|\bstart\b|" +
                @"\bdefer\b|\bignore\b|\bto\b|\bentry\b|\bexit\b|" +
                @"\bcreate\b|\braise\b|\bsend\b|" +
                @"\bon\b|\bdo\b|\bgoto\b|" +
                @"\bvar\b|" +
                @"\bnew\b|\bin\b|\bas\b" +
                @"|\s+)";
            return pattern;
        }

        #endregion
    }
}
