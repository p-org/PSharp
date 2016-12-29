using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Xml;

namespace Scheduler.Tests
{

    class Program
    {

        public static readonly string PSharpTester = "PSharpTester.exe";
        public static readonly string PSharpCompiler = "PSharpCompiler.exe";
        public static readonly string PSharpMerger = "PSharpCoverageReportMerger.exe";

        public static string PSharpBinaries = "";

        public static string SchedulerPrefix = "";
        public static string ProjectPrefix = "";
        public static string PrevResultsFile = null;

        static string[] Projects = {
            "ChainReplication", "Chameneos", "Chord", "FailureDetector",
            "German", "LeaderElection", "MultiPaxos", "Raft", "ReplicatingStorage",
            "Swordfish", "TwoPhaseCommit"
        };

        static Tuple<string, string>[] Schedulers =
        {
            Tuple.Create("Random1", "/sch:random /sch-seed:50"),
            Tuple.Create("Random2", "/sch:random /sch-seed:51"),
            Tuple.Create("ProbabilisticRandom5", "/sch:probabilistic:5 /sch-seed:50"),
            Tuple.Create("ProbabilisticRandom10", "/sch:probabilistic:10 /sch-seed:50"),
            Tuple.Create("DFS", "/shh:dfs /sch-seed:50"),
            Tuple.Create("IDDFS", "/sch:iddfs /sch-seed:50"),
            Tuple.Create("DelayBounding2", "/sch:db:2 /sch-seed:50"),
            Tuple.Create("DelayBounding10", "/sch:db:10 /sch-seed:50"),
            Tuple.Create("RandomDelayBounding2", "/sch:rdb:2 /sch-seed:50"),
            Tuple.Create("RandomDelayBounding10", "/sch:rdb:10 /sch-seed:50"),
            Tuple.Create("PCT2", "/sch:pct:2 /sch-seed:50"),
            Tuple.Create("PCT5", "/sch:pct:5 /sch-seed:50"),
            Tuple.Create("PCT10", "/sch:pct:10 /sch-seed:50"),
            Tuple.Create("MaceMC", "/sch:macemc /sch-seed:50 /prefix:50"),
            Tuple.Create("RTC2", "/sch:rtc:2 /sch-seed:50"),
            Tuple.Create("RTC10", "/sch:rtc:10 /sch-seed:50")
        };

        static void Main(string[] args)
        {
            // Find the executables
            PSharpBinaries = Environment.GetEnvironmentVariable("PSharpBinaries");

            if (!File.Exists(Path.Combine(PSharpBinaries, PSharpCompiler)))
            {
                Console.WriteLine("Error: PSharp compiler not found");
                return;
            }

            if (args.Any(s => s == "/break"))
            {
                System.Diagnostics.Debugger.Launch();
            }

            if (!File.Exists(Path.Combine(PSharpBinaries, PSharpTester)))
            {
                Console.WriteLine("Error: PSharp tester not found");
                return;
            }

            foreach(var arg in args.Where(s => s.StartsWith("/sch:")))
            {
                SchedulerPrefix = arg.Substring("/sch:".Length);
            }

            foreach (var arg in args.Where(s => s.StartsWith("/test:")))
            {
                ProjectPrefix = arg.Substring("/test:".Length);
            }

            foreach (var arg in args.Where(s => s.StartsWith("/results:")))
            {
                PrevResultsFile = arg.Substring("/results:".Length);
                if(!File.Exists(PrevResultsFile))
                {
                    Console.WriteLine("Error: cannot find file {0}", PrevResultsFile);
                    return;
                }
            }

            SchedulerTestResults PrevResults = null;

            if (PrevResultsFile != null)
            {
                 PrevResults = SchedulerTestResults.DeSerialize(PrevResultsFile);
            }

            if (args.Any(s => s == "/compile"))
            {
                CompileAll();
            }

            if (args.Any(s => s == "/analyze") && PrevResults != null)
            {
                Analyze(PrevResults);
                return;
            }

            // Populate work
            foreach (var project in Projects)
            {
                if (!project.StartsWith(ProjectPrefix))
                    continue;

                foreach(var sch in Schedulers)
                {
                    if (!sch.Item1.StartsWith(SchedulerPrefix))
                        continue;

                    var wi = new WorkItem();
                    wi.Project = project;
                    wi.SchedulerName = sch.Item1;
                    wi.SchedulerArgs = sch.Item2;

                    Worker.WorkItems.Add(wi);
                }
            }

            if(!Directory.Exists("Results"))
            {
                Directory.CreateDirectory("Results");
            } 

            var threads = new List<Thread>();
            for(int i = 0; i < Environment.ProcessorCount; i++)
            {
                var w = new Worker("build" + i.ToString(), Path.Combine("Binaries", "Debug"), "Results");
                threads.Add(new Thread(new ThreadStart(w.Run)));
            }

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            var results = new SchedulerTestResults();
            results.TestResults = Worker.TestResults.ToArray();
            if(PrevResults != null)
            {
                var ls = results.TestResults.Concat(PrevResults.TestResults);
                var nls = new List<TestResult>();
                foreach(var tr in ls)
                {
                    if(!nls.Any(t => t.ProjectName == tr.ProjectName && t.SchedulerName == tr.SchedulerName))
                    {
                        nls.Add(tr);
                    }
                }
                results.TestResults = nls.ToArray();
            }

            results.Output("results.xml");

            Analyze(results);
        }

