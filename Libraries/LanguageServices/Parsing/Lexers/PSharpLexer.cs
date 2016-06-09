//-----------------------------------------------------------------------
// <copyright file="PSharpLexer.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# lexer.
    /// </summary>
    public sealed class PSharpLexer : BaseLexer
    {
        #region protected API

        /// <summary>
        /// Tokenizes the next text unit.
        /// </summary>
        protected override void TokenizeNext()
        {
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

                case "=>":
                    this.Tokens.Add(new Token(unit, TokenType.LambdaOp));
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

                case "%":
                    this.Tokens.Add(new Token(unit, TokenType.ModOp));
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

                case "private":
                    this.Tokens.Add(new Token(unit, TokenType.Private));
                    break;

                case "protected":
                    this.Tokens.Add(new Token(unit, TokenType.Protected));
                    break;

                case "internal":
                    this.Tokens.Add(new Token(unit, TokenType.Internal));
                    break;

                case "public":
                    this.Tokens.Add(new Token(unit, TokenType.Public));
                    break;

                case "partial":
                    this.Tokens.Add(new Token(unit, TokenType.Partial));
                    break;

                case "abstract":
                    this.Tokens.Add(new Token(unit, TokenType.Abstract));
                    break;

                case "virtual":
                    this.Tokens.Add(new Token(unit, TokenType.Virtual));
                    break;

                case "override":
                    this.Tokens.Add(new Token(unit, TokenType.Override));
                    break;

                case "namespace":
                    this.Tokens.Add(new Token(unit, TokenType.NamespaceDecl));
                    break;

                case "class":
                    this.Tokens.Add(new Token(unit, TokenType.ClassDecl));
                    break;

                case "struct":
                    this.Tokens.Add(new Token(unit, TokenType.StructDecl));
                    break;

                case "using":
                    this.Tokens.Add(new Token(unit, TokenType.Using));
                    break;

                case "machine":
                    this.Tokens.Add(new Token(unit, TokenType.MachineDecl));
                    break;

                case "monitor":
                    this.Tokens.Add(new Token(unit, TokenType.Monitor));
                    break;

                case "state":
                    this.Tokens.Add(new Token(unit, TokenType.StateDecl));
                    break;

                case "group":
                    this.Tokens.Add(new Token(unit, TokenType.StateGroupDecl));
                    break;

                case "event":
                    this.Tokens.Add(new Token(unit, TokenType.EventDecl));
                    break;

                case "start":
                    this.Tokens.Add(new Token(unit, TokenType.StartState));
                    break;

                case "hot":
                    this.Tokens.Add(new Token(unit, TokenType.HotState));
                    break;

                case "cold":
                    this.Tokens.Add(new Token(unit, TokenType.ColdState));
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

                case "push":
                    this.Tokens.Add(new Token(unit, TokenType.PushState));
                    break;

                case "with":
                    this.Tokens.Add(new Token(unit, TokenType.WithExit));
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

                case "trigger":
                    this.Tokens.Add(new Token(unit, TokenType.Trigger));
                    break;

                case "this":
                    this.Tokens.Add(new Token(unit, TokenType.This));
                    break;

                case "base":
                    this.Tokens.Add(new Token(unit, TokenType.Base));
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
                    
                case "in":
                    this.Tokens.Add(new Token(unit, TokenType.In));
                    break;

                case "as":
                    this.Tokens.Add(new Token(unit, TokenType.As));
                    break;

                case "sizeof":
                    this.Tokens.Add(new Token(unit, TokenType.SizeOf));
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

                case "foreach":
                    this.Tokens.Add(new Token(unit, TokenType.ForeachLoop));
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

                case "pop":
                    this.Tokens.Add(new Token(unit, TokenType.Pop));
                    break;

                case "lock":
                    this.Tokens.Add(new Token(unit, TokenType.Lock));
                    break;

                case "try":
                    this.Tokens.Add(new Token(unit, TokenType.Try));
                    break;

                case "catch":
                    this.Tokens.Add(new Token(unit, TokenType.Catch));
                    break;

                case "finally":
                    this.Tokens.Add(new Token(unit, TokenType.Finally));
                    break;

                case "async":
                    this.Tokens.Add(new Token(unit, TokenType.Async));
                    break;

                case "await":
                    this.Tokens.Add(new Token(unit, TokenType.Await));
                    break;

                case "create":
                    this.Tokens.Add(new Token(unit, TokenType.CreateMachine));
                    break;

                case "remote":
                    this.Tokens.Add(new Token(unit, TokenType.CreateRemoteMachine));
                    break;

                case "send":
                    this.Tokens.Add(new Token(unit, TokenType.SendEvent));
                    break;

                case "raise":
                    this.Tokens.Add(new Token(unit, TokenType.RaiseEvent));
                    break;

                case "jump":
                    this.Tokens.Add(new Token(unit, TokenType.Jump));
                    break;

                case "assert":
                    this.Tokens.Add(new Token(unit, TokenType.Assert));
                    break;

                case "assume":
                    this.Tokens.Add(new Token(unit, TokenType.Assume));
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

                case "void":
                    this.Tokens.Add(new Token(unit, TokenType.Void));
                    break;

                case "object":
                    this.Tokens.Add(new Token(unit, TokenType.Object));
                    break;

                case "string":
                    this.Tokens.Add(new Token(unit, TokenType.String));
                    break;

                case "sbyte":
                    this.Tokens.Add(new Token(unit, TokenType.Sbyte));
                    break;

                case "byte":
                    this.Tokens.Add(new Token(unit, TokenType.Byte));
                    break;

                case "short":
                    this.Tokens.Add(new Token(unit, TokenType.Short));
                    break;

                case "ushort":
                    this.Tokens.Add(new Token(unit, TokenType.Ushort));
                    break;

                case "int":
                    this.Tokens.Add(new Token(unit, TokenType.Int));
                    break;

                case "uint":
                    this.Tokens.Add(new Token(unit, TokenType.Uint));
                    break;

                case "long":
                    this.Tokens.Add(new Token(unit, TokenType.Long));
                    break;

                case "ulong":
                    this.Tokens.Add(new Token(unit, TokenType.Ulong));
                    break;
                    
                case "char":
                    this.Tokens.Add(new Token(unit, TokenType.Char));
                    break;

                case "bool":
                    this.Tokens.Add(new Token(unit, TokenType.Bool));
                    break;

                case "decimal":
                    this.Tokens.Add(new Token(unit, TokenType.Decimal));
                    break;

                case "float":
                    this.Tokens.Add(new Token(unit, TokenType.Float));
                    break;

                case "double":
                    this.Tokens.Add(new Token(unit, TokenType.Double));
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
        }

        /// <summary>
        /// Returns the regex pattern.
        /// </summary>
        /// <returns></returns>
        protected override string GetPattern()
        {
            var pattern = @"(//|/\*|\*/|#|" +
                @"\.|:|,|;|" +
                @"=>|==|=|\+=|-=|!=|<=|>=|<|>|" +
                @"\+|-|\*|/|" +
                @"!|&&|\|\||" +
                @"%|\$|" +
                @"{|}|\(|\)|\[|\]|" +
                @"\busing\b|\bnamespace\b|\bclass\b|\bstruct\b|" +
                @"\bmachine\b|\bmonitor\b|\bstate\b|\bevent\b|\baction\b|" +
                @"\bstart\b|\bhot\b|\bcold\b|" +
                @"\bdefer\b|\bignore\b|\bentry\b|\bexit\b|" +
                @"\bcreate\b|\braise\b|\bsend\b|\bto\b|" +
                @"\bon\b|\bdo\b|\bgoto\b|\bpush\b|\bwith\b|" +
                @"\bprivate\b|\bprotected\b|\binternal\b|\bpublic\b|" +
                @"\bpartial\b|\babstract\b|\bvirtual\b|\boverride\b|" +
                @"\block\b|\btry\b|\bcatch\b|" +
                @"\basync\b|bawait\b|" +
                @"\bvar\b|" +
                @"\bnew\b|\bin\b|\bas\b" +
                @"|\s+)";
            return pattern;
        }

        #endregion
    }
}
