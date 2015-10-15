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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A P# project.
    /// </summary>
    public sealed class PSharpProject
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        internal CompilationContext CompilationContext;

        /// <summary>
        /// The P# project name.
        /// </summary>
        internal string Name;

        /// <summary>
        /// List of P# programs in the project.
        /// </summary>
        internal List<PSharpProgram> PSharpPrograms;

        /// <summary>
        /// List of C# programs in the project.
        /// </summary>
        internal List<CSharpProgram> CSharpPrograms;

        /// <summary>
        /// Map from P# programs to syntax trees.
        /// </summary>
        internal Dictionary<IPSharpProgram, SyntaxTree> ProgramMap;

        #endregion

        #region API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PSharpProject()
        {
            this.CompilationContext = CompilationContext.Create();
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        public PSharpProject(CompilationContext context)
        {
            this.CompilationContext = context;
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <param name="projectName">Project name</param>
        public PSharpProject(CompilationContext context, string projectName)
        {
            this.CompilationContext = context;
            this.Name = projectName;
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Parses the project.
        /// </summary>
        public void Parse()
        {
            var project = this.CompilationContext.GetProjectWithName(this.Name);
            var compilation = project.GetCompilationAsync().Result;

            foreach (var tree in compilation.SyntaxTrees.ToList())
            {
                if (this.CompilationContext.IsPSharpFile(tree))
                {
                    this.ParsePSharpSyntaxTree(tree);
                }
                else if (this.CompilationContext.IsCSharpFile(tree))
                {
                    this.ParseCSharpSyntaxTree(tree);
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

        /// <summary>
        /// Is the identifier a machine type.
        /// </summary>
        /// <param name="identifier">IdentifierNameSyntax</param>
        /// <returns>Boolean value</returns>
        internal bool IsMachineType(SyntaxToken identifier)
        {
            var result = this.PSharpPrograms.Any(p => p.NamespaceDeclarations.Any(n => n.MachineDeclarations.Any(
                m => m.Identifier.TextUnit.Text.Equals(identifier.ValueText))));

            return result;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Parses a P# syntax tree to C#.
        /// th
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        private void ParsePSharpSyntaxTree(SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var tokens = new PSharpLexer().Tokenize(root.ToFullString());
            var program = new PSharpParser(this, tree).ParseTokens(tokens);

            this.PSharpPrograms.Add(program as PSharpProgram);
            this.ProgramMap.Add(program, tree);
        }

        /// <summary>
        /// Parses a C# syntax tree to C#.
        /// th
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        private void ParseCSharpSyntaxTree(SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var program = new CSharpParser(this, tree).Parse();

            this.CSharpPrograms.Add(program as CSharpProgram);
            this.ProgramMap.Add(program, tree);
        }

        /// <summary>
        /// Rewrites a P# or P program to C#.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="tree">SyntaxTree</param>
        private void RewriteProgram(IPSharpProgram program, SyntaxTree tree)
        {
            program.Rewrite();

            var project = this.CompilationContext.GetProjectWithName(this.Name);
            this.CompilationContext.ReplaceSyntaxTree(program.GetSyntaxTree(), project);
        }

        #endregion
    }
}
