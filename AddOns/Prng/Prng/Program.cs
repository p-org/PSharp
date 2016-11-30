using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prng
{
    class Program
    {
        public static readonly int N = 1000000;

        static void Main(string[] args)
        {
            Run(new RandomWrapper());
        }

        static void Run(IGenerator gen)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var dist = GetDistribution(gen);

            sw.Stop();
            Console.WriteLine("Time for {0}: {1}", gen.Name(), sw.Elapsed.TotalSeconds.ToString("F5"));

            var buckets = GetBuckets(dist, 10);
            foreach (var tup in buckets)
            {
                Console.WriteLine("Count for bucket {0} for {1}: {2}%", tup.Key, gen.Name(), (tup.Value * 100.0 / N).ToString("F2"));
            }
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
