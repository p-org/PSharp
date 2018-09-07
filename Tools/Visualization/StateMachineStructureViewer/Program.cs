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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
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
            var projectFile = String.Empty;
            var solutionFile = String.Empty;
            var csVersion = new Version(0, 0);

            var usage = "Usage: PSharpStateMachineStructureViewer.exe file.psharp [file.dgml] [/csVersion:major.minor]";
            
            if (args.Length >= 1 && args.Length <= 3)
            {
                foreach (var arg in args)
                {
                    if (arg.StartsWith("/") || arg.StartsWith("-"))
                    {
                        var parts = arg.Substring(1).Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
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

                            case "p":
                                projectFile = parts[1];
                                break;
                            case "s":
                                solutionFile = parts[1];
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


            if( (projectFile.Length == 0 || solutionFile.Length==0) && infile.Length == 0)
            {
                Output.WriteLine(usage);
                return;
            }


            var configuration = Configuration.Create();
            configuration.Verbose = 2;
            configuration.RewriteCSharpVersion = csVersion;

            CompilationContext context = null;
            if (projectFile.Length > 0 && solutionFile.Length > 0 )
            {
                configuration.ProjectName = projectFile;
                configuration.SolutionFilePath = solutionFile;
                context = CompilationContext.Create(configuration).LoadSolution();
            }
            else if (infile.Length > 0)
            {

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
                context = CompilationContext.Create(configuration).LoadSolution(input_string);
            }

            


            // Translates and prints on console or to file.
            string errors = "";

            var output = CreateDgml(context, out errors, csVersion);
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

        private static void testStuff(CompilationContext context, out string errors)
        {
            errors = null;
            ParsingEngine.Create(context).Run();
            Console.WriteLine("Found nPsharpPrograms=" + context.GetProjects()[0].PSharpPrograms.Count);
            foreach (PSharpProgram prog in context.GetProjects()[0].PSharpPrograms)
            {
                Console.WriteLine(" -- start program --");
                ResolutionHelper.Instance().populateMachines(prog);
                int x = 5;
                if (x > 4)
                {
                    foreach (var ns in prog.NamespaceDeclarations)
                    {
                        Console.WriteLine(ns.QualifiedName);
                    }
                    List<string> activeNamespaces = ResolutionHelper.GetActiveNamespacesFromUsingDirectives(prog);
                    foreach (var asn in activeNamespaces)
                    {
                        Console.WriteLine("- " + asn);
                    }
                }
                Console.WriteLine(" -- end program --");
            }


            foreach (var minfoKv in ResolutionHelper.Instance().machineLookup)
            {
                List<string> activeNamespaces = ResolutionHelper.GetActiveNamespacesFromUsingDirectives(minfoKv.Value.program);
                minfoKv.Value.resolveBaseMachine(activeNamespaces);
                Console.WriteLine("{0} < {1} ", minfoKv.Value.uniqueName, (minfoKv.Value.baseMachine != null) ? minfoKv.Value.baseMachine.uniqueName : "null");
            }
            
            
            

            foreach (PSharpProgram prog in context.GetProjects()[0].PSharpPrograms)
            {

            }

        }


        /// <summary>
        /// Produces the Dgml file for the given program
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Text</returns>
        public static string CreateDgml(CompilationContext context, out string errors, Version csVersion)
        {
            int x = 5;
            if (x > 4)
            {
                
                testStuff(context, out errors);
                return "";
            }

            try
            {
                ParsingEngine.Create(context).Run();
                errors = null;
                
                MemoryStream memStream = new MemoryStream();
                using (var writer = new XmlTextWriter(memStream, Encoding.UTF8))
                {
                    EmitStateMachineStructure(context.GetProjects()[0].PSharpPrograms[0], writer);
                    //context.GetProjects()[0].PSharpPrograms[0].EmitStateMachineStructure(writer);
                }
                return Encoding.UTF8.GetString(memStream.ToArray());
            }
            catch (ParsingException ex)
            {
                errors = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Emits dgml representation of the state machine structure
        /// </summary>
        /// <param name="writer">XmlTestWriter</param>
        public static void EmitStateMachineStructure(PSharpProgram prog, XmlTextWriter writer)
        {


            // Starts document.
            writer.WriteStartDocument(true);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;

            // Starts DirectedGraph element.
            
            DgmlWriter.WriteAll(ResolutionHelper.Instance().GetAllMachines(), writer);


            // Ends DirectedGraph element.
            writer.WriteEndElement();

            // Ends document.
            writer.WriteEndDocument();

        }


    }

}
