using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace TicketBoxOfficeRacy
{
    class Program
    {
        public static void Main(string[] args)
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            MachineId mid = runtime.CreateMachine(typeof(BoxOfficeMachine));
            runtime.SendEvent(mid, new BoxOfficeMachine.eBuyAtSeat());
            Console.WriteLine("[Done; Press any key to continue]");
            Console.ReadLine();
        }
    }
}
