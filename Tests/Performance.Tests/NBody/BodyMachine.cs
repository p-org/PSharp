using System;
using System.Collections.Generic;

using Microsoft.PSharp;

namespace NBody
{
    public class BodyMachine : Machine
    {
        internal class Config : Event
        {
            public MachineId Simulation;
            public List<MachineId> Bodies;

            public Config(MachineId simulation, List<MachineId> bodies)
                : base()
            {
                this.Simulation = simulation;
                this.Bodies = bodies;
            }
        }

        internal class EnergyRequest : Event { }

        internal class EnergyResponse : Event
        {
            public double X;
            public double Y;
            public double Z;
            public double VX;
            public double VY;
            public double VZ;
            public double Mass;

            public EnergyResponse(double x, double y, double z, double vx,
                double vy, double vz, double mass)
                : base()
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.VX = vx;
                this.VY = vy;
                this.VZ = vz;
                this.Mass = mass;
            }
        }

        internal class Advance : Event { }

        internal class AccelerateRequest : Event
        {
            public double X;
            public double Y;
            public double Z;
            public double VX;
            public double VY;
            public double VZ;
            public double Mass;

            public AccelerateRequest(double x, double y, double z,
                double vx, double vy, double vz, double mass)
                : base()
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.VX = vx;
                this.VY = vy;
                this.VZ = vz;
                this.Mass = mass;
            }
        }

        internal class AccelerateResponse : Event { }

        internal class MoveRequest : Event { }

        internal class MoveResponse : Event { }

        private MachineId Simulation;
        private List<MachineId> Bodies;

        private double X;
        private double Y;
        private double Z;
        private double VX;
        private double VY;
        private double VZ;
        private double Mass;

        private readonly double TimeStep = 0.001;
        private readonly double EPS = 0.01;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            Random rnd = new Random(DateTime.Now.Millisecond);

            this.X = rnd.NextDouble();
            this.Y = rnd.NextDouble();
            this.Z = rnd.NextDouble();
            this.VX = rnd.NextDouble();
            this.VY = rnd.NextDouble();
            this.VZ = rnd.NextDouble();
            this.Mass = rnd.NextDouble();
        }

        void Configure()
        {
            this.Simulation = (this.ReceivedEvent as Config).Simulation;
            this.Bodies = (this.ReceivedEvent as Config).Bodies;
            this.Goto(typeof(Active));
        }

        [OnEventDoAction(typeof(EnergyRequest), nameof(SendEnergy))]
        [OnEventDoAction(typeof(Advance), nameof(DoAdvance))]
        [OnEventDoAction(typeof(AccelerateRequest), nameof(DoAccelerate))]
        [OnEventDoAction(typeof(MoveRequest), nameof(DoMove))]
        class Active : MachineState { }

        void SendEnergy()
        {
            this.Send(this.Simulation, new EnergyResponse(this.X, this.Y,
                this.Z, this.VX, this.VY, this.VZ, this.Mass));
        }

        void DoAdvance()
        {
            foreach (var body in this.Bodies)
            {
                if (this.Id == body)
                {
                    continue;
                }

                this.Send(body, new AccelerateRequest(this.X, this.Y,
                this.Z, this.VX, this.VY, this.VZ, this.Mass));
            }
        }

        void DoAccelerate()
        {
            var x = (this.ReceivedEvent as AccelerateRequest).X;
            var y = (this.ReceivedEvent as AccelerateRequest).Y;
            var z = (this.ReceivedEvent as AccelerateRequest).Z;
            var vx = (this.ReceivedEvent as AccelerateRequest).VX;
            var vy = (this.ReceivedEvent as AccelerateRequest).VY;
            var vz = (this.ReceivedEvent as AccelerateRequest).VZ;
            var mass = (this.ReceivedEvent as AccelerateRequest).Mass;

            double dx = this.X - x;
            double dy = this.Y - y;
            double dz = this.Z - z;

            double dSquared = dx * dx + dy * dy + dz * dz + this.EPS;
            double distance = Math.Sqrt(dSquared);
            double mag = this.TimeStep / (dSquared * distance);
            double mmag = mass * mag;

            this.VX -= dx * mass * mag;
            this.VY -= dy * mass * mag;
            this.VZ -= dz * mass * mag;

            this.Send(this.Simulation, new AccelerateResponse());
        }

        void DoMove()
        {
            this.X += this.TimeStep * this.VX;
            this.Y += this.TimeStep * this.VY;
            this.Z += this.TimeStep * this.VZ;

            this.Send(this.Simulation, new MoveResponse());
        }
    }
}
