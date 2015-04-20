//-----------------------------------------------------------------------
// <copyright file="Container.cs" company="Microsoft">
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

namespace Microsoft.PSharp.Collections
{
    /// <summary>
    /// Class implementing a container.
    /// </summary>
    public static class Container
    {
        /// <summary>
        /// Creates a new container with the given items.
        /// </summary>
        /// <param name="item1">Item</param>
        /// <param name="item2">Item</param>
        /// <returns>PContainer</returns>
        public static Container<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        {
            return new Container<T1, T2>(item1, item2);
        }

        /// <summary>
        /// Creates a new container with the given items.
        /// </summary>
        /// <param name="item1">Item</param>
        /// <param name="item2">Item</param>
        /// <param name="item3">Item</param>
        /// <returns>PContainer</returns>
        public static Container<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
        {
            return new Container<T1, T2, T3>(item1, item2, item3);
        }
    }

    /// <summary>
    /// Class implementing a container for 2 items.
    /// </summary>
    public class Container<T1, T2>
    {
        /// <summary>
        /// Item 1.
        /// </summary>
        public T1 Item1;

        /// <summary>
        /// Item 2.
        /// </summary>
        public T2 Item2;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Container()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="item1">Item</param>
        /// <param name="item2">Item</param>
        public Container(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }

    /// <summary>
    /// Class implementing a container for 3 items.
    /// </summary>
    public class Container<T1, T2, T3>
    {
        /// <summary>
        /// Item 1.
        /// </summary>
        public T1 Item1;

        /// <summary>
        /// Item 2.
        /// </summary>
        public T2 Item2;

        /// <summary>
        /// Item 3.
        /// </summary>
        public T3 Item3;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Container()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="item1">Item</param>
        /// <param name="item2">Item</param>
        /// <param name="item3">Item</param>
        public Container(T1 item1, T2 item2, T3 item3)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }
    }
}
