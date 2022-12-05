using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace kouek
{
    public class SWC2OBJ
    {
        public List<int> vert2Neurons;

        public SWC2OBJ()
        {
            vert2Neurons = new();
        }
    }

    public class SWC2OBJLoader
    {
        public static SWC2OBJ Load(string absPath, int vertCnt)
        {
            SWC2OBJ swc2obj = new();

            for (int i = 0; i < vertCnt; ++i)
                swc2obj.vert2Neurons.Add(-1);

            var lines = File.ReadLines(absPath);
            int[] i2 = new int[2];
            foreach (var line in lines)
            {
                if (line.Length == 0) continue;

                var parts = line.Split(' ');
                i2[0] = System.Convert.ToInt32(parts[0]);
                i2[1] = System.Convert.ToInt32(parts[1]);

                swc2obj.vert2Neurons[i2[0]] = i2[1];
            }

            return swc2obj;
        }
    }
}
