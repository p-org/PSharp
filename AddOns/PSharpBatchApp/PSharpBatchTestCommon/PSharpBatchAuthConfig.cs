using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSharpBatchTestCommon
{
    public class PSharpBatchAuthConfig
    {
        // Batch account credentials
        public string BatchAccountName;
        public string BatchAccountKey;
        public string BatchAccountUrl;

        // Storage account credentials
        public string StorageAccountName;
        public string StorageAccountKey;

        public void SaveAsXML(string path)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                this.XMLSerialize(fileStream);
                fileStream.Close();
            }
        }

        public static PSharpBatchAuthConfig LoadFromXML(string path)
        {
            PSharpBatchAuthConfig config = null;
            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                config = XMLDeserialize(fileStream);
                fileStream.Close();
            }
            return config;
        }

        public void XMLSerialize(Stream writeStream)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(PSharpBatchAuthConfig));
            xmlSerializer.Serialize(writeStream, this);
        }

        public static PSharpBatchAuthConfig XMLDeserialize(Stream readStream)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(PSharpBatchAuthConfig));
            return xmlSerializer.Deserialize(readStream) as PSharpBatchAuthConfig;
        }

        public bool Validate()
        {
            if (string.IsNullOrEmpty(this.BatchAccountName) || string.IsNullOrEmpty(this.BatchAccountKey)
                || string.IsNullOrEmpty(this.BatchAccountUrl) || string.IsNullOrEmpty(this.StorageAccountName)
                || string.IsNullOrEmpty(this.StorageAccountKey))
            {
                return false;
            }
            return true;
        }
    }
}
