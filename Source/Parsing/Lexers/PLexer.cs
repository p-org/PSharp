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
    public sealed class PLexer : BaseLexer
    {
        #region protected API

        /// <summary>
        /// Tokenizes the next text unit.
        /// </summary>
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
                    this.Tokens.Add(new Token(unit, TokenType.NewLine));
                    break;

                case " ":
                    this.Tokens.Add(new Token(unit, TokenType.WhiteSpace));
                    break;

                case "//":
                    this.Tokens.Add(new Token(unit, TokenType.CommentLine));
                    break;

                case "/*":
                    this.Tokens.Add(new Token(unit, TokenType.CommentStart));
                    break;

                case "*/":
                    this.Tokens.Add(new Token(unit, TokenType.CommentEnd));
                    break;

                case "#":
                    this.Tokens.Add(new Token(unit, TokenType.Region));
                    break;

                case "{":
                    this.Tokens.Add(new Token(unit, TokenType.LeftCurlyBracket));
                    break;

                case "}":
                    this.Tokens.Add(new Token(unit, TokenType.RightCurlyBracket));
                    break;

                case "(":
                    this.Tokens.Add(new Token(unit, TokenType.LeftParenthesis));
                    break;

                case ")":
                    this.Tokens.Add(new Token(unit, TokenType.RightParenthesis));
                    break;

                case "[":
                    this.Tokens.Add(new Token(unit, TokenType.LeftSquareBracket));
                    break;

                case "]":
                    this.Tokens.Add(new Token(unit, TokenType.RightSquareBracket));
                    break;

                case "<":
                    this.Tokens.Add(new Token(unit, TokenType.LeftAngleBracket));
                    break;

                case ">":
                    this.Tokens.Add(new Token(unit, TokenType.RightAngleBracket));
                    break;

                case ";":
                    this.Tokens.Add(new Token(unit, TokenType.Semicolon));
                    break;

                case ":":
                    this.Tokens.Add(new Token(unit, TokenType.Colon));
                    break;

                case ",":
                    this.Tokens.Add(new Token(unit, TokenType.Comma));
                    break;

                case ".":
                    this.Tokens.Add(new Token(unit, TokenType.Dot));
                    break;

                case "==":
                    this.Tokens.Add(new Token(unit, TokenType.EqualOp));
                    break;

                case "=":
                    this.Tokens.Add(new Token(unit, TokenType.AssignOp));
                    break;

                case "+=":
                    this.Tokens.Add(new Token(unit, TokenType.InsertOp));
                    break;

                case "-=":
                    this.Tokens.Add(new Token(unit, TokenType.RemoveOp));
                    break;

                case "!=":
                    this.Tokens.Add(new Token(unit, TokenType.NotEqualOp));
                    break;

                case "<=":
                    this.Tokens.Add(new Token(unit, TokenType.LessOrEqualOp));
                    break;

                case ">=":
                    this.Tokens.Add(new Token(unit, TokenType.GreaterOrEqualOp));
                    break;

                case "+":
                    this.Tokens.Add(new Token(unit, TokenType.PlusOp));
                    break;

                case "-":
                    this.Tokens.Add(new Token(unit, TokenType.MinusOp));
                    break;

                case "*":
                    this.Tokens.Add(new Token(unit, TokenType.MulOp));
                    break;

                case "/":
                    this.Tokens.Add(new Token(unit, TokenType.DivOp));
                    break;

                case "!":
                    this.Tokens.Add(new Token(unit, TokenType.LogNotOp));
                    break;

                case "&&":
                    this.Tokens.Add(new Token(unit, TokenType.LogAndOp));
                    break;

                case "||":
                    this.Tokens.Add(new Token(unit, TokenType.LogOrOp));
                    break;

                case "$":
                    this.Tokens.Add(new Token(unit, TokenType.NonDeterministic));
                    break;

                case "machine":
                    this.Tokens.Add(new Token(unit, TokenType.MachineDecl));
                    break;

                case "model":
                    this.Tokens.Add(new Token(unit, TokenType.ModelDecl));
                    break;

                case "monitor":
                    this.Tokens.Add(new Token(unit, TokenType.MonitorDecl));
                    break;

                case "state":
                    this.Tokens.Add(new Token(unit, TokenType.StateDecl));
                    break;

                case "event":
                    this.Tokens.Add(new Token(unit, TokenType.EventDecl));
                    break;

                case "action":
                    this.Tokens.Add(new Token(unit, TokenType.ActionDecl));
                    break;
                case "fun":
                    this.Tokens.Add(new Token(unit, TokenType.FunDecl));
                    break;

                case "main":
                    this.Tokens.Add(new Token(unit, TokenType.MainMachine));
                    break;

                case "start":
                    this.Tokens.Add(new Token(unit, TokenType.StartState));
                    break;

                case "on":
                    this.Tokens.Add(new Token(unit, TokenType.OnAction));
                    break;

                case "do":
                    this.Tokens.Add(new Token(unit, TokenType.DoAction));
                    break;

                case "goto":
                    this.Tokens.Add(new Token(unit, TokenType.GotoState));
                    break;

                case "defer":
                    this.Tokens.Add(new Token(unit, TokenType.DeferEvent));
                    break;

                case "ignore":
                    this.Tokens.Add(new Token(unit, TokenType.IgnoreEvent));
                    break;

                case "entry":
                    this.Tokens.Add(new Token(unit, TokenType.Entry));
                    break;

                case "exit":
                    this.Tokens.Add(new Token(unit, TokenType.Exit));
                    break;

                case "this":
                    this.Tokens.Add(new Token(unit, TokenType.This));
                    break;

                case "new":
                    this.Tokens.Add(new Token(unit, TokenType.New));
                    break;

                case "null":
                    this.Tokens.Add(new Token(unit, TokenType.Null));
                    break;

                case "true":
                    this.Tokens.Add(new Token(unit, TokenType.True));
                    break;

                case "false":
                    this.Tokens.Add(new Token(unit, TokenType.False));
                    break;

                case "sizeof":
                    this.Tokens.Add(new Token(unit, TokenType.SizeOf));
                    break;

                case "in":
                    this.Tokens.Add(new Token(unit, TokenType.In));
                    break;

                case "as":
                    this.Tokens.Add(new Token(unit, TokenType.As));
                    break;

                case "keys":
                    this.Tokens.Add(new Token(unit, TokenType.Keys));
                    break;

                case "values":
                    this.Tokens.Add(new Token(unit, TokenType.Values));
                    break;

                case "if":
                    this.Tokens.Add(new Token(unit, TokenType.IfCondition));
                    break;

                case "else":
                    this.Tokens.Add(new Token(unit, TokenType.ElseCondition));
                    break;

                case "for":
                    this.Tokens.Add(new Token(unit, TokenType.ForLoop));
                    break;

                case "while":
                    this.Tokens.Add(new Token(unit, TokenType.WhileLoop));
                    break;

                case "break":
                    this.Tokens.Add(new Token(unit, TokenType.Break));
                    break;

                case "continue":
                    this.Tokens.Add(new Token(unit, TokenType.Continue));
                    break;

                case "return":
                    this.Tokens.Add(new Token(unit, TokenType.Return));
                    break;

                case "send":
                    this.Tokens.Add(new Token(unit, TokenType.SendEvent));
                    break;

                case "raise":
                    this.Tokens.Add(new Token(unit, TokenType.RaiseEvent));
                    break;

                case "delete":
                    this.Tokens.Add(new Token(unit, TokenType.DeleteMachine));
                    break;

                case "assert":
                    this.Tokens.Add(new Token(unit, TokenType.Assert));
                    break;

                case "assume":
                    this.Tokens.Add(new Token(unit, TokenType.Assume));
                    break;

                case "payload":
                    this.Tokens.Add(new Token(unit, TokenType.Payload));
                    break;

                case "halt":
                    this.Tokens.Add(new Token(unit, TokenType.HaltEvent));
                    break;

                case "default":
                    this.Tokens.Add(new Token(unit, TokenType.DefaultEvent));
                    break;

                case "var":
                    this.Tokens.Add(new Token(unit, TokenType.Var));
                    break;

                case "int":
                    this.Tokens.Add(new Token(unit, TokenType.Int));
                    break;

                case "bool":
                    this.Tokens.Add(new Token(unit, TokenType.Bool));
                    break;

                case "foreign":
                    this.Tokens.Add(new Token(unit, TokenType.Foreign));
                    break;

                case "any":
                    this.Tokens.Add(new Token(unit, TokenType.Any));
                    break;

                case "seq":
                    this.Tokens.Add(new Token(unit, TokenType.Seq));
                    break;

                case "map":
                    this.Tokens.Add(new Token(unit, TokenType.Map));
                    break;

                default:
                    if (String.IsNullOrWhiteSpace(unit.Text))
                    {
                        this.Tokens.Add(new Token(unit, TokenType.WhiteSpace));
                    }
                    else
                    {
                        this.Tokens.Add(new Token(unit, TokenType.Identifier));
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
                @"\$|" +
                @"{|}|\(|\)|\[|\]|" +
                @"\bmachine\b|\bmodel\b|\bmonitor\b|\bstate\b|\bevent\b|\bfun\b|" +
                @"\bmain\b|\bstart\b|" +
                @"\bdefer\b|\bignore\b|\bentry\b|\bexit\b|" +
                @"\braise\b|\bsend\b|" +
                @"\bon\b|\bdo\b|\bgoto\b|" +
                @"\bvar\b|" +
                @"\bnew\b|\bin\b|\bas\b" +
                @"|\s+)";
            return pattern;
        }

        #endregion
    }
}
