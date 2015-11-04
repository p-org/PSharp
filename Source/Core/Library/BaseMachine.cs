//-----------------------------------------------------------------------
// <copyright file="BaseMachine.cs" company="Microsoft">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a base P# machine.
    /// </summary>
    public abstract class BaseMachine
    {
        #region static fields

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        private static ulong OperationIdCounter;

        #endregion

        #region static methods

        /// <summary>
        /// Gets a new fresh OperationId from the monotonically increasing 
        /// OperationIdCounter counter.
        /// </summary>
        /// <returns></returns>
        internal static ulong FreshOperationId()
        {
            return ++BaseMachine.OperationIdCounter;
        }

        #endregion

        #region fields

        /// <summary>
        /// Unique machine id.
        /// </summary>
        public readonly MachineId Id;

        /// <summary>
        /// Last operation's ID.
        /// </summary>
        internal ulong OperationId = 0;

        #endregion

        #region machine constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        protected BaseMachine()
        {
            this.Id = new MachineId(this.GetType());
        }

        #endregion

        #region generic public and override methods

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean value</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            BaseMachine m = obj as BaseMachine;
            if (m == null ||
                this.GetType() != m.GetType())
            {
                return false;
            }

            return this.Id.Value == m.Id.Value;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.GetType().Name;
        }

        #endregion
    }
}
