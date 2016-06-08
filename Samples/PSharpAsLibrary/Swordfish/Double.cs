 namespace Swordfish
{
    internal class Double
    {
        private double Value;

        public Double(double value)
        {
            this.Value = value;
        }

        public static implicit operator Double(double value)
        {
            return new Double(value);
        }

        public static implicit operator double(Double value)
        {
            return value.Value;
        }

        public static double operator +(Double one, Double two)
        {
            return one.Value + two.Value;
        }

        public static Double operator +(double one, Double two)
        {
            return new Double(one + two);
        }

        public static double operator -(Double one, Double two)
        {
            return one.Value - two.Value;
        }

        public static Double operator -(double one, Double two)
        {
            return new Double(one - two);
        }
    }
}
