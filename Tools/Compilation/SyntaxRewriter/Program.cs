//-----------------------------------------------------------------------
// <copyright file="Program.cs">
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

using Microsoft.Build.Framework;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# syntax rewriter.
    /// </summary>
    public class SyntaxRewriter
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Output.WriteLine("Usage: PSharpSyntaxRewriter.exe file.psharp");
                return;
            }

            // Get input file as string
            var input_string = "";
            try
            {
                input_string = System.IO.File.ReadAllText(args[0]);
            }
            catch (System.IO.IOException e)
            {
                Output.WriteLine("Error: {0}", e.Message);
                return;
            }

            // Translate and print on console
            string errors = "";
            var output = Translate(input_string, out errors);
            Output.WriteLine("{0}", output == null ? "Parse Error: " + errors : output);
        }

        /// <summary>
        /// Translates the specified text from P# to C#.
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Text</returns>
        public static string Translate(string text, out string errors)
        {
            var configuration = Configuration.Create();
            configuration.Verbose = 2;
            errors = null;

            var context = CompilationContext.Create(configuration).LoadSolution(text);

            try
            {
                ParsingEngine.Create(context).Run();
                RewritingEngine.Create(context).Run();

                var syntaxTree = context.GetProjects()[0].PSharpPrograms[0].GetSyntaxTree();

                return syntaxTree.ToString();
            }
            catch (ParsingException ex)
            {
                errors = ex.Message;
                return null;
            }
            catch (RewritingException ex)
            {
                errors = ex.Message;
                return null;
            }
        }
    }

    public class Rewriter : ITask
    {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public ITaskItem[] InputFiles { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        public bool Execute()
        {
            for (int i = 0; i < InputFiles.Length; i++)
            {
                var inp = System.IO.File.ReadAllText(InputFiles[i].ItemSpec);
                string errors;
                var outp = SyntaxRewriter.Translate(inp, out errors);
                if (outp != null)
                {
                    System.IO.File.WriteAllText(OutputFiles[i].ItemSpec, outp);
                }
                else
                {
                    // replace Program.psharp with the actual file name
                    errors = errors.Replace("Program.psharp", System.IO.Path.GetFileName(InputFiles[i].ItemSpec));
                    // print a compiler error with log
                    System.IO.File.WriteAllText(OutputFiles[i].ItemSpec, 
                        string.Format("#error Psharp Compiler Error {0} /* {0} {1} {0} */ ", "\n", errors));
                }
            }

            return true;
        }
    }
}
