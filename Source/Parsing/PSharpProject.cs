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
using Microsoft.CodeAnalysis.CSharp;
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

        #region internal API

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
            tree = this.RewriteDeclarations(program, tree);
            tree = this.RewriteTypes(tree);
            tree = this.RewriteStatements(tree);
            tree = this.RewriteExpressions(tree);

            tree = this.InsertLibraries(tree);

            var project = this.Project;
            ProgramInfo.ReplaceSyntaxTree(tree, ref project);
            this.Project = project;
        }

        /// <summary>
        /// Rewrites the P# declarations to C#.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        private SyntaxTree RewriteDeclarations(IPSharpProgram program, SyntaxTree tree)
        {
            string text = null;
            if (Configuration.RunStaticAnalysis || Configuration.RunDynamicAnalysis)
            {
                text = program.Model();
            }
            else
            {
                text = program.Rewrite();
            }

            return this.UpdateSyntaxTree(tree, text);
        }

        /// <summary>
        /// Rewrites the P# types to C#.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        private SyntaxTree RewriteTypes(SyntaxTree tree)
        {
            tree = new MachineTypeRewriter(this).Rewrite(tree);
            tree = new HaltEventRewriter(this).Rewrite(tree);

            return tree;
        }

        /// <summary>
        /// Rewrites the P# statements to C#.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        private SyntaxTree RewriteStatements(SyntaxTree tree)
        {
            tree = new CreateMachineRewriter(this).Rewrite(tree);
            tree = new SendRewriter(this).Rewrite(tree);
            tree = new RaiseRewriter(this).Rewrite(tree);
            tree = new PopRewriter(this).Rewrite(tree);
            tree = new AssertRewriter(this).Rewrite(tree);
            
            return tree;
        }

        /// <summary>
        /// Rewrites the P# expressions to C#.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        private SyntaxTree RewriteExpressions(SyntaxTree tree)
        {
            tree = new PayloadRewriter(this).Rewrite(tree);
            tree = new FieldAccessRewriter(this).Rewrite(tree);
            tree = new ThisRewriter(this).Rewrite(tree);
            tree = new NondeterministicChoiceRewriter(this).Rewrite(tree);

            return tree;
        }

        /// <summary>
        /// Inserts the P# libraries.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        private SyntaxTree InsertLibraries(SyntaxTree tree)
        {
            var list = new List<UsingDirectiveSyntax>();

            var systemLib = this.CreateLibrary("System");
            var psharpLib = this.CreateLibrary("Microsoft.PSharp");

            list.Add(systemLib);
            list.Add(psharpLib);

            if (ProgramInfo.IsPFile(tree))
            {
                var psharpColletionsLib = this.CreateLibrary("Microsoft.PSharp.Collections");
                list.Add(psharpColletionsLib);
            }

            list.AddRange(tree.GetCompilationUnitRoot().Usings);

            var root = tree.GetCompilationUnitRoot().WithUsings(SyntaxFactory.List(list));

            return this.UpdateSyntaxTree(tree, root.SyntaxTree.ToString());
        }

        /// <summary>
        /// Creates a new library using syntax node.
        /// </summary>
        /// <param name="name">Library name</param>
        /// <returns>UsingDirectiveSyntax</returns>
        private UsingDirectiveSyntax CreateLibrary(string name)
        {
            var leading = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
            var trailing = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(""));

            var identifier = SyntaxFactory.Identifier(leading, name, trailing);
            var identifierName = SyntaxFactory.IdentifierName(identifier);

            var usingDirective = SyntaxFactory.UsingDirective(identifierName);
            usingDirective = usingDirective.WithSemicolonToken(usingDirective.SemicolonToken.
                WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("\n"))));

            return usingDirective;
        }

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="text">Text</param>
        /// <returns>SyntaxTree</returns>
        private SyntaxTree UpdateSyntaxTree(SyntaxTree tree, string text)
        {
            var source = SourceText.From(text);
            return tree.WithChangedText(source);
        }

        #endregion
    }
}
