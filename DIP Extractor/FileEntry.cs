using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIP_Extractor
{
    public class FileEntry
    {
        public string Name = "";
        public uint Size;
        public uint DataOffset;
        public string Path = "";
        public int NameIndex;
    }
}
