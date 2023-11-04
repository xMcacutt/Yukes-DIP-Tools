using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DIP_Extractor
{
    internal class Repacker
    {
        public static int CurrentNameIndex;
        public static int RunningCount = 1;
        public static Dictionary<string, int> FileNames = new Dictionary<string, int>();

        public static void Repack(string baseDirPath, string outPath)
        {
            CurrentNameIndex = 0;
            Dir root = new();
            root.Name = "root";
            root.RunningCount = RunningCount;
            root.SubEntryCount = Directory.GetFileSystemEntries(baseDirPath).Length;
            RunningCount += root.SubEntryCount;
            var entries = GetEntryData(baseDirPath);
            root.Files = entries.Item1;
            root.Directories = entries.Item2;
            root.NameIndex = CurrentNameIndex;
            CurrentNameIndex++;

            MemoryStream fileDataStream = new();
            foreach(Dir directory in root.Directories)
            {
                GenerateFileData(directory, fileDataStream);
            }
            for (int fileIndex = 0; fileIndex < root.Files.Count; fileIndex++)
            {
                root.Files[fileIndex].DataOffset = (uint)fileDataStream.Position;
                fileDataStream.Write(File.ReadAllBytes(root.Files[fileIndex].Path));
            }

            FileNames.Clear();
            MemoryStream nameTableStream = new();
            FileNames.Add("root", CurrentNameIndex);
            nameTableStream.Write(Encoding.ASCII.GetBytes("root"));
            int remByteCount = 32 - 4;
            nameTableStream.Write(new byte[remByteCount]);
            for (int fileIndex = 0; fileIndex < root.Files.Count; fileIndex++)
            {
                FileEntry f = root.Files[fileIndex];
                if (!FileNames.ContainsKey(f.Name))
                {
                    FileNames.Add(f.Name, CurrentNameIndex);
                    nameTableStream.Write(Encoding.ASCII.GetBytes(f.Name));
                    remByteCount = 32 - f.Name.Length;
                    nameTableStream.Write(new byte[remByteCount]);
                    CurrentNameIndex++;
                }
                root.Files[fileIndex].NameIndex = FileNames[f.Name];

            }
            for (int dirIndex = 0; dirIndex < root.Directories.Count; dirIndex++)
            {
                Dir d = root.Directories[dirIndex];
                if (!FileNames.ContainsKey(d.Name))
                {
                    FileNames.Add(d.Name, CurrentNameIndex);
                    nameTableStream.Write(Encoding.ASCII.GetBytes(d.Name));
                    remByteCount = 32 - d.Name.Length;
                    nameTableStream.Write(new byte[remByteCount]);
                    CurrentNameIndex++;
                }
                root.Directories[dirIndex].NameIndex = FileNames[d.Name];

                GenerateNameTable(nameTableStream, root.Directories[dirIndex]);
            }

            MemoryStream entryListStream = new();
            entryListStream.Write(new byte[] { 0x0, 0x0 });
            entryListStream.Write(BitConverter.GetBytes((ushort)root.NameIndex));
            entryListStream.Write(BitConverter.GetBytes((uint)root.SubEntryCount));
            entryListStream.Write(BitConverter.GetBytes(root.RunningCount));
            foreach(Dir dir in root.Directories)
            {
                entryListStream.Write(new byte[] { 0x0, 0x0 });
                entryListStream.Write(BitConverter.GetBytes((ushort)dir.NameIndex));
                entryListStream.Write(BitConverter.GetBytes((uint)dir.SubEntryCount));
                entryListStream.Write(BitConverter.GetBytes(dir.RunningCount));
            }
            foreach(FileEntry file in root.Files)
            {
                entryListStream.Write(new byte[] { 0xFF, 0x00 });
                entryListStream.Write(BitConverter.GetBytes((ushort)file.NameIndex));
                entryListStream.Write(BitConverter.GetBytes(file.Size));
                entryListStream.Write(BitConverter.GetBytes(file.DataOffset));
            }
            foreach(Dir dir in root.Directories)
            {
                GenerateEntryList(entryListStream, dir);
            }

            //COMBINE DATA AND GENERATE HEADER
            DIP dip = new DIP();
            dip.EntryListLength = (uint)entryListStream.Length;
            dip.NameTableLength = (uint)nameTableStream.Length;
            dip.FileDataLength = (uint)fileDataStream.Length;
            dip.EntryListOffset = 0x20;
            dip.NameTableOffset = dip.EntryListOffset + dip.EntryListLength;
            dip.FileDataOffset = dip.NameTableOffset + dip.NameTableLength + 0x514;

            MemoryStream headerStream = new();
            headerStream.Write(BitConverter.GetBytes(dip.EntryListLength));
            headerStream.Write(BitConverter.GetBytes(dip.NameTableLength));
            headerStream.Write(BitConverter.GetBytes(dip.FileDataLength));
            headerStream.Write(BitConverter.GetBytes(dip.EntryListOffset));
            headerStream.Write(BitConverter.GetBytes(dip.NameTableOffset));
            headerStream.Write(BitConverter.GetBytes(dip.FileDataOffset));
            headerStream.Write(BitConverter.GetBytes(1));
            headerStream.Write(BitConverter.GetBytes(0));

            var outStream = File.OpenWrite(outPath);
            outStream.Write(headerStream.ToArray());
            headerStream.Close();
            outStream.Write(entryListStream.ToArray());
            entryListStream.Close();
            outStream.Write(nameTableStream.ToArray());
            outStream.Write(new byte[1300]);
            nameTableStream.Close();
            outStream.Write(fileDataStream.ToArray());
            fileDataStream.Close();
            outStream.Close();
        }

        public static (List<FileEntry>, List<Dir>) GetEntryData(string path) 
        {
            List<FileEntry> files = new List<FileEntry>();
            List<Dir> dirs = new List<Dir>();
            string[] entries = Directory.GetFileSystemEntries(path);
            foreach (string entry in entries)
            {
                if (File.Exists(entry))
                {
                    FileEntry file = new();
                    file.Name = Path.GetFileName(entry);
                    file.Path = entry;
                    file.Size = (uint)new FileInfo(entry).Length / 0x20;
                    files.Add(file);
                }
                else if (Directory.Exists(entry))
                {
                    Dir subDir = new();
                    var dirInfo = new DirectoryInfo(entry);
                    subDir.Name = dirInfo.Name;
                    subDir.RunningCount = RunningCount;
                    subDir.SubEntryCount = Directory.GetFileSystemEntries(entry).Length;
                    RunningCount += subDir.SubEntryCount;
                    var subEntries = GetEntryData(entry);
                    subDir.Path = entry;
                    subDir.Files = subEntries.Item1;
                    subDir.Directories = subEntries.Item2;
                    dirs.Add(subDir);
                }
            }
            return (files, dirs);
        }

        public static void GenerateFileData(Dir dir, MemoryStream stream)
        {
            foreach (Dir subDirectory in dir.Directories)
            {
                GenerateFileData(subDirectory, stream);
            }
            for (int fileIndex = 0; fileIndex < dir.Files.Count; fileIndex++)
            {
                dir.Files[fileIndex].DataOffset = (uint)stream.Position;
                stream.Write(File.ReadAllBytes(dir.Files[fileIndex].Path));
            }
        }

        public static void GenerateNameTable(MemoryStream stream, Dir dir)
        {
            for (int fileIndex = 0; fileIndex < dir.Files.Count; fileIndex++)
            {
                FileEntry f = dir.Files[fileIndex];
                if (!FileNames.ContainsKey(f.Name))
                {
                    FileNames.Add(f.Name, CurrentNameIndex);
                    stream.Write(Encoding.ASCII.GetBytes(f.Name));
                    int remByteCount = 32 - f.Name.Length;
                    stream.Write(new byte[remByteCount]);
                    CurrentNameIndex++;
                }
                dir.Files[fileIndex].NameIndex = FileNames[f.Name];

            }
            for (int dirIndex = 0; dirIndex < dir.Directories.Count; dirIndex++)
            {
                Dir d = dir.Directories[dirIndex];
                if (!FileNames.ContainsKey(d.Name))
                {
                    FileNames.Add(d.Name, CurrentNameIndex);
                    stream.Write(Encoding.ASCII.GetBytes(d.Name));
                    int remByteCount = 32 - d.Name.Length;
                    stream.Write(new byte[remByteCount]);
                    CurrentNameIndex++;
                }
                dir.Directories[dirIndex].NameIndex = FileNames[d.Name];

                GenerateNameTable(stream, dir.Directories[dirIndex]);
            }
        }

        public static void GenerateEntryList(MemoryStream stream, Dir dir)
        {
            foreach (Dir subDir in dir.Directories)
            {
                stream.Write(new byte[] { 0x0, 0x0 });
                stream.Write(BitConverter.GetBytes((ushort)subDir.NameIndex));
                stream.Write(BitConverter.GetBytes((uint)subDir.SubEntryCount));
                stream.Write(BitConverter.GetBytes(subDir.RunningCount));
            }
            foreach (FileEntry file in dir.Files)
            {
                stream.Write(new byte[] { 0xFF, 0x00 });
                stream.Write(BitConverter.GetBytes((ushort)file.NameIndex));
                stream.Write(BitConverter.GetBytes(file.Size));
                stream.Write(BitConverter.GetBytes(file.DataOffset));
            }
            foreach (Dir subDir in dir.Directories)
            {
                GenerateEntryList(stream, subDir);
            }
        }

    }
}
