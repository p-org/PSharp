﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Coverage;

namespace Microsoft.PSharp
{
    /// <summary>
    /// P# coverage report merger.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Input coverage info.
        /// </summary>
        private static List<CoverageInfo> InputFiles;

        /// <summary>
        /// Output file prefix.
        /// </summary>
        private static string OutputFilePrefix;

        private static void Main(string[] args)
        {
            if (!ParseArgs(args))
            {
                return;
            }

            if (InputFiles.Count == 0)
            {
                Console.WriteLine("Error: No input files provided");
                return;
            }

            var cinfo = new CoverageInfo();
            foreach (var other in InputFiles)
            {
                cinfo.Merge(other);
            }

            // Dump
            string name = OutputFilePrefix;
            string directoryPath = Environment.CurrentDirectory;

            var activityCoverageReporter = new ActivityCoverageReporter(cinfo);

            string[] graphFiles = Directory.GetFiles(directoryPath, name + "_*.dgml");
            string graphFilePath = Path.Combine(directoryPath, name + "_" + graphFiles.Length + ".dgml");

            Output.WriteLine($"... Writing {graphFilePath}");
            activityCoverageReporter.EmitVisualizationGraph(graphFilePath);

            string[] coverageFiles = Directory.GetFiles(directoryPath, name + "_*.coverage.txt");
            string coverageFilePath = Path.Combine(directoryPath, name + "_" + coverageFiles.Length + ".coverage.txt");

            Output.WriteLine($"... Writing {coverageFilePath}");
            activityCoverageReporter.EmitCoverageReport(coverageFilePath);
        }

        /// <summary>
        /// Parses the arguments.
        /// </summary>
        private static bool ParseArgs(string[] args)
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
                    // Check the suffix.
                    if (!arg.EndsWith(".sci"))
                    {
                        Console.WriteLine("Error: Only sci files accepted as input, got {0}", arg);
                        return false;
                    }

                    // Check if the file exists?
                    if (!File.Exists(arg))
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
