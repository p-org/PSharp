namespace Swordfish
{
    internal class String
    {
        private string Value;

        public String(string value)
        {
            this.Value = value;
        }

        public static implicit operator String(string value)
        {
            return new String(value);
        }

        public static implicit operator string(String value)
        {
            return (string)value.Value;
        }

        public string StringValue()
        {
            return this.Value;
        }
    }
}
