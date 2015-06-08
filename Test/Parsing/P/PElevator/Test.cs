using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PElevator
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.CreateMachine<User>();
            Runtime.WaitMachines();
        }
    }
}
