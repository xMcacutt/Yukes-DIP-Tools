
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using DIP_Extractor;

public class Program
{
    static string _path = "";

    public static void Main(string[] args)
    {
        while(true)
        {
            Console.Clear();
            Console.WriteLine("DIP Tools for The Yukes PS2 Games");

            Console.WriteLine("Would you like to exract or repack   (e/r)");
            string? input = Console.ReadLine();
            while (!string.Equals(input, "r", StringComparison.CurrentCultureIgnoreCase)
                && !string.Equals(input, "e", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Input was invalid");
                Console.WriteLine("Would you like to exract or repack   (e/r)");
                input = Console.ReadLine();
            }

            if(string.Equals(input, "e", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Please enter path to DIP");
                _path = Console.ReadLine().Replace("\"", "");
                while (!File.Exists(_path) || !_path.EndsWith(".DIP"))
                {
                    Console.WriteLine("Path was invalid");
                    Console.WriteLine("Please enter path to DIP");
                    _path = Console.ReadLine().Replace("\"", " ");
                }

                Console.WriteLine("Please provide an output directory");
                string? outDir = Console.ReadLine().Replace("\"", " ");
                while (!Directory.Exists(outDir))
                {
                    Console.WriteLine("Path was invalid");
                    Console.WriteLine("Please provide an output directory");
                    outDir = Console.ReadLine().Replace("\"", " ");
                }
                Console.WriteLine("Extracting...");
                Extractor.Extract(_path, outDir);
                Console.WriteLine("Extract completed successfully");
            }
            else
            {
                Console.WriteLine("Please enter path to root directory");
                _path = Console.ReadLine().Replace("\"", "");
                while (!Directory.Exists(_path))
                {
                    Console.WriteLine("Path was invalid");
                    Console.WriteLine("Please enter path to root directory");
                    _path = Console.ReadLine().Replace("\"", " ");
                }

                Console.WriteLine("Please provide an output file name");
                string? outFileName = Console.ReadLine().Replace("\"", "");
                Console.WriteLine("Repacking...");
                var dirInfo = new DirectoryInfo(_path);
                var parentDir = dirInfo.Parent.FullName;
                Repacker.Repack(_path, Path.Combine(parentDir, outFileName + ".DIP"));
                Console.WriteLine("Repack completed successfully");
            }
            Console.ReadLine();
        }
    }
}