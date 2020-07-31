using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    public class PlayerDictionarySerializationSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var list = new List<Serializeable<string, Player>>();

            // Convert the dictionary to a list
            Dictionary<string, Player> dict = obj as Dictionary<string, Player>;
            foreach(string key in (dict.Keys))
            {
                Serializeable<string, Player> ret = new Serializeable<string, Player>();
                ret.Key = key;
                ret.Value = dict[key];
                list.Add(ret);
            }

            // Add the list to serializationinfo
            for(int i = 0; i < list.Count; i++)
            {
                info.AddValue(i.ToString(), list[i], typeof(Serializeable<string, Player>));
            }
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Dictionary<string, Player> ret = new Dictionary<string, Player>();
            for(int i = 0; i < info.MemberCount; i++)
            {
                // Obtain the pair that was saved
                Serializeable<string, Player> item = info.GetValue(i.ToString(), typeof(Serializeable<string,Player>)) as Serializeable<string, Player>;
                // Add it to the dictionary
                ret.Add(item.Key, item.Value);
            }
            return ret;
        }
    }
}
