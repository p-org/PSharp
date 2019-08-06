// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp
{
    /// <summary>
    /// Represents various options to be considered during machine creation.
    /// </summary>
    public class CreateOptions
    {
        /// <summary>
        /// The default create options.
        /// </summary>
        public static CreateOptions Default = new CreateOptions();

        /// <summary>
        /// Failure domain name to which the newly created machine belongs to.
        /// </summary>
        public FailureDomain MachineFailureDomain;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOptions"/> class.
        /// </summary>
        public CreateOptions (FailureDomain machineFailureDomain = null)
        {
            this.MachineFailureDomain = machineFailureDomain;
        }
    }
}
