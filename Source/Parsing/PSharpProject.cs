//-----------------------------------------------------------------------
// <copyright file="PSharpProject.cs">
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
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Microsoft.PSharp.Parsing.Syntax;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// A P# project.
    /// </summary>
    public sealed class PSharpProject
    {
        #region fields

        /// <summary>
        /// The C# project.
        /// </summary>
        internal Project Project;

        /// <summary>
        /// List of P# programs in the project.
        /// </summary>
        internal List<PSharpProgram> PSharpPrograms;

        /// <summary>
        /// List of P programs in the project.
        /// </summary>
        internal List<PProgram> PPrograms;

        /// <summary>
        /// Map from P# programs to syntax trees.
        /// </summary>
        internal Dictionary<IPSharpProgram, SyntaxTree> ProgramMap;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">Project</param>
        public PSharpProject(Project project)
        {
            this.Project = project;
            this.PSharpPrograms = new List<PSharpProgram>();
            this.PPrograms = new List<PProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Parses the project.
        /// </summary>
        public void Parse()
        {
            var compilation = this.Project.GetCompilationAsync().Result;

            foreach (var tree in compilation.SyntaxTrees.ToList())
            {
                if (ProgramInfo.IsPSharpFile(tree))
                {
                    this.ParsePSharpSyntaxTree(tree);
                }
                else if (ProgramInfo.IsPFile(tree))
                {
                    this.ParsePSyntaxTree(tree);
                }
            }
        }

        /// <summary>
        /// Rewrites the P# project to the C#-IR.
        /// </summary>
        public void Rewrite()
        {
            foreach (var kvp in this.ProgramMap)
            {
                this.RewriteProgram(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region private API

        /// <summary>
        /// Parses a P# syntax tree to C#.
        /// th
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        private void ParsePSharpSyntaxTree(SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var tokens = new PSharpLexer().Tokenize(root.ToFullString());
            var program = new PSharpParser(this, tree.FilePath).ParseTokens(tokens);

            this.PSharpPrograms.Add(program as PSharpProgram);
            this.ProgramMap.Add(program, tree);
        }

        /// <summary>
        /// Parses a P syntax tree to C#.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        private void ParsePSyntaxTree(SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var tokens = new PLexer().Tokenize(root.ToFullString());
            var program = new PParser(this, tree.FilePath).ParseTokens(tokens);

            this.PPrograms.Add(program as PProgram);
            this.ProgramMap.Add(program, tree);
        }

        /// <summary>
        /// Rewrites a P# or P program to C#.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="tree">SyntaxTree</param>
        private void RewriteProgram(IPSharpProgram program, SyntaxTree tree)
        {
            string rewrittenTree = null;

            if (Configuration.RunStaticAnalysis || Configuration.RunDynamicAnalysis)
            {
                rewrittenTree = program.Model();
            }
            else
            {
                rewrittenTree = program.Rewrite();
            }

            var source = SourceText.From(rewrittenTree);

            var project = this.Project;
            ProgramInfo.ReplaceSyntaxTree(tree.WithChangedText(source), ref project);
            this.Project = project;
        }

        #endregion
    }
}
