//-----------------------------------------------------------------------
// <copyright file="PSharpSyntaxNode.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// P# syntax node.
    /// </summary>
    internal abstract class PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// True if the node is a model.
        /// </summary>
        internal readonly bool IsModel;

        /// <summary>
        /// The text unit.
        /// </summary>
        internal TextUnit TextUnit;

        #endregion

        #region protected API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="isModel">Is a model</param>
        protected PSharpSyntaxNode(bool isModel)
        {
            this.IsModel = isModel;
        }

        #endregion

        #region internal API

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal abstract string GetRewrittenText();

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal abstract void Rewrite(IPSharpProgram program);

        #endregion
    }
}
