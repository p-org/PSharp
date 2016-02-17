using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace FailureDetector
{
    internal class Driver : Machine
    {
        internal class RegisterClient : Event
        {
            public MachineId Client;

            public RegisterClient(MachineId client)
                : base()
            {
                this.Client = client;
            }
        }

        internal class UnregisterClient : Event
        {
            public MachineId Client;

            public UnregisterClient(MachineId client)
                : base()
            {
                this.Client = client;
            }
        }

        MachineId FailureDetector;
        List<MachineId> NodeSeq;
        Dictionary<MachineId, bool> NodeMap;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.NodeSeq = new List<MachineId>();
            this.NodeMap = new Dictionary<MachineId, bool>();

            this.Initialize();

            this.CreateMonitor(typeof(Safety));
            this.FailureDetector = this.CreateMachine(typeof(FailureDetector));

            this.Send(this.FailureDetector, new FailureDetector.Config(this.NodeSeq));
            this.Send(this.FailureDetector, new RegisterClient(this.Id));

            this.Fail();
        }

		void Initialize()
        {
            for (int i = 0; i < 2; i++)
            {
                var node = this.CreateMachine(typeof(Node));
                this.NodeSeq.Add(node);
                this.NodeMap.Add(node, true);
            }
        }

		void Fail()
        {
            for (int i = 0; i < 2; i++)
            {
                this.Send(this.NodeSeq[i], new Halt());
            }
        }
    }
}

