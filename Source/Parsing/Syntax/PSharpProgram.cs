//-----------------------------------------------------------------------
// <copyright file="PSharpProgram.cs">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// A P# program.
    /// </summary>
    public sealed class PSharpProgram : AbstractPSharpProgram
    {
        #region fields
        
        /// <summary>
        /// List of using declarations.
        /// </summary>
        internal List<UsingDeclarationNode> UsingDeclarations;

        /// <summary>
        /// List of namespace declarations.
        /// </summary>
        internal List<NamespaceDeclarationNode> NamespaceDeclarations;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="filePath">File path</param>
        public PSharpProgram(PSharpProject project, string filePath)
            : base(project, filePath)
        {
            this.UsingDeclarations = new List<UsingDeclarationNode>();
            this.NamespaceDeclarations = new List<NamespaceDeclarationNode>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        /// <returns>Rewritten text</returns>
        public override string Rewrite()
        {
            this.RewrittenText = "";

            this.RewrittenText += base.InstrumentSystemLibrary();
            this.RewrittenText += base.InstrumentSystemCollectionsGenericLibrary();
            this.RewrittenText += base.InstrumentPSharpLibrary();

            foreach (var node in this.UsingDeclarations)
            {
                node.Rewrite(this);
                this.RewrittenText += node.GetRewrittenText();
            }

            foreach (var node in this.NamespaceDeclarations)
            {
                node.Rewrite(this);
                this.RewrittenText += node.GetRewrittenText();
            }

            return this.RewrittenText;
        }

        #endregion
    }
}
