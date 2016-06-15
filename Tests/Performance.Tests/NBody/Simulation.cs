using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace NBody
{
    internal class Simulation : Machine
    {
        internal class Config : Event
        {
            public int NumberOfBodies;
            public int NumberOfSteps;

            public Config(int numOfBodies, int numOfSteps)
                : base()
            {
                this.NumberOfBodies = numOfBodies;
                this.NumberOfSteps = numOfSteps;
            }
        }

        private List<MachineId> Bodies;

        private double Energy;
        private int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            int numberOfBodies = (this.ReceivedEvent as Config).NumberOfBodies;
            int numberOfSteps = (this.ReceivedEvent as Config).NumberOfSteps;

            this.Bodies = new List<MachineId>();
            for (int idx = 0; idx < numberOfBodies; idx++)
            {
                var body = this.CreateMachine(typeof(BodyMachine));
                this.Bodies.Add(body);
            }

            foreach (var body in this.Bodies)
            {
                this.Send(body, new BodyMachine.Config(this.Id, this.Bodies));
            }

            this.Goto(typeof(ComputeEnergyStart));
        }
        
        [OnEntry(nameof(ComputeEnergyStartOnEntry))]
        [OnEventDoAction(typeof(BodyMachine.EnergyResponse), nameof(ProcessEnergyStart))]
        class ComputeEnergyStart : MachineState { }

        void ComputeEnergyStartOnEntry()
        {
            this.Energy = 0.0;
            this.Counter = 0;

            foreach (var body in this.Bodies)
            {
                this.Send(body, new BodyMachine.EnergyRequest());
            }
        }

        void ProcessEnergyStart()
        {
            var x = (this.ReceivedEvent as BodyMachine.EnergyResponse).X;
            var y = (this.ReceivedEvent as BodyMachine.EnergyResponse).Y;
            var z = (this.ReceivedEvent as BodyMachine.EnergyResponse).Z;
            var vx = (this.ReceivedEvent as BodyMachine.EnergyResponse).VX;
            var vy = (this.ReceivedEvent as BodyMachine.EnergyResponse).VY;
            var vz = (this.ReceivedEvent as BodyMachine.EnergyResponse).VZ;
            var mass = (this.ReceivedEvent as BodyMachine.EnergyResponse).Mass;

            this.Energy += x + y + z + vx + vy + vz + mass;

            this.Counter++;
            if (this.Counter == this.Bodies.Count)
            {
                Console.WriteLine("RESULT: {0:f9}", this.Energy);
                this.Goto(typeof(Accelerate));
            }
        }

        [OnEntry(nameof(AccelerateOnEntry))]
        [OnEventDoAction(typeof(BodyMachine.AccelerateResponse), nameof(CheckIfAccelerated))]
        class Accelerate : MachineState { }

        void AccelerateOnEntry()
        {
            this.Counter = 0;
            foreach (var body in this.Bodies)
            {
                this.Send(body, new BodyMachine.Advance());
            }
        }

        void CheckIfAccelerated()
        {
            this.Counter++;
            if (this.Counter == (this.Bodies.Count * this.Bodies.Count) - this.Bodies.Count)
            {
                this.Goto(typeof(Move));
            }
        }

        [OnEntry(nameof(MoveOnEntry))]
        [OnEventDoAction(typeof(BodyMachine.MoveResponse), nameof(CheckIfMoved))]
        class Move : MachineState { }

        void MoveOnEntry()
        {
            this.Counter = 0;
            foreach (var body in this.Bodies)
            {
                this.Send(body, new BodyMachine.MoveRequest());
            }
        }

        void CheckIfMoved()
        {
            this.Counter++;
            if (this.Counter == this.Bodies.Count)
            {
                this.Goto(typeof(ComputeEnergyEnd));
            }
        }

        [OnEntry(nameof(ComputeEnergyEndOnEntry))]
        [OnEventDoAction(typeof(BodyMachine.EnergyResponse), nameof(ProcessEnergyEnd))]
        class ComputeEnergyEnd : MachineState { }

        void ComputeEnergyEndOnEntry()
        {
            this.Energy = 0.0;
            this.Counter = 0;

            foreach (var body in this.Bodies)
            {
                this.Send(body, new BodyMachine.EnergyRequest());
            }
        }

        void ProcessEnergyEnd()
        {
            var x = (this.ReceivedEvent as BodyMachine.EnergyResponse).X;
            var y = (this.ReceivedEvent as BodyMachine.EnergyResponse).Y;
            var z = (this.ReceivedEvent as BodyMachine.EnergyResponse).Z;
            var vx = (this.ReceivedEvent as BodyMachine.EnergyResponse).VX;
            var vy = (this.ReceivedEvent as BodyMachine.EnergyResponse).VY;
            var vz = (this.ReceivedEvent as BodyMachine.EnergyResponse).VZ;
            var mass = (this.ReceivedEvent as BodyMachine.EnergyResponse).Mass;

            this.Energy += x + y + z + vx + vy + vz + mass;

            this.Counter++;
            if (this.Counter == this.Bodies.Count)
            {
                Console.WriteLine("RESULT: {0:f9}", this.Energy);
                this.Raise(new Halt());
            }
        }
    }
}
