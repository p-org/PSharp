using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace TypesAndGenerics
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.CreateMachine<Server>();
            Runtime.WaitMachines();
        }
    }
}
