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
using System.Runtime.Serialization;
using System.IO;

using Microsoft.PSharp.TestingServices.Coverage;
using System.Xml;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// P# coverage report merger.
    /// </summary>
    class Program
    {
        #region fields

        /// <summary>
        /// Input coverage info.
        /// </summary>
        static List<CoverageInfo> InputFiles;

        /// <summary>
        /// Output file prefix.
        /// </summary>
        static string OutputFilePrefix;

        #endregion

        static void Main(string[] args)
        {
            if(!ParseArgs(args))
            {
                return;
            }

            if(InputFiles.Count == 0)
            {
                Console.WriteLine("Error: No input files provided");
                return;
            }
            
            var cinfo = new CoverageInfo();
            foreach(var other in InputFiles)
            {
                cinfo.Merge(other);
            }

            // Dump
            string name = OutputFilePrefix;
            string directoryPath = Environment.CurrentDirectory;

            var codeCoverageReporter = new CodeCoverageReporter(cinfo);

            string[] graphFiles = Directory.GetFiles(directoryPath, name + "_*.dgml");
            string graphFilePath = Path.Combine(directoryPath, name + "_" + graphFiles.Length + ".dgml");

            IO.Error.PrintLine($"... Writing {graphFilePath}");
            codeCoverageReporter.EmitVisualizationGraph(graphFilePath);

            string[] coverageFiles = Directory.GetFiles(directoryPath, name + "_*.coverage.txt");
            string coverageFilePath = Path.Combine(directoryPath, name + "_" + coverageFiles.Length + ".coverage.txt");

            IO.Error.PrintLine($"... Writing {coverageFilePath}");
            codeCoverageReporter.EmitCoverageReport(coverageFilePath);
        }


        /// <summary>
        /// Parses the arguments.
        /// </summary>
        /// <param name="args"></param>
        static bool ParseArgs(string[] args)
        {
            InputFiles = new List<CoverageInfo>();
            OutputFilePrefix = "merged";

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: PSharpMergeCoverageReports.exe file1.sci file2.sci ... [/output:prefix]");
                return false;
            }
            foreach (var arg in args)
            {
                if (arg.StartsWith("/output:"))
                {
                    OutputFilePrefix = arg.Substring("/output:".Length);
                    continue;
                }
                else if (arg.StartsWith("/"))
                {
                    Console.WriteLine("Error: Unknown flag {0}", arg);
                    return false;
                }
                else
                {
                    // suffix
                    if (!arg.EndsWith(".sci"))
                    {
                        Console.WriteLine("Error: Only sci files accepted as input, got {0}", arg);
                        return false;
                    }

                    // file exists?
                    if (!System.IO.File.Exists(arg))
                    {
                        Console.WriteLine("Error: File {0} not found", arg);
                        return false;
                    }

                    try
                    {
                        using (var fs = new FileStream(arg, FileMode.Open))
                        {
                            using (var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas()))
                            {
                                var ser = new DataContractSerializer(typeof(CoverageInfo));
                                var cinfo = (CoverageInfo)ser.ReadObject(reader, true);
                                InputFiles.Add(cinfo);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: got exception while trying to read input objects: {0}", e.Message);
                        return false;
                    }
                }
            }

            return true;
        }

    }
}
