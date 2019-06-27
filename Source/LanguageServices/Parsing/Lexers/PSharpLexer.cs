// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# lexer.
    /// </summary>
    public sealed class PSharpLexer : BaseLexer
    {
        /// <summary>
        /// Tokenizes the next text unit.
        /// </summary>
        protected override void TokenizeNext()
        {
            var unit = this.TextUnits[this.Index++];

            bool isQuotedString() => unit.Text.Length >= 2 && (unit.Text[0] == '"' || unit.Text[0] == '$') && unit.Text[unit.Text.Length - 1] == '"';

            this.Tokens.Add(isQuotedString() ? new Token(unit, TokenType.QuotedString) : this.GetFixedLengthToken(unit));
        }

        private Token GetFixedLengthToken(TextUnit unit)
        {
            switch (unit.Text)
            {
                case "\n":
                    return new Token(unit, TokenType.NewLine);

                case " ":
                    return new Token(unit, TokenType.WhiteSpace);

                case "//":
                    return new Token(unit, TokenType.CommentLine);

                case "/*":
                    return new Token(unit, TokenType.CommentStart);

                case "*/":
                    return new Token(unit, TokenType.CommentEnd);

                case "#":
                    return new Token(unit, TokenType.Region);

                case "{":
                    return new Token(unit, TokenType.LeftCurlyBracket);

                case "}":
                    return new Token(unit, TokenType.RightCurlyBracket);

                case "(":
                    return new Token(unit, TokenType.LeftParenthesis);

                case ")":
                    return new Token(unit, TokenType.RightParenthesis);

                case "[":
                    return new Token(unit, TokenType.LeftSquareBracket);

                case "]":
                    return new Token(unit, TokenType.RightSquareBracket);

                case "<":
                    return new Token(unit, TokenType.LeftAngleBracket);

                case ">":
                    return new Token(unit, TokenType.RightAngleBracket);

                case ";":
                    return new Token(unit, TokenType.Semicolon);

                case ":":
                    return new Token(unit, TokenType.Colon);

                case ",":
                    return new Token(unit, TokenType.Comma);

                case ".":
                    return new Token(unit, TokenType.Dot);

                case "==":
                    return new Token(unit, TokenType.EqualOp);

                case "=":
                    return new Token(unit, TokenType.AssignOp);

                case "+=":
                    return new Token(unit, TokenType.InsertOp);

                case "-=":
                    return new Token(unit, TokenType.RemoveOp);

                case "!=":
                    return new Token(unit, TokenType.NotEqualOp);

                case "<=":
                    return new Token(unit, TokenType.LessOrEqualOp);

                case ">=":
                    return new Token(unit, TokenType.GreaterOrEqualOp);

                case "=>":
                    return new Token(unit, TokenType.LambdaOp);

                case "+":
                    return new Token(unit, TokenType.PlusOp);

                case "-":
                    return new Token(unit, TokenType.MinusOp);

                case "*":
                    return new Token(unit, TokenType.MulOp);

                case "/":
                    return new Token(unit, TokenType.DivOp);

                case "%":
                    return new Token(unit, TokenType.ModOp);

                case "!":
                    return new Token(unit, TokenType.LogNotOp);

                case "&&":
                    return new Token(unit, TokenType.LogAndOp);

                case "||":
                    return new Token(unit, TokenType.LogOrOp);

                case "$":
                    return new Token(unit, TokenType.NonDeterministic);

                case "private":
                    return new Token(unit, TokenType.Private);

                case "protected":
                    return new Token(unit, TokenType.Protected);

                case "internal":
                    return new Token(unit, TokenType.Internal);

                case "public":
                    return new Token(unit, TokenType.Public);

                case "partial":
                    return new Token(unit, TokenType.Partial);

                case "abstract":
                    return new Token(unit, TokenType.Abstract);

                case "virtual":
                    return new Token(unit, TokenType.Virtual);

                case "override":
                    return new Token(unit, TokenType.Override);

                case "namespace":
                    return new Token(unit, TokenType.NamespaceDecl);

                case "class":
                    return new Token(unit, TokenType.ClassDecl);

                case "struct":
                    return new Token(unit, TokenType.StructDecl);

                case "using":
                    return new Token(unit, TokenType.Using);

                case "machineid":
                    return new Token(unit, TokenType.MachineIdDecl);

                case "machine":
                    return new Token(unit, TokenType.MachineDecl);

                case "monitor":
                    return new Token(unit, TokenType.MonitorDecl);

                case "state":
                    return new Token(unit, TokenType.StateDecl);

                case "group":
                    return new Token(unit, TokenType.StateGroupDecl);

                case "extern":
                    return new Token(unit, TokenType.ExternDecl);

                case "event":
                    return new Token(unit, TokenType.EventDecl);

                case "start":
                    return new Token(unit, TokenType.StartState);

                case "hot":
                    return new Token(unit, TokenType.HotState);

                case "cold":
                    return new Token(unit, TokenType.ColdState);

                case "on":
                    return new Token(unit, TokenType.OnAction);

                case "do":
                    return new Token(unit, TokenType.DoAction);

                case "goto":
                    return new Token(unit, TokenType.GotoState);

                case "push":
                    return new Token(unit, TokenType.PushState);

                case "with":
                    return new Token(unit, TokenType.WithExit);

                case "defer":
                    return new Token(unit, TokenType.DeferEvent);

                case "ignore":
                    return new Token(unit, TokenType.IgnoreEvent);

                case "entry":
                    return new Token(unit, TokenType.Entry);

                case "exit":
                    return new Token(unit, TokenType.Exit);

                case "trigger":
                    return new Token(unit, TokenType.Trigger);

                case "this":
                    return new Token(unit, TokenType.This);

                case "base":
                    return new Token(unit, TokenType.Base);

                case "new":
                    return new Token(unit, TokenType.New);

                case "null":
                    return new Token(unit, TokenType.Null);

                case "true":
                    return new Token(unit, TokenType.True);

                case "false":
                    return new Token(unit, TokenType.False);

                case "in":
                    return new Token(unit, TokenType.In);

                case "as":
                    return new Token(unit, TokenType.As);

                case "sizeof":
                    return new Token(unit, TokenType.SizeOf);

                case "if":
                    return new Token(unit, TokenType.IfCondition);

                case "else":
                    return new Token(unit, TokenType.ElseCondition);

                case "for":
                    return new Token(unit, TokenType.ForLoop);

                case "foreach":
                    return new Token(unit, TokenType.ForeachLoop);

                case "while":
                    return new Token(unit, TokenType.WhileLoop);

                case "break":
                    return new Token(unit, TokenType.Break);

                case "continue":
                    return new Token(unit, TokenType.Continue);

                case "return":
                    return new Token(unit, TokenType.Return);

                case "pop":
                    return new Token(unit, TokenType.PopState);

                case "lock":
                    return new Token(unit, TokenType.Lock);

                case "try":
                    return new Token(unit, TokenType.Try);

                case "catch":
                    return new Token(unit, TokenType.Catch);

                case "finally":
                    return new Token(unit, TokenType.Finally);

                case "async":
                    return new Token(unit, TokenType.Async);

                case "await":
                    return new Token(unit, TokenType.Await);

                case "create":
                    return new Token(unit, TokenType.CreateMachine);

                case "remote":
                    return new Token(unit, TokenType.CreateRemoteMachine);

                case "send":
                    return new Token(unit, TokenType.SendEvent);

                case "raise":
                    return new Token(unit, TokenType.RaiseEvent);

                case "jump":
                    return new Token(unit, TokenType.Jump);

                case "assert":
                    return new Token(unit, TokenType.Assert);

                case "assume":
                    return new Token(unit, TokenType.Assume);

                case "halt":
                    return new Token(unit, TokenType.HaltEvent);

                case "default":
                    return new Token(unit, TokenType.DefaultEvent);

                case "var":
                    return new Token(unit, TokenType.Var);

                case "void":
                    return new Token(unit, TokenType.Void);

                case "object":
                    return new Token(unit, TokenType.Object);

                case "string":
                    return new Token(unit, TokenType.String);

                case "sbyte":
                    return new Token(unit, TokenType.Sbyte);

                case "byte":
                    return new Token(unit, TokenType.Byte);

                case "short":
                    return new Token(unit, TokenType.Short);

                case "ushort":
                    return new Token(unit, TokenType.Ushort);

                case "int":
                    return new Token(unit, TokenType.Int);

                case "uint":
                    return new Token(unit, TokenType.Uint);

                case "long":
                    return new Token(unit, TokenType.Long);

                case "ulong":
                    return new Token(unit, TokenType.Ulong);

                case "char":
                    return new Token(unit, TokenType.Char);

                case "bool":
                    return new Token(unit, TokenType.Bool);

                case "decimal":
                    return new Token(unit, TokenType.Decimal);

                case "float":
                    return new Token(unit, TokenType.Float);

                case "double":
                    return new Token(unit, TokenType.Double);

                default:
                    return string.IsNullOrWhiteSpace(unit.Text)
                        ? new Token(unit, TokenType.WhiteSpace)
                        : new Token(unit, TokenType.Identifier);
            }
        }

        /// <summary>
        /// Returns the regex pattern.
        /// </summary>
        protected override string GetPattern() =>
            "(" +
            "\\$?\"(?:[^\"\\\\]|\\\\.)*\"|" + // quoted string (possibly interpolated, and allowing escaped internal quotes)
            @"//|/\*|\*/|#|" +
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
            @"\bnew\b|\bin\b|\bas\b|" +
            @"\s+" +
            ")";
    }
}
