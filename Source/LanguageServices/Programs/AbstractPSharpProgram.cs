//-----------------------------------------------------------------------
// <copyright file="AbstractPSharpProgram.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// An abstract P# program.
    /// </summary>
    public abstract class AbstractPSharpProgram : IPSharpProgram
    {
        #region fields

        /// <summary>
        /// The project that this program belongs to.
        /// </summary>
        internal PSharpProject Project;

        /// <summary>
        /// The syntax tree.
        /// </summary>
        protected SyntaxTree SyntaxTree;

        /// <summary>
        /// File path of the P# program.
        /// </summary>
        protected readonly string FilePath;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        internal AbstractPSharpProgram(PSharpProject project, SyntaxTree tree)
        {
            this.Project = project;
            this.SyntaxTree = tree;
        }

        #endregion

        #region public API

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        public abstract void Rewrite();

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        /// <returns>SyntaxTree</returns>
        public SyntaxTree GetSyntaxTree()
        {
            return this.SyntaxTree;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Creates a new library using syntax node.
        /// </summary>
        /// <param name="name">Library name</param>
        /// <returns>UsingDirectiveSyntax</returns>
        protected UsingDirectiveSyntax CreateLibrary(string name)
        {
            var leading = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
            var trailing = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(""));

            var identifier = SyntaxFactory.Identifier(leading, name, trailing);
            var identifierName = SyntaxFactory.IdentifierName(identifier);

            var usingDirective = SyntaxFactory.UsingDirective(identifierName);
            usingDirective = usingDirective.WithSemicolonToken(usingDirective.SemicolonToken.
                WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("\n"))));

            return usingDirective;
        }

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        /// <param name="text">Text</param>
        protected void UpdateSyntaxTree(string text)
        {
            var source = SourceText.From(text);
            this.SyntaxTree = this.SyntaxTree.WithChangedText(source);
        }

        #endregion
    }
}
