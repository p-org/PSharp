using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace FailureDetector
{
    internal class FailureDetector : Machine
    {
        internal class Config : Event
        {
            public List<MachineId> Nodes;

            public Config(List<MachineId> nodes)
                : base()
            {
                this.Nodes = nodes;
            }
        }

        private class TimerCancelled : Event { }
        private class RoundDone : Event { }
        private class Unit : Event { }

        List<MachineId> Nodes;
        Dictionary<MachineId, bool> Clients;
		int Attempts;
        Dictionary<MachineId, bool> Alive;
        Dictionary<MachineId, bool> Responses;
        MachineId Timer;

        [Start]
        [OnEventPushState(typeof(Unit), typeof(SendPing))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventDoAction(typeof(Driver.RegisterClient), nameof(RegisterClientAction))]
        [OnEventDoAction(typeof(Driver.UnregisterClient), nameof(UnregisterClientAction))]
        class Init : MachineState { }

        void Configure()
        {
            this.Nodes = new List<MachineId>();
            this.Clients = new Dictionary<MachineId, bool>();
            this.Alive = new Dictionary<MachineId, bool>();
            this.Responses = new Dictionary<MachineId, bool>();

            this.Nodes = (this.ReceivedEvent as Config).Nodes;
            foreach (var node in this.Nodes)
            {
                this.Alive.Add(node, true);
            }

            this.Timer = this.CreateMachine(typeof(Timer));
            this.Send(this.Timer, new Timer.Config(this.Id));

            this.Raise(new Unit());
        }

        void RegisterClientAction()
        {
            var client = (this.ReceivedEvent as Driver.RegisterClient).Client;
            this.Clients[client] = true;
        }

        void UnregisterClientAction()
        {
            var client = (this.ReceivedEvent as Driver.UnregisterClient).Client;
            if (this.Clients.ContainsKey(client))
            {
                this.Clients.Remove(client);
            }
        }

        [OnEntry(nameof(SendPingOnEntry))]
        [OnEventGotoState(typeof(RoundDone), typeof(Reset))]
        [OnEventGotoState(typeof(Unit), typeof(SendPing))]
        [OnEventPushState(typeof(TimerCancelled), typeof(WaitForCancelResponse))]
        [OnEventDoAction(typeof(Node.Pong), nameof(PongAction))]
        [OnEventDoAction(typeof(Timer.Timeout), nameof(TimeoutAction))]
        class SendPing : MachineState { }

        void SendPingOnEntry()
        {
            foreach (var node in this.Nodes)
            {
                if (this.Alive.ContainsKey(node) && !this.Responses.ContainsKey(node))
                {
                    this.Monitor<Safety>(new Safety.MPing(node));
                    this.Send(node, new Node.Ping(this.Id));
                }
            }

            this.Send(this.Timer, new Timer.StartTimer(100));
        }

        void PongAction()
        {
            var node = (this.ReceivedEvent as Node.Pong).Node;
            if (this.Alive.ContainsKey(node))
            {
                this.Responses[node] = true;
                if (this.Responses.Count == this.Alive.Count)
                {
                    this.Send(this.Timer, new Timer.CancelTimer());
                    this.Raise(new TimerCancelled());
                }
            }
        }

        void TimeoutAction()
        {
            this.Attempts++;
            if (this.Responses.Count < this.Alive.Count && this.Attempts < 2)
            {
                this.Raise(new Unit());
            }
            else
            {
                this.CheckAliveSet();
                this.Raise(new RoundDone());
            }
        }

        [OnEventDoAction(typeof(Timer.CancelSuccess), nameof(CancelSuccessAction))]
        [OnEventDoAction(typeof(Timer.CancelFailure), nameof(CancelFailure))]
        [DeferEvents(typeof(Timer.Timeout), typeof(Node.Pong))]
        class WaitForCancelResponse : MachineState { }

        void CancelSuccessAction()
        {
            this.Raise(new RoundDone());
        }

        void CancelFailure()
        {
            this.Pop();
        }

        [OnEntry(nameof(ResetOnEntry))]
        [OnEventGotoState(typeof(Timer.Timeout), typeof(SendPing))]
        [IgnoreEvents(typeof(Node.Pong))]
        class Reset : MachineState { }

        void ResetOnEntry()
        {
            this.Attempts = 0;
            this.Responses.Clear();
            this.Send(this.Timer, new Timer.StartTimer(1000));
        }

		void CheckAliveSet()
        {
            foreach (var node in this.Nodes)
            {
                if (this.Alive.ContainsKey(node) && !this.Responses.ContainsKey(node))
                {
                    this.Alive.Remove(node);
                }
            }
        }
    }
}
