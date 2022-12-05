using System.IO;
using System.Collections.Generic;

namespace kouek
{
    public class SWCAttrib
    {
        public List<int> dendrites;
        public List<int> axons;
        public List<int> rootChildAxons;

        public SWCAttrib()
        {
            dendrites = new();
            axons = new();
            rootChildAxons = new();
        }
    }

    public class SWCAttribLoader
    {
        private static void parseLine(SWCAttrib swcAttrib, string line, int ln)
        {
            switch (ln)
            {
                case 0:
                    {
                        var parts = line.Split(' ');
                        foreach (var idStr in parts)
                            swcAttrib.dendrites.Add(System.Convert.ToInt32(idStr));
                        break;
                    }
                case 1:
                    {
                        var parts = line.Split(' ');
                        foreach (var idStr in parts)
                            swcAttrib.axons.Add(System.Convert.ToInt32(idStr));
                        break;
                    }
                case 2:
                    break;
                case 3:
                    {
                        var parts = line.Split(' ');
                        foreach (var idStr in parts)
                            swcAttrib.rootChildAxons.Add(System.Convert.ToInt32(idStr));
                        break;
                    }
                default: break;
            }
        }

        public static SWCAttrib Load(string absPath)
        {
            SWCAttrib swcAttrib = new();

            var lines = File.ReadLines(absPath);
            int ln = 0;
            foreach (var line in lines)
            {
                parseLine(swcAttrib, line, ln);
                ++ln;
            }

            return swcAttrib;
        }
    }
}
