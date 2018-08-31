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
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A tool to produce Dgml files reflecting the State Diagram of machines in the PSharp program
    /// </summary>
    public class StateDiagramViewer
    {
        static void Main(string[] args)
        {
            var infile = string.Empty;
            var outfile = string.Empty;
            var csVersion = new Version(0, 0);

            var usage = "Usage: PSharpStateMachineStructureViewer.exe file.psharp [file.dgml] [/csVersion:major.minor]";

            if (args.Length >= 1 && args.Length <= 3)
            {
                foreach (var arg in args)
                {
                    if (arg.StartsWith("/") || arg.StartsWith("-"))
                    {
                        var parts = arg.Substring(1).Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        switch (parts[0].ToLower())
                        {
                            case "?":
                                Output.WriteLine(usage);
                                return;
                            case "csversion":
                                bool parseVersion(string value, out Version version)
                                {
                                    if (int.TryParse(value, out int intVer)) {
                                        version = new Version(intVer, 0);
                                        return true;
                                    }
                                    return Version.TryParse(value, out version);
                                }
                                if (parts.Length != 2 || !parseVersion(parts[1], out csVersion))
                                {
                                    Output.WriteLine("Error: option csVersion requires a version major[.minor] value");
                                    return;
                                }
                                break;
                            default:
                                Output.WriteLine($"Error: unknown option {parts[0]}");
                                return;
                        }
                    }
                    else if (infile.Length == 0)
                    {
                        infile = arg;
                    }
                    else
                    {
                        outfile = arg;
                    }
                }
            }

            if (infile.Length == 0)
            {
                Output.WriteLine(usage);
                return;
            }

            // Gets input file as string.
            var input_string = "";
            try
            {
                input_string = File.ReadAllText(infile);
            }
            catch (IOException e)
            {
                Output.WriteLine("Error: {0}", e.Message);
                return;
            }

            // Translates and prints on console or to file.
            string errors = "";
            var output = CreateDgml(input_string, out errors, csVersion);
            var result = string.Format("{0}", output == null ? "Parse Error: " + errors :
                output);

            if (!string.IsNullOrEmpty(outfile))
            {
                try
                {
                    File.WriteAllLines(outfile, new[] { result });
                    return;
                }
                catch (Exception ex)
                {
                    Output.WriteLine("Error writing to file: {0}", ex.Message);
                }
            }

            Output.WriteLine("{0}", result);
        }

        /// <summary>
        /// Produces the Dgml file for the given program
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Text</returns>
        public static string CreateDgml(string text, out string errors, Version csVersion)
        {
            var configuration = Configuration.Create();
            configuration.Verbose = 2;
            configuration.RewriteCSharpVersion = csVersion;
            errors = null;

            var context = CompilationContext.Create(configuration).LoadSolution(text);

            try
            {
                ParsingEngine.Create(context).Run();
                //RewritingEngine.Create(context).Run(); // I don't think we need this.

                var syntaxTree = context.GetProjects()[0].PSharpPrograms[0].GetSyntaxTree();
                MemoryStream memStream = new MemoryStream();
                using (var writer = new XmlTextWriter(memStream, Encoding.UTF8))
                {
                    context.GetProjects()[0].PSharpPrograms[0].EmitStateMachineStructure(writer);
                }
                return Encoding.UTF8.GetString(memStream.ToArray());
            }
            catch (ParsingException ex)
            {
                errors = ex.Message;
                return null;
            }
            //catch (RewritingException ex)
            //{
            //    errors = ex.Message;
            //    return null;
            //}
        }

    }

}
