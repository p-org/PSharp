using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static PSharpBatchTestCommon.PSharpOperations;

namespace PSharpBatchTestCommon
{
    public class PSharpBatchConfig
    {

        //Job and pool details
        public string PoolId;
        public string JobDefaultId;

        //Task Details
        public string TaskDefaultId;

        //Storage Constants
        public int BlobContainerSasExpiryHours;

        //Node Details
        public int NumberOfNodesInPool;
        public string NodeOsFamily; //Default Value : 5
        public string NodeVirtualMachineSize; //Default value : small

        //File Details
        public string PSharpBinariesFolderPath;

        //Output
        public string OutputFolderPath;

        //Task Wait Time
        public int TaskWaitHours;

        //Delete job
        public bool DeleteJobAfterDone;

        //Delete containers
        public bool DeleteContainerAfterDone;

        //Number of Tasks
        [XmlIgnore]
        public int NumberOfTasks;

        //Iterations per Task
        [XmlIgnore]
        public int IterationsPerTask;

        //Task Application Path
        //[XmlIgnore]
        //public string TestApplicationPath;

        [XmlArray("Commands")]
        [XmlArrayItem("Command")]
        public List<PSharpCommandEntities> CommandEntities;


        public class PSharpCommandEntities
        {
            public int NumberOfParallelTasks;
            public int IterationsPerTask;
            public string TestApplicationPath;
            public string CommandFlags;

            public PSharpCommandEntities()
            {
                NumberOfParallelTasks = 1;
                IterationsPerTask = 1;
            }

            public override string ToString()
            {
                string format = "NumberOfParallelTasks:{0}\nIterations:{1}\nApplicationPath:{2}\nCommandFlags:{3}";
                return string.Format(format, NumberOfParallelTasks, IterationsPerTask, TestApplicationPath, CommandFlags);
            }
        }

        public PSharpBatchConfig()
        {
            //Default Values
            this.NodeOsFamily = "5";
            this.NodeVirtualMachineSize = "small";
        }

        public void SaveAsXML(string path)
        {
            using(FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                this.XMLSerialize(fileStream);
                fileStream.Close();
            }
        }

        public static PSharpBatchConfig LoadFromXML(string path)
        {
            PSharpBatchConfig config = null;
            using(FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                config = XMLDeserialize(fileStream);
                fileStream.Close();
            }
            return config;
        }

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

            if(string.IsNullOrEmpty(this.PoolId) || string.IsNullOrEmpty(this.JobDefaultId) 
                || string.IsNullOrEmpty(this.TaskDefaultId))
            {
                return false;
            }
            
            if(BlobContainerSasExpiryHours<1 || this.NumberOfNodesInPool < 2 || this.TaskWaitHours<1)
            {
                return false;
            }
            
            if(string.IsNullOrEmpty(this.PSharpBinariesFolderPath) || string.IsNullOrEmpty(this.OutputFolderPath))
            {
                return false;
            }
            
            return true;
        }
    }
}
