using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace MixedProgramming
{
    class Program
    {
        static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();
            Program.Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            // The type "A" is visible to IntelliSense
            // (after building once)
            runtime.CreateMachine(typeof(A));
        }

    }

    // IntelliSense knows that A is an extension of
    // a Machine class (after building once)
    partial class A
    {
        public void foo(MachineId Bid)
        {
            // "evt" is visible to IntelliSense
            // (after building once)
            //
            // Navigating to the definition of "evt" will take
            // you to the generated code where IntelliSense picks up
            // the type
            this.Send(Bid, new evt("hello", "world"));
        }
    }

    // IntelliSense knows that B is an extension of
    // a Machine class (after building once)
    partial class B
    {
        private void bar()
        {
            var msg = (this.ReceivedEvent as evt);
            Console.WriteLine("Got message {0}, {1}", msg.f1, msg.f2);
        }
    }
}
