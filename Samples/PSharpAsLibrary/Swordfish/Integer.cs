 namespace Swordfish
{
    internal class Integer
    {
        private int Value;

        public Integer(int value)
        {
            this.Value = value;
        }

        public static implicit operator Integer(int value)
        {
            return new Integer(value);
        }

        public static implicit operator int(Integer integer)
        {
            return integer.Value;
        }

        public static int operator +(Integer one, Integer two)
        {
            return one.Value + two.Value;
        }

        public static Integer operator +(int one, Integer two)
        {
            return new Integer(one + two);
        }

        public static int operator -(Integer one, Integer two)
        {
            return one.Value - two.Value;
        }

        public static Integer operator -(int one, Integer two)
        {
            return new Integer(one - two);
        }
    }
}
