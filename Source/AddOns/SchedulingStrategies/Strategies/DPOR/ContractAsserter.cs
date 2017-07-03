using System.Diagnostics;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// 
    /// </summary>
    public class ContractAsserter : IAsserter
    {
        #region Implementation of IAsserter

        /// <summary>
        /// Assert a condition that should be true.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="msg">An error message if the condition is false.</param>
        public void Assert(bool condition, string msg = "")
        {
            Debug.Assert(condition, msg);
        }

        #endregion
    }
}