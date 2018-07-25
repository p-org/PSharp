namespace Microsoft.PSharp.ServiceFabric
{
    public interface IPSharpEventSourceLogger
    {
        void Message(string message);
        void Message(string message, params object[] args);
    }

    public class NullPSharpServiceLogger : IPSharpEventSourceLogger
    {
        public void Message(string message)
        {
            // no-op
        }

        public void Message(string message, params object[] args)
        {
            // no-op
        }
    }
}
