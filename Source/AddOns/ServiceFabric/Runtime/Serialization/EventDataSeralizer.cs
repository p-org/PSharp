using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.ServiceFabric
{
    public class EventDataSeralizer<T> : IStateSerializer<T>
    {
        IEnumerable<Type> knownTypes;

        public EventDataSeralizer(IEnumerable<Type> knownTypes)
        {
            this.knownTypes = knownTypes;
        }

        public T Read(BinaryReader binaryReader)
        {
            var ser = new DataContractSerializer(typeof(T), knownTypes);
            return (T)ser.ReadObject(binaryReader.BaseStream);
        }

        public void Write(T value, BinaryWriter binaryWriter)
        {
            var ser = new DataContractSerializer(typeof(T), knownTypes);
            ser.WriteObject(binaryWriter.BaseStream, value);
        }

        public T Read(T baseValue, BinaryReader binaryReader)
        {
            return this.Read(binaryReader);
        }

        public void Write(T baseValue, T targetValue, BinaryWriter binaryWriter)
        {
            this.Write(targetValue, binaryWriter);
        }
    }
}
