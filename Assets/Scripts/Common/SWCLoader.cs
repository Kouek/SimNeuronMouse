using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace kouek
{
    public class SWC
    {
        public Vector3 somaPos;
        public Vector3 minPos, maxPos;
        public List<Vector3> positions;
        public List<int> parents;
        public List<List<int>> children;

        public SWC()
        {
            somaPos = new();
            minPos = new(float.MaxValue, float.MaxValue, float.MaxValue);
            maxPos = new(float.MinValue, float.MinValue, float.MinValue);

            positions = new();
            parents = new();
            children = new();
        }
    }

    public class SWCLoader
    {
        public static SWC Load(string absPath, bool swapXYZ = false)
        {
            SWC swc = new();

            Action<Vector3, Vector3> getSmallComp = (Vector3 i, Vector3 o) =>
            {
                o.x = i.x < o.x ? i.x : o.x;
                o.y = i.y < o.y ? i.y : o.y;
                o.z = i.z < o.z ? i.z : o.z;
            };
            Action<Vector3, Vector3> getLargerComp = (Vector3 i, Vector3 o) =>
            {
                o.x = i.x > o.x ? i.x : o.x;
                o.y = i.y > o.y ? i.y : o.y;
                o.z = i.z > o.z ? i.z : o.z;
            };

            var lines = File.ReadLines(absPath);
            float[] d3 = new float[3];
            foreach (var line in lines)
            {
                if (line.Length == 0) continue;
                if (line[0] == '#') continue;

                var parts = line.Split(' ');

                for (byte i = 2; i < 5; ++i)
                    d3[i - 2] = (float)System.Convert.ToDouble(parts[i]);
                var parent = (int)System.Convert.ToInt32(parts[6]);
                if (parent != -1)
                    --parent;

                if (swapXYZ)
                    swc.positions.Add(new Vector3(-d3[2], d3[0], d3[1]));
                else
                    swc.positions.Add(new Vector3(d3[0], d3[1], -d3[2]));

                var last = swc.positions[swc.positions.Count - 1];
                getSmallComp(last, swc.minPos);
                getLargerComp(last, swc.maxPos);

                swc.parents.Add(parent);
            }
            lines = null;

            swc.somaPos = swc.positions[0];

            for (int parIdx = 0; parIdx < swc.parents.Count; ++parIdx)
                swc.children.Add(null);

            for (int chIdx = 0; chIdx < swc.parents.Count; ++chIdx)
            {
                var parentIdx = swc.parents[chIdx];
                if (parentIdx == -1) continue;

                if (swc.children[parentIdx] == null)
                    swc.children[parentIdx] = new List<int>();
                swc.children[parentIdx].Add(chIdx);
            }

            return swc;
        }
    }
}
