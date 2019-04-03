// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The default event.
    /// </summary>
    [DataContract]
    public sealed class Default : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Default"/> class.
        /// </summary>
        public Default()
            : base()
        {
        }
    }
}
