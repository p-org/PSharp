//-----------------------------------------------------------------------
// <copyright file="Seq.cs" company="Microsoft">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.Collections
{
    /// <summary>
    /// Class implementing a sequence of T elements.
    /// </summary>
    public class Seq<T>
    {
        /// <summary>
        /// Sequence of T elements.
        /// </summary>
        private List<T> Sequence;

        /// <summary>
        /// Performs indexing.
        /// </summary>
        /// <param name="key">Index</param>
        /// <returns>Element</returns>
        public T this[int index]
        {
            get
            {
                return this.Sequence[index];
            }
            set
            {
                this.Sequence[index] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements actually contained in the sequence.
        /// </summary>
        public int Count
        {
            get
            {
                return this.Sequence.Count;
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Seq()
        {
            this.Sequence = new List<T>();
        }

        /// <summary>
        /// Inserts an element in the sequence at the specified index.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="item">Element</param>
        public void Insert(int index, T item)
        {
            this.Sequence.Insert(index, item);
        }

        /// <summary>
        /// Removes the element at the specified index of the sequence.
        /// </summary>
        /// <param name="index">Index</param>
        public void RemoveAt(int index)
        {
            this.Sequence.RemoveAt(index);
        }
    }
}
