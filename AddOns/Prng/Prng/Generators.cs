using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prng
{
    interface IGenerator
    {
        string Name();

        int Next();

        int Next(int low, int high);
    }

    class RandomWrapper : IGenerator
    {
        Random rnd;

        public string Name()
        {
            return "System.Random";
        }

        public RandomWrapper()
        {
            rnd = new Random();
        }

        public int Next()
        {
            return rnd.Next();
        }

        public int Next(int low, int high)
        {
            return rnd.Next(low, high);
        }
    }
}
