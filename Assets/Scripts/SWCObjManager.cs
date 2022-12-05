using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using SignalTransferTree = System.Collections.Generic.List<
        System.Collections.Generic.HashSet<int>>;

/// <summary>
/// Manage all SWCObjs.
/// </summary>
public class SWCObjManager : MonoBehaviour
{
    public GameObject SWCObjGroup;

    public int cometLength = 25;

    [SerializeField, SetProperty("cometStayElapse")]
    private float _cometStayElapse = 0.05f;
    public float cometStayElapse
    {
        get { return _cometStayElapse; }
        set
        {
            _cometStayElapse = value;
            coroWFS = null;
            coroWFS = new(_cometStayElapse);
        }
    }

    public List<kouek.SWC> swcs { get; private set; }
    public List<kouek.SWCAttrib> swcAttribs { get; private set; }
    private List<int> vsCnts;
    private List<Material> matrs;
    private List<ComputeBuffer> d_vert2Neuronss;

    private ComputeBuffer d_neuron2SignalLvls;
    private int[] coroOneNeuronBuf;
    private List<int> coroClearBuf;
    private List<int> coroNeurons;
    private List<int> coroSignalLvls;
    private WaitForSeconds coroWFS;

    private int constructingSTTBufIdx = 0;
    private bool constructFinished = false;
    private bool transferFinished = false;
    private SignalTransferTree[] STTBuf;

    private void loadSWCs(SWCOBJInfo SWCOBJInfo)
    {
        var swcDir = new DirectoryInfo(SWCOBJInfo.SWCDir);

        int loadIdx = 0;
        swcs = new();
        foreach (var file in swcDir.GetFiles())
        {
            if (file.Extension != ".swc") continue;
            if (loadIdx < SWCOBJInfo.LoadStart)
            {
                ++loadIdx;
                continue;
            }

            swcs.Add(kouek.SWCLoader.Load(file.FullName));

            ++loadIdx;
            if (loadIdx >= SWCOBJInfo.LoadNum) break;
        }
    }

    private void loadSWCAttribs(SWCOBJInfo SWCOBJInfo)
    {
        var swcAttrDir = new DirectoryInfo(SWCOBJInfo.SWCAttribDir);

        int loadIdx = 0;
        swcAttribs = new();
        foreach (var file in swcAttrDir.GetFiles())
        {
            if (file.Extension != ".swcattr") continue;
            if (loadIdx < SWCOBJInfo.LoadStart)
            {
                ++loadIdx;
                continue;
            }

            swcAttribs.Add(kouek.SWCAttribLoader.Load(file.FullName));

            ++loadIdx;
            if (loadIdx >= SWCOBJInfo.LoadNum) break;
        }
    }

    private void loadOBJs(SWCOBJInfo SWCOBJInfo)
    {
        var objDir = new DirectoryInfo(SWCOBJInfo.OBJDir);

        int loadIdx = 0;
        vsCnts = new();
        matrs = new();
        foreach (var file in objDir.GetFiles())
        {
            if (file.Extension != ".obj") continue;
            if (loadIdx < SWCOBJInfo.LoadStart)
            {
                ++loadIdx;
                continue;
            }

            var loadedObj = kouek.OBJLoader.Load(file.FullName);
            vsCnts.Add(loadedObj.vs.Count);

            var mesh = new Mesh();

            mesh.vertices = loadedObj.vs.ToArray();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.triangles = loadedObj.fvs.ToArray();

            var gameObj = new GameObject();
            var filter = gameObj.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            var renderer = gameObj.AddComponent<MeshRenderer>();
            renderer.material = Resources.Load<Material>("Materials/Neuron");
            matrs.Add(renderer.material);

            gameObj.name = loadedObj.name;
            gameObj.transform.SetParent(SWCObjGroup.transform);

            ++loadIdx;
            if (loadIdx >= SWCOBJInfo.LoadNum) break;
        }
    }

    private void loadSWC2OBJs(SWCOBJInfo SWCOBJInfo)
    {
        var objDir = new DirectoryInfo(SWCOBJInfo.SWC2OBJDir);

        int loadIdx = 0;
        d_vert2Neuronss = new();
        foreach (var file in objDir.GetFiles())
        {
            if (file.Extension != ".swc2obj") continue;
            if (loadIdx < SWCOBJInfo.LoadStart)
            {
                ++loadIdx;
                continue;
            }

            var swc2obj = kouek.SWC2OBJLoader.Load(
                file.FullName, vsCnts[loadIdx - SWCOBJInfo.LoadStart]);
            var cmptBuf = new ComputeBuffer(swc2obj.vert2Neurons.Count, sizeof(int));
            cmptBuf.SetData(swc2obj.vert2Neurons);
            d_vert2Neuronss.Add(cmptBuf);

            ++loadIdx;
            if (loadIdx >= SWCOBJInfo.LoadNum) break;
        }

        for (int i = 0; i < matrs.Count; ++i)
        {
            matrs[i].SetBuffer("vert2Neurons", d_vert2Neuronss[i]);
            matrs[i].SetInt("cometLen", 0);
        }
    }

