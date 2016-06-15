using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NBody
{
    public class TPLTest
    {
        private ConcurrentBag<Body> Bodies;

        private readonly double TimeStep = 0.001;
        private readonly double EPS = 0.01;

        public void Start(int numOfBodies, int steps)
        {
            this.Bodies = new ConcurrentBag<Body>();
            this.GenerateBodies(numOfBodies);
            
            Console.WriteLine("{0:f9}", this.ComputeEnergy());

            for (int i = 0; i < steps; i++)
            {
                this.DoStep();
            }
            
            Console.WriteLine("{0:f9}", this.ComputeEnergy());
        }

        public void DoStep()
        {
            Parallel.ForEach(this.Bodies, (bi) =>
            {
                foreach (Body bj in this.Bodies)
                {
                    if (bi == bj)
                    {
                        continue;
                    }

                    double dx = bi.X - bj.X;
                    double dy = bi.Y - bj.Y;
                    double dz = bi.Z - bj.Z;

                    double dSquared = dx * dx + dy * dy + dz * dz + this.EPS;
                    double distance = Math.Sqrt(dSquared);
                    double mag = this.TimeStep / (dSquared * distance);
                    double mmag = bj.Mass * mag;

                    bi.VX -= dx * bj.Mass * mag;
                    bi.VY -= dy * bj.Mass * mag;
                    bi.VZ -= dz * bj.Mass * mag;
                }
            });

            Parallel.ForEach(this.Bodies, (body) =>
            {
                body.X += this.TimeStep * body.VX;
                body.Y += this.TimeStep * body.VY;
                body.Z += this.TimeStep * body.VZ;
            });
        }

        public double ComputeEnergy()
        {
            double res = 0.0;

            foreach (var body in this.Bodies)
            {
                res += body.X + body.Y + body.Z + body.VX + body.VY + body.VZ + body.Mass;
            }

            return res;
        }

        private void GenerateBodies(int numOfBodies)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            for (int idx = 0; idx < numOfBodies; idx++)
            {
                this.Bodies.Add(new Body(rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble(),
                    rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble()));
            }
        }
    }
}
