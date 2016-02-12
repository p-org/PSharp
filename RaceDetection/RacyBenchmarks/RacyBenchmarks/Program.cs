using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace BasicPaxosRacy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements Lamport's Paxos distributed
    /// concencus algorithm.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(GodMachine));
        }
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting BasicPaxos");
            Go();
            Console.WriteLine("Done Execution");
            Console.WriteLine("[Press any key to exit]");
            Console.ReadLine();
        }
    }
}