    private void Awake()
    {
        STTBuf = new SignalTransferTree[2];
        STTBuf[0] = new();
        STTBuf[1] = new();

        var SWCOBJInfo = Serializables.Instance.SWCOBJInfo;
        loadSWCs(SWCOBJInfo);
        loadSWCAttribs(SWCOBJInfo);
        loadOBJs(SWCOBJInfo);
        loadSWC2OBJs(SWCOBJInfo);
    }

    private IEnumerator constructNeuronPath(int swcIdx, int dendriteIdx, int axonIdx = -1)
    {
        var swc = swcs[swcIdx];
        var signalTransferTree = STTBuf[constructingSTTBufIdx];

        // move to root
        var parIdx = dendriteIdx;
        int layerIdx = 0;
        while (true)
        {
            if (signalTransferTree.Count == layerIdx)
                signalTransferTree.Add(new());
            else signalTransferTree[layerIdx].Clear();

            signalTransferTree[layerIdx].Add(parIdx);

            if (swc.parents[parIdx] == -1)
            {
                ++layerIdx;
                break;
            }
            parIdx = swc.parents[parIdx];
            ++layerIdx;
        }

        yield return null;

        // move to leaves
        Stack<(int, int)> stk = new();
        stk.Push((parIdx, -1));
        var downStartIdx = layerIdx;
        var everDownIdx = layerIdx;
        while (stk.Count > 0)
        {
            (int par, int lastChild) = stk.Peek();
            if (swc.children[par] != null && swc.children[par].Count > lastChild + 1)
            {
                ++lastChild;
                stk.Pop();
                stk.Push((par, lastChild));

                var isAxon = true;
                if (par == 0)
                    isAxon = swcAttribs[swcIdx].rootChildAxons.Contains(swc.children[par][lastChild]);
                if (isAxon)
                    stk.Push((swc.children[par][lastChild], -1));
                ++layerIdx;
            }
            else if (swc.children[par] == null && (par == axonIdx || axonIdx == -1))
            {
                for (; everDownIdx <= layerIdx; ++everDownIdx)
                    if (signalTransferTree.Count <= everDownIdx)
                        signalTransferTree.Add(new());
                    else signalTransferTree[everDownIdx].Clear();

                int i = layerIdx;
                while (par != 0)
                {
                    signalTransferTree[i].Add(par);
                    --i;
                    par = swc.parents[par];
                }

                stk.Pop();
                --layerIdx;

                yield return null;
            }
            else
            {
                stk.Pop();
                --layerIdx;
            }
        }

        constructFinished = true;
    }

    private IEnumerator transfer(int swcIdx)
    {
        // use the buffer has been computed
        var signalTransferTree = STTBuf[constructingSTTBufIdx == 0 ? 1 : 0];
        var num = swcs[swcIdx].positions.Count;

        if (d_neuron2SignalLvls == null)
            d_neuron2SignalLvls = new(num, sizeof(int));
        else if (d_neuron2SignalLvls.count < num)
        {
            d_neuron2SignalLvls.Release();
            d_neuron2SignalLvls = new(num, sizeof(int));
        }

        matrs[swcIdx].SetBuffer("neuron2SignalLvls", d_neuron2SignalLvls);
        for (int i = 0; i < matrs.Count; ++i)
            matrs[i].SetInt("cometLen", i == swcIdx ? cometLength : 0);

        if (coroClearBuf == null)
            coroClearBuf = new(num);
        coroClearBuf.Clear();
        for (int i = 0; i < num; ++i)
            coroClearBuf.Add(0);
        d_neuron2SignalLvls.SetData(coroClearBuf);

        if (coroNeurons == null)
            coroNeurons = new(cometLength);
        coroNeurons.Clear();
        if (coroSignalLvls == null)
            coroSignalLvls = new(cometLength);
        coroSignalLvls.Clear();

        yield return coroWFS;

        int layerIdx = 0;
        while (true)
        {
            for (int idx = 0; idx < coroNeurons.Count; ++idx)
            {
                --coroSignalLvls[idx];
                d_neuron2SignalLvls.SetData(coroSignalLvls, idx, coroNeurons[idx], 1);
                if (coroSignalLvls[idx] == 0)
                    coroNeurons[idx] = -1;
            }
            coroNeurons.RemoveAll(nIdx => nIdx == -1);
            coroSignalLvls.RemoveAll(lvl => lvl == 0);

            foreach (var nIdx in signalTransferTree[layerIdx])
            {
                coroNeurons.Add(nIdx);
                coroSignalLvls.Add(cometLength);
            }

            ++layerIdx;
            if (layerIdx == signalTransferTree.Count)
                break;
            yield return coroWFS;
        }

        while (coroNeurons.Count != 0)
        {
            for (int idx = 0; idx < coroNeurons.Count; ++idx)
            {
                --coroSignalLvls[idx];
                d_neuron2SignalLvls.SetData(coroSignalLvls, idx, coroNeurons[idx], 1);
                if (coroSignalLvls[idx] == 0)
                    coroNeurons[idx] = -1;
            }
            coroNeurons.RemoveAll(nIdx => nIdx == -1);
            coroSignalLvls.RemoveAll(lvl => lvl == 0);

            yield return coroWFS;
        }

        transferFinished = true;
    }

