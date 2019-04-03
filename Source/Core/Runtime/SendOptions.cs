// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Optional parameters for a send operation.
    /// </summary>
    public class SendOptions
    {
        /// <summary>
        /// Operation group id.
        /// </summary>
        public Guid? OperationGroupId;

        /// <summary>
        /// Is this a MustHandle event?
        /// </summary>
        public bool MustHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendOptions"/> class.
        /// </summary>
        public SendOptions()
        {
            this.OperationGroupId = null;
            this.MustHandle = false;
        }

        /// <summary>
        /// A string that represents the current options.
        /// </summary>
        public override string ToString()
        {
            return $"SendOptions[Guid='{this.OperationGroupId}', MustHandle='{this.MustHandle}']";
        }

        /// <summary>
        /// Implicit conversion from a <see cref="Guid"/>.
        /// </summary>
        public static implicit operator SendOptions(Guid operationGroupId)
        {
            return new SendOptions { OperationGroupId = operationGroupId };
        }

        /// <summary>
        /// Creates a new instance of <see cref="SendOptions"/> from the specified <see cref="Guid"/>.
        /// </summary>
        public static SendOptions FromGuid(Guid operationGroupId)
        {
            return operationGroupId;
        }
    }
}
