using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ActivityServer
{
    public class Serializer
    {
        public Serializer()
        {
        }

        public void SerializeObject(string filename, List<Team> obj)
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

        public List<Team> DeSerializeObject(string filename)
        {
            try
            {
                List<Team> obj;
                Stream stream = File.Open(filename, FileMode.Open);
                BinaryFormatter bFormatter = new BinaryFormatter();
                obj = (List<Team>)bFormatter.Deserialize(stream);
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
