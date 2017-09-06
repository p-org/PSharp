using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnBenchmark
{
    public class SpawnMachine : Machine
    {
        private int Todo = 10;
        private long Count = 0L;
        private MachineId Parent;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(HandleStart))]
        [OnEventDoAction(typeof(NumberEvent), nameof(HandleNumberEvent))]        
        class Init : MachineState { }
               
        public class Start : Event
        {
            public Start(int level, long number, MachineId parent)
            {
                Level = level;
                Number = number;
                Parent = parent;
            }

            public int Level { get; }

            public long Number { get; }

            public MachineId Parent { get; }
        }

        public class NumberEvent : Event
        {
            public NumberEvent(long number)
            {                
                Number = number;                
            }

            public long Number { get; }           
        }
    
        void HandleStart()
        {
            var start = this.ReceivedEvent as Start;
            this.Parent = start.Parent;
            if (start.Level == 1)
            {
                    this.Send(start.Parent, new NumberEvent(start.Number));
                    Raise(new Halt());
            }
            else
            {
                var startNumber = start.Number * 10;
                for (int i = 0; i <= 9; i++)
                {
                        this.CreateMachine(typeof(SpawnMachine), new Start(start.Level - 1, startNumber + i, this.Id));
                }
            }
         }

         void HandleNumberEvent()
         {
            var e = this.ReceivedEvent as NumberEvent;
            Todo -= 1;
            Count += e.Number;
            if (Todo == 0)
            {
                this.Send(this.Parent, new NumberEvent(Count));
                Raise(new Halt());
            }
        }      
    }
}
