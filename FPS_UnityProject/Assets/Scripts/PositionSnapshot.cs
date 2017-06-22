using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

using UnityEngine;


namespace FPS.Assets.Scripts
{
    [DataContract]
    public class PositionSnapshot
    {
        [DataMember]
        private float _position;

        public float Position { get { return _position; } }


        public PositionSnapshot(float position)
        {
            _position = position;
        }


        public byte[] Serialize()
        {
            MemoryStream memoryStream = new MemoryStream();

            try
            {
                DataContractSerializer serializer = new DataContractSerializer(GetType());

                serializer.WriteObject(memoryStream, this);

                return memoryStream.ToArray();
            }
            finally
            {
                memoryStream.Close();
            }
        }

        public void Deserialize(byte[] data)
        {
            MemoryStream memoryStream = new MemoryStream();
            DataContractSerializer serializer = new DataContractSerializer(GetType());
            
            memoryStream.Write(data, 0, data.Length);
            memoryStream.Position = 0;
            PositionSnapshot converted = (PositionSnapshot)serializer.ReadObject(memoryStream);

            memoryStream.Close();

            _position = converted.Position;
        }
    }
}
