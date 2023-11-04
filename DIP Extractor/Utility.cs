using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIP_Extractor
{
    internal class Utility
    {
        public static string ReadString(FileStream f)
        {
            string s = "";
            byte b = (byte)f.ReadByte();
            while (b != 0x0)
            {
                s += Encoding.ASCII.GetString(new byte[] { b });
                b = (byte)f.ReadByte();
            }
            return s;
        }

        public static byte[] ReadBytes(FileStream f, int length)
        {
            byte[] buffer = new byte[length];
            f.Read(buffer, 0, length);
            return buffer;
        }
    }
}
