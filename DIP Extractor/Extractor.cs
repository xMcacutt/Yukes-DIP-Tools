using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIP_Extractor
{
    internal class Extractor
    {
        public static void Extract(string path, string outDir)
        {
            var f = File.OpenRead(path);
            DIP dip = new DIP();
            // READ HEADER
            dip.EntryListLength = BitConverter.ToUInt32(Utility.ReadBytes(f, 4));
            dip.NameTableLength = BitConverter.ToUInt32(Utility.ReadBytes(f, 4));
            dip.FileDataLength = BitConverter.ToUInt32(Utility.ReadBytes(f, 4));
            dip.EntryListOffset = BitConverter.ToUInt32(Utility.ReadBytes(f, 4));
            dip.NameTableOffset = BitConverter.ToUInt32(Utility.ReadBytes(f, 4));
            dip.FileDataOffset = BitConverter.ToUInt32(Utility.ReadBytes(f, 4));
            dip.EntryCount = dip.EntryListLength / 0xC;

            f.Seek(dip.EntryListOffset, SeekOrigin.Begin);

            //HANDLE ROOT DIRECTORY
            Dir root = new();
            int directoryIndicator = BitConverter.ToInt16(Utility.ReadBytes(f, 2));

            int nameIndex = BitConverter.ToUInt16(Utility.ReadBytes(f, 2));
            long pos = f.Position;
            f.Seek(dip.NameTableOffset + (0x20 * nameIndex), SeekOrigin.Begin);
            root.Name = Utility.ReadString(f);
            f.Seek(pos, SeekOrigin.Begin);

            root.SubEntryCount = BitConverter.ToInt32(Utility.ReadBytes(f, 4));

            f.Seek(0x4, SeekOrigin.Current);

            HandleEntry(f, dip, root);
            GenerateFiles(root, dip, f, outDir);
        }

        public static void HandleEntry(FileStream f, DIP dip, Dir root)
        {
            for (int i = 0; i < root.SubEntryCount; i++)
            {
                int directoryIndicator = BitConverter.ToInt16(Utility.ReadBytes(f, 2));
                if (directoryIndicator == 0)
                {
                    Dir subDir = new();
                    int nameIndex = BitConverter.ToUInt16(Utility.ReadBytes(f, 2));
                    long pos = f.Position;
                    f.Seek(dip.NameTableOffset + (0x20 * nameIndex), SeekOrigin.Begin);
                    subDir.Name = Utility.ReadString(f);
                    f.Seek(pos, SeekOrigin.Begin);

                    subDir.SubEntryCount = BitConverter.ToInt32(Utility.ReadBytes(f, 4));

                    f.Seek(0x4, SeekOrigin.Current);

                    root.Directories.Add(subDir);
                }
                else
                {
                    FileEntry file = new();
                    int nameIndex = BitConverter.ToUInt16(Utility.ReadBytes(f, 2));
                    long pos = f.Position;
                    f.Seek(dip.NameTableOffset + (0x20 * nameIndex), SeekOrigin.Begin);
                    file.Name = Utility.ReadString(f);
                    f.Seek(pos, SeekOrigin.Begin);

                    file.Size = BitConverter.ToUInt32(Utility.ReadBytes(f, 4)) * 0x20;

                    file.DataOffset = BitConverter.ToUInt32(Utility.ReadBytes(f, 4));

                    root.Files.Add(file);
                }
            }
            foreach (Dir dir in root.Directories)
            {
                HandleEntry(f, dip, dir);
            }
        }

        public static void GenerateFiles(Dir dir, DIP dip, FileStream f, string outDir)
        {
            Directory.CreateDirectory(Path.Combine(outDir, dir.Name));
            foreach (Dir subDir in dir.Directories)
            {
                GenerateFiles(subDir, dip, f, Path.Combine(outDir, dir.Name));
            }
            foreach (FileEntry file in dir.Files)
            {
                var o = File.Create(Path.Combine(outDir, dir.Name, file.Name));
                f.Seek(dip.FileDataOffset + file.DataOffset, SeekOrigin.Begin);
                byte[] fileBuffer = new byte[file.Size];
                f.Read(fileBuffer, 0x0, (int)file.Size);
                o.Write(fileBuffer);
                o.Close();
            }
        }
    }
}
