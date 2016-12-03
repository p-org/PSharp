using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

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
            return "Random";
        }

        public RandomWrapper()
        {
            rnd = new Random();
        }

        public RandomWrapper(int seed)
        {
            rnd = new Random(seed);
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

    // a copy of:
    // https://referencesource.microsoft.com/#mscorlib/system/random.cs,bb77e610694e64ca
    class MyRandom : IGenerator
    {
        //
        // Private Constants 
        //
        private const int MBIG = Int32.MaxValue;
        private const int MSEED = 161803398;
        private const int MZ = 0;


        //
        // Member Variables
        //
        private int inext;
        private int inextp;
        private int[] SeedArray = new int[56];



        public string Name()
        {
            return "MyRandom";
        }

        public MyRandom() : this(Environment.TickCount)
        {
            
        }

        public MyRandom(int seed)
        {
            int ii;
            int mj, mk;

            //Initialize our Seed array.
            //This algorithm comes from Numerical Recipes in C (2nd Ed.)
            int subtraction = (seed == Int32.MinValue) ? Int32.MaxValue : Math.Abs(seed);
            mj = MSEED - subtraction;
            SeedArray[55] = mj;
            mk = 1;
            for (int i = 1; i < 55; i++)
            {  //Apparently the range [1..55] is special (Knuth) and so we're wasting the 0'th position.
                ii = (21 * i) % 55;
                SeedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0) mk += MBIG;
                mj = SeedArray[ii];
            }
            for (int k = 1; k < 5; k++)
            {
                for (int i = 1; i < 56; i++)
                {
                    SeedArray[i] -= SeedArray[1 + (i + 30) % 55];
                    if (SeedArray[i] < 0) SeedArray[i] += MBIG;
                }
            }
            inext = 0;
            inextp = 21;
            seed = 1;
        }

        private int InternalSample()
        {
            int retVal;
            int locINext = inext;
            int locINextp = inextp;

            if (++locINext >= 56) locINext = 1;
            if (++locINextp >= 56) locINextp = 1;

            retVal = SeedArray[locINext] - SeedArray[locINextp];

            if (retVal == MBIG) retVal--;
            if (retVal < 0) retVal += MBIG;

            SeedArray[locINext] = retVal;

            inext = locINext;
            inextp = locINextp;

            return retVal;
        }

        public int Next()
        {
            return InternalSample();
        }

        public int Next(int low, int high)
        {
            throw new NotImplementedException();
        }
    }

class AesGenerator : IGenerator
    {
        AesManaged rnd;
        ICryptoTransform trans;
        byte[] buf;

        public AesGenerator()
        {
            rnd = new AesManaged();
            trans = rnd.CreateEncryptor();
            buf = BitConverter.GetBytes(0);
        }

        public AesGenerator(int seed)
        {
            rnd = new AesManaged();

            var size = rnd.BlockSize / 8;

            var key = new byte[size];
            var skey = BitConverter.GetBytes(seed);
            for (int i = 0; i < size; i++)
            {
                key[i] = skey[i % 4];
            }

            var iv = new byte[size];
            for (int i = 0; i < size; i++)
            {
                iv[i] = 0;
            }
            trans = rnd.CreateEncryptor(key, iv);
            buf = BitConverter.GetBytes(0);
        }

        public string Name()
        {
            return "AES";
        }

        public int Next()
        {
            //counter++;
            for(int i = 0; i < 4; i++)
            {
                buf[i]++;
                if (buf[i] != 0) break;
            }
            

            var cipher = trans.TransformFinalBlock(buf, 0, 4);

            var x = BitConverter.ToInt32(cipher, 0);

            if (x < 0) return (-1 * x);
            return x;
        }

        public int Next(int low, int high)
        {
            throw new NotImplementedException();
        }
    }
    /*
    class AesGenerator : IGenerator
    {
        AesManaged rnd;
        ICryptoTransform trans;
        int counter;
        byte[] buf;
        byte[] cipherBytes;
        int cipherBytesIndex;
        RandomNumberGenerator rng;

        public AesGenerator()
        {
            rnd = new AesManaged();
            trans = rnd.CreateEncryptor();
            counter = 0;
            cipherBytes = new byte[1028];
            cipherBytesIndex = 0;
            rng = RandomNumberGenerator.Create()
        }

        public string Name()
        {
            return "AES";
        }

        public int Next()
        {
            //counter++;
            //byte[] inp = BitConverter.GetBytes(counter);
            //buf = trans.TransformFinalBlock(inp, 0, 4);

            if (cipherBytesIndex + 4 > cipherBytes.Length)
            {
                rng.GetBytes(cipherBytes);
                cipherBytesIndex = 0;
            }

            /*
            if (cipherBytes == null || cipherBytesIndex + 4 > cipherBytes.Length)
            {
                var stream = new MemoryStream();
                var cryptoStream = new CryptoStream(stream, trans, CryptoStreamMode.Write);
                var writer = new StreamWriter(cryptoStream);
                
                for (int i = counter; i < counter + 100; i++)
                {
                    var inp = BitConverter.GetBytes(i);
                    writer.Write((char)inp[0]);
                    writer.Write((char)inp[1]);
                    writer.Write((char)inp[2]);
                    writer.Write((char)inp[3]);
                }

                writer.Close();
                cryptoStream.Close();
                stream.Close();

                cipherBytes = stream.ToArray();
                cipherBytesIndex = 0;
            }
               
            var x = BitConverter.ToInt32(cipherBytes, cipherBytesIndex);
            cipherBytesIndex += 4;

            if (x < 0) return (-1 * x);
            return x;
        }

        public int Next(int low, int high)
        {
            throw new NotImplementedException();
        }
        
    }
    */
}
