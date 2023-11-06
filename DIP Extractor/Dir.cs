using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIP_Extractor
{
    public class Dir
    {
        public List<Dir> Directories = new List<Dir>();
        public List<FileEntry> Files = new List<FileEntry>();
        public string Name = "";
        public string Path = "";
        public int RunningCount;
        public int SubEntryCount;
        public int NameIndex;
    }
}
