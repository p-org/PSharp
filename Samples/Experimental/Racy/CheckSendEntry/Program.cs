using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace CheckSendEntry
{
    class Program
    {
        public static void Main(string[] args)
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(SendMachine));
            Console.WriteLine("Enter to exit");
            Console.ReadLine();
        }
    }
}
