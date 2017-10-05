//-----------------------------------------------------------------------
// <copyright file="PSharpSyntaxNode.cs">
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
using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// P# syntax node.
    /// </summary>
    internal abstract class PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The program this node belongs to.
        /// </summary>
        protected IPSharpProgram Program;

        /// <summary>
        /// The text unit.
        /// </summary>
        internal protected TextUnit TextUnit
        {
            get; protected set;
        }

        protected const int SpacesPerIndent = 4;
        protected static string OneIndent = new string(' ', SpacesPerIndent);

        /// <summary>
        /// The range of accumulated header tokens
        /// </summary>
        internal TokenRange HeaderTokenRange;

        /// <summary>
        /// Offset and other information that will be used to create the VS Language Service
        /// Projection Buffers for the rewritten form of this declaration.
        /// </summary>
        internal ProjectionInfo ProjectionInfo;

        #endregion

        #region protected API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        protected PSharpSyntaxNode(IPSharpProgram program)
        {
            this.Program = program;
            this.ProjectionInfo = new ProjectionInfo(this);
        }

        /// <summary>
        /// Get the Configuration object.
        /// </summary>
        protected Configuration Configuration => this.Program.GetProject().CompilationContext.Configuration;

        /// <summary>
        /// Creates a string to be used for the specified level of indentation.
        /// </summary>
        protected string GetIndent(int indentLevel)
        {
            return indentLevel == 0
                ? string.Empty
                : new System.Text.StringBuilder().Insert(0, OneIndent, indentLevel).ToString();
        }

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal abstract void Rewrite(int indentLevel);

        #endregion
    }
}
