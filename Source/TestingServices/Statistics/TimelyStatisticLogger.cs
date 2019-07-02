// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Statistics
{
    internal class TimelyStatisticLogger<T> : IEnumerable<Tuple<int, T>>
    {
        private readonly List<T> ValueList;

        public TimelyStatisticLogger()
        {
            this.ValueList = new List<T>();
        }

        public void AddValue(T val)
        {
            this.ValueList.Add(val);
        }

        public IEnumerator<Tuple<int, T>> GetEnumerator()
        {
            int i = 1;
            int multiplier = 1;
            int idx = i * multiplier;
            while (idx <= this.ValueList.Count )
            {
                yield return Tuple.Create<int, T>(idx, this.ValueList[idx - 1]);
                switch ( i % 10 )
                {
                    case 1:
                        i = 2;
                        break;
                    case 2:
                        i = 5;
                        break;
                    case 5:
                        i = 1;
                        multiplier *= 10;
                        break;
                }

                idx = i * multiplier;
            }
        }

        public Tuple<int, T> GetFinalValue()
        {
            return Tuple.Create<int, T>(this.ValueList.Count, this.ValueList[this.ValueList.Count - 1]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
