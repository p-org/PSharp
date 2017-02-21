using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
        public string TaskDefaultId;

        //Storage Constants
        public int BlobContainerSasExpiryHours;

        //Node Details
        public int NumberOfNodesInPool;

        //File Details
        public string PSharpBinariesFolderPath;

        //Output
        public string OutputFolderPath;

        //Task Wait Time
        public int TaskWaitHours;

        //PSharpTesting
        public string PSharpTestCommand;

        //Flags in command
        [XmlIgnore]
        public string CommandFlags;

        //Number of Tasks
        [XmlIgnore]
        public int NumberOfTasks;

        //Iterations per Task
        [XmlIgnore]
        public int IterationsPerTask;

        //Task Application Path
        [XmlIgnore]
        public string TestApplicationPath;

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

        public bool Validate()
        {
            //Validate all the properties
            if (string.IsNullOrEmpty(this.BatchAccountName) || string.IsNullOrEmpty(this.BatchAccountKey) 
                || string.IsNullOrEmpty(this.BatchAccountUrl) || string.IsNullOrEmpty(this.StorageAccountName) 
                || string.IsNullOrEmpty(this.StorageAccountKey))
            {
                return false;
            }

            if(string.IsNullOrEmpty(this.PoolId) || string.IsNullOrEmpty(this.JobDefaultId) 
                || string.IsNullOrEmpty(this.TaskDefaultId))
            {
                return false;
            }
            
            if(BlobContainerSasExpiryHours<1 || this.NumberOfNodesInPool < 2 || this.TaskWaitHours<1)
            {
                return false;
            }
            
            if(string.IsNullOrEmpty(this.PSharpBinariesFolderPath) || string.IsNullOrEmpty(this.OutputFolderPath) || string.IsNullOrEmpty(PSharpTestCommand))
            {
                return false;
            }
            
            return true;
        }
    }
}
