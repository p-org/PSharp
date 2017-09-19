using Microsoft.PSharp;
using System;
using System.Threading.Tasks;

namespace StressLogger
{    
    public partial class SimpleMachine
    {
        private PSharpRuntime instance;
        //private string dictName = "SomeDictionary";

        public SimpleMachine()
        {
            this.Name = "SimpleMachine";
            this.Counter = 0;            
        }

        public void SendMessageToPSharp()
        {
            
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(0/*TimeSpan.FromSeconds(10)*/);            
                this.instance.SendEvent(this.Id, new Timeout());
            });
        }

        public void PersistInline()
        {
            
        }
    }


    public class SetContextMessage : Event
    {
        public PSharpRuntime runtime;

        public SetContextMessage(PSharpRuntime runtime)
            : base()
        {
            this.runtime = runtime;
        }
    }

    public partial class SimpleMachine : Machine
    {
        internal class Timeout : Event
        {
            public Timeout()
                : base()
            {
            }
        }

        //ISimpleMachineContext context;
        string Name;
        int Counter;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(InitOnEntry))]
        class Unknown : MachineState
        {
        }

        [OnEntry("psharp_SimpleThinking_on_entry_action")]
        [OnEventDoAction(typeof(SimpleMachine.Timeout), "IncrementCounter")]
        class SimpleThinking : MachineState
        {
        }

        [OnEntry("psharp_AdvancedThinking_on_entry_action")]
        [OnEventDoAction(typeof(SimpleMachine.Timeout), "IncrementCounter")]
        class AdvancedThinking : MachineState
        {
        }

        void InitOnEntry()
        {            
            this.Counter = 0;
            this.instance = (this.ReceivedEvent as SetContextMessage).runtime;
            this.SendMessageToPSharp();
            this.Goto<SimpleThinking>();
        }

        void IncrementCounter()
        {
            this.Counter++;

            this.SendMessageToPSharp();

            if (this.Counter == 10)
            {
                this.Goto<AdvancedThinking>();
            }

            if (this.Counter == 20)
            {
                this.Counter = 0;
                this.Goto<SimpleThinking>();
            }
        }

        protected void psharp_SimpleThinking_on_entry_action()
        {
            if (this.Counter == 0)
            {
                for (int i = 0; i < 500; i++) ;
            }

            this.SendMessageToPSharp();
        }

        protected void psharp_AdvancedThinking_on_entry_action()
        {
            if (this.Counter == 10)
            {
                // Print to screen
                //context.EventSource.Message("{0}: Inside Advance Thinking", this.Name);

                // Persist
                this.PersistInline();
            }

            this.SendMessageToPSharp();
        }
    }
}
