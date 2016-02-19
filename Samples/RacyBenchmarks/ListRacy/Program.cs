using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListRacy
{
    class Program
    {
        public static void Main(string[] args)
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(GodMachine));
            Console.WriteLine("[Done; Press enter to exit]");
            Console.ReadLine();
        }
    }
}
