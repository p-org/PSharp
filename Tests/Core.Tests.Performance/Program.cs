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

using System.Reflection;

using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Loggers;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    /// <summary>
    /// The P# performance test runner.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // BenchmarkDotNet compiles the benchmark to a form that doesn't support debugging so call directly.
            if (args.Length > 0 && args[0].ToLowerInvariant() == "--debug")
            {
                RunDebug(args.Length > 1 ? args[1] : string.Empty);
                return;
            }

            var desc = string.Empty;
            IEnumerable<string> extractDescriptionArg()
            {
                for (var ii = 0; ii < args.Length; ++ii)
                {
                    var arg = args[ii];
                    if (string.Compare(arg, "--desc", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        desc = args[++ii];
                        continue;
                    }
                    yield return arg;
                }
            }

            var benchmarkSwitcher = BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly);
            var summaries = benchmarkSwitcher.Run(extractDescriptionArg().ToArray());

            if (benchmarkSwitcher.ShouldDisplayOptions(args))
            {
                // The run just displayed help, so add the --desc output, matching the BenchmarkDotNet display format.
                ConsoleLogger.Default.WriteLine();
                ConsoleLogger.Default.WriteLineHeader("App Options:");

                void writeOption(string option, string info)
                {
                    ConsoleLogger.Default.WriteResult($"  {option}".PadRight(30));
                    ConsoleLogger.Default.WriteLineInfo($": {info}");
                }

                writeOption("--desc <DESCRIPTION>", "A description of the run, to be displayed on completion");
                writeOption("--debug <CLASSNAME>", "Must be first arg: Use debugger to step through direct execution of CLASSNAME");
                return;
            }
            else if (desc.Length > 0)
            {
                ConsoleLogger.Default.WriteLineInfo($"Run description: {desc}");
            }
        }

        private static void RunDebug(string className)
        {
            switch (className) {
                case "CreateMachinesTest":
                    {
                        var tester = new CreateMachinesTest();
                        tester.Size = 10;
                        tester.CreateMachines();
                    }
                    return;
                case "MailboxTest":
                    {
                        var tester = new MailboxTest();
                        tester.Clients = 2;
                        tester.EventsPerClient = 10;
                        tester.SendMessages();
                    }
                    return;
                case "MethodOverheadTest":
                    {
                        var tester = new MethodOverheadTest();
                        tester.Size = 10;
                        tester.Reps = 1;
                        tester.CreateAndRunMachines();
                    }
                    return;
            }
            Console.WriteLine(className.Length > 0 ? $"Unknown class name: {className}" : "'--debug' option requires class name");
        }
    }
}