        static void CompileAll()
        {
            foreach(var project in Projects)
            {
                if (!project.StartsWith(ProjectPrefix))
                    continue;

                var output =
                    Util.run(Environment.CurrentDirectory,
                        Path.Combine(PSharpBinaries, PSharpCompiler),
                        string.Format("/s:Samples.sln /p:{0} /t:test", project));

                output.ForEach(s => Console.WriteLine("{0}", s));
            }
        }

        static void Analyze(SchedulerTestResults results)
        {
            var schedulers = new HashSet<string>();
            results.TestResults.Iter(tr => schedulers.Add(tr.SchedulerName));

            var projects = new HashSet<string>();
            results.TestResults.Iter(tr => projects.Add(tr.ProjectName));

            var nbugs = new Dictionary<string, int>();
            schedulers.Iter(s => nbugs.Add(s, 0));

            var coverage = new Dictionary<string, double>();
            schedulers.Iter(s => coverage.Add(s, 0));

            var iterations = new Dictionary<string, int>();
            schedulers.Iter(s => iterations.Add(s, 0));

            foreach(var tr in results.TestResults)
            {
                nbugs[tr.SchedulerName] += tr.nbugs;
                iterations[tr.SchedulerName] += tr.iterations;
                coverage[tr.SchedulerName] += GetCoverage(tr.sciFileName);
            }

            Console.WriteLine("Scheduler\tBugs\tIters\tCoverage");

            foreach (var s in schedulers)
            {
                Console.WriteLine("{0}\t{1}\t{2}\t{3}%", s, nbugs[s], iterations[s], (coverage[s] / projects.Count).ToString("F2"));
            }

            // TODO: Find the best two combination
        }

        static double GetCoverage(params string[] sciFiles)
        {
            // rm *.coverage.txt
            foreach(var f in Directory.GetFiles("Results", "*.coverage.txt"))
            {
                File.Delete(f);
            }
            foreach (var f in Directory.GetFiles("Results", "*.dgml"))
            {
                File.Delete(f);
            }

            var args = "";
            sciFiles.Iter(s => args += Path.Combine("..", s) + " ");
                
            var output = Util.run(Path.Combine(Environment.CurrentDirectory, "Results"),
                Path.Combine(PSharpBinaries, PSharpMerger),
                args);

            var outFile = Path.Combine(Environment.CurrentDirectory, "Results", "merged_0.coverage.txt");
            if (!File.Exists(outFile))
            {
                throw new Exception("Error getting coverage information");
            }

            var lines = File.ReadAllLines(outFile);
            var regex = new Regex("Total event coverage: (.*)%");
            var coverage = 0.0;
            foreach(var line in lines)
            {
                var m = regex.Match(line);
                if(m.Success)
                {
                    coverage = float.Parse(m.Groups[1].Value);
                    break;
                }
            }

            return coverage;
        }
    }

    [XmlType(TypeName = "SchedulerTestResults")]
    [Serializable]
    public class SchedulerTestResults
    {
        [XmlArrayItemAttribute("TestResult", typeof(TestResult))]
        public TestResult[] TestResults;

        public static SchedulerTestResults DeSerialize(string file)
        {
            var x = new XmlSerializer(typeof(SchedulerTestResults));
            using (FileStream fsr = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var ret = (SchedulerTestResults)x.Deserialize(fsr);
                return ret;
            }
        }

