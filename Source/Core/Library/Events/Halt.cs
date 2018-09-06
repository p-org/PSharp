﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The halt event.
    /// </summary>
    [DataContract]
    public sealed class Halt : Event
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public Halt()
            : base()
        {
            
        }
    }
}
