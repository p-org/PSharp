using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHittingTestingClient
{
    public class TimelyStatisticLogger<T> : IEnumerable<Tuple<int, T>>
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
            while (idx <= this.ValueList.Count)
            {
                yield return Tuple.Create<int, T>(idx, this.ValueList[idx - 1]);
                i++;
                if (i == 10)
                {
                    i = 1;
                    multiplier *= 10;
                }

                idx = i * multiplier;
            }
        }

        public Tuple<int, T> GetFinalValue()
        {
            return Tuple.Create<int, T>(this.ValueList.Count, this.ValueList[this.ValueList.Count - 1]);
        }

        IEnumerator<Tuple<int, T>> IEnumerable<Tuple<int, T>>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
