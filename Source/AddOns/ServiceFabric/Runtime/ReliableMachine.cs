namespace Microsoft.PSharp.ServiceFabric
{
    public abstract class ReliableMachine : Machine
    {
        // TODO make operations async
        // TODO create an Activate method which gets invoked when the Service becomes primary - create the reliable structures there
        internal override void Enqueue(EventInfo eventInfo, ref bool runNewHandler)
        {
            // TODO: Put the message in the reactive reliable queue
            base.Enqueue(eventInfo, ref runNewHandler);
        }
    }
}
