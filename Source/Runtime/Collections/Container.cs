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

using System;

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
        /// <returns>PContainer</returns>
        public static Container<T1> Create<T1>(T1 item1)
        {
            return new Container<T1>(item1);
        }

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
    /// Class implementing a container for 1 item.
    /// </summary>
    public class Container<T1> : ICloneable
    {
        /// <summary>
        /// Item 1.
        /// </summary>
        public T1 Item1;

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
        public Container(T1 item1)
        {
            this.Item1 = item1;
        }

        /// <summary>
        /// Clones the container.
        /// </summary>
        /// <returns>Clone</returns>
        public object Clone()
        {
            var clone = new Container<T1>();

            var elementType1 = this.Item1.GetType();

            if (typeof(ICloneable).IsAssignableFrom(elementType1))
            {
                var clonedItem1 = (this.Item1 as ICloneable).Clone();
                clone.Item1 = (T1)Convert.ChangeType(clonedItem1, typeof(T1));
            }
            else
            {
                clone.Item1 = this.Item1;
            }

            return clone;
        }
    }

    /// <summary>
    /// Class implementing a container for 2 items.
    /// </summary>
    public class Container<T1, T2> : ICloneable
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

        /// <summary>
        /// Clones the container.
        /// </summary>
        /// <returns>Clone</returns>
        public object Clone()
        {
            var clone = new Container<T1, T2>();

            var elementType1 = this.Item1.GetType();
            var elementType2 = this.Item2.GetType();

            if (typeof(ICloneable).IsAssignableFrom(elementType1))
            {
                var clonedItem1 = (this.Item1 as ICloneable).Clone();
                clone.Item1 = (T1)Convert.ChangeType(clonedItem1, typeof(T1));
            }
            else
            {
                clone.Item1 = this.Item1;
            }

            if (typeof(ICloneable).IsAssignableFrom(elementType2))
            {
                var clonedItem2 = (this.Item2 as ICloneable).Clone();
                clone.Item2 = (T2)Convert.ChangeType(clonedItem2, typeof(T2));
            }
            else
            {
                clone.Item2 = this.Item2;
            }

            return clone;
        }
    }

    /// <summary>
    /// Class implementing a container for 3 items.
    /// </summary>
    public class Container<T1, T2, T3> : ICloneable
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

        /// <summary>
        /// Clones the container.
        /// </summary>
        /// <returns>Clone</returns>
        public object Clone()
        {
            var clone = new Container<T1, T2, T3>();

            var elementType1 = this.Item1.GetType();
            var elementType2 = this.Item2.GetType();
            var elementType3 = this.Item3.GetType();

            if (typeof(ICloneable).IsAssignableFrom(elementType1))
            {
                var clonedItem1 = (this.Item1 as ICloneable).Clone();
                clone.Item1 = (T1)Convert.ChangeType(clonedItem1, typeof(T1));
            }
            else
            {
                clone.Item1 = this.Item1;
            }

            if (typeof(ICloneable).IsAssignableFrom(elementType2))
            {
                var clonedItem2 = (this.Item2 as ICloneable).Clone();
                clone.Item2 = (T2)Convert.ChangeType(clonedItem2, typeof(T2));
            }
            else
            {
                clone.Item2 = this.Item2;
            }

            if (typeof(ICloneable).IsAssignableFrom(elementType3))
            {
                var clonedItem3 = (this.Item3 as ICloneable).Clone();
                clone.Item3 = (T3)Convert.ChangeType(clonedItem3, typeof(T3));
            }
            else
            {
                clone.Item3 = this.Item3;
            }

            return clone;
        }
    }
}
