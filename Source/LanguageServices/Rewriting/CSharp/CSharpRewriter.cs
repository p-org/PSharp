//-----------------------------------------------------------------------
// <copyright file="CSharpRewriter.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// An abstract C# program rewriter.
    /// </summary>
    internal abstract class CSharpRewriter
    {
        #region fields

        /// <summary>
        /// The P# project.
        /// </summary>
        protected PSharpProject Project;

        #endregion

        #region protected API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        protected CSharpRewriter(PSharpProject project)
        {
            this.Project = project;
        }

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="text">Text</param>
        /// <returns>SyntaxTree</returns>
        protected SyntaxTree UpdateSyntaxTree(SyntaxTree tree, string text)
        {
            var source = SourceText.From(text);
            return tree.WithChangedText(source);
        }

        #endregion
    }
}
