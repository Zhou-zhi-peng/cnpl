using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    static class Utils
    {
        public static byte[] IntTo7BitEncode(ulong value)
        {
            var bytes = new List<byte>();
            ulong mask = 0x7F;
            ulong temp = 0;
            do
            {
                temp = value & mask;
                value = value >> 7;
                if (value > 0)
                    temp = temp | 0x80;
                bytes.Add((byte)temp);
            } while (value > 0);
            return bytes.ToArray();
        }
    }
}
