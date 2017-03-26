using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RunRaft
{
    class Program
    {
        static bool debugOutput = false;

        static void Main(string[] args)
        {
            var total = 1000;
            var failed = 0;

            for (int i = 0; i < total; i++)
            {
                Console.Write("Iteration {0}: ", i + 1);

                var output = run(Environment.CurrentDirectory,
                    "wlimit.exe", "/w 5 Binaries\\Debug\\Raft.PSharpLibrary.exe");

                if (output.Any(s => s.StartsWith("wlimit>  Exit code: 0")))
                {
                    Console.WriteLine("Pass");
                }
                else if (output.Any(s => s.StartsWith("wlimit>  Exit code: -")))
                {
                    Console.WriteLine("Timeout");
                }
                else
                {
                    Console.WriteLine("Fail");
                    //output.ForEach(s => Console.WriteLine("{0}", s));
                    failed++;
                }

            }
            Console.WriteLine("Failed: {0} of {1}", failed, total);
        }


        public static List<string> run(string dir, string cmd, string args)
        {
            var ret = new List<string>();

            if (debugOutput)
            {
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Running: " + cmd + " " + args);
            }

            var proc = new System.Diagnostics.Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = !debugOutput;
            proc.StartInfo.FileName = cmd;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.WorkingDirectory = dir;
            Debug.Assert(System.IO.Path.IsPathRooted(dir));
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();
            var str = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            foreach (var s in str.Split(new string[] { System.Environment.NewLine, "\n" }, StringSplitOptions.None))
            {
                ret.Add(s);
            }

            if (debugOutput)
            {
                Console.WriteLine("-----------------------------------");
            }

            return ret;
        }

    }
}
