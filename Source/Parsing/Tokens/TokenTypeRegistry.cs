﻿//-----------------------------------------------------------------------
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

                case TokenType.LeftAngleBracket:
                    text = "<";
                    break;

                case TokenType.RightAngleBracket:
                    text = ">";
                    break;

                case TokenType.Semicolon:
                    text = ";";
                    break;

                case TokenType.Colon:
                    text = ":";
                    break;

                case TokenType.Comma:
                    text = ",";
                    break;

                case TokenType.Dot:
                    text = ".";
                    break;

                case TokenType.EqualOp:
                    text = "==";
                    break;

                case TokenType.AssignOp:
                    text = "=";
                    break;

                case TokenType.InsertOp:
                    text = "+=";
                    break;

                case TokenType.RemoveOp:
                    text = "-=";
                    break;

                case TokenType.NotEqualOp:
                    text = "!=";
                    break;

                case TokenType.LessOrEqualOp:
                    text = "<=";
                    break;

                case TokenType.GreaterOrEqualOp:
                    text = ">=";
                    break;

                case TokenType.LambdaOp:
                    text = "=>";
                    break;

                case TokenType.PlusOp:
                    text = "+";
                    break;

                case TokenType.MinusOp:
                    text = "-";
                    break;

                case TokenType.MulOp:
                    text = "*";
                    break;

                case TokenType.DivOp:
                    text = "/";
                    break;

                case TokenType.ModOp:
                    text = "%";
                    break;

                case TokenType.LogNotOp:
                    text = "!";
                    break;

                case TokenType.LogAndOp:
                    text = "&&";
                    break;

                case TokenType.LogOrOp:
                    text = "||";
                    break;

                case TokenType.NonDeterministic:
                    text = "$";
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

                case TokenType.ModelDecl:
                    text = "model";
                    break;

                case TokenType.Monitor:
                    text = "monitor";
                    break;

                case TokenType.StateDecl:
                    text = "state";
                    break;

                case TokenType.EventDecl:
                    text = "event";
                    break;

                case TokenType.FunDecl:
                    text = "fun";
                    break;

                case TokenType.MainMachine:
                    text = "main";
                    break;

                case TokenType.StartState:
                    text = "start";
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

                case TokenType.PushState:
                    text = "push";
                    break;

                case TokenType.WithExit:
                    text = "with";
                    break;

                case TokenType.DeferEvent:
                    text = "defer";
                    break;

                case TokenType.IgnoreEvent:
                    text = "ignore";
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

                case TokenType.Null:
                    text = "null";
                    break;

                case TokenType.True:
                    text = "true";
                    break;

                case TokenType.False:
                    text = "false";
                    break;

                case TokenType.SizeOf:
                    text = "sizeof";
                    break;

                case TokenType.In:
                    text = "in";
                    break;

                case TokenType.As:
                    text = "as";
                    break;

                case TokenType.Keys:
                    text = "keys";
                    break;

                case TokenType.Values:
                    text = "values";
                    break;

                case TokenType.IfCondition:
                    text = "if";
                    break;

                case TokenType.ElseCondition:
                    text = "else";
                    break;

                case TokenType.DoLoop:
                    text = "do";
                    break;

                case TokenType.ForLoop:
                    text = "for";
                    break;

                case TokenType.ForeachLoop:
                    text = "foreach";
                    break;

                case TokenType.WhileLoop:
                    text = "while";
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

                case TokenType.Pop:
                    text = "pop";
                    break;

                case TokenType.Lock:
                    text = "lock";
                    break;

                case TokenType.Try:
                    text = "try";
                    break;

                case TokenType.Catch:
                    text = "catch";
                    break;

                case TokenType.Finally:
                    text = "finally";
                    break;

                case TokenType.Async:
                    text = "async";
                    break;

                case TokenType.Await:
                    text = "await";
                    break;

                case TokenType.CreateMachine:
                    text = "create";
                    break;

                case TokenType.SendEvent:
                    text = "send";
                    break;

                case TokenType.ToMachine:
                    text = "to";
                    break;

                case TokenType.RaiseEvent:
                    text = "raise";
                    break;

                case TokenType.Assert:
                    text = "assert";
                    break;

                case TokenType.Assume:
                    text = "assume";
                    break;

                case TokenType.Payload:
                    text = "payload";
                    break;

                case TokenType.Trigger:
                    text = "trigger";
                    break;

                case TokenType.HaltEvent:
                    text = "halt";
                    break;

                case TokenType.DefaultEvent:
                    text = "default";
                    break;

                case TokenType.ColdState:
                    text = "cold";
                    break;

                case TokenType.HotState:
                    text = "state";
                    break;

                case TokenType.Var:
                    text = "var";
                    break;

                case TokenType.Void:
                    text = "void";
                    break;

                case TokenType.Object:
                    text = "object";
                    break;

                case TokenType.String:
                    text = "string";
                    break;

                case TokenType.Sbyte:
                    text = "sbyte";
                    break;

                case TokenType.Byte:
                    text = "byte";
                    break;

                case TokenType.Short:
                    text = "short";
                    break;

                case TokenType.Ushort:
                    text = "ushort";
                    break;

                case TokenType.Int:
                    text = "int";
                    break;

                case TokenType.Uint:
                    text = "uint";
                    break;

                case TokenType.Long:
                    text = "long";
                    break;

                case TokenType.Ulong:
                    text = "ulong";
                    break;

                case TokenType.Char:
                    text = "char";
                    break;

                case TokenType.Bool:
                    text = "bool";
                    break;

                case TokenType.Decimal:
                    text = "decimal";
                    break;

                case TokenType.Float:
                    text = "float";
                    break;

                case TokenType.Double:
                    text = "double";
                    break;

                case TokenType.Foreign:
                    text = "foreign";
                    break;

                case TokenType.Any:
                    text = "any";
                    break;

                case TokenType.Seq:
                    text = "seq";
                    break;

                case TokenType.Map:
                    text = "map";
                    break;

                default:
                    break;
            }

            return text;
        }

        #endregion
    }
}
