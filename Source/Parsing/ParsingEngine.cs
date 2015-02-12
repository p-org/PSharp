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
using System.Collections.Generic;
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
            foreach (var programUnit in ProgramInfo.ProgramUnits.ToList())
            {
                var project = programUnit.Project;

                // Performs rewriting.
                ParsingEngine.RewriteSyntaxTrees(project);
            }

            // Updates the program info.
            ProgramInfo.Update();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrite P# syntax trees to C# syntax trees of the project.
        /// </summary>
        /// <param name="project">Project</param>
        private static void RewriteSyntaxTrees(Project project)
        {
            var compilation = project.GetCompilationAsync().Result;

            var rewrittenTrees = new HashSet<SyntaxTree>();
            foreach (var tree in compilation.SyntaxTrees.ToList())
            {
                if (!ParsingEngine.IsProgramSyntaxTree(tree))
                {
                    continue;
                }

                var root = (CompilationUnitSyntax)tree.GetRoot();

                var tokens = new Lexer(root.ToFullString()).GetTokens();
                tokens = new TopLevelRewriter(tokens).GetRewrittenTokens();

                var rewrittenTree = ParsingEngine.ConvertToText(tokens);
                var source = SourceText.From(rewrittenTree);
                rewrittenTrees.Add(tree.WithChangedText(source));
            }

            ProgramInfo.ReplaceSyntaxTrees(project, rewrittenTrees);
        }

        /// <summary>
        /// Converts the tokens to text.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <returns>Text</returns>
        private static string ConvertToText(List<Token> tokens)
        {
            var text = "";
            foreach (var token in tokens)
            {
                text += token.String;
            }

            return text;
        }

        /// <summary>
        /// Returns true if the syntax tree belongs to the P# program.
        /// Else returns false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        private static bool IsProgramSyntaxTree(SyntaxTree tree)
        {
            if (tree.FilePath.Contains("\\AssemblyInfo.cs") ||
                    tree.FilePath.Contains(".NETFramework,"))
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
