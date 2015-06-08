//-----------------------------------------------------------------------
// <copyright file="ISendable.cs" company="Microsoft">
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

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Supports sending, which enqueues a P# event with an optional
    /// payload to the class that implements this interface.
    /// </summary>
    public interface ISendable
    {
        /// <summary>
        /// Used to assign a P# machine id and an optional payload.
        /// </summary>
        /// <param name="mid">Machine id</param>
        /// <param name="payload">Optional payload</param>
        void Create(MachineId mid, object payload);

        /// <summary>
        /// Used to send a P# event.
        /// </summary>
        /// <param name="e">Event</param>
        void Send(Event e);
    }
}
