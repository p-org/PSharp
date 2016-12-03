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
