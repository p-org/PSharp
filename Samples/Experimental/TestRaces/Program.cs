using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace TestRaces
{
    public class ChessTest
    {
        //static object lck = new object();

        public class Obj
        {
            public int x;
        }

        class E : Event
        {
            public Obj obj;
        }

        class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(bar))]
            class Init : MachineState { }

            public void bar()
            {
                var re = this.ReceivedEvent as E;
                //lock (lck) { }
                var o = re.obj;
                //o.x = 2;
            }

        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(foo))]
            class Init : MachineState { }

            public void foo()
            {
                var m = CreateMachine(typeof(M));
                var p = new E();
                p.obj = new Obj();
                p.obj.x = 1;

                //lock (lck) { }

                this.Send(m, p);

                //p.obj.x = 3;
            }
        }



        public static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(Harness));
            runtime.Wait();
        }

        public static bool Run()
        {
            var runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(Harness));
            //runtime.Wait();
            return true;
        }

        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(Harness));
        }

    }
}
