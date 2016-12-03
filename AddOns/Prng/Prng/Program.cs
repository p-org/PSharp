using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prng
{
    class Program
    {
        public static readonly int N = 1000000; // 1 million
        public static int seed1 = 0;
        public static int seed2 = 0;
        public static bool dump = false;

        static void Main(string[] args)
        {
            if (args.Any(s => s == "/break"))
            {
                System.Diagnostics.Debugger.Launch();
            }

            foreach (var s in args.Where((s => s.StartsWith("/seed:"))))
            {
                int seed;
                if (Int32.TryParse(s.Substring("/seed:".Length), out seed))
                {
                    if (seed1 == 0) seed1 = seed;
                    else seed2 = seed;
                }
                else
                {
                    Console.WriteLine("Unable to parse flag: {0}", s);
                    return;
                }
            }

            foreach (var s in args.Where((s => s == "/dump")))
            {
                if(seed1 == 0 || seed2 != 0)
                {
                    Console.WriteLine("Expected exactly one seed on the command line to use /dump");
                }
                dump = true;
            }


            if (seed1 == 0 && seed2 == 0)
            {
                Run(new RandomWrapper());
                Run(new AesGenerator());
            } else if(seed2 == 0 && !dump)
            {
                Run(new RandomWrapper(seed1));
                Run(new AesGenerator(seed1));
            }
            else if(seed2 == 0 && dump)
            {
                var g1 = new RandomWrapper(seed1);
                var g2 = new AesGenerator(seed1);

                var dist1 = GetDistribution(g1);
                var dist2 = GetDistribution(g2);

                var file1 = string.Format("out_{0}_{1}.txt", g1.Name(), seed1);
                using (var fs = new System.IO.StreamWriter(file1))
                {
                    dist1.ForEach(v => fs.WriteLine("{0}", v));
                }

                var file2 = string.Format("out_{0}_{1}.txt", g2.Name(), seed1);
                using (var fs = new System.IO.StreamWriter(file2))
                {
                    dist2.ForEach(v => fs.WriteLine("{0}", v));
                }

                Console.WriteLine("Written {0}", file1);
                Console.WriteLine("Written {0}", file2);
            }
            else if(seed1 != 0 && seed2 != 0)
            {
                Compare(new RandomWrapper(seed1), new RandomWrapper(seed2));
                Compare(new AesGenerator(seed1), new AesGenerator(seed2));
            } 
        }

        static void Run(IGenerator gen)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var dist = GetDistribution(gen);

            sw.Stop();
            Console.WriteLine("Time for {0}: {1}", gen.Name(), sw.Elapsed.TotalSeconds.ToString("F5"));

            Console.WriteLine("======= {0} ========", gen.Name());
            TestRandomness(dist);
        }

        static void TestRandomness(List<int> dist)
        {
            Console.WriteLine("Distribution mod 10");
            var buckets = GetBuckets(dist, 10);
            foreach (var tup in buckets)
            {
                Console.WriteLine("Count for bucket {0}: {1}%", tup.Key, (tup.Value * 100.0 / N).ToString("F2"));
            }

            Console.WriteLine("Distribution mod 2");
            buckets = GetBuckets(dist, 2);
            foreach (var tup in buckets)
            {
                Console.WriteLine("Count for bucket {0}: {1}%", tup.Key, (tup.Value * 100.0 / N).ToString("F2"));
            }

            var freq = new Dictionary<int, int>();
            var max = 0;
            foreach (var x in dist)
            {
                if (!freq.ContainsKey(x)) freq.Add(x, 0);
                freq[x]++;
                max = Math.Max(max, freq[x]);
            }

            Console.WriteLine("Maximum frequency in distribution: {0}", max);
            Console.WriteLine("Number of unique values: {0}", freq.Keys.Count);
        }

        static void Compare(IGenerator gen1, IGenerator gen2)
        {
            Console.WriteLine("======= {0} ========", gen1.Name());

            var dist1 = GetDistribution(gen1);
            var dist2 = GetDistribution(gen2);

            var identical = CompareDist(dist1, dist2);
            Console.WriteLine("Distributions identical: {0}", identical);

            var dist = new List<int>();
            for (int i = 0; i < N; i++)
            {
                //var v = dist1[i] + (dist2[i] - dist1[i]) / 2;
                var v = (dist1[i] > dist2[i]) ? (dist1[i] - dist2[i]) : (dist2[i] - dist1[i]); 
                dist.Add(v);
            }

            TestRandomness(dist);
        }

        static bool CompareDist(List<int> dist1, List<int> dist2)
        {
            var identical = true;
            for (int i = 0; i < N; i++)
            {
                if (dist1[i] != dist2[i])
                {
                    identical = false;
                    break;
                }
            }

            return identical;
        }

        static List<int> GetDistribution(IGenerator gen)
        {
            var ret = new List<int>();
            for (int i = 0; i < N; i++)
            {
                ret.Add(gen.Next());
            }
            return ret;
        }

        static Dictionary<int, int> GetBuckets(List<int> dist, int n)
        {
            var ret = new Dictionary<int, int>();
            for (int i = 0; i < n; i++)
            {
                ret.Add(i, 0);
            }

            foreach(var x in dist)
            {
                ret[x % n]++;
            }

            return ret;
        }

    }



}
