using System;
using System.Threading;

namespace NBody
{
    public class Body
    {
        public double X;
        public double Y;
        public double Z;
        public double VX;
        public double VY;
        public double VZ;
        public double Mass;

        public Body(double x, double y, double z, double vx, double vy,
            double vz, double mass)
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
}
