namespace Microsoft.PSharp.ServiceFabric
{
    public interface IPSharpEventSourceLogger
    {
        void Message(string message);
        void Message(string message, params object[] args);
    }
}
