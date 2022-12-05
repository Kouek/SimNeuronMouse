using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SWCOBJInfo
{
    public string SWCDir;
    public string SWCAttribDir;
    public string OBJDir;
    public string SWC2OBJDir;
    public int LoadStart;
    public int LoadNum;
}

[Serializable]
public class Story
{
    [Serializable]
    public class Node
    {
        public int swc;
        public int time;
        public int from;
        public int to;
    }

    [SerializeField]
    public List<Node> nodes;
}


public class Serializables
{
    private static Serializables instance;
    public static Serializables Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new();
            }
            return instance;
        }
    }

    public SWCOBJInfo SWCOBJInfo { get; private set; }
    public Story story { get; private set; }

    /// <summary>
    /// Load the data should be prepared at the time of initialization
    /// </summary>
    private Serializables()
    {
        var json = Resources.Load<TextAsset>("JSONs/NeuronModels");
        SWCOBJInfo = new();
        SWCOBJInfo = JsonUtility.FromJson<SWCOBJInfo>(json.text);
    }

    /// <summary>
    /// Load the data should be loaded at runtime
    /// </summary>
    public void Load()
    {
        var json = Resources.Load<TextAsset>("JSONs/Story");
        story = new();
        story = JsonUtility.FromJson<Story>(json.text);
        story.nodes.Sort((a, b) => a.time.CompareTo(b.time));
    }
}
