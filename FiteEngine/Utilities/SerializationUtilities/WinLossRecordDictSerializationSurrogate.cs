using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    public class WinLossRecordDictSerializationSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var list = new List<Serializeable<string, WinLossRecord>>();

            Dictionary<string, WinLossRecord> dict = obj as Dictionary<string, WinLossRecord>;
            foreach(string key in (dict.Keys))
            {
                Serializeable<string, WinLossRecord> ret = new Serializeable<string, WinLossRecord>();
                ret.Key = key;
                ret.Value = dict[key];
                list.Add(ret);
            }

            // Add the list to serializationinfo
            for(int i = 0; i < list.Count; i++)
            {
                info.AddValue(i.ToString(), list[i], typeof(Serializeable<string, WinLossRecord>));
            }
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Dictionary<string, WinLossRecord> ret = new Dictionary<string, WinLossRecord>();
            for(int i = 0; i < info.MemberCount; i++)
            {
                // Obtain the pair that was saved
                Serializeable<string, WinLossRecord> item = info.GetValue(i.ToString(), typeof(Serializeable<string, WinLossRecord>)) as Serializeable<string, WinLossRecord>;
                // Add it to the dictionary
                ret.Add(item.Key, item.Value);
            }
            return ret;
        }
    }
}
