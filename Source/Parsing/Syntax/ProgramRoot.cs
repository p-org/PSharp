//-----------------------------------------------------------------------
// <copyright file="ProgramRoot.cs">
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
    /// The root to a P# program.
    /// </summary>
    public sealed class ProgramRoot
    {
        #region fields

        /// <summary>
        /// The rewritten text.
        /// </summary>
        private string RewrittenText;

        /// <summary>
        /// File path of P# program.
        /// </summary>
        internal readonly string FilePath;

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
        public ProgramRoot(string filePath)
            : base()
        {
            this.RewrittenText = "";
            this.FilePath = filePath;

            this.UsingDeclarations = new List<UsingDeclarationNode>();
            this.NamespaceDeclarations = new List<NamespaceDeclarationNode>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        /// <returns>Rewritten text</returns>
        public string Rewrite()
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
        public string GetFullText()
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
        /// Returns the rewritten to C#-IR text of this P# program.
        /// </summary>
        /// <returns>Rewritten text</returns>
        public string GetRewrittenText()
        {
            return this.RewrittenText;
        }

        #endregion

        #region internal API

        /// <summary>
        /// Generates the text units of this P# program.
        /// </summary>
        internal void GenerateTextUnits()
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

        #region private API

        /// <summary>
        /// Instrument the P# dll.
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Text</returns>
        private string InstrumentPSharpDll(ref int position)
        {
            var text = "using Microsoft.PSharp;\n";
            position += text.Length;
            return text;
        }

        #endregion
    }
}
