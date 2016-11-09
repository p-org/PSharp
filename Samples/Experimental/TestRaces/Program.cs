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
        class E1 : Event
        {
            public MachineId Id;
            public E1(MachineId Id)
            {
                this.Id = Id;
            }
        }

        class E2 : Event
        {
            public MachineId Id;
            public E2(MachineId Id)
            {
                this.Id = Id;
            }

        }


        class M : Machine
        {
            MachineId other = null;

            [Start]
            [OnEventDoAction(typeof(E1), nameof(bar))]
            [OnEventDoAction(typeof(E2), nameof(bar))]
            class Init : MachineState { }

            public void bar()
            {
                update(this.ReceivedEvent);

                if (this.Random())
                {
                    var e = this.Receive(typeof(E1), typeof(Halt));
                    if (e is Halt)
                    {
                        this.Raise(e);
                        return;
                    }
                    update(e);
                }
                else if (other != null)
                {
                    var x = this.RandomInteger(3);
                    while (x >= 0)
                    {
                        x--;
                        if (this.Random())
                        {
                            this.Send(other, new E1(this.Id));
                        }
                        else 
                        {
                            this.Send(other, new E2(this.Id));
                        }
                    }

                    if (this.RandomInteger(10) == 0)
                    {
                        this.Send(other, new Halt());
                    }
                }

            }

            void update(Event e)
            {
                if (this.Random())
                {
                    if (e is E1) other = (e as E1).Id;
                    else other = (e as E2).Id;
                }
            }

        }

        class Harness : Machine
        {
            //public static TaskCompletionSource<bool> Waiter = new TaskCompletionSource<bool>();
            static object lck = new object();

            [Start]
            [OnEntry(nameof(foo))]
            class Init : MachineState { }

            public void foo()
            {
                var size = 10;
                var allMachines = new MachineId[size];

                for (int i = 0; i < size; i++)
                {
                    allMachines[i] = this.CreateMachine(typeof(M));
                }

                for (int i = 0; i < size; i++)
                {
                    this.Send(allMachines[i], new E1(allMachines[this.RandomInteger(size)]));
                }

                for (int i = 0; i < size; i++)
                {
                    this.Send(allMachines[i], new Halt());
                }

                this.Raise(new Halt());
                //Waiter.SetResult(true);
            }
        }



        public static void Main(string[] args)
        {
            var config = Microsoft.PSharp.Utilities.Configuration.Create();
            config.WithVerbosityEnabled(2);

            var runtime = PSharpRuntime.Create(config);
            runtime.CreateMachine(typeof(Harness));
            runtime.Wait();
        }

        public static bool Run()
        {
            //Harness.Waiter = new TaskCompletionSource<bool>();
            var runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(Harness));
            runtime.Wait();
            //Harness.Waiter.Task.Wait();
            return true;
        }

        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(Harness));
        }

    }
}
