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
using Microsoft.PSharp.Parsing.Syntax;

namespace Microsoft.PSharp.Parsing
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
        public List<UsingDeclarationNode> UsingDeclarations;

        /// <summary>
        /// List of namespace declarations.
        /// </summary>
        public List<NamespaceDeclarationNode> NamespaceDeclarations;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path</param>
        public PSharpProgram(string filePath)
            : base(filePath)
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
            int position = 0;

            this.RewrittenText += this.InstrumentPSharpDll(ref position);
            foreach (var node in this.UsingDeclarations)
            {
                node.Rewrite(ref position);
                this.RewrittenText += node.GetRewrittenText();
            }

            foreach (var node in this.NamespaceDeclarations)
            {
                node.Rewrite(ref position);
                this.RewrittenText += node.GetRewrittenText();
            }

            return this.RewrittenText;
        }

        /// <summary>
        /// Returns the full text of this P# program.
        /// </summary>
        /// <returns>Full text</returns>
        public override string GetFullText()
        {
            var text = "";

            foreach (var node in this.UsingDeclarations)
            {
                text += node.GetFullText();
            }

            foreach (var node in this.NamespaceDeclarations)
            {
                text += node.GetFullText();
            }

            return text;
        }

        /// <summary>
        /// Generates the text units of this P# program.
        /// </summary>
        public override void GenerateTextUnits()
        {
            foreach (var node in this.UsingDeclarations)
            {
                node.GenerateTextUnit();
            }

            foreach (var node in this.NamespaceDeclarations)
            {
                node.GenerateTextUnit();
            }
        }

        #endregion
    }
}
