using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spreader
{   
    class Spreader : Machine
    {
        long _count;
        MachineId _parent;
        long _result;
        long _received;
        TaskCompletionSource<bool> hasCompleted;

        internal class Config : Event
        {
            

            public Config(MachineId parent, long count, TaskCompletionSource<bool> hasCompleted)
            {
                this.Parent = parent;
                this.Count = count;
                this.hasCompleted = hasCompleted;
            }

            public MachineId Parent { get; private set; }
            public long Count { get; private set; }
            public TaskCompletionSource<bool> hasCompleted { get; private set; }
        }

        internal class ResultEvent : Event
        {
            public ResultEvent(long count)
            {                
                this.Count = count;
            }
            
            public long Count { get; private set; }
        }


        void SpawnChild()
        {
            this.CreateMachine(typeof(Spreader), new Config(this.Id, _count - 1, null));
        }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(ResultEvent), nameof(Result))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            var e = this.ReceivedEvent as Config;
            this._parent = e.Parent;
            this._count = e.Count;
            this.hasCompleted = e.hasCompleted;
            if (_count == 1)
            {
                this.Send(_parent, new ResultEvent(1L));
            }
            else
            {
                SpawnChild();
                SpawnChild();
            }
        }

        

        void Result()
        {
            var e = this.ReceivedEvent as ResultEvent;
            _received = _received + 1;
            _result = _result + e.Count;
            if(_received == 2)
            {
                if(_parent != null)
                {
                    this.Send(_parent, new ResultEvent(_result + 1));
                }
                else
                {
                    Console.WriteLine("{0} Machines", _result+1);
                    this.hasCompleted.SetResult(true);
                }
            }

        }
    }
}
