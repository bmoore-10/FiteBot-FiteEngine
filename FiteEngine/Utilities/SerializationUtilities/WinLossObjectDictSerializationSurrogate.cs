using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static FiteEngine.Utilities.WinLossRecord;

namespace FiteEngine.Utilities
{
    public class WinLossObjectDictSerializationSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var list = new List<Serializeable<string, WinLossObject>>();

            Dictionary<string, WinLossObject> dict = obj as Dictionary<string, WinLossObject>;
            foreach(string key in (dict.Keys))
            {
                Serializeable<string, WinLossObject> ret = new Serializeable<string, WinLossObject>();
                ret.Key = key;
                ret.Value = dict[key];
                list.Add(ret);
            }

            // Add the list to serializationinfo
            for(int i = 0; i < list.Count; i++)
            {
                info.AddValue(i.ToString(), list[i], typeof(Serializeable<string, WinLossObject>));
            }
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Dictionary<string, WinLossObject> ret = new Dictionary<string, WinLossObject>();
            for(int i = 0; i < info.MemberCount; i++)
            {
                // Obtain the pair that was saved
                Serializeable<string, WinLossObject> item = info.GetValue(i.ToString(), typeof(Serializeable<string, WinLossObject>)) as Serializeable<string, WinLossObject>;
                // Add it to the dictionary
                ret.Add(item.Key, item.Value);
            }
            return ret;
        }
    }
}
