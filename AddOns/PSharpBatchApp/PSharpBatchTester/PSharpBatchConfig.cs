using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSharpBatchTester
{
    public class PSharpBatchConfig
    {
        // Batch account credentials
        public string BatchAccountName;
        public string BatchAccountKey;
        public string BatchAccountUrl;

        // Storage account credentials
        public string StorageAccountName;
        public string StorageAccountKey;

        //Job and pool details
        public string PoolId;
        public string JobDefaultId;

        //Task Details
        public int MaxIterationPerTask;
        public int TotalIterations;
        public string TaskDefaultId;

        //Storage Constants
        public int BlobContainerSasExpiryHours;

        //Node Details
        public int NumberOfNodesInPool;

        //File Details
        public string PSharpBinariesFolderPath;
        public string TestApplicationPath;

        //Output
        public string OutputFolderPath;

        //Task Wait Time
        public int TaskWaitHours;


        public void XMLSerialize(Stream writeStream)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(PSharpBatchConfig));
            xmlSerializer.Serialize(writeStream, this);
        }

        public static PSharpBatchConfig XMLDeserialize(Stream readStream)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(PSharpBatchConfig));
            return xmlSerializer.Deserialize(readStream) as PSharpBatchConfig;
        }
    }
}
