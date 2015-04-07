using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ActivityClient
{
    public class Serializer
    {
        public Serializer()
        {
        }

        public void SerializeObject(string filename, Settings obj)
        {
            try
            {
                Stream stream = File.Open(filename, FileMode.Create);
                BinaryFormatter bFormatter = new BinaryFormatter();
                bFormatter.Serialize(stream, obj);
                stream.Close();
            }
            catch (Exception) {}
        }

        public Settings DeSerializeObject(string filename)
        {
            try
            {
                Settings obj;
                Stream stream = File.Open(filename, FileMode.Open);
                BinaryFormatter bFormatter = new BinaryFormatter();
                obj = (Settings)bFormatter.Deserialize(stream);
                stream.Close();
                return obj;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
