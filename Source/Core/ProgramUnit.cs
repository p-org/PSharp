//-----------------------------------------------------------------------
// <copyright file="ProgramUnit.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.Tooling
{
    public class ProgramUnit
    {
        #region fields

        /// <summary>
        /// The name of this program unit.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The P# project contains in this unit.
        /// </summary>
        private Project Project;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new P# program unit.
        /// </summary>
        /// <param name="project">Project</param>
        /// <returns>ProgramUnit</returns>
        public static ProgramUnit Create(Project project)
        {
            return new ProgramUnit(project);
        }

        /// <summary>
        /// Returns the project.
        /// </summary>
        /// <returns>Project</returns>
        public Project GetProject()
        {
            this.Project = ProgramInfo.Solution.Projects.FirstOrDefault(
                val => val.Name.Equals(Project.Name));
            return this.Project;
        }

        /// <summary>
        /// Prints the syntax trees of the compilation unit.
        /// </summary>
        public void Print()
        {
            var compilation = this.Project.GetCompilationAsync().Result;
            foreach (var tree in compilation.SyntaxTrees)
            {
                var root = (CompilationUnitSyntax)tree.GetRoot();
                Console.WriteLine(root.GetText());
            }
        }

        #endregion

        #region private API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project"></param>
        private ProgramUnit(Project project)
        {
            this.Name = project.Name;
            this.Project = project;
        }

        #endregion
    }
}
