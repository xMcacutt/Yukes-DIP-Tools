
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using DIP_Extractor;

public class Program
{
    static string _path;
    static bool _run = true;

    public static void Main()
    {
        while(_run)
        {
            Console.Clear();
            Console.WriteLine("DIP Extractor for The DOG Island PS2");

            Console.WriteLine("Please enter path to DIP");
            _path = Console.ReadLine().Replace("\"", "");
            while(!File.Exists(_path) || !_path.EndsWith(".DIP"))
            {
                Console.WriteLine("Path was invalid");
                Console.WriteLine("Please enter path to DIP");
                _path = Console.ReadLine().Replace("\"", " ");
            }

            Console.WriteLine("Please provide an output directory");
            string outDir = Console.ReadLine().Replace("\"", " ");
            while (!Directory.Exists(outDir))
            {
                Console.WriteLine("Path was invalid");
                Console.WriteLine("Please provide an output directory");
                outDir = Console.ReadLine().Replace("\"", " ");
            }
            Console.WriteLine("Extracting...");
            Extract(_path, outDir);
            Console.WriteLine("Extract completed successfully");
        }
    }

    public static void Extract(string path, string outDir)
    {
        var f = File.OpenRead(path);
        DIP dip = new DIP();
        byte[] intBuffer = new byte[4];
        byte[] shortBuffer = new byte[2];
        byte[] nameBuffer = new byte[0x20];
        // READ HEADER
        f.Read(intBuffer, 0x0, 4);
        dip.EntryListLength = BitConverter.ToUInt32(intBuffer);
        f.Read(intBuffer, 0x0, 4);
        dip.NameTableLength = BitConverter.ToUInt32(intBuffer);
        f.Read(intBuffer, 0x0, 4);
        dip.FileDataLength = BitConverter.ToUInt32(intBuffer);
        f.Read(intBuffer, 0x0, 4);
        dip.EntryListOffset = BitConverter.ToUInt32(intBuffer);
        f.Read(intBuffer, 0x0, 4);
        dip.NameTableOffset = BitConverter.ToUInt32(intBuffer);
        f.Read(intBuffer, 0x0, 4);
        dip.FileDataOffset = BitConverter.ToUInt32(intBuffer);
        dip.EntryCount = dip.EntryListLength / 0xC;

        f.Seek(dip.EntryListOffset, SeekOrigin.Begin);

        //HANDLE ROOT DIRECTORY
        Dir root = new();
        f.Read(shortBuffer, 0x0, 2);
        int directoryIndicator = BitConverter.ToInt16(shortBuffer);

        f.Read(shortBuffer, 0x0, 2);
        int nameIndex = BitConverter.ToUInt16(shortBuffer);
        long pos = f.Position;
        f.Seek(dip.NameTableOffset + (0x20 * nameIndex), SeekOrigin.Begin);
        f.Read(nameBuffer, 0x0, 0x20);
        f.Seek(pos, SeekOrigin.Begin);
        root.Name = ReadString(nameBuffer, 0);

        f.Read(intBuffer, 0x0, 4);
        root.SubEntryCount = BitConverter.ToInt32(intBuffer);

        f.Seek(0x4, SeekOrigin.Current);

        HandleEntry(f, dip, root);
        GenerateFiles(root, dip, f, outDir);
    }

    public static void HandleEntry(FileStream f, DIP dip, Dir root)
    {
        byte[] intBuffer = new byte[4];
        byte[] shortBuffer = new byte[2];
        byte[] nameBuffer = new byte[0x20];
        for(int i = 0; i < root.SubEntryCount; i++)
        {
            f.Read(shortBuffer, 0x0, 2);
            int directoryIndicator = BitConverter.ToInt16(shortBuffer);
            if (directoryIndicator == 0)
            {
                Dir subDir = new();
                f.Read(shortBuffer, 0x0, 2);
                int nameIndex = BitConverter.ToUInt16(shortBuffer);
                long pos = f.Position;
                f.Seek(dip.NameTableOffset + (0x20 * nameIndex), SeekOrigin.Begin);
                f.Read(nameBuffer, 0x0, 0x20);
                f.Seek(pos, SeekOrigin.Begin);
                subDir.Name = ReadString(nameBuffer, 0);

                f.Read(intBuffer, 0x0, 4);
                subDir.SubEntryCount = BitConverter.ToInt32(intBuffer);

                f.Seek(0x4, SeekOrigin.Current);

                root.Directories.Add(subDir);
            }
            else
            {
                FileEntry file = new();
                f.Read(shortBuffer, 0x0, 2);
                int nameIndex = BitConverter.ToUInt16(shortBuffer);
                long pos = f.Position;
                f.Seek(dip.NameTableOffset + (0x20 * nameIndex), SeekOrigin.Begin);
                f.Read(nameBuffer, 0x0, 0x20);
                f.Seek(pos, SeekOrigin.Begin);
                file.Name = ReadString(nameBuffer, 0);

                f.Read(intBuffer, 0x0, 4);
                file.Size = BitConverter.ToUInt32(intBuffer) * 2;

                f.Read(intBuffer, 0x0, 4);
                file.DataOffset = BitConverter.ToUInt32(intBuffer);

                root.Files.Add(file);
            }
        }
        foreach(Dir dir in root.Directories)
        {
            HandleEntry(f, dip, dir);
        }
    }

    public static void GenerateFiles(Dir dir, DIP dip, FileStream f, string outDir)
    {
        Directory.CreateDirectory(Path.Combine(outDir, dir.Name));
        foreach(Dir subDir in dir.Directories)
        {
            GenerateFiles(subDir, dip, f, Path.Combine(outDir, dir.Name));
        }
        foreach(FileEntry file in dir.Files)
        {
            var o = File.Create(Path.Combine(outDir, dir.Name, file.Name));
            f.Seek(dip.FileDataOffset + file.DataOffset, SeekOrigin.Begin);
            byte[] fileBuffer = new byte[file.Size];
            f.Read(fileBuffer, 0x0, (int)file.Size);
            o.Write(fileBuffer);
            o.Close();
        }
    }

    public static string ReadString(byte[] bytes, int position)
    {
        int endOfString = Array.IndexOf<byte>(bytes, 0x0, position);
        if (endOfString == position) return string.Empty;
        string s = Encoding.ASCII.GetString(bytes, position, endOfString - position);
        return s;
    }
}