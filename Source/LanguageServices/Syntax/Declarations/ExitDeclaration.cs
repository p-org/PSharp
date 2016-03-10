//-----------------------------------------------------------------------
// <copyright file="ExitDeclaration.cs">
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
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Exit declaration syntax node.
    /// </summary>
    internal sealed class ExitDeclaration : PSharpSyntaxNode
    {
        #region fields

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

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="stateNode">StateDeclaration</param>
        internal ExitDeclaration(IPSharpProgram program, StateDeclaration stateNode)
            : base(program)
        {
            this.State = stateNode;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            this.StatementBlock.Rewrite();

            string text = "protected override void OnExit()";
            text += StatementBlock.TextUnit.Text;

            base.TextUnit = new TextUnit(text, this.ExitKeyword.TextUnit.Line);
        }

        #endregion
    }
}
