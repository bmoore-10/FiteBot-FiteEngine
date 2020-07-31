using System;

namespace FiteEngine.Utilities
{
    [Serializable]
    public class Serializeable<T1, T2>
    {
        public T1 Key { get; set; }
        public T2 Value { get; set; }
    }
}
