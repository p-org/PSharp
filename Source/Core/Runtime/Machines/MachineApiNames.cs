//-----------------------------------------------------------------------
// <copyright file="MachineApiNames.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// List of machine API names.
    /// </summary>
    internal static class MachineApiNames
    {
        /// <summary>
        /// The create machine API name.
        /// </summary>
        internal const string CreateMachineApiName = "CreateMachine";

        /// <summary>
        /// The send event API name.
        /// </summary>
        internal const string SendEventApiName = "SendEvent";

        /// <summary>
        /// The create machine and execute API name.
        /// </summary>
        internal const string CreateMachineAndExecuteApiName = "CreateMachineAndExecute";

        /// <summary>
        /// The send event and execute API name.
        /// </summary>
        internal const string SendEventAndExecuteApiName = "SendEventAndExecute";

        /// <summary>
        /// The raise event API name.
        /// </summary>
        internal const string RaiseEventApiName = "Raise";

        /// <summary>
        /// The pop state API name.
        /// </summary>
        internal const string PopStateApiName = "Pop";

        /// <summary>
        /// The monitor event API name.
        /// </summary>
        internal const string MonitorEventApiName = "Monitor";

        /// <summary>
        /// The random API name.
        /// </summary>
        internal const string RandomApiName = "Random";

        /// <summary>
        /// The random integer API name.
        /// </summary>
        internal const string RandomIntegerApiName = "RandomInteger";

        /// <summary>
        /// The fair random API name.
        /// </summary>
        internal const string FairRandomApiName = "FairRandom";

        /// <summary>
        /// The receive event API name.
        /// </summary>
        internal const string ReceiveEventApiName = "Receive";
    }
}
