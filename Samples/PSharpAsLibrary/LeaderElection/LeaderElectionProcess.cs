using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LeaderElection
{
    internal class LeaderElectionProcess : Machine
    {
        internal class Config : Event
        {
            public int Id;
            public MachineId RightProcess;

            public Config(int id, MachineId process)
                : base()
            {
                this.Id = id;
                this.RightProcess = process;
            }
        }

        internal class Request : Event
        {
            public int Type;
            public int MaxId;

            public Request(int type, int maxId)
                : base()
            {
                this.Type = type;
                this.MaxId = maxId;
            }
        }

        internal class Start : Event { }

        int ProcessId;

        MachineId RightProcess;

        int Number;
        int RightProcessNumber;

        bool IsActive;

        int MaxProcessId;

        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [DeferEvents(typeof(Start))]
        class Init : MachineState { }

        void Configure()
        {
            this.ProcessId = (this.ReceivedEvent as Config).Id;
            this.RightProcess = (this.ReceivedEvent as Config).RightProcess;
            this.MaxProcessId = this.ProcessId;
            this.IsActive = true;
            
            this.Goto(typeof(Active));
        }
        
        [OnEventDoAction(typeof(Start), nameof(DoStart))]
        [OnEventDoAction(typeof(Request), nameof(ProcessRequest))]
        class Active : MachineState { }
        
        void DoStart()
        {
            this.Send(this.RightProcess, new Request(0, this.MaxProcessId));
        }

        void ProcessRequest()
        {
            var type = (this.ReceivedEvent as Request).Type;
            var id = (this.ReceivedEvent as Request).MaxId;

            if (type == 0)
            {
                var number = id;
                if (this.IsActive && number != this.MaxProcessId)
                {
                    this.Send(this.RightProcess, new Request(1, number));
                    this.RightProcessNumber = number;
                }
                else if (!this.IsActive)
                {
                    this.Send(this.RightProcess, new Request(0, number));
                }
            }
            else if (type == 1)
            {
                var number = id;
                if (this.IsActive)
                {
                    if (this.RightProcessNumber > number &&
                        this.RightProcessNumber > this.MaxProcessId)
                    {
                        this.MaxProcessId = this.RightProcessNumber;
                        this.Send(this.RightProcess, new Request(0, this.RightProcessNumber));
                    }
                    else
                    {
                        this.IsActive = false;
                    }
                }
                else
                {
                    this.Send(this.RightProcess, new Request(1, number));
                }
            }
        }
    }
}
