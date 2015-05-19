//-----------------------------------------------------------------------
// <copyright file="PProgram.cs">
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
    /// A P program.
    /// </summary>
    public sealed class PProgram : AbstractPSharpProgram
    {
        #region fields

        /// <summary>
        /// List of event declarations.
        /// </summary>
        public List<EventDeclarationNode> EventDeclarations;

        /// <summary>
        /// List of machine declarations.
        /// </summary>
        public List<MachineDeclarationNode> MachineDeclarations;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path</param>
        public PProgram(string filePath)
            : base(filePath)
        {
            this.EventDeclarations = new List<EventDeclarationNode>();
            this.MachineDeclarations = new List<MachineDeclarationNode>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        /// <returns>Rewritten text</returns>
        public override string Rewrite()
        {
            this.RewrittenText = "";
            int position = 0;

            this.RewrittenText += base.InstrumentSystemLibrary(ref position);
            this.RewrittenText += base.InstrumentSystemCollectionsGenericLibrary(ref position);
            this.RewrittenText += base.InstrumentPSharpLibrary(ref position);
            this.RewrittenText += base.InstrumentPSharpCollectionsLibrary(ref position);

            this.RewrittenText += "namespace Microsoft.PSharp\n";
            this.RewrittenText += "{\n";

            foreach (var node in this.EventDeclarations)
            {
                node.Rewrite(ref position);
                this.RewrittenText += node.GetRewrittenText();
            }

            foreach (var node in this.MachineDeclarations)
            {
                node.Rewrite(ref position);
                this.RewrittenText += node.GetRewrittenText();
            }

            this.RewrittenText += "}\n";

            return this.RewrittenText;
        }

        /// <summary>
        /// Returns the full text of this P# program.
        /// </summary>
        /// <returns>Full text</returns>
        public override string GetFullText()
        {
            var text = "";

            foreach (var node in this.EventDeclarations)
            {
                text += node.GetFullText();
            }

            foreach (var node in this.MachineDeclarations)
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
            foreach (var node in this.EventDeclarations)
            {
                node.GenerateTextUnit();
            }

            foreach (var node in this.MachineDeclarations)
            {
                node.GenerateTextUnit();
            }
        }

        #endregion
    }
}
