using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GermanRacy
{
    class Program
    {
        public static void Main(string[] args)
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            MachineId id = runtime.CreateMachine(typeof(Host));
            runtime.SendEvent(id, new Host.eInitialize(3));
            Console.WriteLine("Done; Press any key to exit");
            Console.ReadLine();
        }
    }
}
