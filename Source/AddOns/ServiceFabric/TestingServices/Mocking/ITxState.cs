using Microsoft.ServiceFabric.Data;

namespace Microsoft.PSharp.ServiceFabric.TestingServices
{
    public interface ITxState : IReliableState
    {
        void Commit(ITransaction tx);

        void Abort(ITransaction tx);
    }
}
