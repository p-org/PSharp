//-----------------------------------------------------------------------
// <copyright file="EntryDeclaration.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Entry declaration syntax node.
    /// </summary>
    internal sealed class EntryDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The state parent node.
        /// </summary>
        internal readonly StateDeclaration State;

        /// <summary>
        /// The entry keyword.
        /// </summary>
        internal Token EntryKeyword;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal BlockSyntax StatementBlock;

        /// <summary>
        /// True if the entry action is async.
        /// </summary>
        internal readonly bool IsAsync;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="stateNode">StateDeclaration</param>
        /// <param name="isAsync">True if the entry action is async</param>
        internal EntryDeclaration(IPSharpProgram program, StateDeclaration stateNode, bool isAsync = false)
            : base(program)
        {
            this.State = stateNode;
            this.IsAsync = isAsync;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            this.StatementBlock.Rewrite(indentLevel);

            var typeStr = this.IsAsync ? "async Task" : "void";
            var suffix = this.IsAsync ? "_async()" : "()";
            string text = GetIndent(indentLevel) + $"protected {typeStr} psharp_" + this.State.GetFullyQualifiedName() +
                $"_on_entry_action{suffix}";
            text += "\n" + StatementBlock.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.EntryKeyword.TextUnit.Line);
        }

        #endregion
    }
}
