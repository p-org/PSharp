// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
