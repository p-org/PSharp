//-----------------------------------------------------------------------
// <copyright file="ParsingEngine.cs">
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
using System.Linq;

using Microsoft.PSharp.Tooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# parsing engine.
    /// </summary>
    public static class ParsingEngine
    {
        #region public API

        /// <summary>
        /// Runs the P# parsing engine.
        /// </summary>
        public static void Run()
        {
            foreach (var programUnit in ProgramInfo.ProgramUnits)
            {
                var project = programUnit.GetProject();

                // Performs rewriting.
                ParsingEngine.RewriteSyntaxTrees(project);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites syntax trees to C#.
        /// </summary>
        /// <param name="project">Project</param>
        private static void RewriteSyntaxTrees(Project project)
        {
            var compilation = project.GetCompilationAsync().Result;

            foreach (var tree in compilation.SyntaxTrees.ToList())
            {
                if (ProgramInfo.IsPSharpFile(tree))
                {
                    ParsingEngine.RewritePSharpSyntaxTree(ref project, tree);
                }
                else if (ProgramInfo.IsPFile(tree))
                {
                    ParsingEngine.RewritePSyntaxTree(ref project, tree);
                }
            }
        }

        /// <summary>
        /// Rewrites a P# syntax tree to C#.
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="tree">SyntaxTree</param>
        private static void RewritePSharpSyntaxTree(ref Project project, SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var tokens = new PSharpLexer().Tokenize(root.ToFullString());
            var program = new PSharpParser(tree.FilePath).ParseTokens(tokens);
            var rewrittenTree = program.Rewrite();

            var source = SourceText.From(rewrittenTree);

            ProgramInfo.ReplaceSyntaxTree(tree.WithChangedText(source), ref project);
        }

        /// <summary>
        /// Rewrites a P syntax tree to C#.
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="tree">SyntaxTree</param>
        private static void RewritePSyntaxTree(ref Project project, SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var tokens = new PLexer().Tokenize(root.ToFullString());

            foreach (var tok in tokens)
            {
                Console.Write(tok.TextUnit.Text);
            }

            Environment.Exit(1);
        }

        #endregion
    }
}
