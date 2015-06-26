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
        internal List<EventDeclaration> EventDeclarations;

        /// <summary>
        /// List of machine declarations.
        /// </summary>
        internal List<MachineDeclaration> MachineDeclarations;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="filePath">File path</param>
        public PProgram(PSharpProject project, string filePath)
            : base(project, filePath)
        {
            this.EventDeclarations = new List<EventDeclaration>();
            this.MachineDeclarations = new List<MachineDeclaration>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        /// <returns>Rewritten text</returns>
        public override string Rewrite()
        {
            this.RewrittenText = "";

            this.RewrittenText += "namespace Microsoft.PSharp\n";
            this.RewrittenText += "{\n";

            foreach (var node in this.EventDeclarations)
            {
                node.Rewrite();
                this.RewrittenText += node.TextUnit.Text;
            }

            foreach (var node in this.MachineDeclarations)
            {
                node.Rewrite();
                this.RewrittenText += node.TextUnit.Text;
            }

            this.RewrittenText += "}\n";

            return this.RewrittenText;
        }

        /// <summary>
        /// Models the P# program to the C#-IR.
        /// </summary>
        /// <returns>Model text</returns>
        public override string Model()
        {
            this.ModelText = "";

            this.ModelText += "namespace Microsoft.PSharp\n";
            this.ModelText += "{\n";

            foreach (var node in this.EventDeclarations)
            {
                node.Model();
                this.ModelText += node.TextUnit.Text;
            }

            foreach (var node in this.MachineDeclarations)
            {
                node.Model();
                this.ModelText += node.TextUnit.Text;
            }

            this.ModelText += "}\n";

            return this.ModelText;
        }

        #endregion
    }
}
