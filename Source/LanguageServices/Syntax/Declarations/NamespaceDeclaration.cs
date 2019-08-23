using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Namespace declaration syntax node.
    /// </summary>
    internal sealed class NamespaceDeclaration : PSharpSyntaxNode
    {
        /// <summary>
        /// The namespace keyword.
        /// </summary>
        internal Token NamespaceKeyword;

        /// <summary>
        /// The identifier tokens.
        /// </summary>
        internal List<Token> IdentifierTokens;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        internal Token LeftCurlyBracketToken;

        /// <summary>
        /// List of event declarations.
        /// </summary>
        internal EventDeclarations EventDeclarations;

        /// <summary>
        /// List of machine declarations.
        /// </summary>
        internal List<MachineDeclaration> MachineDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        /// <summary>
        /// Qualified name of the namespace.
        /// </summary>
        internal string QualifiedName
        {
            get
            {
                return this.IdentifierTokens.Select(t => t.TextUnit.Text).
                    Aggregate(string.Empty, (acc, name) => acc + name);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceDeclaration"/> class.
        /// </summary>
        internal NamespaceDeclaration(IPSharpProgram program)
            : base(program)
        {
            this.IdentifierTokens = new List<Token>();
            this.EventDeclarations = new EventDeclarations();
            this.MachineDeclarations = new List<MachineDeclaration>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            var indent = GetIndent(indentLevel); // indent here will likely always be 0
            foreach (var node in this.EventDeclarations)
            {
                this.ProjectionNode.AddChild(node.ProjectionNode);
                node.Rewrite(indentLevel + 1);
            }

            foreach (var node in this.MachineDeclarations)
            {
                this.ProjectionNode.AddChild(node.ProjectionNode);
                node.Rewrite(indentLevel + 1);
            }

            var newLine = string.Empty;
            var text = indent + this.GetRewrittenNamespaceDeclaration(ref newLine);

            var realMachines = this.MachineDeclarations.FindAll(m => !m.IsMonitor);
            var monitors = this.MachineDeclarations.FindAll(m => m.IsMonitor);

            foreach (var node in realMachines)
            {
                node.ProjectionNode.SetOffsetInParent(text.Length);
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            foreach (var node in monitors)
            {
                node.ProjectionNode.SetOffsetInParent(text.Length);
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            text += indent + this.RightCurlyBracketToken.TextUnit.Text + "\n";

            this.TextUnit = this.NamespaceKeyword.TextUnit.WithText(text);
        }

        /// <summary>
        /// Returns the rewritten namespace declaration.
        /// </summary>
        private string GetRewrittenNamespaceDeclaration(ref string newLine)
        {
            var text = this.NamespaceKeyword.TextUnit.Text;

            text += " ";

            foreach (var token in this.IdentifierTokens)
            {
                text += token.TextUnit.Text;
            }

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.EventDeclarations)
            {
                node.ProjectionNode.SetOffsetInParent(text.Length);
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            return text;
        }
    }
}
