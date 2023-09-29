
using System.Text;

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
            _path = Console.ReadLine();
            while(!File.Exists(_path) || !_path.EndsWith(".DIP"))
            {
                Console.WriteLine("Path was invalid");
                Console.WriteLine("Please enter path to DIP");
                _path = Console.ReadLine();
            }
            Console.WriteLine("Please provide an output directory");
            string outDir = Console.ReadLine();
            Extract(_path, outDir);
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

        for(int entryIndex = 0; entryIndex < dip.EntryCount; entryIndex++) 
        {
            f.Seek(dip.EntryListOffset + 0xC * entryIndex, SeekOrigin.Begin);
            //DIRECTORY INDICATOR
            f.Read(shortBuffer, 0x0, 2);
            int directoryIndicator = BitConverter.ToInt16(shortBuffer);

            //NAME
            f.Read(shortBuffer, 0x0, 2);
            int nameIndex = BitConverter.ToUInt16(shortBuffer);
            long pos = f.Position;
            f.Seek(dip.NameTableOffset + (0x20 * nameIndex), SeekOrigin.Begin);
            f.Read(nameBuffer, 0x0, 0x20);
            string entryName = ReadString(nameBuffer, 0);
            f.Seek(pos, SeekOrigin.Begin);

            //FILESIZE / COUNT
            f.Read(intBuffer, 0x0, 4);
            int fileCount = 0;
            int fileSize = 0;
            if (directoryIndicator == 0)
            {
                fileCount = BitConverter.ToInt32(intBuffer);
            }
            else
            {
                fileSize = BitConverter.ToInt32(intBuffer) * 2;
            }

            //FILEDATAOFFSET / RUNNINGFILECOUNT
            f.Read(intBuffer, 0x0, 4);
            uint offset = BitConverter.ToUInt32(intBuffer);
            
            if(directoryIndicator != 0)
            {
                var o = File.Create(Path.Combine(outDir, entryName));
                f.Seek(dip.FileDataOffset + offset, SeekOrigin.Begin);
                byte[] fileBuffer = new byte[fileSize];
                f.Read(fileBuffer, 0x0, fileSize);
                o.Write(fileBuffer);
            }
        }
        Console.ReadLine();

    }

    public static string ReadString(byte[] bytes, int position)
    {
        int endOfString = Array.IndexOf<byte>(bytes, 0x0, position);
        if (endOfString == position) return string.Empty;
        string s = Encoding.ASCII.GetString(bytes, position, endOfString - position);
        return s;
    }
}