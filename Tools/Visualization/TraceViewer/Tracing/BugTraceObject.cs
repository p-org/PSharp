// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Class implementing a bug trace object.
    /// </summary>
    public class BugTraceObject
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Machine { get; set; }
        public string MachineState { get; set; }
        public string Action { get; set; }
        public string TargetMachine { get; set; }
    }
}
