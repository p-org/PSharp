using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

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

    class AesGenerator : IGenerator
    {
        AesManaged rnd;
        ICryptoTransform trans;
        int counter;
        byte[] buf;

        public AesGenerator()
        {
            rnd = new AesManaged();
            trans = rnd.CreateEncryptor();
            counter = 0;
            buf = new byte[4];
        }

        public string Name()
        {
            return "AES";
        }

        public int Next()
        {
            counter++;
            byte[] inp = BitConverter.GetBytes(counter);
            buf = trans.TransformFinalBlock(inp, 0, 4);

            var x = BitConverter.ToInt32(buf, 0);
            if (x < 0) return (-1 * x);
            return x;
        }

        public int Next(int low, int high)
        {
            throw new NotImplementedException();
        }
    }
}
