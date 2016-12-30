using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Scheduler.Tests
{
    public static class Util
    {
        public static bool debugOutput = false;
        public static HashSet<Process> SpawnedProcesses = new HashSet<Process>();

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
            proc.StartInfo.RedirectStandardError = true;

            lock (SpawnedProcesses)
            {
                SpawnedProcesses.Add(proc);
            }

            proc.Start();
            var str1 = proc.StandardOutput.ReadToEnd();
            var str2 = proc.StandardError.ReadToEnd();
            proc.WaitForExit();


            lock (SpawnedProcesses)
            {
                SpawnedProcesses.Remove(proc);
            }

            foreach (var s in str1.Split(new string[] { System.Environment.NewLine, "\n" }, StringSplitOptions.None))
            {
                ret.Add(s);
            }
            foreach (var s in str2.Split(new string[] { System.Environment.NewLine, "\n" }, StringSplitOptions.None))
            {
                ret.Add(s);
            }

            if (debugOutput)
            {
                Console.WriteLine("-----------------------------------");
            }

            return ret;
        }

        public static bool CleanDirectory(string dir)
        {
            var trycount = 0;
            var success = false;
            while (trycount < 5)
            {
                trycount++;
                try
                {
                    foreach (var f in System.IO.Directory.GetFiles(dir))
                    {
                        System.IO.File.Delete(f);
                    }
                    success = true;
                    break;
                }
                catch (System.IO.IOException)
                {
                    Thread.Sleep(1000);
                }
            }
            return success;
        }

    }

    public static class LinqExtender
    {
        public static void Iter<T>(this IEnumerable<T> coll, Action<T> fn)
        {
            foreach (var e in coll) fn(e);
        }
    }
    }