        public void Output(string file)
        {
            using (var writer = new XmlTextWriter(file, Encoding.UTF8))
            {
                writer.WriteStartDocument(true);
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;

                writer.WriteStartElement("SchedulerTestResults");

                writer.WriteStartElement("TestResults");

                foreach (var result in TestResults)
                {
                    result.Output(writer);
                }

                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }
    }

    [XmlRootAttribute("Util", Namespace = "")]
    public class TestResult
    {
        [XmlAttributeAttribute()]
        public string ProjectName;

        [XmlAttributeAttribute()]
        public string SchedulerName;

        [XmlAttributeAttribute()]
        public string Args;

        [XmlAttributeAttribute()]
        public int iterations;

        [XmlAttributeAttribute()]
        public int timeout;

        [XmlAttributeAttribute()]
        public int maxSteps;

        [XmlAttributeAttribute()]
        public int nbugs;

        [XmlAttributeAttribute()]
        public string sciFileName;

        public static TestResult Parse(List<string> output)
        {
            var bugRegex = new Regex("... Found (.*) bugs?\\.");
            var iterationsRegex = new Regex("..... Explored (.*) schedule");
            var sciFileRegex = new Regex("... Writing (.*sci)");

            var result = new TestResult();
            result.nbugs = 0;
            result.iterations = 0;
            result.sciFileName = "";

            foreach (var line in output)
            {
                var m = bugRegex.Match(line);
                if (m.Success)
                {
                    result.nbugs = Int32.Parse(m.Groups[1].Value);
                }

                m = iterationsRegex.Match(line);
                if(m.Success)
                {
                    result.iterations = Int32.Parse(m.Groups[1].Value);
                }

                m = sciFileRegex.Match(line);
                if(m.Success)
                {
                    result.sciFileName = m.Groups[1].Value;
                }
            }

            return result;
        }

        public void Output(XmlTextWriter writer)
        {
            writer.WriteStartElement("TestResult");
            writer.WriteAttributeString("ProjectName", ProjectName);
            writer.WriteAttributeString("SchedulerName", SchedulerName);
            writer.WriteAttributeString("Args", Args);
            writer.WriteAttributeString("iterations", iterations.ToString());
            writer.WriteAttributeString("timeout", timeout.ToString());
            writer.WriteAttributeString("maxSteps", maxSteps.ToString());
            writer.WriteAttributeString("nbugs", nbugs.ToString());
            writer.WriteAttributeString("sciFileName", sciFileName);
            writer.WriteEndElement();
        }
    }

    class WorkItem
    {
        public string SchedulerName;
        public string SchedulerArgs;
        public string Project;
    }

    class Worker
    {
        public static System.Collections.Concurrent.ConcurrentBag<WorkItem> WorkItems =
            new System.Collections.Concurrent.ConcurrentBag<WorkItem>();

        public static System.Collections.Concurrent.ConcurrentBag<TestResult> TestResults =
            new System.Collections.Concurrent.ConcurrentBag<TestResult>();

        string dir;
        string dll_dir;
        string output_dir;

        public Worker(string dir, string dll_dir, string output_dir)
        {
            this.dir = dir;
            this.dll_dir = dll_dir;
            this.output_dir = output_dir;
        }

        public void Run()
        {
            // Get a clean working directory
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            else
            {
                Util.CleanDirectory(dir);
            }

            // copy over the DLLs
            foreach(var f in Directory.GetFiles(dll_dir))
            {
                File.Copy(f, Path.Combine(dir, Path.GetFileName(f)));
            }

            while(true)
            {
                WorkItem wi;
                if(!WorkItems.TryTake(out wi))
                {
                    break;
                }

                var output = Util.run(Path.Combine(Environment.CurrentDirectory, dir),
                    Path.Combine(Program.PSharpBinaries, Program.PSharpTester),
                    string.Format("/test:{0}.dll /timeout:10 /explore /max-steps:500 /coverage-report {1}", wi.Project, wi.SchedulerArgs));

                var result = TestResult.Parse(output);
                result.maxSteps = 500;
                result.ProjectName = wi.Project;
                result.SchedulerName = wi.SchedulerName;
                result.Args = wi.SchedulerArgs;
                result.timeout = 10;

                // copy over the file
                var nFileName = Path.Combine(output_dir, $"{result.SchedulerName}_{result.ProjectName}.sci");
                File.Copy(result.sciFileName, nFileName, true);
                result.sciFileName = nFileName;

                TestResults.Add(result);
            }


        }

    }
}
