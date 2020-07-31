using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    public class GameRankingDictSerializationSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var list = new List<Serializeable<string, GameRanking>>();

            // Convert the dictionary to a list
            Dictionary<string, GameRanking> dict = obj as Dictionary<string, GameRanking>;
            foreach(string key in (dict.Keys))
            {
                Serializeable<string, GameRanking> ret = new Serializeable<string, GameRanking>();
                ret.Key = key;
                ret.Value = dict[key];
                list.Add(ret);
            }

            // Add the list to serializationinfo
            for(int i = 0; i < list.Count; i++)
            {
                info.AddValue(i.ToString(), list[i], typeof(Serializeable<string, GameRanking>));
            }
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Dictionary<string, GameRanking> ret = new Dictionary<string, GameRanking>();
            for(int i = 0; i < info.MemberCount; i++)
            {
                // Obtain the pair that was saved
                Serializeable<string, GameRanking> item = info.GetValue(i.ToString(), typeof(Serializeable<string, GameRanking>)) as Serializeable<string, GameRanking>;
                // Add it to the dictionary
                ret.Add(item.Key, item.Value);
            }
            return ret;
        }
    }
}
