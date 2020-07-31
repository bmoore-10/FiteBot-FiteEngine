using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities.SerializationUtilities
{
    class StringListSerializationSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            List<string> list = obj as List<string>;
            for(int i = 0; i < list.Count; i++)
            {
                info.AddValue(i.ToString(), list[i]);
            }
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            List<string> ret = new List<string>();
            for(int i = 0; i < info.MemberCount; i++)
            {
                ret.Insert(i, info.GetString(i.ToString()));
            }
            return ret;
        }
    }
}
