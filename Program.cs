using CafeLibrary;
using FinalFantasy16;

namespace FF16Converter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Tool by KillzXGaming");
            Console.WriteLine($"https://github.com/KillzXGaming/FF16Converter");
            Console.WriteLine($"");

            if (args.Length == 0)
            {
                Console.WriteLine($"Credits:");
                Console.WriteLine($"https://github.com/Nenkai/FF16Tools for docs, gdeflate, and format enums.");
                Console.WriteLine($"");

                Console.WriteLine($"Drag/drop a .tex/.pkg to dump. Drag/drop converted .png/.dds/.xml to replace back");
                Console.WriteLine($"Only replacing tex currently supported, new files and multiple textures in one .tex in future updates.");
                return;
            }

            foreach (string arg in args)
            {
                if (arg.EndsWith(".tex"))
                {
                    string ext = args.Contains("-dds") ? ".dds" : ".png";
                    Console.WriteLine($"converting .tex to {ext}");

                    TexFile texFile = new TexFile(File.OpenRead(arg));
                    foreach (var tex in texFile.Textures)
                        tex.Export(arg + ext);

                    //texFile.Save("testRB.tex");
                }
                else if (arg.EndsWith(".png") ||
                         arg.EndsWith(".dds") ||
                         arg.EndsWith(".tga") ||
                         arg.EndsWith(".tiff") ||
                         arg.EndsWith(".bmp") ||
                         arg.EndsWith(".jpg"))
                {
                    string ext = Path.GetExtension(arg);

                    Console.WriteLine($"converting {ext} to .tex");

                    if (File.Exists(arg.Replace(ext, ""))) //replace existing .tex
                    {
                        TexFile texFile = new TexFile(File.OpenRead(arg.Replace(ext, "")));
                        texFile.Textures[0].Replace(arg);
                        texFile.Save(arg.Replace(ext, ""));
                    }
                    else //create from scatch
                    {
                        TexFile texFile = new TexFile();
                        texFile.Textures[0].Replace(arg);
                        texFile.Save(arg.Replace(ext, ".tex"));
                    }
                }
                else if (arg.EndsWith(".pzd"))
                {
                    PzdFile pzdFile = new PzdFile(File.OpenRead(arg));
                    File.WriteAllText(arg + ".xml", pzdFile.ToXml());
                }
                else if (arg.EndsWith(".pzd.xml"))
                {
                    string name = Path.GetFileName(arg);
                    string dir = Path.GetDirectoryName(arg);

                    PzdFile pzdFile = new PzdFile();
                    pzdFile.FromXML(File.ReadAllText(arg));
                    pzdFile.Save(Path.Combine(dir, $"{name}RB.pzd"));
                }
            }
            Console.WriteLine($"Finished converting!");
        }
    }
}