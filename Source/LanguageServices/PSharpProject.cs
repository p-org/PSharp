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

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A P# project.
    /// </summary>
    public sealed class PSharpProject
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        internal LanguageServicesConfiguration Configuration;

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
        /// List of P programs in the project.
        /// </summary>
        internal List<PProgram> PPrograms;

        /// <summary>
        /// Map from P# programs to syntax trees.
        /// </summary>
        internal Dictionary<IPSharpProgram, SyntaxTree> ProgramMap;

        #endregion

        #region API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public PSharpProject(LanguageServicesConfiguration configuration)
        {
            this.Configuration = configuration;
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.PPrograms = new List<PProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="projectName">Project name</param>
        public PSharpProject(LanguageServicesConfiguration configuration, string projectName)
        {
            this.Configuration = configuration;
            this.Name = projectName;
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.PPrograms = new List<PProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Parses the project.
        /// </summary>
        public void Parse()
        {
            var project = ProgramInfo.GetProjectWithName(this.Name);
            var compilation = project.GetCompilationAsync().Result;

            foreach (var tree in compilation.SyntaxTrees.ToList())
            {
                if (ProgramInfo.IsPSharpFile(tree))
                {
                    this.ParsePSharpSyntaxTree(tree);
                }
                else if (ProgramInfo.IsCSharpFile(tree))
                {
                    this.ParseCSharpSyntaxTree(tree);
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

        /// <summary>
        /// Is the identifier a machine type.
        /// </summary>
        /// <param name="identifier">IdentifierNameSyntax</param>
        /// <returns>Boolean value</returns>
        internal bool IsMachineType(SyntaxToken identifier)
        {
            var result = false;

            result = this.PSharpPrograms.Any(p => p.NamespaceDeclarations.Any(n => n.MachineDeclarations.Any(
                    m => m.Identifier.TextUnit.Text.Equals(identifier.ValueText))));

            if (!result)
            {
                result = this.PPrograms.Any(p => p.MachineDeclarations.Any(
                    m => m.Identifier.TextUnit.Text.Equals(identifier.ValueText)));
            }

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
        /// Parses a P syntax tree to C#.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        private void ParsePSyntaxTree(SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var tokens = new PLexer().Tokenize(root.ToFullString());
            var program = new PParser(this, tree).ParseTokens(tokens);

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
            program.Rewrite();

            var project = ProgramInfo.GetProjectWithName(this.Name);
            ProgramInfo.ReplaceSyntaxTree(program.GetSyntaxTree(), project);
        }

        #endregion
    }
}
