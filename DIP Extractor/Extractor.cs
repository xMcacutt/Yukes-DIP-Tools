using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binft;

namespace DIP_Extractor
{
    internal class Extractor
    {
        public static void Extract(string path, string outDir)
        {
            Binf binf = Binft.Binft.OpenBinf(path, true);
            DIP dip = new DIP();
            dip.EntryListLength = binf.ReadUInt();
            dip.NameTableLength = binf.ReadUInt();
            dip.FileDataLength = binf.ReadUInt();
            dip.EntryListOffset = binf.ReadUInt();
            dip.NameTableOffset = binf.ReadUInt();
            dip.FileDataOffset = binf.ReadUInt();
            dip.EntryCount = dip.EntryListLength / 0xC;
            binf.GoTo(dip.EntryListOffset);

            Dir root = new();
            int directoryIndicator = binf.ReadShort();
            int nameIndex = binf.ReadUShort();
            long pos = binf.Position;
            binf.GoTo(dip.NameTableOffset + (0x20 * nameIndex));
            root.Name = binf.ReadString();
            binf.GoTo(pos);
            root.SubEntryCount = binf.ReadInt();
            binf.Skip(4);

            HandleEntry(binf, dip, root);
            GenerateFiles(root, dip, binf, outDir);
        }

        public static void HandleEntry(Binf binf, DIP dip, Dir root)
        {
            for (int i = 0; i < root.SubEntryCount; i++)
            {
                int directoryIndicator = binf.ReadShort();
                int nameIndex = binf.ReadUShort();
                long pos = binf.Position;
                binf.GoTo(dip.NameTableOffset + (0x20 * nameIndex));
                string name = binf.ReadString();
                binf.GoTo(pos);
                if (directoryIndicator == 0)
                {
                    Dir subDir = new();
                    subDir.Name = name;
                    subDir.SubEntryCount = binf.ReadInt();
                    binf.Skip(4);
                    root.Directories.Add(subDir);
                    continue;
                }
                FileEntry file = new()
                {
                    Name = name,
                    Size = binf.ReadUInt() * 0x20,
                    DataOffset = binf.ReadUInt()
                };
                root.Files.Add(file);   
            }
            foreach (Dir dir in root.Directories)
            {
                HandleEntry(binf, dip, dir);
            }
        }

        public static void GenerateFiles(Dir dir, DIP dip, Binf binf, string outDir)
        {
            Directory.CreateDirectory(Path.Combine(outDir, dir.Name));
            foreach (Dir subDir in dir.Directories)
            {
                GenerateFiles(subDir, dip, binf, Path.Combine(outDir, dir.Name));
            }
            foreach (FileEntry file in dir.Files)
            {
                var o = File.Create(Path.Combine(outDir, dir.Name, file.Name));
                binf.GoTo(dip.FileDataOffset + file.DataOffset);
                o.Write(binf.ReadBytes((int)file.Size));
                o.Close();
            }
        }
    }
}
