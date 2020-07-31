using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    public class GameDictSerializationSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var list = new List<Serializeable<string, Game>>();

            // Convert the dictionary to a list
            Dictionary<string, Game> dict = obj as Dictionary<string, Game>;
            foreach(string key in (dict.Keys))
            {
                Serializeable<string, Game> ret = new Serializeable<string, Game>();
                ret.Key = key;
                ret.Value = dict[key];
                list.Add(ret);
            }

            // Add the list to serializationinfo
            for(int i = 0; i < list.Count; i++)
            {
                info.AddValue(i.ToString(), list[i], typeof(Serializeable<string, Game>));
            }
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Dictionary<string, Game> ret = new Dictionary<string, Game>();
            for(int i = 0; i < info.MemberCount; i++)
            {
                // Obtain the pair that was saved
                Serializeable<string, Game> item = info.GetValue(i.ToString(), typeof(Serializeable<string, Game>)) as Serializeable<string, Game>;
                // Add it to the dictionary
                ret.Add(item.Key, item.Value);
            }
            return ret;
        }
    }
}
