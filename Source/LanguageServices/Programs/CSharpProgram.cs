//-----------------------------------------------------------------------
// <copyright file="CSharpProgram.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A C# program.
    /// </summary>
    public sealed class CSharpProgram : AbstractPSharpProgram
    {
        #region fields

        /// <summary>
        /// List of event identifiers.
        /// </summary>
        internal List<ClassDeclarationSyntax> EventIdentifiers;

        /// <summary>
        /// List of machine identifiers.
        /// </summary>
        internal List<ClassDeclarationSyntax> MachineIdentifiers;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        public CSharpProgram(PSharpProject project, SyntaxTree tree)
            : base(project, tree)
        {
            this.EventIdentifiers = new List<ClassDeclarationSyntax>();
            this.MachineIdentifiers = new List<ClassDeclarationSyntax>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        public override void Rewrite()
        {

        }

        #endregion

        #region private API


        #endregion
    }
}
