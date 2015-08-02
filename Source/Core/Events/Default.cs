﻿//-----------------------------------------------------------------------
// <copyright file="Default.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

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
        /// Default constructor.
        /// </summary>
        public Default()
            : base(-1, -1)
        {

        }
    }
}
