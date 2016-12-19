using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace MonitorInheritance
{
    internal class E : Event
    {
        public E()
        {
        }
    }

    class Harness : Machine
    {
        [Start]
        [OnEntry(nameof(bar))]
        [OnEventDoAction(typeof(E), nameof(foo))]
        class Init : MachineState { }

        void bar()
        {
            Monitor<M1>(new MoveToHot());
            this.Send(this.Id, new E());
        }

        void foo()
        {
            this.Send(this.Id, new E());
        }
    }


    class MoveToHot : Event { }
    class MoveToCold : Event { }
    class MoveToWarm : Event { }

    abstract class M : Monitor
    {
        [Start]
        [Cold]
        [OnEventGotoState(typeof(MoveToHot), typeof(HotState))]
        [OnEventGotoState(typeof(MoveToWarm), typeof(WarmState))]
        [IgnoreEvents(typeof(MoveToCold))]
        [OnEntry(nameof(InCold))]
        class ColdState : MonitorState { }

        [Hot]
        [OnEventGotoState(typeof(MoveToCold), typeof(ColdState))]
        [OnEventGotoState(typeof(MoveToWarm), typeof(WarmState))]
        [IgnoreEvents(typeof(MoveToHot))]
        [OnEntry(nameof(InHot))]
        class HotState : MonitorState { }

        [OnEventGotoState(typeof(MoveToHot), typeof(HotState))]
        [OnEventGotoState(typeof(MoveToCold), typeof(ColdState))]
        [IgnoreEvents(typeof(MoveToWarm))]
        [OnEntry(nameof(InWarm))]
        class WarmState : MonitorState { }

        abstract protected void InCold();
        abstract protected void InHot();
        abstract protected void InWarm();
    }

    class M1 : M
    {
        protected override void InCold()
        {
            Console.WriteLine("InCold");
        }

        protected override void InHot()
        {
            Console.WriteLine("InHot");
        }

        protected override void InWarm()
        {
            Console.WriteLine("InWarm");
        }
    }


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
            runtime.RegisterMonitor(typeof(M1));
            runtime.CreateMachine(typeof(Harness));
        }
    }
}
