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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# parsing engine.
    /// </summary>
    public static class ParsingEngine
    {
        #region fields

        private static List<PSharpProject> Projects;

        #endregion

        #region public API

        /// <summary>
        /// Runs the P# parsing engine.
        /// </summary>
        public static void Run()
        {
            ParsingEngine.Projects = new List<PSharpProject>();

            // Parse the projects.
            foreach (var programUnit in ProgramInfo.ProgramUnits)
            {
                var project = new PSharpProject(programUnit.GetProject());
                project.Parse();
                ParsingEngine.Projects.Add(project);
            }

            // Rewrite the projects.
            foreach (var project in ParsingEngine.Projects)
            {
                project.Rewrite();
            }
        }

        #endregion
    }
}
