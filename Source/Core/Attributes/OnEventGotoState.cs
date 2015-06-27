//-----------------------------------------------------------------------
// <copyright file="OnEventGotoState.cs" company="Microsoft">
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
    /// Attribute for declaring which state a machine should transition to
    /// when it receives an event in a given state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OnEventGotoState : Attribute
    {
        /// <summary>
        /// Event type.
        /// </summary>
        private Type Event;

        /// <summary>
        /// State type.
        /// </summary>
        private Type State;

        /// <summary>
        /// Action name.
        /// </summary>
        private string Action;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="stateType">State type</param>
        public OnEventGotoState(Type eventType, Type stateType)
        {
            this.Event = eventType;
            this.State = stateType;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="stateType">State type</param>
        /// <param name="actionName">Name of action to perform on exit</param>
        public OnEventGotoState(Type eventType, Type stateType, string actionName)
        {
            this.Event = eventType;
            this.State = stateType;
            this.Action = actionName;
        }
    }
}
