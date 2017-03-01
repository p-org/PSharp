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

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Build.Framework;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# syntax rewriter.
    /// </summary>
    public class SyntaxRewriterProcess
    {
        static void Main(string[] args)
        {
            // number of args must be even
            if (args.Length % 2 != 0)
            {
                IO.PrintLine("Usage: PSharpSyntaxRewriterProcess.exe file1.psharp, outfile1.cs, file2.pshap, outfile2.cs, ...");
                return;
            }

            int count = 0;
            while (count < args.Length)
            {
                string inputFileName = args[count];
                count++;
                string outputFileName = args[count];
                count++;
                // Get input file as string
                var inputString = "";
                try
                {
                    inputString = System.IO.File.ReadAllText(inputFileName);
                }
                catch (System.IO.IOException e)
                {
                    IO.PrintLine("Error: {0}", e.Message);
                    return;
                }

                // Translate and write to output file
                string errors = "";
                var outputString = Translate(inputString, out errors);
                if (outputString == null)
                {
                    // replace Program.psharp with the actual file name
                    errors = errors.Replace("Program.psharp", System.IO.Path.GetFileName(inputFileName));
                    // print a compiler error with log
                    System.IO.File.WriteAllText(outputFileName,
                        string.Format("#error Psharp Compiler Error {0} /* {0} {1} {0} */ ", "\n", errors));
                }
                else
                {
                    System.IO.File.WriteAllText(outputFileName, outputString);
                }
            }
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

    public class RewriterAsSeparateProcess : ITask
    {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public ITaskItem[] InputFiles { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        public bool Execute()
        {
            string processInputString = "";
            for (int i = 0; i < InputFiles.Length; i++)
            {
                processInputString += InputFiles[i].ItemSpec;
                processInputString += " ";
                processInputString += OutputFiles[i].ItemSpec;
                if (i + 1 < InputFiles.Length)
                {
                    processInputString += " ";
                }
            }
            var process = new Process();
            var processStartInfo = new ProcessStartInfo(this.GetType().Assembly.Location, processInputString);
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();
            return true;
        }
    }
}
