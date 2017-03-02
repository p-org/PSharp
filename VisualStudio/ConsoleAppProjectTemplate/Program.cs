using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;
using Microsoft.PSharp;

namespace $safeprojectname$
{
    class Program
    {
        static void Main(string[] args)
        {
            PSharpRuntime.Create().CreateMachine(typeof(Machine1));

            // TODO Replace with app code
            Console.ReadLine();
        }
    }
}
