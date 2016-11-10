using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace MachineInheritance
{
    class E : Event { }

    abstract class BaseMachine : Machine
    {
        [OnEntry(nameof(EnterA))]
        [OnEventGotoState(typeof(E), typeof(B))]
        public class A : MachineState { }

        [OnEntry(nameof(EnterB))]
        public class B : MachineState { }

        public virtual void EnterA()
        {
            Console.WriteLine("Entered A");
        }

        public virtual void EnterB()
        {
            Console.WriteLine("Entered B");
        }

    }

    class DerivedMachine : BaseMachine
    {
        [Start]
        [OnEntry(nameof(EnterInit))]
        [OnEventGotoState(typeof(E), typeof(A))]
        class Init : MachineState { }

        public void EnterInit()
        {
            Console.WriteLine("Entered Init");
        }

        public override void EnterA()
        {
            Console.WriteLine("Entered Derived-A");
            base.EnterA();
        }

        public override void EnterB()
        {
            Console.WriteLine("Entered Derived-B");
            base.EnterB();
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();
            var d = runtime.CreateMachine(typeof(DerivedMachine));
            runtime.SendEvent(d, new E());
            runtime.SendEvent(d, new E());
            Console.ReadLine();
        }
    }
}
