﻿// ------------------------------------------------------------------------------------------------

using System;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp
{
    /// <summary>
    /// This is the MSBuild task referenced by the UsingTask element in the PSharp.targets file.
    /// </summary>
    public class Rewriter : ITask
    {
        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
        public ITaskItem[] InputFiles { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        private Version CsVersion = new Version();

        public string CSharpVersion
        {
            get => this.CsVersion.ToString();

            set
            {
                // Version.Parse errors if there is no ".minor" part. Allow exceptions to propagate.
                this.CsVersion = string.IsNullOrEmpty(value) ?
                    new Version() : int.TryParse(value, out int intVer) ?
                    new Version(intVer, 0) : new Version(value);
            }
        }

        public bool Execute()
        {
            if (this.InputFiles is null)
            {
                // Target was included but no .psharp files are present in the project. Skip
                // execution.
                return true;
            }

            for (int i = 0; i < this.InputFiles.Length; i++)
            {
                var inp = File.ReadAllText(this.InputFiles[i].ItemSpec);
                string errors = string.Empty;
                var outp = SyntaxRewriter.Translate(inp, out errors, this.CsVersion);
                if (outp != null)
                {
                    // Tagging the generated .cs files with the "<auto-generated>" tag so as to avoid StyleCop build errors.
                    outp = "//  <auto-generated />\n" + outp;
                    File.WriteAllText(this.OutputFiles[i].ItemSpec, outp);
                }
                else
                {
                    // Replaces Program.psharp with the actual file name.
                    errors = errors.Replace("Program.psharp", Path.GetFileName(this.InputFiles[i].ItemSpec));

                    // Prints a compiler error with log.
                    File.WriteAllText(
                        this.OutputFiles[i].ItemSpec,
                        string.Format("#error Psharp Compiler Error {0} /* {0} {1} {0} */ ", "\n", errors));
                }
            }

            return true;
        }
    }
}
