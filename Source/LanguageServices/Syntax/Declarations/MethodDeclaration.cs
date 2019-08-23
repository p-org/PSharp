using System;
using System.Collections.Generic;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Method declaration syntax node.
    /// </summary>
    internal sealed class MethodDeclaration : PSharpSyntaxNode
    {
        /// <summary>
        /// The machine parent node.
        /// </summary>
        internal readonly MachineDeclaration Machine;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal AccessModifier AccessModifier;

        /// <summary>
        /// The inheritance modifier.
        /// </summary>
        internal InheritanceModifier InheritanceModifier;

        /// <summary>
        /// Is the method async.
        /// </summary>
        internal bool IsAsync;

        /// <summary>
        /// Is the method partial.
        /// </summary>
        internal bool IsPartial;

        /// <summary>
        /// The type identifier.
        /// </summary>
        internal Token TypeIdentifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// List of parameter tokens.
        /// </summary>
        internal List<Token> Parameters;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        internal Token SemicolonToken;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal BlockSyntax StatementBlock;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodDeclaration"/> class.
        /// </summary>
        internal MethodDeclaration(IPSharpProgram program, MachineDeclaration machineNode)
            : base(program)
        {
            this.Parameters = new List<Token>();
            this.Machine = machineNode;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            string text = string.Empty;

            try
            {
                text = this.GetRewrittenMethodDeclaration(indentLevel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception was thrown during rewriting:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit(
                    "Failed to rewrite method '{0}' of machine '{1}'.",
                    this.Identifier.TextUnit.Text,
                    this.Machine.Identifier.TextUnit.Text);
            }

            this.TextUnit = this.TypeIdentifier.TextUnit.WithText(text);
        }

        /// <summary>
        /// Returns the rewritten method declaration.
        /// </summary>
        private string GetRewrittenMethodDeclaration(int indentLevel)
        {
            var indent = GetIndent(indentLevel);
            string text = indent;

            if (this.AccessModifier == AccessModifier.Protected)
            {
                text += "protected ";
            }
            else if (this.AccessModifier == AccessModifier.Private)
            {
                text += "private ";
            }

            if (this.IsPartial)
            {
                text += "partial ";
            }

            if (this.IsAsync)
            {
                text += "async ";
            }

            if (this.InheritanceModifier == InheritanceModifier.Abstract)
            {
                text += "abstract ";
            }
            else if (this.InheritanceModifier == InheritanceModifier.Virtual)
            {
                text += "virtual ";
            }
            else if (this.InheritanceModifier == InheritanceModifier.Override)
            {
                text += "override ";
            }

            text += this.TypeIdentifier.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            for (int idx = 0; idx < this.Parameters.Count; idx++)
            {
                text += this.Parameters[idx].TextUnit.Text;

                if (idx < this.Parameters.Count - 1)
                {
                    text += " ";
                }
            }

            text += this.RightParenthesisToken.TextUnit.Text;

            if (this.StatementBlock != null)
            {
                this.StatementBlock.Rewrite(indentLevel);
                text += "\n" + this.StatementBlock.TextUnit.Text;
            }
            else
            {
                text += this.SemicolonToken.TextUnit.Text;
            }

            text += "\n";

            return text;
        }
    }
}
