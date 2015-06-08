using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace AbstractPong
{
    public class Program
    {
        static void Main(string[] args)
        {
            Runtime.CreateMachine<Server>();
            Runtime.WaitMachines();
        }
    }
}
