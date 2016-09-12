using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

// This sample outlines a generic way of implementing a Receive statement
// that does not block its calling thread: it is called ReceiveAsync in the
// code below. The only restriction is that ReceiveAsync be the 
// last statement of its calling action. ReceiveAsync takes a delegate that
// is called after the receive is successful.

namespace AsyncReceive
{
    // The program has two event types: E1 and E2, each carrying an "int" payload.

    class E1 : Event
    {
        public int x;
        public E1(int x)
        {
            this.x = x;
        }
    }

    class E2 : Event
    {
        public int x;
        public E2(int x)
        {
            this.x = x;
        }
    }

    // Special events that need to be created for each event type
    // on which a Receive has to be done.
    class ReceiveE1Event : Event { }
    class ReceiveE2Event : Event { }

    // The purpose of this machine is to receive three events, of type E1, E2 and E1, respectively
    // and compute and display the sum of their payload. Using a blocking receive, this would be done
    // as:
    // var e1 = Receive(typeof(E1));
    // var e2 = Receive(typeof(E2));
    // var e3 = Receive(typeof(E1));
    // print e1.x + e2.x + e3.x;
    // 
    // But, it is instead done using ReceiveAsync in the method InitAction below.
    class A : Machine
    {
        Action<Event> pending = null;
        Event pendingEvent = null;
        bool HasRaised = false;

        [Start]
        [OnEntry(nameof(InitAction))]
        [OnEventPushState(typeof(ReceiveE1Event), typeof(ReceiveE1State))]
        [OnEventPushState(typeof(ReceiveE2Event), typeof(ReceiveE2State))]
        class Init : MachineState { }

        void InitAction()
        {
            ReceiveAsync(typeof(E1), e1 =>
            {
                ReceiveAsync(typeof(E2), e2 =>
                {
                    ReceiveAsync(typeof(E1), e3 =>
                    {
                        Test.outp.WriteLine("Total = {0}", (e1 as E1).x + (e2 as E2).x + (e3 as E1).x);
                    });
                });
            });
        }

        // Code below is generic (i.e., doesn't care about what machine A is trying to do)

        // Defer all, except the event that we want
        [OnEventDoAction(typeof(E1), nameof(Received))]
        [DeferEvents(typeof(WildCardEvent))]
        class ReceiveE1State : MachineState { }

        // Defer all, except the event that we want
        [OnEventDoAction(typeof(E2), nameof(Received))]
        [DeferEvents(typeof(WildCardEvent))]
        class ReceiveE2State : MachineState { }

        // Called when Receive happens. It invokes the
        // pending delegate and then pops.
        void Received()
        {
            pendingEvent = this.ReceivedEvent;
            if (pending != null && pendingEvent != null)
            {
                var act = pending;
                var ev = pendingEvent;

                pending = null;
                pendingEvent = null;
                HasRaised = false;

                // This may do a raise
                act(ev);

                if(!HasRaised)
                {
                    this.Pop();
                }
            }
        }

        // Implementation of ReceiveAsync. It stashes the delegate
        // to a member field and does a raise to push the appropriate
        // state whose sole job is to Receive. This implementation is mostly
        // generic, except it needs to know upfront what events the receive
        // will be on.
        void ReceiveAsync(Type ev, Action<Event> action)
        {
            pending = action;
            if (ev == typeof(E1))
            {
                HasRaised = true;
                this.Raise(new ReceiveE1Event());
            }
            else if (ev == typeof(E2))
            {
                HasRaised = true;
                this.Raise(new ReceiveE2Event());
            }
        }
    }


    class B : Machine
    {
        [Start]
        [OnEntry(nameof(Conf))]
        class Init : MachineState { }

        void Conf()
        {
            var a = this.CreateMachine(typeof(A));
            this.Send(a, new E1(3));
            this.Send(a, new E2(6));
            this.Send(a, new E1(7));
        }

    }


    public class Test
    {
        public static System.IO.TextWriter outp;

        public static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();
            Test.Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            outp = new System.IO.StreamWriter("out.txt");
            runtime.CreateMachine(typeof(B));
        }

        [Microsoft.PSharp.TestDispose]
        public static void Finish()
        {
            outp.Close();
        }

    }
}
