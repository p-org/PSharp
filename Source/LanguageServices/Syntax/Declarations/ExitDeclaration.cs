using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Exit declaration syntax node.
    /// </summary>
    internal sealed class ExitDeclaration : PSharpSyntaxNode
    {
        /// <summary>
        /// The state parent node.
        /// </summary>
        internal readonly StateDeclaration State;

        /// <summary>
        /// The exit keyword.
        /// </summary>
        internal Token ExitKeyword;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal BlockSyntax StatementBlock;

        /// <summary>
        /// True if the exit action is async.
        /// </summary>
        internal readonly bool IsAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExitDeclaration"/> class.
        /// </summary>
        internal ExitDeclaration(IPSharpProgram program, StateDeclaration stateNode, bool isAsync = false)
            : base(program)
        {
            this.State = stateNode;
            this.IsAsync = isAsync;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C# representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            this.StatementBlock.Rewrite(indentLevel);

            var typeStr = this.IsAsync ? "async System.Threading.Tasks.Task" : "void";
            var suffix = this.IsAsync ? "_async()" : "()";
            var indent = GetIndent(indentLevel);
            string text = indent + $"protected {typeStr} psharp_" + this.State.GetFullyQualifiedName() +
                $"_on_exit_action{suffix}";

            this.ProjectionNode.SetHeaderInfo(this.HeaderTokenRange, indent.Length, text);

            text += "\n";
            this.ProjectionNode.SetCodeChunkInfo(this.StatementBlock.OpenBraceToken.TextUnit.Start, this.StatementBlock.TextUnit.Text, text.Length);
            text += this.StatementBlock.TextUnit.Text + "\n";

            this.TextUnit = this.ExitKeyword.TextUnit.WithText(text);
        }
    }
}
