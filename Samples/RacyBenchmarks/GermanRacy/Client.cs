using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GermanRacy
{
    class Client : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        private class eLocal : Event { }

        public class eInitialize : Event
        {
            public Tuple<int, MachineId, bool> initPayload;

            public eInitialize(Tuple<int, MachineId, bool> initPayload)
            {
                this.initPayload = initPayload;
            }
        }
        #endregion

        #region fields
        private int Identity;

        private MachineId Host;

        private bool Pending;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(onInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        private class WaitingForInit : MachineState { }
        #endregion

        #region actions
        private void onInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[Client] Initializing ...\n");

            Identity = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            Host = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            Pending = (this.ReceivedEvent as eInitialize).initPayload.Item3;

            this.Raise(new eLocal());
        }
        #endregion
    }
}

internal class Client : Machine
{
    private class Invalid : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] Invalid ...\n");
        }
    }

    private class AskedShare : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] AskedShare ...\n");

            var message = new Message(machine.Id, machine.Pending);
            this.Send(machine.Host, new eShareReq(message));
            machine.Pending = message.Pending;

            this.Raise(new eLocal());
        }
    }

    private class AskedExcl : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] AskedExcl ...\n");

            var message = new Message(machine.Id, machine.Pending);
            this.Send(machine.Host, new eExclReq(message));
            machine.Pending = message.Pending;

            this.Raise(new eLocal());
        }
    }

    private class InvalidWaiting : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] InvalidWaiting ...\n");
        }

        protected override HashSet<Type> DefineDeferredEvents()
        {
            return new HashSet<Type>
                {
                    typeof(eAskShare),
                    typeof(eAskExcl)
                };
        }
    }

    private class AskedEx2 : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] AskedEx2 ...\n");

            var message = new Message(machine.Id, machine.Pending);
            this.Send(machine.Host, new eExclReq(message));
            machine.Pending = message.Pending;

            this.Raise(new eLocal());
        }
    }

    private class Sharing : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] Sharing ...\n");

            machine.Pending = false;
        }
    }

    private class ShareWaiting : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] ShareWaiting ...\n");
        }
    }

    private class Exclusive : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] Exclusive ...\n");

            machine.Pending = false;
        }

        protected override HashSet<Type> DefineIgnoredEvents()
        {
            return new HashSet<Type>
                {
                    typeof(eAskShare),
                    typeof(eAskExcl)
                };
        }
    }

    private class Invalidating : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as Client;

            Console.WriteLine("[Client] Invalidating ...\n");

            if (machine.Pending)
            {
                this.Raise(new eWait());
            }
            else
            {
                this.Raise(new eNormal());
            }
        }
    }

    private void Ack()
    {
        var cpu = (Machine)this.Payload;

        this.Send(cpu, new eAck());
    }

    private void Stop()
    {
        Console.WriteLine("[Client] Stopping ...\n");

        this.Delete();
    }

    protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
    {
        Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

        StepStateTransitions initDict = new StepStateTransitions();
        initDict.Add(typeof(eLocal), typeof(Invalid));

        StepStateTransitions invalidDict = new StepStateTransitions();
        invalidDict.Add(typeof(eAskShare), typeof(AskedShare));
        invalidDict.Add(typeof(eAskExcl), typeof(AskedExcl));
        invalidDict.Add(typeof(eInvalidate), typeof(Invalidating));
        invalidDict.Add(typeof(eGrantExcl), typeof(Exclusive));
        invalidDict.Add(typeof(eGrantShare), typeof(Sharing));

        StepStateTransitions askedShareDict = new StepStateTransitions();
        askedShareDict.Add(typeof(eLocal), typeof(InvalidWaiting));

        StepStateTransitions askedExclDict = new StepStateTransitions();
        askedExclDict.Add(typeof(eLocal), typeof(InvalidWaiting));

        StepStateTransitions invalidWaitingDict = new StepStateTransitions();
        invalidWaitingDict.Add(typeof(eInvalidate), typeof(Invalidating));
        invalidWaitingDict.Add(typeof(eGrantExcl), typeof(Exclusive));
        invalidWaitingDict.Add(typeof(eGrantShare), typeof(Sharing));

        StepStateTransitions askedEx2Dict = new StepStateTransitions();
        askedEx2Dict.Add(typeof(eLocal), typeof(ShareWaiting));

        StepStateTransitions sharingDict = new StepStateTransitions();
        sharingDict.Add(typeof(eInvalidate), typeof(Invalidating));
        sharingDict.Add(typeof(eGrantShare), typeof(Sharing));
        sharingDict.Add(typeof(eGrantExcl), typeof(Exclusive));
        sharingDict.Add(typeof(eAskExcl), typeof(AskedEx2));

        StepStateTransitions shareWaitingDict = new StepStateTransitions();
        shareWaitingDict.Add(typeof(eInvalidate), typeof(Invalidating));
        shareWaitingDict.Add(typeof(eGrantShare), typeof(Sharing));
        shareWaitingDict.Add(typeof(eGrantExcl), typeof(Exclusive));

        StepStateTransitions exclusiveDict = new StepStateTransitions();
        exclusiveDict.Add(typeof(eInvalidate), typeof(Invalidating));
        exclusiveDict.Add(typeof(eGrantShare), typeof(Sharing));
        exclusiveDict.Add(typeof(eGrantExcl), typeof(Exclusive));

        StepStateTransitions invalidatingDict = new StepStateTransitions();
        invalidatingDict.Add(typeof(eWait), typeof(InvalidWaiting));
        invalidatingDict.Add(typeof(eNormal), typeof(Invalid));

        dict.Add(typeof(Init), initDict);
        dict.Add(typeof(Invalid), invalidDict);
        dict.Add(typeof(AskedShare), askedShareDict);
        dict.Add(typeof(AskedExcl), askedExclDict);
        dict.Add(typeof(InvalidWaiting), invalidWaitingDict);
        dict.Add(typeof(AskedEx2), askedEx2Dict);
        dict.Add(typeof(Sharing), sharingDict);
        dict.Add(typeof(ShareWaiting), shareWaitingDict);
        dict.Add(typeof(Exclusive), exclusiveDict);
        dict.Add(typeof(Invalidating), invalidatingDict);

        return dict;
    }

    protected override Dictionary<Type, ActionBindings> DefineActionBindings()
    {
        Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

        ActionBindings invalidDict = new ActionBindings();
        invalidDict.Add(typeof(eStop), new Action(Stop));

        ActionBindings invalidWaitingDict = new ActionBindings();
        invalidWaitingDict.Add(typeof(eStop), new Action(Stop));

        ActionBindings sharingDict = new ActionBindings();
        sharingDict.Add(typeof(eStop), new Action(Stop));
        sharingDict.Add(typeof(eAskShare), new Action(Ack));

        ActionBindings shareWaitingDict = new ActionBindings();
        shareWaitingDict.Add(typeof(eStop), new Action(Stop));

        ActionBindings exclusiveDict = new ActionBindings();
        exclusiveDict.Add(typeof(eStop), new Action(Stop));

        dict.Add(typeof(Invalid), invalidDict);
        dict.Add(typeof(InvalidWaiting), invalidWaitingDict);
        dict.Add(typeof(Sharing), sharingDict);
        dict.Add(typeof(ShareWaiting), shareWaitingDict);
        dict.Add(typeof(Exclusive), exclusiveDict);

        return dict;
    }
}
