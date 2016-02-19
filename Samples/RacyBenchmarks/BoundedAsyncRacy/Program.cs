using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace BoundedAsyncRacy
{
    /// <summary>
    /// This is an example of using P#.
    /// 
    /// This example implements an asynchronous scheduler communicating
    /// with a number of processes under a predefined bound.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(Scheduler));
            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
