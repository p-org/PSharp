//-----------------------------------------------------------------------
// <copyright file="TokenTypeRegistry.cs">
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

namespace Microsoft.PSharp.Parsing
{
    public static class TokenTypeRegistry
    {
        #region public API

        /// <summary>
        /// Returns the text representing the given token type.
        /// </summary>
        /// <param name="type">TokenType</param>
        /// <returns>Text</returns>
        public static string GetText(TokenType type)
        {
            var text = "";

            switch (type)
            {
                case TokenType.LeftCurlyBracket:
                case TokenType.MachineLeftCurlyBracket:
                case TokenType.StateLeftCurlyBracket:
                    text = "{";
                    break;

                case TokenType.RightCurlyBracket:
                case TokenType.MachineRightCurlyBracket:
                case TokenType.StateRightCurlyBracket:
                    text = "}";
                    break;

                case TokenType.LeftParenthesis:
                    text = "(";
                    break;

                case TokenType.RightParenthesis:
                    text = ")";
                    break;

                case TokenType.LeftSquareBracket:
                    text = "[";
                    break;

                case TokenType.RightSquareBracket:
                    text = "]";
                    break;

                case TokenType.Semicolon:
                    text = ";";
                    break;

                case TokenType.Doublecolon:
                    text = ":";
                    break;

                case TokenType.Comma:
                    text = ",";
                    break;

                case TokenType.Dot:
                    text = ".";
                    break;

                case TokenType.AndOperator:
                    text = "&";
                    break;

                case TokenType.OrOperator:
                    text = "|";
                    break;

                case TokenType.NotOperator:
                    text = "!";
                    break;

                case TokenType.EqualOperator:
                    text = "=";
                    break;

                case TokenType.LessThanOperator:
                    text = "<";
                    break;

                case TokenType.GreaterThanOperator:
                    text = ">";
                    break;

                case TokenType.PlusOperator:
                    text = "+";
                    break;

                case TokenType.MinusOperator:
                    text = "-";
                    break;

                case TokenType.MultiplyOperator:
                    text = "-";
                    break;

                case TokenType.DivideOperator:
                    text = "/";
                    break;

                case TokenType.ModOperator:
                    text = "%";
                    break;

                case TokenType.Private:
                    text = "private";
                    break;

                case TokenType.Protected:
                    text = "protected";
                    break;

                case TokenType.Internal:
                    text = "internal";
                    break;

                case TokenType.Public:
                    text = "public";
                    break;

                case TokenType.Abstract:
                    text = "abstract";
                    break;

                case TokenType.Virtual:
                    text = "virtual";
                    break;

                case TokenType.Override:
                    text = "override";
                    break;

                case TokenType.NamespaceDecl:
                    text = "namespace";
                    break;

                case TokenType.ClassDecl:
                    text = "class";
                    break;

                case TokenType.StructDecl:
                    text = "struct";
                    break;

                case TokenType.Using:
                    text = "using";
                    break;

                case TokenType.MachineDecl:
                    text = "machine";
                    break;

                case TokenType.StateDecl:
                    text = "state";
                    break;

                case TokenType.EventDecl:
                    text = "event";
                    break;

                case TokenType.ActionDecl:
                    text = "action";
                    break;

                case TokenType.OnAction:
                    text = "on";
                    break;

                case TokenType.DoAction:
                    text = "do";
                    break;

                case TokenType.GotoState:
                    text = "goto";
                    break;

                case TokenType.DeferEvent:
                    text = "defer";
                    break;

                case TokenType.IgnoreEvent:
                    text = "ignore";
                    break;

                case TokenType.ToMachine:
                    text = "to";
                    break;

                case TokenType.Entry:
                    text = "entry";
                    break;

                case TokenType.Exit:
                    text = "exit";
                    break;

                case TokenType.This:
                    text = "this";
                    break;

                case TokenType.Base:
                    text = "base";
                    break;

                case TokenType.New:
                    text = "new";
                    break;

                case TokenType.As:
                    text = "as";
                    break;

                case TokenType.ForLoop:
                    text = "for";
                    break;

                case TokenType.WhileLoop:
                    text = "while";
                    break;

                case TokenType.DoLoop:
                    text = "do";
                    break;

                case TokenType.IfCondition:
                    text = "if";
                    break;

                case TokenType.ElseCondition:
                    text = "else";
                    break;

                case TokenType.Break:
                    text = "break";
                    break;

                case TokenType.Continue:
                    text = "continue";
                    break;

                case TokenType.Return:
                    text = "return";
                    break;

                case TokenType.CreateMachine:
                    text = "create";
                    break;

                case TokenType.SendEvent:
                    text = "send";
                    break;

                case TokenType.RaiseEvent:
                    text = "raise";
                    break;

                case TokenType.DeleteMachine:
                    text = "delete";
                    break;

                case TokenType.Assert:
                    text = "assert";
                    break;

                case TokenType.Payload:
                    text = "payload";
                    break;

                default:
                    break;
            }

            return text;
        }

        #endregion
    }
}
