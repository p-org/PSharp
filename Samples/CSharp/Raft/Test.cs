using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    public class Test
    {
        static void Main(string[] args)
        {
            Test.Execute();
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute()
        {
            var environment = PSharpRuntime.CreateMachine(typeof(ClusterManager));
        }
    }
}
