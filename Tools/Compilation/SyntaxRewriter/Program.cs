﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# syntax rewriter.
    /// </summary>
    public sealed class SyntaxRewriter
    {
        private static void Main(string[] args)
        {
            var infile = string.Empty;
            var outfile = string.Empty;
            var csVersion = new Version(0, 0);

            var usage = "Usage: PSharpSyntaxRewriter.exe file.psharp [file.psharp.cs] [/csVersion:major.minor]";

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
                                    if (int.TryParse(value, out int intVer))
                                    {
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
            var input_string = string.Empty;
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
            string errors = string.Empty;
            var output = Translate(input_string, out errors, csVersion);
            var result = string.Format("{0}", output is null ? "Parse Error: " + errors :
                "//  <auto-generated />\n" + output);

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
        /// Translates the specified text from P# to C#.
        /// </summary>
        public static string Translate(string text, out string errors, Version csVersion)
        {
            var configuration = Configuration.Create();
            configuration.IsVerbose = true;
            configuration.RewriteCSharpVersion = csVersion;
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
}
