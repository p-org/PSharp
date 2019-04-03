// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Expression node.
    /// </summary>
    internal class ExpressionNode : PSharpSyntaxNode
    {
        /// <summary>
        /// The block node.
        /// </summary>
        internal readonly BlockSyntax Parent;

        /// <summary>
        /// The statement tokens.
        /// </summary>
        internal List<Token> StmtTokens;

        /// <summary>
        /// The rewritten statement tokens.
        /// </summary>
        internal List<Token> RewrittenStmtTokens;

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionNode"/> class.
        /// </summary>
        internal ExpressionNode(IPSharpProgram program, BlockSyntax node)
            : base(program)
        {
            this.Parent = node;
            this.StmtTokens = new List<Token>();
            this.RewrittenStmtTokens = new List<Token>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C# representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            if (this.StmtTokens.Count == 0)
            {
                this.TextUnit = new TextUnit(string.Empty, 0);
                return;
            }

            this.Index = 0;
            this.RewrittenStmtTokens = this.StmtTokens.ToList();
            this.RewriteNextToken();

            var text = GetIndent(indentLevel);

            foreach (var token in this.RewrittenStmtTokens)
            {
                text += token.TextUnit.Text;
            }

            this.TextUnit = new TextUnit(text, this.StmtTokens.First().TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the machine type.
        /// </summary>
        protected void RewriteMachineType()
        {
            var textUnit = new TextUnit("MachineId", this.RewrittenStmtTokens[this.Index].TextUnit.Line);
            this.RewrittenStmtTokens[this.Index] = new Token(textUnit, this.RewrittenStmtTokens[this.Index].Type);
        }

        /// <summary>
        /// Rewrites the non-deterministic choice.
        /// </summary>
        protected void RewriteNonDeterministicChoice()
        {
            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "this.Random()";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
        }

        /// <summary>
        /// Rewrites the next token.
        /// </summary>
        private void RewriteNextToken()
        {
            if (this.Index == this.RewrittenStmtTokens.Count)
            {
                return;
            }

            var token = this.RewrittenStmtTokens[this.Index];
            if (token.Type == TokenType.MachineDecl)
            {
                this.RewriteMachineType();
            }
            else if (token.Type == TokenType.This)
            {
                this.RewriteThis();
            }
            else if (token.Type == TokenType.Trigger)
            {
                this.RewriteTrigger();
            }
            else if (token.Type == TokenType.NonDeterministic)
            {
                this.RewriteNonDeterministicChoice();
            }
            else if (token.Type == TokenType.Identifier)
            {
                this.RewriteIdentifier();
            }

            this.Index++;
            this.RewriteNextToken();
        }

        /// <summary>
        /// Rewrites the this keyword.
        /// </summary>
        private void RewriteThis()
        {
            var removeIdx = this.Index + 1;
            this.SkipWhiteSpaceTokens();

            if (removeIdx < this.RewrittenStmtTokens.Count &&
                this.RewrittenStmtTokens[removeIdx].Type == TokenType.Dot)
            {
                this.RewrittenStmtTokens.RemoveAt(this.Index);
                this.RewrittenStmtTokens.RemoveAt(this.Index);
                this.Index--;
            }
            else
            {
                int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
                var text = "this.Id";
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
            }
        }

        /// <summary>
        /// Rewrites the trigger keyword.
        /// </summary>
        private void RewriteTrigger()
        {
            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "this.ReceivedEvent";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
        }

        /// <summary>
        /// Rewrites the identifier.
        /// </summary>
        private void RewriteIdentifier()
        {
            if (this.Parent.Machine == null || this.Parent.State == null ||
                !(this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text)) ||
                this.Parent.Machine.MethodDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "(this.";

            if (this.Parent.Machine.IsMonitor)
            {
                text += "Monitor";
            }
            else
            {
                text += "Machine";
            }

            text += " as " + this.Parent.Machine.Identifier.TextUnit.Text + ").";
            this.RewrittenStmtTokens.Insert(this.Index, new Token(new TextUnit(text, line)));
            this.Index++;
        }

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        private void SkipWhiteSpaceTokens()
        {
            while (this.Index < this.RewrittenStmtTokens.Count &&
                (this.RewrittenStmtTokens[this.Index].Type == TokenType.WhiteSpace ||
                this.RewrittenStmtTokens[this.Index].Type == TokenType.NewLine))
            {
                this.Index++;
            }

            return;
        }
    }
}
