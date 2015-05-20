//-----------------------------------------------------------------------
// <copyright file="MachineInfo.cs" company="Microsoft">
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp.BugFinding;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class implementing machine related information for scheduling purposes.
    /// </summary>
    public sealed class MachineInfo
    {
        /// <summary>
        /// Machine Id.
        /// </summary>
        internal int Id;

        /// <summary>
        /// Is machine active.
        /// </summary>
        internal bool IsActive = false;

        /// <summary>
        /// Is machine paused because its event queue is empty.
        /// </summary>
        internal bool IsPaused = false;

        /// <summary>
        /// Is machine halted.
        /// </summary>
        internal bool IsHalted = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal MachineInfo(int id)
        {
            this.Id = id;
            this.IsActive = false;
            this.IsPaused = false;
            this.IsHalted = false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            MachineInfo mi = obj as MachineInfo;
            if (mi == null)
            {
                return false;
            }

            return this.Id.Equals(mi.Id);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