    private void clearTransfer()
    {
        for (int i = 0; i < matrs.Count; ++i)
            matrs[i].SetInt("cometLen", 0);
    }

    private IEnumerator forward()
    {
        Serializables.Instance.Load();
        var SWCOBJInfo = Serializables.Instance.SWCOBJInfo;
        var storyNodes = Serializables.Instance.story.nodes;
        if (storyNodes.Count == 0) yield break;

        // ensure the first STT is constructed
        yield return StartCoroutine(
            constructNeuronPath(
                storyNodes[0].swc - SWCOBJInfo.LoadStart,
                storyNodes[0].from, storyNodes[0].to));

        int time = 1;
        while (time < storyNodes.Count)
        {
            constructFinished = transferFinished = false;
            constructingSTTBufIdx = constructingSTTBufIdx == 0 ? 1 : 0; // swap buffer

            StartCoroutine(
                transfer(storyNodes[time - 1].swc - SWCOBJInfo.LoadStart));
            StartCoroutine(
                constructNeuronPath(
                    storyNodes[time].swc - SWCOBJInfo.LoadStart,
                    storyNodes[time].from, storyNodes[time].to));

            yield return new WaitUntil(() => constructFinished && transferFinished);

            ++time;
        }

        constructingSTTBufIdx = constructingSTTBufIdx == 0 ? 1 : 0; // swap buffer
        yield return StartCoroutine(
            transfer(storyNodes[time - 1].swc - SWCOBJInfo.LoadStart));
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Play animation according to the configuration files
    /// </summary>
    public void PlayAnime()
    {
        StopAnime();
        StartCoroutine(forward());
    }

    public void StopAnime()
    {
        StopAllCoroutines();
        clearTransfer();
    }

    private IEnumerator blink(int swcIdx, int endPtIdx)
    {
        var num = swcs[swcIdx].positions.Count;

        if (d_neuron2SignalLvls == null)
            d_neuron2SignalLvls = new(num, sizeof(int));
        else if (d_neuron2SignalLvls.count < num)
        {
            d_neuron2SignalLvls.Release();
            d_neuron2SignalLvls = new(num, sizeof(int));
        }

        if (coroClearBuf == null)
            coroClearBuf = new(num);
        coroClearBuf.Clear();
        for (int i = 0; i < num; ++i)
            coroClearBuf.Add(0);
        d_neuron2SignalLvls.SetData(coroClearBuf);

        if (coroOneNeuronBuf == null)
            coroOneNeuronBuf = new int[1];
        coroOneNeuronBuf[0] = cometLength;
        d_neuron2SignalLvls.SetData(coroOneNeuronBuf, 0, endPtIdx, 1);

        matrs[swcIdx].SetBuffer("neuron2SignalLvls", d_neuron2SignalLvls);

        yield return coroWFS;

        for (int blinkTime = 0; blinkTime < 5; ++blinkTime)
        {
            if (blinkTime % 2 == 0)
                matrs[swcIdx].SetInt("cometLen", cometLength);
            else
                matrs[swcIdx].SetInt("cometLen", 0);

            yield return coroWFS;
        }
        matrs[swcIdx].SetInt("cometLen", 0);
    }

    public void PlaySelectAnime(int swcIdx, int endPtIdx)
    {
        StopAnime();
        StartCoroutine(blink(swcIdx, endPtIdx));
    }
}
