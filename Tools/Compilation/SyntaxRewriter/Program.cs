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
using Microsoft.PSharp.Utilities;
using System.Collections.Generic;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# syntax rewriter.
    /// </summary>
    public class SyntaxRewriter
    {
        static void Main(string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                Output.WriteLine("Usage: PSharpSyntaxRewriter.exe file[.cs|.psharp] [ProjectFile.csproj]");
                return;
            }

            var configuration = Configuration.Create();
            var filename = args[0];

            if(!System.IO.File.Exists(filename))
            {
                Output.WriteLine("Error: File not found: {0}", filename);
                return;
            }

            if(args.Length == 2)
            {
                if(!args[1].EndsWith(".csproj") || !System.IO.File.Exists(args[1]))
                {
                    Output.WriteLine("Error: Please provide a valid csproj file: {0}", args[1]);
                    return;
                }

                configuration.ProjectFilePath = args[1];
                configuration.ProjectName = args[1].Replace(".csproj", "");
            }
            

            if (configuration.ProjectName == "")
            {
                // Get input file as string
                var input_string = "";
                try
                {
                    input_string = System.IO.File.ReadAllText(filename);
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
            else
            {
                string errors = "";
                var output = Translate(configuration, new HashSet<string> { filename }, out errors);
                Output.WriteLine("{0}", output == null ? "Parse Error: " + errors : output[filename]);
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

        /// <summary>
        /// Translates the specified text from P# to C#.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="text">Text</param>
        /// <returns>Text for each file</returns>
        public static Dictionary<string, string> 
            Translate(Configuration configuration, HashSet<string> filenames, out string errors)
        {
            errors = null;

            var context = CompilationContext.Create(configuration).LoadSolution();
            var ret = new Dictionary<string, string>();

            try
            {
                ParsingEngine.Create(context).Run();
                RewritingEngine.Create(context).Run();

                var project = context.GetProjects()[0];

                foreach (var filename in filenames)
                {
                    var fullpath = System.IO.Path.GetFullPath(filename);

                    var syntaxTree = filename.EndsWith(".psharp")
                        ? project.PSharpPrograms.Find(p => p.GetSyntaxTree().FilePath == fullpath).GetSyntaxTree()
                        : project.CSharpPrograms.Find(p => p.GetSyntaxTree().FilePath == fullpath).GetSyntaxTree();

                    ret.Add(filename, syntaxTree.ToString());
                }
                return ret;
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
            if (InputFiles.Length == 0)
            {
                return true;
            }

            // Get project file
            var projectFile = InputFiles[0].GetMetadata("DefiningProjectFullPath");
            var projectName = InputFiles[0].GetMetadata("DefiningProjectName");

            // sanity check
            for (int i = 0; i < InputFiles.Length; i++)
            {
                if (InputFiles[i].GetMetadata("DefiningProjectFullPath") != projectFile)
                {
                    return false;
                }
            }

            if (!projectFile.ToLower().EndsWith(".csproj"))
            {
                return false;
            }

            var configuration = Configuration.Create();
            configuration.ProjectFilePath = projectFile;
            configuration.ProjectName = projectName;

            var files = new HashSet<string>();
            string errors = "";

            for (int i = 0; i < InputFiles.Length; i++)
            {
                files.Add(InputFiles[i].ItemSpec);
            }

            var outp = SyntaxRewriter.Translate(configuration, files, out errors);

            if (outp != null)
            {
                for (int i = 0; i < InputFiles.Length; i++)
                {
                    System.IO.File.WriteAllText(OutputFiles[i].ItemSpec, outp[InputFiles[i].ItemSpec]);
                }
            }
            else
            {
                return false;
                
                // replace Program.psharp with the actual file name
                //errors = errors.Replace("Program.psharp", System.IO.Path.GetFileName(InputFiles[i].ItemSpec));
                // print a compiler error with log
                //System.IO.File.WriteAllText(OutputFiles[i].ItemSpec,
                //    string.Format("#error Psharp Compiler Error {0} /* {0} {1} {0} */ ", "\n", errors));
                
            }

            return true;
        }
    }
}
