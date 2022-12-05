using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace kouek
{
    public class OBJ
    {
        public string name;
        public Vector3 minPos;
        public Vector3 maxPos;
        public List<Vector3> vs;
        public List<Vector2> vts;
        public List<Vector3> vns;
        public List<int> fvs;
        public List<int> fvts;
        public List<int> fvns;

        public OBJ(string name)
        {
            this.name = name;

            minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            vs = new();
            vts = new();
            vns = new();
            fvs = new();
            fvts = new();
            fvns = new();
        }
    }

    public class OBJLoader
    {
        public static OBJ Load(string absPath, bool swapXYZ = false)
        {
            OBJ obj = new(Path.GetFileName(absPath));

            var lines = File.ReadLines(absPath);
            float[] d3 = new float[3];
            int[,] idx33 = new int[4, 3];
            foreach (var line in lines)
            {
                var parts = line.Split(' ');
                if (parts[0] == "v")
                {
                    for (byte i = 0; i < 3; ++i)
                        d3[i] = (float)System.Convert.ToDouble(parts[i + 1]);
                    if (swapXYZ)
                        obj.vs.Add(new Vector3(-d3[2], d3[0], d3[1]));
                    else
                        obj.vs.Add(new Vector3(d3[0], d3[1], -d3[2]));

                    var appended = obj.vs[obj.vs.Count - 1];
                    if ((appended.x < obj.minPos.x) ||
                        (appended.y < obj.minPos.y) ||
                        (appended.z < obj.minPos.z))
                        obj.minPos = appended;
                    if ((appended.x > obj.maxPos.x) ||
                        (appended.y > obj.maxPos.y) ||
                        (appended.z > obj.maxPos.z))
                        obj.maxPos = appended;
                }
                else if (parts[0] == "vt")
                {
                    for (byte i = 0; i < 2; ++i)
                        d3[i] = (float)System.Convert.ToDouble(parts[i + 1]);
                    obj.vts.Add(new Vector2(d3[0], d3[1]));
                }
                else if (parts[0] == "vn")
                {
                    for (byte i = 0; i < 3; ++i)
                        d3[i] = (float)System.Convert.ToDouble(parts[i + 1]);
                    if (swapXYZ)
                        obj.vns.Add(-new Vector3(-d3[2], d3[0], d3[1]));
                    else
                        obj.vns.Add(-new Vector3(d3[0], d3[1], -d3[2]));
                }
                else if (parts[0] == "f")
                {
                    bool isTri = parts.Length == 4;
                    for (byte f = 0; f < (isTri ? 3 : 4); ++f)
                    {
                        var substr = parts[1 + f].Split('/');
                        idx33[f, 0] = System.Convert.ToInt32(substr[0]);
                        idx33[f, 1] = substr[1].Length == 0 ? 0 : System.Convert.ToInt32(substr[1]);
                        idx33[f, 2] = substr[2].Length == 0 ? 0 : System.Convert.ToInt32(substr[2]);
                    }

                    for (byte f = 0; f < (isTri ? 3 : 4); ++f)
                        for (byte vtn = 0; vtn < 3; ++vtn)
                            --idx33[f, vtn];

                    // insert vertex 0,1,2
                    for (byte f = 0; f < 3; ++f)
                    {
                        obj.fvs.Add(idx33[f, 0]);
                        if (idx33[f, 1] != -1)
                            obj.fvts.Add(idx33[f, 1]);
                        if (idx33[f, 2] != -1)
                            obj.fvns.Add(idx33[f, 2]);
                    }

                    if (!isTri)
                        // insert vertex 2,3,0
                        for (byte f = 0; f < 3; ++f)
                        {
                            obj.fvs.Add(idx33[(f + 2) % 4, 0]);
                            if (idx33[f, 1] != -1)
                                obj.fvts.Add(idx33[(f + 2) % 4, 1]);
                            if (idx33[f, 2] != -1)
                                obj.fvns.Add(idx33[(f + 2) % 4, 2]);
                        }
                }
            }

            return obj;
        }
    }
}
