//-----------------------------------------------------------------------
// <copyright file="BasePSharpTest.cs">
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

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public abstract class BasePSharpTest
    {
        /// <summary>
        /// Get solution from the given text.
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Solution</returns>
        protected Solution GetSolution(string text)
        {
            var workspace = new AdhocWorkspace();
            var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create());
            var solution = workspace.AddSolution(solutionInfo);
            var project = workspace.AddProject("Test", "C#");

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Machine).Assembly.Location)
            };

            project = project.AddMetadataReferences(references);
            workspace.TryApplyChanges(project.Solution);

            var sourceText = SourceText.From(text);
            var doc = project.AddDocument("Program", sourceText, null, "Program.psharp");

            return doc.Project.Solution;
        }
    }
}
