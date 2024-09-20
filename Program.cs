using CafeLibrary;
using FinalFantasy16;

namespace FF16Converter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // args = new string[] { "t_c1002h0101_occl.tex", "-dds" };
            //  args = new string[] { "t_c1002h0101_occl.tex", "-png" };
           //   args = new string[] { "t_c1002h0101_occl.tex.dds" };

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
                }
                else if (arg.EndsWith(".tex.png"))
                {
                    Console.WriteLine($"converting .png to .tex");

                    TexFile texFile = new TexFile(File.OpenRead(arg.Replace(".png", "")));
                    texFile.Textures[0].Replace(arg);
                    texFile.Save(arg.Replace(".png", ""));
                }
                else if (arg.EndsWith(".tex.dds"))
                {
                    Console.WriteLine($"converting .dds to .tex");

                    TexFile texFile = new TexFile(File.OpenRead(arg.Replace(".dds", "")));
                    texFile.Textures[0].Replace(arg);
                    texFile.Save(arg.Replace(".dds", ""));
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