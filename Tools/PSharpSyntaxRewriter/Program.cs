using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Microsoft.Build.Framework;

//using Microsoft.VisualStudio;
//using Microsoft.VisualStudio.Shell.Interop;

namespace PSharpSyntaxRewriter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: PSharpSyntaxRewriter.exe file.psharp");
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
                Console.WriteLine("Error: {0}", e.Message);
                return;
            }

            // Translate and print on console
            Console.WriteLine("{0}", Translate(input_string));
        }

        public static string Translate(string text)
        {
            //System.Diagnostics.Debugger.Launch();
            var configuration = Configuration.Create();
            configuration.Verbose = 2;

            var solution = GetSolution(text);
            var context = CompilationContext.Create(configuration).LoadSolution(solution);

            try
            {
                ParsingEngine.Create(context).Run();
                RewritingEngine.Create(context).Run();

                var syntaxTree = context.GetProjects()[0].PSharpPrograms[0].GetSyntaxTree();

                return syntaxTree.ToString();
            }
            catch (ParsingException)
            {
                return null;
            }
            catch (RewritingException)
            {
                return null;
            }
        }

        static Solution GetSolution(string text, string suffix = "psharp")
        {
            var workspace = new AdhocWorkspace();
            var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create());
            var solution = workspace.AddSolution(solutionInfo);
            var project = workspace.AddProject("Test", "C#");

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.PSharp.Machine).Assembly.Location)
            };

            project = project.AddMetadataReferences(references);
            workspace.TryApplyChanges(project.Solution);

            var sourceText = SourceText.From(text);
            var doc = project.AddDocument("Program", sourceText, null, "Program." + suffix);

            return doc.Project.Solution;
        }
    }

    /*
    public class PSharpCodeGenerator : Microsoft.VisualStudio.Shell.Interop.IVsSingleFileGenerator
    {
        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            throw new NotImplementedException();
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            throw new NotImplementedException();
        }
    }
    */

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
                var outp = Program.Translate(inp);
                if (outp == null) return false;
                System.IO.File.WriteAllText(OutputFiles[i].ItemSpec, outp);
            }
            return true;
        }

    }
}
